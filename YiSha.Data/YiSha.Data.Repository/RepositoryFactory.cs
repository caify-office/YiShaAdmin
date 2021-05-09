using System;
using YiSha.Data.EF.Database;
using YiSha.Data.Helper;
using YiSha.Util.Model;

namespace YiSha.Data.Repository
{
    public class RepositoryFactory
    {
        public Repository BaseRepository()
        {
            switch (GlobalContext.SystemConfig.DbProvider)
            {
                case "SqlServer":
                    DbHelper.DbType = DatabaseType.SqlServer;
                    return new Repository(new SqlServerDatabase(GlobalContext.SystemConfig.DbConnectionString));
                case "MySql":
                    DbHelper.DbType = DatabaseType.MySql;
                    return new Repository(new MySqlDatabase(GlobalContext.SystemConfig.DbConnectionString));
                // case "Oracle":
                    // DbHelper.DbType = DatabaseType.Oracle;
                    // 支持Oracle或是更多数据库请参考上面SqlServer或是MySql的写法
                    // break;
                default: throw new Exception("未找到数据库配置");
            }
        }
    }
}