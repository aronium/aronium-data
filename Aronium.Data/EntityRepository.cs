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

        private bool _disposed;
        private Type _type;
        private string _selectQuery;

        private static readonly string INSERT = "INSERT INTO [{0}] ({1}) VALUES ({2})";
        private static readonly string SELECT = "SELECT {0} FROM [{1}]";
        private static readonly string DELETE = "DELETE FROM [{0}] WHERE ID=@ID";
        private static readonly string SELECT_BY_ID = "SELECT {0} FROM [{1}] WHERE ID=@ID";

        #endregion

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
        /// Gets select query for entity type.
        /// </summary>
        protected string SelectQueryString
        {
            get
            {
                if (_selectQuery == null)
                {
                    var properties = GetEntityPropertyNames();

                    var columns = string.Join(",", properties.Select(property => string.Format("[{0}]", property)));

                    _selectQuery = string.Format(SELECT, columns, EntityType.Name);
                }

                return _selectQuery;
            }
        }

        #endregion

        #region - Private methods -

        /// <summary>
        /// Gets list of property names.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> GetEntityPropertyNames()
        {
            return EntityType.GetProperties().Where(x => x.CanWrite && !x.GetSetMethod().IsVirtual).Select(x => x.Name);
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
            var properties = GetEntityPropertyNames();

            var columns = string.Join(",", properties.Select(property => string.Format("[{0}]", property)));
            var arguments = string.Join(",", properties.Select(property => string.Format("@{0}", property)));

            var sql = string.Format(INSERT, EntityType.Name, columns, arguments);

            List<QueryParameter> parameters = new List<QueryParameter>();

            OnBeforeInsert(entity);

            foreach (var prop in properties)
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

            var rowsAffected = Execute(sql, parameters);

            OnAfterInsert(entity);

            return rowsAffected > 0;
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

                var sql = string.Format(SELECT_BY_ID, columns, EntityType.Name);

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
            var sql = string.Format(DELETE, EntityType.Name);

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
