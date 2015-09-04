using System;
using System.Collections.Generic;

namespace Aronium.Data
{
    public abstract class Repository : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Dispose object.
        /// </summary>
        /// <param name="disposing">Value indicating whether disposal is in progress.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose objects...
                }
            }

            _disposed = true;
        }

        /// <summary>
        /// Gets instance of specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type to instantiate.</typeparam>
        /// <param name="query">SQL query.</param>
        /// <returns>Instance of <typeparamref name="T"/></returns>
        protected T Get<T>(string query)
        {
            return Get<T>(query, null, null);
        }

        /// <summary>
        /// Gets instance of specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type to instantiate.</typeparam>
        /// <param name="query">SQL query.</param>
        /// <param name="args">SQL query arguments.</param>
        /// <returns>Instance of <typeparamref name="T"/></returns>
        protected T Get<T>(string query, IEnumerable<QueryParameter> args)
        {
            return Get<T>(query, args, null);
        }

        /// <summary>
        /// Gets instance of specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type to instantiate.</typeparam>
        /// <param name="query">SQL query.</param>
        /// <param name="rowMapper">Row mapper instance used to instantiate specified type.</param>
        /// <returns>Instance of <typeparamref name="T"/></returns>
        protected T Get<T>(string query, IRowMapper<T> rowMapper)
        {
            return Get(query, null, rowMapper);
        }

        /// <summary>
        /// Gets instance of specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type to instantiate.</typeparam>
        /// <param name="query">SQL query.</param>
        /// <param name="args">SQL query arguments.</param>
        /// <param name="rowMapper">Row mapper instance used to instantiate specified type.</param>
        /// <returns>Instance of <typeparamref name="T"/></returns>
        protected T Get<T>(string query, IEnumerable<QueryParameter> args, IRowMapper<T> rowMapper)
        {
            using (var connector = new Connector())
            {
                return connector.SelectValue(query, args, rowMapper);
            }
        }

        /// <summary>
        /// Gets list of specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type to instantiate.</typeparam>
        /// <param name="query">SQL query.</param>
        /// <returns>List of instances of type <typeparamref name="T"/>.</returns>
        protected IEnumerable<T> GetList<T>(string query)
        {
            return GetList<T>(query, null, null);
        }

        /// <summary>
        /// Gets list of specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type to instantiate.</typeparam>
        /// <param name="query">SQL query.</param>
        /// <param name="rowMapper">Row mapper instance used to instantiate specified type.</param>
        /// <returns>List of instances of type <typeparamref name="T"/>.</returns>
        protected IEnumerable<T> GetList<T>(string query, IRowMapper<T> rowMapper)
        {
            return GetList<T>(query, null, rowMapper);
        }

        /// <summary>
        /// Gets list of specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type to instantiate.</typeparam>
        /// <param name="query">SQL query.</param>
        /// <param name="args">SQL query arguments.</param>
        /// <returns>List of instances of type <typeparamref name="T"/>.</returns>
        protected IEnumerable<T> GetList<T>(string query, IEnumerable<QueryParameter> args)
        {
            return GetList<T>(query, args, null);
        }

        /// <summary>
        /// Gets list of specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type to instantiate.</typeparam>
        /// <param name="query">SQL query.</param>
        /// <param name="args">SQL query arguments.</param>
        /// <param name="rowMapper">Row mapper instance used to instantiate specified type.</param>
        /// <returns>List of instances of type <typeparamref name="T"/>.</returns>
        protected IEnumerable<T> GetList<T>(string query, IEnumerable<QueryParameter> args, IRowMapper<T> rowMapper)
        {
            using (var connector = new Connector())
            {
                return connector.Select(query, args, rowMapper);
            }
        }

        /// <summary>
        /// Executes specified SQL query and returns number of rows affected.
        /// </summary>
        /// <param name="query">SQL query.</param>
        /// <param name="args">SQL query arguments.</param>
        /// <returns>Number of rows affected by the query execution.</returns>
        protected int Execute(string query, IEnumerable<QueryParameter> args)
        {
            return Execute(query, args, false);
        }

        /// <summary>
        /// Executes specified SQL query and returns number of rows affected.
        /// </summary>
        /// <param name="query">SQL query.</param>
        /// <param name="args">SQL query arguments.</param>
        /// <param name="isStoredProcedure">Value indicating whether query is command or stored procedure.</param>
        /// <returns>Number of rows affected by the query execution.</returns>
        protected int Execute(string query, IEnumerable<QueryParameter> args, bool isStoredProcedure)
        {
            using (var connector = new Connector())
            {
                return connector.Execute(query, args, isStoredProcedure);
            }
        }

        /// <summary>
        /// Dispose object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
