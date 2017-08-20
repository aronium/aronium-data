using System;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Aronium.Data.Exceptions;
using System.Collections;
using System.Web.Configuration;

namespace Aronium.Data
{
    public class Connector : IDisposable
    {
        #region - Fields -

        private static string _connectionString;
        private static int _connectionTimeout = 30;
        private static string _server;
        private static string _database;
        private static string _username;
        private static string _password;
        private static string _connectionStringName;

        #endregion

        #region - Properties -

        /// <summary>
        /// Gets or sets server name.
        /// </summary>
        public string Server
        {
            get { return _server; }
            set { _server = value; }
        }

        /// <summary>
        /// Gets or sets database name.
        /// </summary>
        public string Database
        {
            get { return _database; }
            set { _database = value; }
        }

        /// <summary>
        /// Gets or sets database user username
        /// </summary>
        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }

        /// <summary>
        /// Gets or sets database user password.
        /// </summary>
        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        /// <summary>
        /// Gets or sets default connection timeout.
        /// </summary>
        public static int ConnectionTimeout
        {
            get { return _connectionTimeout; }
            set { _connectionTimeout = value; }
        }

        /// <summary>
        /// Gets or sets connection string.
        /// </summary>
        public static string ConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    string appName = null;
                    if (System.Reflection.Assembly.GetEntryAssembly() != null)
                        appName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;

