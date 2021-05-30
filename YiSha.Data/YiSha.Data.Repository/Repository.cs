﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;
using YiSha.Data.EF.Database;
using YiSha.Data.Helper;
using YiSha.Util.Model;

namespace YiSha.Data.Repository
{
    /// <summary>
    /// 创建人：admin
    /// 日 期：2018.10.18
    /// 描 述：定义仓储模型中的数据标准操作接口
    /// </summary>
    public class Repository
    {
        #region Constructor

        private IDatabase Database { get; }

        public DbSet<TEntity> Set<TEntity>() where TEntity : class => Database.DbContext.Set<TEntity>();

        public Repository(IDatabase database)
        {
            Database = database;
        }

        #endregion

        #region Transaction

        public async Task<Repository> BeginTrans()
        {
            await Database.BeginTrans();
            return this;
        }

        public async Task<int> CommitTrans()
        {
            return await Database.CommitTrans();
        }

        public async Task RollbackTrans()
        {
            await Database.RollbackTrans();
        }

        #endregion

        #region Execute

        public async Task<int> ExecuteBySql(string sql, params DbParameter[] dbParameter)
        {
            return await Database.ExecuteBySql(sql, dbParameter);
        }

        public async Task<int> ExecuteByProc(string procName, params DbParameter[] dbParameter)
        {
            return await Database.ExecuteByProc(procName, dbParameter);
        }

        #endregion

        #region Insert

        public async Task<int> Insert<T>(T entity) where T : class
        {
            return await Database.Insert(entity);
        }

        public async Task<int> Insert<T>(IEnumerable<T> entity) where T : class
        {
            return await Database.Insert(entity);
        }

        #endregion

        #region Delete

        public async Task<int> Delete<T>() where T : class
        {
            return await Database.Delete<T>();
        }

        public async Task<int> Delete<T>(T entity) where T : class
        {
            return await Database.Delete(entity);
        }

        public async Task<int> Delete<T>(IEnumerable<T> entity) where T : class
        {
            return await Database.Delete(entity);
        }

        public async Task<int> Delete<T>(Expression<Func<T, bool>> condition) where T : class, new()
        {
            return await Database.Delete(condition);
        }

        public async Task<int> Delete<T>(params object[] id) where T : class
        {
            return await Database.Delete<T>(id);
        }

        public async Task<int> Delete<T>(string propertyName, object propertyValue) where T : class
        {
            return await Database.Delete<T>(propertyName, propertyValue);
        }

        #endregion

        #region Update

        public async Task<int> Update<T>(T entity) where T : class
        {
            return await Database.Update(entity);
        }

        public async Task<int> Update<T>(IEnumerable<T> entity) where T : class
        {
            return await Database.Update(entity);
        }

        public async Task<int> Update<T>(Expression<Func<T, bool>> condition) where T : class, new()
        {
            return await Database.Update(condition);
        }

        #endregion

        #region Find

        public async Task<T> FindEntity<T>(object id) where T : class
        {
            return await Database.FindEntity<T>(id);
        }

        public async Task<T> FindEntity<T>(Expression<Func<T, bool>> condition) where T : class, new()
        {
            return await Database.FindEntity(condition);
        }

        public async Task<T> FindEntity<T>(string sql, params DbParameter[] dbParameter)
        {
            return await Database.FindEntity<T>(sql, dbParameter);
        }

        public async Task<T> FindEntityAnonymousParameter<T>(string sql, object param = null)
        {
            if (param == null)
            {
                return await Database.FindEntity<T>(sql);
            }
            return await Database.FindEntity<T>(sql, DbParameterHelper.CreateParameters(ref sql, param));
        }

        public async Task<List<T>> FindList<T>() where T : class, new()
        {
            return await Database.FindList<T>();
        }

        public async Task<List<T>> FindList<T>(Expression<Func<T, bool>> condition) where T : class, new()
        {
            return await Database.FindList(condition);
        }

        public async Task<List<T>> FindList<T>(string strSql) where T : class
        {
            return await Database.FindList<T>(strSql);
        }

        public async Task<List<T>> FindList<T>(string strSql, params DbParameter[] dbParameter) where T : class
        {
            return await Database.FindList<T>(strSql, dbParameter);
        }

        public async Task<(int total, List<T> list)> FindList<T>(Pagination pagination) where T : class, new()
        {
            int total = pagination.TotalCount;
            var data = await Database.FindList<T>(pagination.Sort, pagination.SortType.ToLower() == "asc", pagination.PageSize, pagination.PageIndex, null);
            pagination.TotalCount = total;
            return data;
        }

        public async Task<List<T>> FindList<T>(Expression<Func<T, bool>> condition, Pagination pagination) where T : class, new()
        {
            var data = await Database.FindList(pagination.Sort, pagination.SortType.ToLower() == "asc", pagination.PageSize, pagination.PageIndex, condition);
            pagination.TotalCount = data.total;
            return data.list;
        }

        public async Task<(int total, List<T> list)> FindList<T>(string strSql, Pagination pagination) where T : class
        {
            int total = pagination.TotalCount;
            var data = await Database.FindList<T>(strSql, pagination.Sort, pagination.SortType.ToLower() == "asc", pagination.PageSize, pagination.PageIndex);
            pagination.TotalCount = total;
            return data;
        }

        public async Task<List<T>> FindList<T>(string strSql, Pagination pagination, params DbParameter[] dbParameter) where T : class
        {
            var data = await Database.FindList<T>(strSql, pagination.Sort, pagination.SortType.ToLower() == "asc", pagination.PageSize, pagination.PageIndex, dbParameter);
            pagination.TotalCount = data.total;
            return data.Item2;
        }

        public async Task<DataTable> FindTable(string sql, params DbParameter[] dbParameter)
        {
            return await Database.FindTable(sql, dbParameter);
        }

        public async Task<DataTable> FindTable(string sql, Pagination pagination, params DbParameter[] dbParameter)
        {
            var (total, dataTable) = await Database.FindTable(sql, pagination.Sort, pagination.SortType.ToLower() == "asc", pagination.PageSize, pagination.PageIndex, dbParameter);
            pagination.TotalCount = total;
            return dataTable;
        }

        #endregion
    }
}