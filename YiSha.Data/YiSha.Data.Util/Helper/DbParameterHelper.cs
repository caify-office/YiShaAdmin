using Microsoft.Data.SqlClient;
using MySqlConnector;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using YiSha.Util.Extension;

namespace YiSha.Data.Helper
{
    public static class DbParameterHelper
    {
        private const RegexOptions _Options = RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant;

        /// <summary>
        /// 根据配置文件中所配置的数据库类型
        /// 来创建相应数据库的参数对象
        /// </summary>
        public static DbParameter CreateDbParameter(string name, object value)
        {
            return DbHelper.DbType switch
            {
                DatabaseType.SqlServer => new SqlParameter(name, value),
                DatabaseType.MySql => new MySqlParameter(name, value),
                DatabaseType.Oracle => new OracleParameter(name, value),
                _ => throw new Exception("数据库类型目前不支持！")
            };
        }

        public static DbParameter[] CreateDbParameters(string name, object[] values)
        {
            return values.TryAny() ? values.Select((_, i) => CreateDbParameter(name + i, values[i])).ToArray() : null;
        }

        /// <summary>
        /// 简单匿名参数映射
        /// </summary>
        public static Func<object, DbParameter[]> CreateParameterFunc(string sql, object param)
        {
            return CreateDbParameterCache.GetFunc(sql, param);
        }

        public static DbParameter[] CreateParameters(ref string sql, object param = null)
        {
            if (param == null)
            {
                return default;
            }

            var strSql = sql;
            var list = new List<DbParameter>();
            if (param is ExpandoObject)
            {
                var dict = (IDictionary<string, object>)param;
                foreach (var (key, value) in dict)
                {
                    var parameters = ResovleParameter(ref sql, strSql, key, value, value.GetType().GetUnderlyingType());
                    list.AddRange(parameters);
                }
            }
            else
            {
                foreach (var prop in param.GetType().GetProperties())
                {
                    var parameter = ResovleParameter(ref sql, strSql, prop.Name, prop.GetValue(param), prop.PropertyType.GetUnderlyingType());
                    list.AddRange(parameter);
                }
            }
            return list.ToArray();
        }

        private static IEnumerable<DbParameter> ResovleParameter(ref string sql, string strSql, string name, object value, Type propType)
        {
            var list = new List<DbParameter>();
            if (Regex.IsMatch(strSql, $@"[?@:]{name}([^\p{{L}}\p{{N}}_]+|$)", _Options))
            {
                if (propType.IsElementaryType())
                {
                    list.Add(CreateDbParameter($"@{name}", value));
                }
                else if (typeof(IEnumerable).IsAssignableFrom(propType))
                {
                    int i = 0;
                    foreach (var item in (IEnumerable)value)
                    {
                        list.Add(CreateDbParameter($"@{name}{i++}", item));
                    }
                    var paramStr = string.Join(",", list.Select(x => x.ParameterName));
                    sql = Regex.Replace(sql, $"[?@:]{Regex.Escape(name)}", $"({paramStr})", _Options);
                }
            }
            return list;
        }
    }

    internal sealed class CreateDbParameterCache : ConcurrentDictionary<int, Func<object, DbParameter[]>>
    {
        private class ParameterCacheIdentity
        {
            private readonly string _sql;
            private readonly Type _type;
            private readonly int _hashCode;

            public ParameterCacheIdentity(Type type, string sql)
            {
                _type = type;
                _sql = sql;
                unchecked
                {
                    _hashCode = 17;
                    _hashCode = _hashCode * 23 + (type?.GetHashCode() ?? 0);
                    _hashCode = _hashCode * 23 + (sql?.GetHashCode() ?? 0);
                }
            }

            public override int GetHashCode() => _hashCode;

            public override bool Equals(object obj)
            {
                var other = obj as ParameterCacheIdentity;
                if (ReferenceEquals(this, other)) return true;
                if (ReferenceEquals(other, null)) return false;

                return _type == other._type && _sql == other._sql;
            }
        }

        private static readonly ConcurrentDictionary<ParameterCacheIdentity, Func<object, DbParameter[]>> _caches = new();

