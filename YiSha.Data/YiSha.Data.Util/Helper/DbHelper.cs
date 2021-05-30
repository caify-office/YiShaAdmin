using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading.Tasks;
using YiSha.Util.Model;

namespace YiSha.Data.Helper
{
    public class DbHelper
    {
        public DbHelper(DbContext dbContext)
        {
            DbContext = dbContext;
            DbConnection = DbContext.Database.GetDbConnection();
            DbCommand = DbConnection.CreateCommand();
        }

        /// <summary>
        /// 数据库类型
        /// </summary>
        public static DatabaseType DbType { get; set; }

        /// <summary>
        /// 数据库上下文
        /// </summary>
        private DbContext DbContext { get; }

        /// <summary>
        /// 数据库连接对象
        /// </summary>
        private DbConnection DbConnection { get; }

        /// <summary>
        /// 执行命令对象
        /// </summary>
        private DbCommand DbCommand { get; }

        /// <summary>
        /// 执行命令对象的行为
        /// </summary>
        private static CommandBehavior CommandBehavior => CommandBehavior.CloseConnection | CommandBehavior.KeyInfo | CommandBehavior.SequentialAccess | CommandBehavior.SingleResult;

        /// <summary>
        /// 执行SQL返回 DataReader
        /// </summary>
        /// <param name="cmdType">命令的类型</param>
        /// <param name="sql">Sql语句</param>
        /// <param name="dbParameter">Sql参数</param>
        public async Task<IDataReader> ExecuteReadeAsync(CommandType cmdType, string sql, params DbParameter[] dbParameter)
        {
            try
            {
                // 兼容EF Core的DbCommandInterceptor
                var dependencies = ((IDatabaseFacadeDependenciesAccessor)DbContext.Database).Dependencies;
                var relationalDatabaseFacade = (IRelationalDatabaseFacadeDependencies)dependencies;
                var connection = relationalDatabaseFacade.RelationalConnection;
                var logger = relationalDatabaseFacade.CommandLogger;
                var commandId = Guid.NewGuid();

                await PrepareCommand(cmdType, sql, dbParameter);

                var startTime = DateTimeOffset.UtcNow;
                var stopwatch = Stopwatch.StartNew();

                var interceptionResult = logger == null ? default : await logger.CommandReaderExecutingAsync(connection, DbCommand, DbContext, commandId, connection.ConnectionId, startTime);
                var reader = interceptionResult.HasResult ? interceptionResult.Result : await DbCommand.ExecuteReaderAsync(CommandBehavior);
                if (logger != null)
                {
                    reader = await logger.CommandReaderExecutedAsync(connection, DbCommand, DbContext, commandId, connection.ConnectionId, reader, startTime, stopwatch.Elapsed);
                }
                return reader;
            }
            catch (Exception)
            {
                await DbContext.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// 执行查询，并返回查询所返回的结果集
        /// </summary>
        /// <param name="cmdType">命令的类型</param>
        /// <param name="sql">Sql语句</param>
        /// <param name="dbParameter">Sql参数</param>
        public async Task<object> ExecuteScalarAsync(CommandType cmdType, string sql, params DbParameter[] dbParameter)
        {
            try
            {
                // 兼容EF Core的DbCommandInterceptor
                var dependencies = ((IDatabaseFacadeDependenciesAccessor)DbContext.Database).Dependencies;
                var relationalDatabaseFacade = (IRelationalDatabaseFacadeDependencies)dependencies;
                var connection = relationalDatabaseFacade.RelationalConnection;
                var logger = relationalDatabaseFacade.CommandLogger;
                var commandId = Guid.NewGuid();

                await PrepareCommand(cmdType, sql, dbParameter);

                var startTime = DateTimeOffset.UtcNow;
                var stopwatch = Stopwatch.StartNew();

                var interceptionResult = logger == null ? default : await logger.CommandScalarExecutingAsync(connection, DbCommand, DbContext, commandId, connection.ConnectionId, startTime);
                var obj = interceptionResult.HasResult ? interceptionResult.Result : await DbCommand.ExecuteScalarAsync();
                if (logger != null)
                {
                    obj = await logger.CommandScalarExecutedAsync(connection, DbCommand, DbContext, commandId, connection.ConnectionId, obj, startTime, stopwatch.Elapsed);
                }
                return obj;
            }
            catch (Exception)
            {
                await DbContext.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// 为即将执行准备一个命令
        /// </summary>
        /// <param name="cmdType">执行命令的类型（存储过程或T-SQL，等等）</param>
        /// <param name="sql">存储过程名称或者T-SQL命令行, e.g. Select * from Products</param>
        /// <param name="parameters">执行命令所需的sql语句对应参数</param>
        private async Task PrepareCommand(CommandType cmdType, string sql, params DbParameter[] parameters)
        {
            if (DbConnection.State != ConnectionState.Open)
            {
                await DbConnection.OpenAsync().ConfigureAwait(false);
            }
            if (parameters != null)
            {
                DbCommand.Parameters.Clear();
                foreach (var parameter in parameters)
                {
                    DbCommand.Parameters.Add(parameter);
                }
            }
            DbCommand.CommandText = sql;
            DbCommand.CommandType = cmdType;
            DbCommand.CommandTimeout = GlobalContext.SystemConfig.DbCommandTimeout;
        }
    }
}