using System;
using System.Data.Common;
using System.Linq;
using YiSha.Util.Helper;

namespace YiSha.Data.Helper
{
    public static class DbSqlHelper
    {
        /// <summary>
        /// 根据配置文件中所配置的数据库类型
        /// 来创建相应数据库的参数对象
        /// </summary>
        public static string GetPagingSql(string sql, string sort, bool isAsc, int pageSize, int pageIndex)
        {
            return DbHelper.DbType switch
            {
                DatabaseType.SqlServer => SqlPagingSql(sql, sort, isAsc, pageSize, pageIndex),
                DatabaseType.MySql => MySqlPagingSql(sql, sort, isAsc, pageSize, pageIndex),
                DatabaseType.Oracle => OraclePagingSql(sql, sort, isAsc, pageSize, pageIndex),
                _ => throw new Exception("数据库类型目前不支持！")
            };
        }

        private static string SqlPagingSql(string sql, string sort, bool isAsc, int pageSize, int pageIndex)
        {
            CheckSqlParam(sort);

            if (pageIndex == 0)
            {
                pageIndex = 1;
            }
            int start = (pageIndex - 1) * pageSize;
            int end = pageIndex * pageSize;

            string orderBy = "ORDER BY (SELECT 0)";
            if (sort?.Length > 0)
            {
                orderBy = $" ORDER BY {sort} ";
                if (sort.ToUpper().IndexOf("ASC") + sort.ToUpper().IndexOf("DESC") <= 0)
                {
                    orderBy += isAsc ? "ASC" : "DESC";
                }
            }
            return $"SELECT * FROM (SELECT ROW_NUMBER() Over ({orderBy}) AS ROWNUM, * From ({sql}) t ) AS N WHERE ROWNUM > {start} AND ROWNUM <= {end}";
        }

        private static string OraclePagingSql(string sql, string sort, bool isAsc, int pageSize, int pageIndex)
        {
            CheckSqlParam(sort);

            if (pageIndex == 0)
            {
                pageIndex = 1;
            }
            int start = (pageIndex - 1) * pageSize;
            int end = pageIndex * pageSize;

            string orderBy = "";
            if (sort?.Length > 0)
            {
                orderBy = $" ORDER BY {sort} ";
                if (sort.ToUpper().IndexOf("ASC") + sort.ToUpper().IndexOf("DESC") <= 0)
                {
                    orderBy = isAsc ? "ASC" : "DESC";
                }
            }
            return $"SELECT * From (SELECT ROWNUM AS n, T.* From ({sql + orderBy}) t )  N WHERE n > {start} AND n <= {end}";
        }

        private static string MySqlPagingSql(string sql, string sort, bool isAsc, int pageSize, int pageIndex)
        {
            CheckSqlParam(sort);

            if (pageIndex == 0)
            {
                pageIndex = 1;
            }
            int num = (pageIndex - 1) * pageSize;

            string orderBy = "";
            if (sort?.Length > 0)
            {
                orderBy = $" ORDER BY {sort} ";
                if (sort.ToUpper().IndexOf("ASC") + sort.ToUpper().IndexOf("DESC") <= 0)
                {
                    orderBy += isAsc ? "ASC" : "DESC";
                }
            }
            return sql + orderBy + $" LIMIT {num}, {pageSize}";
        }

        public static string GetCountSql(string sql)
        {
            return $"SELECT COUNT(1) FROM ({sql}) t";
        }

        /// <summary>
        /// 拼接删除SQL语句
        /// </summary>
        public static string GetDeleteSql(string tableName)
        {
            return $"DELETE FROM {tableName}";
        }

        /// <summary>
        /// 拼接删除SQL语句
        /// </summary>
        public static (string sql, DbParameter parameter) GetDeleteSql(string tableName, string propertyName, object propertyValue)
        {
            var parameter = DbParameterHelper.CreateDbParameter($"@{propertyName}", propertyValue);
            var sql = $"DELETE FROM {tableName} WHERE {propertyName} = {parameter.ParameterName}";
            return (sql, parameter);
        }

        /// <summary>
        /// 拼接批量删除SQL语句
        /// </summary>
        public static (string sql, DbParameter[] parameters) GetDeleteSql(string tableName, string propertyName, object[] propertyValue)
        {
            var parameters = DbParameterHelper.CreateDbParameters($"@{propertyName}", propertyValue);
            var sql = $"DELETE FROM {tableName} WHERE {propertyName} IN ({string.Join(',', parameters.Select(x => x.ParameterName))})";
            return (sql, parameters);
        }

        /// <summary>
        /// 存储过程语句
        /// </summary>
        public static string BuilderProc(string procName, params DbParameter[] dbParameter)
        {
            return $"EXEC {procName} {string.Join(',', dbParameter.Select(x => $"{x.ParameterName}"))}";
        }

        private static void CheckSqlParam(string param)
        {
            if (!SecurityHelper.IsSafeSqlParam(param))
            {
                throw new ArgumentException("含有Sql注入的参数");
            }
        }
    }
}