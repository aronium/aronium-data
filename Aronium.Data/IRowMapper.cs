using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Aronium.Data
{
    /// <summary>
    /// Map method maps data reader to new instance of type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRowMapper<T>
    {
        /// <summary>
        /// Maps data record to entity type.s
        /// </summary>
        /// <param name="record">Data record.</param>
        /// <returns>Instance of T.</returns>
        T Map(IDataRecord record);
    }
}
