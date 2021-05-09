using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using YiSha.Util.Helper;
using YiSha.Util.Model;

namespace YiSha.Data.EF.DbContext
{
    public class MySqlDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        private static readonly ConcurrentDictionary<string, string[]> _keyCache = new();
        private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        private string ConnectionString { get; }

        private ServerVersion ServerVersion { get; }

        public MySqlDbContext(string connectionString)
        {
            ConnectionString = connectionString;
            var versions = TextHelper.SplitToArray<int>(GlobalContext.SystemConfig.DbVersion, '.');
            ServerVersion = new MySqlServerVersion(new Version(versions[0], versions[1]));
        }

        #region 重载

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            if (!builder.IsConfigured)
            {
                builder.UseLoggerFactory(_loggerFactory)
                       .AddInterceptors(new DbCommandCustomInterceptor())
                       .UseMySql(ConnectionString, ServerVersion, o => o.CommandTimeout(GlobalContext.SystemConfig.DbCommandTimeout));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entityAssembly = Assembly.Load(new AssemblyName("YiSha.Entity"));
            var types = entityAssembly.GetTypes()
                                                .Where(p => p.Namespace?.Length > 0)
                                                .Where(p => p.GetCustomAttribute<TableAttribute>()?.Name.Length > 0);

            foreach (var type in types)
            {
                if (modelBuilder.Model.FindEntityType(type) == null)
                {
                    modelBuilder.Model.AddEntityType(type);
                }

                if (!_keyCache.ContainsKey(type.FullName))
                {
                    var props = ReflectionHelper.GetProperties(type)
                                                .Where(p => p.GetCustomAttribute<KeyAttribute>() is not null)
                                                .Select(x => x.Name).ToArray();
                    _keyCache.TryAdd(type.FullName, props.Any() ? props : new[] { "Id" });
                }
            }

            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                var model = modelBuilder.Entity(entity.Name);
                var currentTableName = model.Metadata.GetTableName();
                model.HasKey(_keyCache[entity.Name]);
                model.ToTable(currentTableName);
            }

            base.OnModelCreating(modelBuilder);
        }

        #endregion
    }
}