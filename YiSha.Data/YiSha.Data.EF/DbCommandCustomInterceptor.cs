using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using YiSha.Data.Extension;
using YiSha.Util.Helper;
using YiSha.Util.Model;

namespace YiSha.Data.EF
{
    /// <summary>
    /// Sql执行拦截器
    /// </summary>
    public class DbCommandCustomInterceptor : DbCommandInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override async ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (eventData.Duration.TotalMilliseconds >= GlobalContext.SystemConfig.DbSlowSqlLogTime * 1000)
            {
                LogHelper.Warning($"耗时的Sql：{command.GetSql()}");
            }
            return await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            return await base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override async ValueTask<object> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object result, CancellationToken cancellationToken = default)
        {
            if (eventData.Duration.TotalMilliseconds >= GlobalContext.SystemConfig.DbSlowSqlLogTime * 1000)
            {
                LogHelper.Warning($"耗时的Sql：{command.GetSql()}");
            }
            return await base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (eventData.Duration.TotalMilliseconds >= GlobalContext.SystemConfig.DbSlowSqlLogTime * 1000)
            {
                LogHelper.Warning($"耗时的Sql：{command.GetSql()}");
            }
            return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }
    }
}