                    _connectionString = CreateConnectionString(appName);
                }

                return _connectionString;
            }
            set
            {
                _connectionString = value;
            }
        }

        /// <summary>
        /// Gets or sets connection string name used for reading connection string from configuration or web.config file.
        /// </summary>
        public static string ConnectionStringName
        {
            get
            {
                return _connectionStringName;
            }
            set
            {
                if (value != null)
                    ConnectionString = WebConfigurationManager.ConnectionStrings[value].ConnectionString;

                _connectionStringName = value;
            }
        }

        #endregion

        #region - Private methods -

        private static string CreateConnectionString(string appName)
        {
            SqlConnectionStringBuilder cb = new SqlConnectionStringBuilder();
            cb.DataSource = _server;
            cb.InitialCatalog = _database;
            cb.MultipleActiveResultSets = true;
            cb.UserID = _username;
            cb.Password = _password;
            cb.Pooling = true;

            if (ConnectionTimeout > 0)
            {
                cb.ConnectTimeout = ConnectionTimeout;
            }

            if (!string.IsNullOrEmpty(appName)) //<-- Value cannot be null
                cb.ApplicationName = appName;

            return cb.ToString();
        }

        private void PrepareCommandParameters(SqlCommand command, IEnumerable<QueryParameter> args)
        {
            if (args != null && args.Any())
            {
                foreach (QueryParameter parameter in args)
                {
                    if (parameter.Value != null && !(parameter.Value is string) && typeof(IEnumerable).IsAssignableFrom(parameter.Value.GetType()))
                    {
                        var parameterName = parameter.Name.Replace("@", string.Empty);

                        string replacement = string.Join(",", ((IEnumerable)parameter.Value).Cast<object>().Select((value, pos) => string.Format("@{0}__{1}", parameterName, pos)));

                        // Replace original command text with parametrized query
                        command.CommandText = command.CommandText.Replace(string.Format("@{0}", parameterName), replacement);

                        command.Parameters.AddRange(((IEnumerable)parameter.Value).Cast<object>().Select((value, pos) => new SqlParameter(string.Format("@{0}__{1}", parameterName, pos), value ?? DBNull.Value)).ToArray());
                    }
                    else
                    {
                        command.Parameters.Add(new SqlParameter(parameter.Name, parameter.Value ?? DBNull.Value)
                        {
                            Direction = parameter.IsOutput ? ParameterDirection.Output : ParameterDirection.Input
                        });
                    }
                }
            }
        }

        private void CollectOutputValues(SqlCommand command, IEnumerable<QueryParameter> queryParameters)
        {
            if (queryParameters != null)
            {
                foreach (QueryParameter arg in queryParameters.Where(x => x.IsOutput))
                {
                    arg.Value = command.Parameters[arg.Name].Value;
                }
            }
        }

        #endregion

        #region - Public methods -

        #region " Connect "

        /// <summary>
        /// Connect to specified database server instance.
        /// </summary>
        /// <param name="server">Database server.</param>
        /// <param name="database">Database name.</param>
        /// <param name="username">Username.</param>
        /// <param name="password">Password.</param>
        public void Connect(string server, string database, string username, string password)
        {
            this.Connect(server, database, username, password, 0);
        }

        /// <summary>
        /// Connect to specified database server instance.
        /// </summary>
        /// <param name="server">Database server.</param>
        /// <param name="database">Database name.</param>
        /// <param name="username">Username.</param>
        /// <param name="password">Password.</param>
        /// <param name="password">Password.</param>
        /// <param name="connectionTimeout">Sets default connection timeout.</param>
        public void Connect(string server, string database, string username, string password, int connectionTimeout)
        {
            this.Server = server;
            this.Database = database;
            this.Username = username;
            this.Password = password;
            ConnectionTimeout = connectionTimeout;

            // When connect is executed, make sure connection string is cleared and reset
            ConnectionString = null;

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();

                    connection.Close();
                }
                catch (SqlException ex)
                {
                    throw ex;
                }
                catch
                {
                    throw;
                }
            }
        }

        #endregion

        /// <summary>
        /// Execute reader and create list of provided type using IRowMapper interface.
        /// </summary>
        /// <typeparam name="T">Type of object to create.</typeparam>
        /// <param name="query">Sql Query.</param>
        /// <param name="rowMapper">IRowMapper used to map object instance from reader.</param>
        /// <param name="isStoredProcedure">indicating if query type is stored procedure.</param>
        /// <returns>List of provided object type.</returns>
        public IEnumerable<T> Select<T>(string query, IRowMapper<T> rowMapper)
        {
            return Select<T>(query, null, rowMapper);
        }

        /// <summary>
        /// Execute reader and create list of provided type using IRowMapper interface.
        /// </summary>
        /// <typeparam name="T">Type of object to create.</typeparam>
        /// <param name="query">Sql Query.</param>
        /// <param name="args">Sql Parameters.</param>
        /// <param name="rowMapper">IRowMapper used to map object instance from reader.</param>
        /// <param name="isStoredProcedure">indicating if query type is stored procedure.</param>
        /// <returns>List of provided object type.</returns>
        public IEnumerable<T> Select<T>(string query, IEnumerable<QueryParameter> args = null, IRowMapper<T> rowMapper = null)
        {
            using (SqlConnection Connection = new SqlConnection(ConnectionString))
            {
                Connection.Open();

                using (SqlCommand command = Connection.CreateCommand())
                {
                    command.CommandText = query;

                    PrepareCommandParameters(command, args);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        var isNullable = Nullable.GetUnderlyingType(typeof(T)) != null;

                        while (reader.Read())
                        {
                            if (rowMapper != null)
                            {
                                yield return rowMapper.Map(reader);
                            }
                            else
                            {
                                // Check for null values and return default instance of T (should be nullable)
                                // If not checked for NULL values, conversion will fail, resulting in InvalidCastException being thrown
                                if (isNullable && reader[0] == Convert.DBNull)
                                {
                                    yield return default(T);
                                }
                                else
                                    yield return (T)reader[0];
                            }
                        }

                        reader.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Execute reader and create list of provided type using IRowMapper interface.
        /// </summary>
        /// <typeparam name="T">Type of object to create.</typeparam>
        /// <param name="query">Sql Query.</param>
        /// <param name="extractor">IDataExtractor used to map object instance from reader.</param>
        /// <param name="isStoredProcedure">indicating if query type is stored procedure.</param>
        /// <returns>List of provided object type.</returns>
        public IEnumerable<T> Select<T>(string query, IDataExtractor<T> extractor)
        {
            return Select<T>(query, null, extractor);
        }

        /// <summary>
        /// Execute reader and create list of provided type using IRowMapper interface.
        /// </summary>
        /// <typeparam name="T">Type of object to create.</typeparam>
        /// <param name="query">Sql Query.</param>
        /// <param name="args">Sql Parameters.</param>
        /// <param name="extractor">IDataExtractor used to map object instance from reader.</param>
        /// <param name="isStoredProcedure">indicating if query type is stored procedure.</param>
        /// <returns>List of provided object type.</returns>
        public IEnumerable<T> Select<T>(string query, IEnumerable<QueryParameter> args, IDataExtractor<T> extractor)
        {
            using (SqlConnection Connection = new SqlConnection(ConnectionString))
            {
                Connection.Open();

                using (SqlCommand command = Connection.CreateCommand())
                {
                    command.CommandText = query;

                    PrepareCommandParameters(command, args);

                    IEnumerable<T> result;

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        result = extractor.Extract(reader);

                        reader.Close();
                    }

                    return result;
                }
            }
        }

        /// <summary>
        /// Execute reader and create instance of provided type using IRowMapper interface.
        /// </summary>
        /// <typeparam name="T">Type of object to create.</typeparam>
        /// <param name="args">Sql Parameters.</param>
        /// <param name="query">Sql Query.</param>
        /// <param name="rowMapper">IRowMapper used to map object instance from reader.</param>
        /// <param name="isStoredProcedure">Indicating whether query type is stored procedure.</param>
        /// <returns>Instance of object type.</returns>
        public T SelectValue<T>(string query, IEnumerable<QueryParameter> args = null, IRowMapper<T> rowMapper = null)
        {
            object obj = null;

            using (SqlConnection Connection = new SqlConnection(ConnectionString))
            {
                Connection.Open();

                using (SqlCommand command = Connection.CreateCommand())
                {
                    command.CommandText = query;

                    PrepareCommandParameters(command, args);

                    using (SqlDataReader reader = command.ExecuteReader(CommandBehavior.SingleResult))
                    {
                        reader.Read();

                        if (reader.HasRows)
                        {
                            if (rowMapper != null)
                            {
                                obj = rowMapper.Map(reader);
                            }
                            else
                            {
                                // Used for primitive types
                                obj = reader[0];
                            }
                        }

                        reader.Close();
                    }
                }
            }

            if (obj == null)
                return default(T);

            return (T)obj;
        }

        /// <summary>
        /// Gets entity instance.
        /// </summary>
        /// <typeparam name="T">Type of object to create</typeparam>
        /// <param name="query">Sql Query</param>
        /// <param name="args">Sql Parameters</param>
        /// <returns>Entity instance.</returns>
        /// <remarks>Instance properties are populated from database record using reflection for the given type.</remarks>
        public T SelectEntity<T>(string query, IEnumerable<QueryParameter> args) where T : class, new()
        {
            T entity = null;

            using (SqlConnection Connection = new SqlConnection(ConnectionString))
            {
                Connection.Open();

                using (SqlCommand command = Connection.CreateCommand())
                {
                    command.CommandText = query;

                    PrepareCommandParameters(command, args);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        reader.Read();

                        if (reader.HasRows)
                        {
                            entity = new T();
                            var type = typeof(T);

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var val = reader[i];

                                var property = type.GetProperty(reader.GetName(i));

                                if (property != null)
                                {
                                    property.SetValue(entity, val == Convert.DBNull ? null : val, null);
                                }
                            }
                        }

                        reader.Close();
                    }
                }
            }

            return entity;
        }

        /// <summary>
        /// Gets list of entities.
        /// </summary>
        /// <typeparam name="T">Type of object to create</typeparam>
        /// <param name="query">Sql Query</param>
        /// <param name="args">Sql Parameters</param>
        /// <returns>List of entities.</returns>
        /// <remarks>Instance properties are populated from database record using reflection for the given type.</remarks>
        public IEnumerable<T> SelectEntities<T>(string query, IEnumerable<QueryParameter> args = null) where T : class, new()
        {
            using (SqlConnection Connection = new SqlConnection(ConnectionString))
            {
                var type = typeof(T);

                Connection.Open();

                using (SqlCommand command = Connection.CreateCommand())
                {
                    command.CommandText = query;

                    PrepareCommandParameters(command, args);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            T entity = new T();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var val = reader[i];

                                var property = type.GetProperty(reader.GetName(i));

                                if (property != null)
                                {
                                    property.SetValue(entity, val == Convert.DBNull ? null : val, null);
                                }
                            }

                            yield return entity;
                        }

                        reader.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Executes sql query and returns number of affected rows.
        /// </summary>
        /// <param name="query">Sql Query to execute</param>
        /// <param name="args">Sql query parameters to use.</param>
        /// <returns>Number of rows affected by command.</returns>
        public int Execute(string query, IEnumerable<QueryParameter> args = null, bool isStoredProcedure = false)
        {
            int affectedRows = 0;

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();

                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = query;

                        if (isStoredProcedure)
                        {
                            command.CommandType = CommandType.StoredProcedure;
                        }

                        PrepareCommandParameters(command, args);

                        affectedRows = command.ExecuteNonQuery();

                        CollectOutputValues(command, args);
                    }
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 547 || ex.Number == 2627 || ex.Number == 2601)
                    {
                        throw new DataConstraintException(ex);
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            return affectedRows;
        }

        #endregion

        #region - IDisposable implementation -

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose this instance and unmanaged resources, if any.
        /// </summary>
        /// <param name="disposing">Value indicating whether dispose is executed by user code, not the system.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {

            }
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~Connector()
        {
            Dispose(false);
        }

        #endregion
    }
}
