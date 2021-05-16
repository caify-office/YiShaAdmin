using Microsoft.Data.SqlClient;
using MySqlConnector;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using YiSha.Util.Extension;

namespace YiSha.Data.Helper
{
    public static class DbParameterHelper
    {
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
            var newExp = Expression.New(ctor, Expression.Constant(props.Count()));
            expressions.Add(Expression.Assign(listExp, newExp));

            // 需要使用到的方法
            var createMi = typeof(DbParameterHelper).GetMethod("CreateDbParameter", new[] { typeof(string), typeof(object) });
            var setMi = typeof(DbParameter[]).GetMethod("Set", new[] { typeof(int), typeof(DbParameter) });

            int i = 0;
            foreach (var prop in props)
            {
                var nameExp = Expression.Constant("@" + prop.Name);
                var valueExp = Expression.Call(objExp, prop.GetGetMethod());
                var boxExp = Expression.Convert(valueExp, typeof(object));
                var createExp = Expression.Call(createMi, nameExp, boxExp);
                var set2Exp = Expression.Call(listExp, setMi, Expression.Constant(i++), createExp);
                expressions.Add(set2Exp);
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

        private static string GetInListRegex(string name)
        {
            return "([?@:]" + Regex.Escape(name) + @")(?!\w)(\s+(?i)unknown(?-i))?";
        }
    }
}