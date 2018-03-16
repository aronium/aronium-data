using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Aronium.Data
{
    /// <summary>
    /// Contains methods for handling database operations for specified type.
    /// </summary>
    /// <typeparam name="TEntity">Entity type.</typeparam>
    public abstract class EntityRepository<TEntity> : Repository, IDisposable where TEntity : class, new()
    {
        #region - Fields -

        private Type _type;
        private string _selectQuery;
        private string _insertQuery;
        private string _insertWithOutputQueryTemplate;

        private static readonly string INSERT = "INSERT INTO [{0}] ({1}) VALUES ({2})";
        private static readonly string INSERT_OUTPUT = "INSERT INTO [{0}] ({1}) OUTPUT INSERTED.[{2}] VALUES ({3})";
        private static readonly string SELECT = "SELECT {0} FROM [{1}]";
        private static readonly string DELETE = "DELETE FROM [{0}] WHERE ID=@ID";
        private static readonly string SELECT_BY_ID = "{0} WHERE ID=@ID";

        private readonly string _tableName;

        #endregion

        public EntityRepository()
        {
            _tableName = this.GetType().GetCustomAttributes(typeof(TableNameAttribute), true).Cast<TableNameAttribute>().FirstOrDefault()?.Name ?? EntityType.Name;
        }

        #region - Properties -

        /// <summary>
        /// Gets {TEntity} entity type.
        /// </summary>
        public Type EntityType
        {
            get
            {
                if (_type == null)
                    _type = typeof(TEntity);

                return _type;
            }
        }

        /// <summary>
        /// Gets table name.
        /// </summary>
        private string TableName
        {
            get
            {
                return _tableName;
            }
        }

        /// <summary>
        /// Gets select query for entity type.
        /// </summary>
        protected virtual string SelectQueryString
        {
            get
            {
                if (_selectQuery == null)
                {
                    var properties = GetEntityPropertyNames();

                    var columns = string.Join(",", properties.Select(property => string.Format("[{0}]", property)));

                    _selectQuery = string.Format(SELECT, columns, TableName);
                }

                return _selectQuery;
            }
        }

        /// <summary>
        /// Gets insert query for entity type.
        /// </summary>
        protected virtual string InsertQueryString
        {
            get
            {
                if (_insertQuery == null)
                {
                    var properties = GetEntityPropertyNames(true);

                    var columns = string.Join(",", properties.Select(property => string.Format("[{0}]", property)));
                    var arguments = string.Join(",", properties.Select(property => string.Format("@{0}", property)));

                    _insertQuery = string.Format(INSERT, EntityType.Name, columns, arguments);
                }

                return _insertQuery;
            }
        }

        /// <summary>
        /// Gets insert query string pattern with output column.
        /// </summary>
        protected virtual string InsertQueryStringWithOutputColumn
        {
            get
            {
                if (_insertWithOutputQueryTemplate == null)
                {
                    var properties = GetEntityPropertyNames(true);

                    var columns = string.Join(",", properties.Select(property => string.Format("[{0}]", property)));
                    var arguments = string.Join(",", properties.Select(property => string.Format("@{0}", property)));

                    _insertWithOutputQueryTemplate = string.Format(INSERT_OUTPUT, TableName, columns, "{0}", arguments);
                }

                return _insertWithOutputQueryTemplate;
            }
        }

        #endregion

        #region - Private methods -

        /// <summary>
        /// Gets list of property names.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> GetEntityPropertyNames(bool excludeIdentityColumns = false)
        {
            return EntityType.GetProperties().Where(x => x.CanWrite && !x.GetSetMethod().IsVirtual && (!excludeIdentityColumns || !x.GetCustomAttributes(typeof(IdentityColumnAttribute), true).Any())).Select(x => x.Name);
        }

        #endregion

        #region - Public methods -

        /// <summary>
        /// Occurs before entity insert.
        /// </summary>
        /// <param name="entity"></param>
        public virtual void OnBeforeInsert(TEntity entity) { }

        /// <summary>
        /// Occurs after entity is inserted.
        /// </summary>
        /// <param name="entity"></param>
        public virtual void OnAfterInsert(TEntity entity) { }

        /// <summary>
        /// Occurs after entity is inserted.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="identity">Inserted identify value.</param>
        public virtual void OnAfterInsert<T>(TEntity entity, T identity) { }

        /// <summary>
        /// Occurs after entity is selected.
        /// </summary>
        /// <param name="entity"></param>
        public virtual void OnAfterSelectEntity(TEntity entity) { }

        /// <summary>
        /// Inserts entity.
        /// </summary>
        /// <param name="entity">Entity to insert.</param>
        /// <returns>True if inserted succesfully, otherwise false.</returns>
        public virtual bool Insert(TEntity entity)
        {
            List<QueryParameter> parameters = new List<QueryParameter>();

            OnBeforeInsert(entity);

            foreach (var prop in GetEntityPropertyNames(true))
            {
                var propertyValue = EntityType.GetProperty(prop).GetValue(entity, null);
                if (propertyValue == null)
                {
                    propertyValue = Convert.DBNull;
                }
                else if (propertyValue.GetType().IsEnum)
                {
                    propertyValue = (int)propertyValue;
                }

                parameters.Add(new QueryParameter(string.Format("@{0}", prop), propertyValue));
            }

            var rowsAffected = Execute(InsertQueryString, parameters);

            OnAfterInsert(entity);

            return rowsAffected > 0;
        }

        /// <summary>
        /// Inserts entity with specified output column to be used as a return value.
        /// </summary>
        /// <param name="entity">Entity to insert.</param>
        /// <param name="identityColumnName">Column name used for output value. Mostly used with auto increment id column.</param>
        /// <returns>True if inserted succesfully, otherwise false.</returns>
        public virtual T Insert<T>(TEntity entity, string identityColumnName)
        {
            List<QueryParameter> parameters = new List<QueryParameter>();

            OnBeforeInsert(entity);

            foreach (var prop in GetEntityPropertyNames(true))
            {
                var propertyValue = EntityType.GetProperty(prop).GetValue(entity, null);
                if (propertyValue == null)
                {
                    propertyValue = Convert.DBNull;
                }
                else if (propertyValue.GetType().IsEnum)
                {
                    propertyValue = (int)propertyValue;
                }

                parameters.Add(new QueryParameter(string.Format("@{0}", prop), propertyValue));
            }

            T result = Get<T>(string.Format(InsertQueryStringWithOutputColumn, identityColumnName), parameters);

            OnAfterInsert(entity, result);

            return result;
        }

        /// <summary>
        /// Gets list of all entities.
        /// </summary>
        /// <returns>Entity list.</returns>
        public virtual IEnumerable<TEntity> All()
        {
            using (var connector = new Connector())
            {
                foreach (var entity in connector.SelectEntities<TEntity>(SelectQueryString))
                {
                    OnAfterSelectEntity(entity);

                    yield return entity;
                }
            }
        }

        /// <summary>
        /// Finds entity by ID.
        /// </summary>
        /// <param name="id">Entity ID.</param>
        /// <returns>Entity instance.</returns>
        public virtual TEntity GetById(object id)
        {
            using (var connector = new Connector())
            {
                var properties = GetEntityPropertyNames();

                var columns = string.Join(",", properties.Select(property => string.Format("[{0}]", property)));

                var sql = string.Format(SELECT_BY_ID, SelectQueryString);

                var entity = connector.SelectEntity<TEntity>(sql, new[] {
                    new QueryParameter("@ID", id)
                });

                OnAfterSelectEntity(entity);

                return entity;
            }
        }

        /// <summary>
        /// Gets auto mapped instance of type <typeparamref name="TEntity"/>.
        /// </summary>
        /// <param name="query">SQL query.</param>
        /// <returns>Entity instance.</returns>
        public virtual TEntity GetEntity(string query)
        {
            return GetEntity(query, null);
        }

        /// <summary>
        /// Gets auto mapped instance of type <typeparamref name="TEntity"/>.
        /// </summary>
        /// <param name="query">SQL query.</param>
        /// <param name="args">Query arguments.</param>
        /// <returns>Entity instance.</returns>
        public virtual TEntity GetEntity(string query, IEnumerable<QueryParameter> args)
        {
            return GetEntity<TEntity>(query, args);
        }

        /// <summary>
        /// Gets auto mapped instance of specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type to instantiate from query result.</typeparam>
        /// <param name="query">SQL query.</param>
        /// <returns>Entity instance.</returns>
        public virtual T GetEntity<T>(string query) where T : class, new()
        {
            return GetEntity<T>(query, null);
        }

        /// <summary>
        /// Gets auto mapped instance of specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type to instantiate from query result.</typeparam>
        /// <param name="query">SQL query.</param>
        /// <param name="args">Query arguments.</param>
        /// <returns>Entity instance.</returns>
        public virtual T GetEntity<T>(string query, IEnumerable<QueryParameter> args) where T : class, new()
        {
            using (var connector = new Connector())
            {
                return connector.SelectEntity<T>(query, args);
            }
        }

        /// <summary>
        /// Gets auto mapped list of entities of type <typeparamref name="TEntity"/>.
        /// </summary>
        /// <param name="query">SQL query.</param>
        /// <returns>Entity instance list.</returns>
        public virtual IEnumerable<TEntity> GetEntities(string query)
        {
            return GetEntities(query, null);
        }

        /// <summary>
        /// Gets auto mapped list of entities of type <typeparamref name="TEntity"/>.
        /// </summary>
        /// <param name="query">SQL query.</param>
        /// <param name="args">Query arguments.</param>
        /// <returns>Entity instance list.</returns>
        public virtual IEnumerable<TEntity> GetEntities(string query, IEnumerable<QueryParameter> args)
        {
            return GetEntities<TEntity>(query, args);
        }

        /// <summary>
        /// Gets auto mapped list of entities of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="query">SQL query.</param>
        /// <returns>Entity instance list.</returns>
        public virtual IEnumerable<T> GetEntities<T>(string query) where T : class, new()
        {
            return GetEntities<T>(query, null);
        }

        /// <summary>
        /// Gets auto mapped list of entities of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="query">SQL query.</param>
        /// <param name="args">Query arguments.</param>
        /// <returns>Entity instance list.</returns>
        public virtual IEnumerable<T> GetEntities<T>(string query, IEnumerable<QueryParameter> args) where T : class, new()
        {
            using (var connector = new Connector())
            {
                return connector.SelectEntities<T>(query, args);
            }
        }

        /// <summary>
        /// Deletes entity.
        /// </summary>
        /// <param name="id">Entity id.</param>
        public virtual void Delete(object id)
        {
            var sql = string.Format(DELETE, TableName);

            OnBeforeDelete(id);

            this.Execute(sql, QueryParameter.Single("@ID", id));

            OnAfterDelete(id);
        }

        /// <summary>
        /// Occurs before entity is deleted.
        /// </summary>
        /// <param name="entity"></param>
        public virtual void OnBeforeDelete(object id) { }

        /// <summary>
        /// Occurs after entity is deleted.
        /// </summary>
        /// <param name="entity"></param>
        public virtual void OnAfterDelete(object id) { }

        #endregion
    }
}
