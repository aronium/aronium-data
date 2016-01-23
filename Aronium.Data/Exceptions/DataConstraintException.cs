using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace Aronium.Data.Exceptions
{
    /// <summary>
    /// Indicates violation kind.
    /// </summary>
    public enum DataConstraindErrorKind
    {
        /// <summary>
        /// Indicates foreign key constraing violation.
        /// </summary>
        Constraint,
        /// <summary>
        /// Indicates existing primary key violation.
        /// </summary>
        DuplicateKey
    }

    /// <summary>
    /// Class representing database key violation exception.
    /// </summary>
    public class DataConstraintException : Exception
    {
        // Constraint and duplicate key exception
        // Refer to: http://msdn.microsoft.com/en-us/library/cc645603.aspx

        /// <summary>
        /// Creates new instance of DataConstraintException class
        /// </summary>
        /// <param name="ex"></param>
        public DataConstraintException(SqlException ex) : base(ex.Message, ex)
        {
            if (ex.Number == 547)
            {
                this.ParseTableName(ex.Message);
                this.Kind = DataConstraindErrorKind.Constraint;
            }
            else if (ex.Number == 2627 || ex.Number == 2601)
            {
                this.Kind = DataConstraindErrorKind.DuplicateKey;
            }
        }

        private void ParseTableName(string message)
        {
            // Extract table name from exception message
            message = message.Substring(message.IndexOf("table \"") + 7);
            message = message.Substring(0, message.IndexOf("\", column")).Replace("dbo.", "");

            ReferencedTable = message;
        }

        /// <summary>
        /// Gets exception kind.
        /// </summary>
        public DataConstraindErrorKind Kind { get; private set; }

        /// <summary>
        /// Gets violated table name.
        /// </summary>
        public string ReferencedTable { get; private set; }
    }
}
