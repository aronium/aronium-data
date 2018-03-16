using System;
using System.Collections.Generic;
using System.Text;

namespace Aronium.Data
{
    /// <summary>
    /// Class representing sql query parameter.
    /// </summary>
    public class QueryParameter
    {
        /// <summary>
        /// Creates new instance of QueryParameter class.
        /// </summary>
        public QueryParameter()
        {

        }

        /// <summary>
        /// Creates new instance of QueryParameter class.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value.</param>
        public QueryParameter(string name, object value)
            : this()
        {
            this.Name = name;
            this.Value = value;
        }

        /// <summary>
        /// Creates new instance of QueryParameter class.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value.</param>
        /// <param name="isOutput">Sets a value indicating if parameter is Output.</param>
        public QueryParameter(string name, object value, bool isOutput)
            : this(name, value)
        {
            this.IsOutput = isOutput;
        }

        /// <summary>
        /// Creates new instance of QueryParameter class.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value.</param>
        /// <param name="isOutput">Sets a value indicating if parameter is Output.</param>
        /// <param name="IsImage">Sets a value indicating if parameter should be treated as SqlDbType.Image.</param>
        public QueryParameter(string name, object value, bool isOutput, bool IsImage)
            : this(name, value, isOutput)
        {
            this.IsImage = IsImage;
        }

        /// <summary>
        /// Gets or sets parameter name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets parameter value.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter expects output value.
        /// </summary>
        public bool IsOutput { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether parameter type should be treated as SqlDbType.Image.
        /// </summary>
        public bool IsImage { get; set; }

        /// <summary>
        /// Gets query parameter array with single parameter value.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value.</param>
        /// <returns>Query parameter array containing instance with specified name and value.</returns>
        public static IEnumerable<QueryParameter> Single(string name, object value)
        {
            return new[] { new QueryParameter(name, value) };
        }

        /// <summary>
        /// String representation of QueryParameter instance.
        /// </summary>
        /// <returns>Parameter name and value.</returns>
        public override string ToString()
        {
            return string.Format("{0}: {1}", Name, Value);
        }
    }
}
