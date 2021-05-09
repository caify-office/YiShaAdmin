using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using YiSha.Util.Helper;
using YiSha.Util.Model;

namespace YiSha.Data.Extension
{
    public static class DbContextExtension
    {
        /// <summary>
        /// 获取实体映射对象
        /// </summary>
        public static IEntityType GetEntityType<T>(this DbContext dbContext) where T : class
        {
            return dbContext.Model.FindEntityType(typeof(T));
        }

        /// <summary>
        /// 把null设置成对应属性类型的默认值
        /// </summary>
        public static void SetDefaultValue(this DbContext dbContext)
        {
            foreach (var entry in dbContext.ChangeTracker.Entries().Where(entry => entry.State == EntityState.Added))
            {
                var type = entry.Entity.GetType();
                var props = ReflectionHelper.GetProperties(type).Where(p => p.Name != "Id");
                foreach (var prop in props)
                {
                    object value = prop.GetValue(entry.Entity, null);
                    if (value == null)
                    {
                        string typeName = GetPropertyTypeName(prop);
                        var defaultValue = GetPropertyDefaultValue(typeName);
                        prop.SetValue(entry.Entity, defaultValue);
                    }
                    else if (value.ToString() == DateTime.MinValue.ToString(CultureInfo.InvariantCulture))
                    {
                        // sql server datetime类型的的范围不到0001-01-01，所以转成1970-01-01
                        prop.SetValue(entry.Entity, GlobalConstant.DefaultTime);
                    }
                }
            }
        }

        private static string GetPropertyTypeName(PropertyInfo prop)
        {
            return prop.PropertyType.GenericTypeArguments.Length > 0 ? prop.PropertyType.GenericTypeArguments[0].Name : prop.PropertyType.Name;
        }

        private static object GetPropertyDefaultValue(string typeName)
        {
            return typeName switch
            {
                "Boolean" => default(bool),
                "Char" => default(char),
                "SByte" => default(sbyte),
                "Byte" => default(char),
                "Int16" => default(short),
                "UInt16" => default(ushort),
                "Int32" => default(int),
                "UInt32" => default(uint),
                "Int64" => default(long),
                "UInt64" => default(ulong),
                "Single" => default(float),
                "Double" => default(double),
                "Decimal" => default(decimal),
                "DateTime" => GlobalConstant.DefaultTime,
                "String" => string.Empty,
                _ => default
            };
        }
    }
}