        internal static Func<object, DbParameter[]> GetFunc(string sql, object param)
        {
            var identity = new ParameterCacheIdentity(param.GetType(), sql);
            if (!_caches.ContainsKey(identity))
            {
                _caches.TryAdd(identity, CreateParameterFunc(sql, param));
            }
            var _ = _caches.TryGetValue(identity, out var value);
            return value;
        }

        /// <summary>
        /// 简单匿名参数映射
        /// </summary>
        private static Func<object, DbParameter[]> CreateParameterFunc(string sql, object param)
        {
            // 1. 反射参数对象，获取匿名对象信息
            var type = param.GetType();
            var props = type.GetProperties().Where(p => p.GetIndexParameters().Length == 0);

            // 2. 根据sql中写的参数过滤出不必要的对象
            var options = RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant;
            props = props.Where(p => Regex.IsMatch(sql, @"[?@:]" + p.Name + @"([^\p{L}\p{N}_]+|$)", options));

            // 使用Expression动态生成参数
            var expressions = new List<Expression>();
            var paramExp = Expression.Parameter(typeof(object), "param");

            var objExp = Expression.Variable(type, "obj");
            expressions.Add(Expression.Assign(objExp, Expression.Convert(paramExp, type)));

            var listExp = Expression.Variable(typeof(DbParameter[]), "list");
            var ctor = typeof(DbParameter[]).GetConstructor(new[] { typeof(int) });
            var ctorExp = Expression.New(ctor, Expression.Constant(props.Count()));
            expressions.Add(Expression.Assign(listExp, ctorExp));

            // 需要使用到的方法
            var setMi = typeof(DbParameter[]).GetMethod("Set", new[] { typeof(int), typeof(DbParameter) });

            int i = 0;
            foreach (var prop in props)
            {
                var newExp = NewParameter(prop, objExp);
                var setExp = Expression.Call(listExp, setMi, Expression.Constant(i++), newExp);
                expressions.Add(setExp);
            }
            // int i = 0;
            // expressions.AddRange(from prop in props
            //                      let nameExp = Expression.Constant("@" + prop.Name)
            //                      let valueExp = Expression.Call(objExp, prop.GetGetMethod())
            //                      let boxExp = Expression.Convert(valueExp, typeof(object))
            //                      select Expression.Call(createMi, nameExp, boxExp)
            //                      into createExp
            //                      select Expression.Call(listExp, setMi, Expression.Constant(i++), createExp));

            /*
            command.CommandText = Regex.Replace(command.CommandText, regexIncludingUnknown, match =>
            {
                var variableName = match.Groups[1].Value;
                var sb = GetStringBuilder().Append('(').Append(variableName).Append(1);
                for (int i = 2; i <= count; i++)
                {
                    sb.Append(',').Append(variableName);
                    if (!byPosition) sb.Append(i); else sb.Append(namePrefix).Append(i).Append(variableName);
                }
                return sb.Append(')').ToStringRecycle();
            }, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
            */
            expressions.Add(listExp);
            var body = Expression.Block(new[] { listExp, objExp }, expressions);
            return Expression.Lambda<Func<object, DbParameter[]>>(body, paramExp).Compile();
        }

        private static MethodCallExpression NewParameter(PropertyInfo prop, Expression objExp)
        {
            var createMi = typeof(DbParameterHelper).GetMethod("CreateDbParameter", new[] { typeof(string), typeof(object) });
            var nameExp = Expression.Constant("@" + prop.Name);
            var valueExp = Expression.Call(objExp, prop.GetGetMethod());

            if (prop.PropertyType.IsValueType)
            {
                var boxExp = Expression.Convert(valueExp, typeof(object));
                return Expression.Call(createMi, nameExp, boxExp);
            }
            if (prop.PropertyType.IsEnum) { }

            return Expression.Call(createMi, nameExp, valueExp);
        }

        private static string GetInListRegex(string name)
        {
            return "([?@:]" + Regex.Escape(name) + @")(?!\w)(\s+(?i)unknown(?-i))?";
        }
    }
}