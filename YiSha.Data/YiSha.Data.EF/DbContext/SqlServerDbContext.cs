using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using YiSha.Util.Model;

namespace YiSha.Data.EF.DbContext
{
    public class SqlServerDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        private static readonly IInterceptor _interceptor = new DbCommandCustomInterceptor();

        private string ConnectionString { get; }

        public SqlServerDbContext(string connectionString)
        {
            ConnectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.AddInterceptors(_interceptor);
                optionsBuilder.UseSqlServer(ConnectionString, p => p.CommandTimeout(GlobalContext.SystemConfig.DbCommandTimeout));
                optionsBuilder.UseLoggerFactory(_loggerFactory);
                optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entityAssembly = Assembly.Load(new AssemblyName("YiSha.Entity"));
            var typesToRegister = entityAssembly.GetTypes().Where(p => !string.IsNullOrEmpty(p.Namespace))
                                                .Where(p => !string.IsNullOrEmpty(p.GetCustomAttribute<TableAttribute>()?.Name));
            foreach (var type in typesToRegister)
            {
                modelBuilder.Model.AddEntityType(type);
            }
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                var currentTableName = modelBuilder.Entity(entity.Name).Metadata.GetTableName();
                modelBuilder.Entity(entity.Name).ToTable(currentTableName);
                modelBuilder.Entity(entity.Name).HasKey("Id");
            }

            base.OnModelCreating(modelBuilder);
        }
    }
}