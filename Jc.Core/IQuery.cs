﻿
using Jc.Core.Data;
using Jc.Core.Data.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jc.Core
{
    /// <summary>
    /// 查询
    /// </summary>
    public class IQuery<T> where T : class, new()
    {
        #region Fields
        private DbContext dbContext = null;
        string tableNamePfx;    //表名称参数或表全称
        Pager pager;
        List<OrderByClause> orderByClauseList = new List<OrderByClause>();
        Expression<Func<T, bool>> query = null;
        Expression<Func<T, object>> select = null;
        Expression<Func<T, object>> unSelect = null;
        #endregion

        #region Properties
        /// <summary>
        /// QueryExpression
        /// </summary>
        internal Expression<Func<T, bool>> QueryExpression
        {
            get { return this.query; }
        }

        /// <summary>
        /// OrderBy
        /// </summary>
        internal List<OrderByClause> OrderByClauseList { get { return orderByClauseList; } set { orderByClauseList = value; } }
        #endregion

        #region Ctor
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="dbContext">dbContext</param>
        public IQuery(DbContext dbContext)
        {
            ExHelper.ThrowIfNull(dbContext, "IQuery初始化失败,dbContext对象不能为空.");
            this.dbContext = dbContext;
        }
        #endregion

        #region Methods

        /// <summary>
        /// 设置查询表名称.
        /// 1.如果设置了Variable=true,则表名称可变.在使用时,请传入tableNamePfx参数
        /// 2.如TableAttr中Name为Data{0}.tableNamePfx参数为2018.则表名称为Data2018
        /// 3.如果未设置Name,将使用传入tableNamePfx参数作为表名称.
        /// 4.可变表一般为分表情况下使用.设置表AutoCreate属性为true.在插入数据时会自动创建表.
        /// 5.如果表名称为空或未设置填充参数{0},则直接使用传入参数tableNamePfx作为表名
        /// </summary>
        /// <param name="tableNamePfx">表名称参数或表全称</param>
        /// <returns></returns>
        public IQuery<T> FromTable(string tableNamePfx)
        {
            this.tableNamePfx = tableNamePfx;
            return this;
        }

        /// <summary>
        /// 选择的字段
        /// </summary>
        /// <param name="select">查询属性</param>
        /// <param name="unSelect">排除查询属性</param>
        /// <returns></returns>
        public IQuery<T> Select(Expression<Func<T, object>> select = null,
            Expression<Func<T, object>> unSelect = null)
        {
            this.select = select;
            this.unSelect = unSelect;
            return this;
        }

        /// <summary>
        /// 排除查询的属性
        /// </summary>
        /// <param name="unSelect">排除查询属性</param>
        /// <returns></returns>
        public IQuery<T> UnSelect(Expression<Func<T, object>> unSelect = null)
        {
            this.unSelect = unSelect;
            return this;
        }

        /// <summary>
        /// whereclip
        /// </summary>
        /// <param name="query">查询条件</param>
        /// <returns></returns>
        public IQuery<T> Where(Expression<Func<T, bool>> query = null)
        {
            this.query = query;
            return this;
        }

        /// <summary>
        /// whereclip
        /// </summary>
        /// <param name="query">查询条件</param>
        /// <param name="select">查询属性</param>
        /// <param name="unSelect">排除查询属性</param>
        /// <returns></returns>
        public IQuery<T> Where(Expression<Func<T, bool>> query = null,
            Expression<Func<T, object>> select = null,
            Expression<Func<T, object>> unSelect = null)
        {
            this.query = query;
            this.select = select;
            this.unSelect = unSelect;
            return this;
        }
        /// <summary>
        /// 追加And查询条件
        /// </summary>
        /// <param name="query">查询条件</param>
        /// <returns></returns>
        public IQuery<T> And(Expression<Func<T, bool>> query = null)
        {
            if (this.query == null)
            {
                this.query = query;
            }
            else
            {
                this.query = this.query.And(query);
            }
            return this;
        }

        /// <summary>
        /// 追加Or查询条件
        /// </summary>
        /// <param name="query">查询条件</param>
        /// <returns></returns>
        public IQuery<T> Or(Expression<Func<T, bool>> query = null)
        {
            if (this.query == null)
            {
                this.query = query;
            }
            else
            {
                this.query = this.query.Or(query);
            }
            return this;
        }

        /// <summary>
        /// 执行查询记录
        /// </summary>
        /// <returns></returns>
        public T FirstOrDefault()
        {
            QueryFilter filter = QueryFilterHelper.GetFilter(select, query, orderByClauseList, unSelect);
            T dto = null;
            using (DbCommand dbCommand = dbContext.DbProvider.GetQueryDbCommand<T>(filter, tableNamePfx))
            {
                try
                {
                    dbCommand.Connection = dbContext.GetDbConnection();
                    DbDataReader dr = dbCommand.ExecuteReader();
                    DataTable dt = dbContext.ConvertDataReaderToDataTable(dr,1);
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        dto = dt.Rows[0].ToEntity<T>();
                    }
                    dbContext.CloseDbConnection(dbCommand);
                }
                catch (Exception ex)
                {
                    dbContext.CloseDbConnection(dbCommand);
                    throw ex;
                }
            }
            return dto;
        }

        /// <summary>
        /// 返回字段Sum
        /// </summary>
        /// <returns></returns>
        public decimal Sum(Expression<Func<T, object>> select = null)
        {
            this.select = select;
            QueryFilter filter = QueryFilterHelper.GetFilter(select, query);

            decimal result = 0;
            using (DbCommand dbCommand = dbContext.DbProvider.GetSumDbCommand<T>(filter, tableNamePfx))
            {
                try
                {
                    dbCommand.Connection = dbContext.GetDbConnection();
                    object objVal = dbCommand.ExecuteScalar();
                    if (objVal != DBNull.Value)
                    {   //使用属性字典
                        result = (decimal)objVal;
                    }
                    dbContext.CloseDbConnection(dbCommand);
                }
                catch (Exception ex)
                {
                    dbContext.CloseDbConnection(dbCommand);
                    throw ex;
                }
            }
            return result;
        }

        /// <summary>
        /// 返回RecCount
        /// </summary>
        /// <returns></returns>
        public int Count(Expression<Func<T, bool>> query = null)
        {
            if (query!=null)
            {
                this.query = query;
            }
            QueryFilter filter = QueryFilterHelper.GetFilter(this.query);

            int result = 0;
            using (DbCommand dbCommand = dbContext.DbProvider.GetCountDbCommand<T>(filter, tableNamePfx))
            {
                try
                {
                    dbCommand.Connection = dbContext.GetDbConnection();
                    object objVal = dbCommand.ExecuteScalar();
                    if (objVal != DBNull.Value)
                    {   //使用属性字典
                        result = Convert.ToInt32(objVal);
                    }
                    dbContext.CloseDbConnection(dbCommand);
                }
                catch (Exception ex)
                {
                    dbContext.CloseDbConnection(dbCommand);
                    throw ex;
                }
            }
            return result;
        }

        /// <summary>
        /// 执行查询记录
        /// </summary>
        /// <returns></returns>
        public List<T> ToList()
        {
            QueryFilter filter = QueryFilterHelper.GetFilter(select,query,orderByClauseList,unSelect);
            if (pager != null)
            {
                filter.InitPage(pager.PageIndex, pager.PageSize);
            }
            List<T> list = new List<T>();
            using (DbCommand dbCommand = this.dbContext.DbProvider.GetQueryDbCommand<T>(filter, tableNamePfx))
            {
                try
                {
                    dbCommand.Connection = this.dbContext.GetDbConnection();
                    DbDataReader dr = dbCommand.ExecuteReader();
                    DataTable dt = this.dbContext.ConvertDataReaderToDataTable(dr);
                    list = dt.ToList<T>();
                    this.dbContext.CloseDbConnection(dbCommand);
                }
                catch (Exception ex)
                {
                    this.dbContext.CloseDbConnection(dbCommand);
                    throw ex;
                };
            }
            return list;
        }

        /// <summary>
        /// 执行查询记录
        /// </summary>
        /// <returns></returns>
        public PageResult<T> ToPageList()
        {
            PageResult<T> result = new PageResult<T>();
            QueryFilter filter = QueryFilterHelper.GetPageFilter(select, query, orderByClauseList, pager,unSelect);

            List<T> list = new List<T>();
            DbCommand dbCommand = null;
            try
            {
                if (!filter.IsPage)
                {
                    throw new Exception("分页查询未指定分页信息");
                }
                else
                {
                    dbCommand = dbContext.DbProvider.GetQueryRecordsPageDbCommand<T>(filter, tableNamePfx);
                }
                dbCommand.Connection = dbContext.GetDbConnection();
                DbDataReader dr = dbCommand.ExecuteReader();
                DataTable dt = dbContext.ConvertDataReaderToDataTable(dr);

                list = dt.ToList<T>();

                int totalCount = 0;
                DbCommand getRecCountDbCommand = dbContext.DbProvider.GetRecCountDbCommand<T>(filter, tableNamePfx);
                getRecCountDbCommand.Connection = dbCommand.Connection;
                object valueObj = getRecCountDbCommand.ExecuteScalar();
                if (valueObj != null && valueObj != DBNull.Value)
                {
                    totalCount = Convert.ToInt32(valueObj);
                }
                //分页查询 获取记录总数
                result.Rows = list;
                result.Total = totalCount;
                filter.TotalCount = totalCount;
                dbContext.CloseDbConnection(dbCommand);
            }
            catch (Exception ex)
            {
                dbContext.CloseDbConnection(dbCommand);
                throw ex;
            }
            return result;
        }
        
        /// <summary>
        /// 执行查询记录
        /// </summary>
        /// <returns></returns>
        public PageResult<T> ToPageList(int pageIndex, int pageSize)
        {
            this.Page(pageIndex, pageSize);
            return ToPageList();
        }

        /// <summary>
        /// whereclip
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public IQuery<T> Page(int pageIndex, int pageSize)
        {
            this.pager = new Pager(pageIndex, pageSize);
            return this;
        }

        /// <summary>
        /// 添加Orderby
        /// </summary>
        /// <param name="sort">排序字段</param>
        /// <param name="order">排序方向 asc desc</param>
        /// <param name="expr">默认排序属性</param>
        /// <param name="defaultOrder">默认排序方向</param>
        /// <returns></returns>
        public IQuery<T> AutoOrderBy(string sort, string order, Expression<Func<T, object>> expr = null,Sorting defaultOrder = Sorting.Asc)
        {
            if (!string.IsNullOrEmpty(sort))
            {
                this.orderByClauseList.Add(new OrderByClause()
                {
                    FieldName = sort,
                    Order = order.ToLower() == "desc" ? Sorting.Desc : Sorting.Asc
                });
            }
            else if (expr != null)
            {
                if(defaultOrder == Sorting.Asc)
                {
                    OrderBy(expr);
                }
                else
                {
                    OrderByDesc(expr);
                }
            }
            return this;
        }
        
        /// <summary>
        /// OrderBy
        /// </summary>
        /// <returns></returns>
        public IQuery<T> OrderBy(Expression<Func<T, object>> expr)
        {
            List<PiMap> piMapList = DtoMappingHelper.GetPiMapList<T>(expr);
            if (piMapList != null && piMapList.Count > 0)
            {
                for (int i = 0; i < piMapList.Count; i++)
                {
                    this.orderByClauseList.Add(new OrderByClause() {
                        FieldName = piMapList[i].FieldName,
                        Order = Sorting.Asc });
                }
            }
            else
            {
                throw new Exception("无效的查询排序表达式.");
            }
            return this;
        }

        /// <summary>
        /// OrderByDesc
        /// </summary>
        /// <returns></returns>
        public IQuery<T> OrderByDesc(Expression<Func<T, object>> expr)
        {
            List<PiMap> piMapList = DtoMappingHelper.GetPiMapList<T>(expr);
            if (piMapList != null && piMapList.Count > 0)
            {
                for (int i = 0; i < piMapList.Count; i++)
                {
                    this.orderByClauseList.Add(new OrderByClause()
                    {
                        FieldName = piMapList[i].FieldName,
                        Order = Sorting.Desc
                    });
                }
            }
            else
            {
                throw new Exception("无效的查询排序表达式.");
            }
            return this;
        }        
        #endregion
    }
}