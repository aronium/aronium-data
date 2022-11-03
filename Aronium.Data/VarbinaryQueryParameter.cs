namespace Aronium.Data
{
    public class VarbinaryQueryParameter : QueryParameter
    {
        /// <summary>
        /// Initalizes new instance of VarbinaryQueryParameter class.
        /// </summary>
        public VarbinaryQueryParameter()
        {
        }

        /// <summary>
        /// Initalizes new instance of VarbinaryQueryParameter class.
        /// </summary>
        /// <param name="name">Parameter name</param>
        /// <param name="value">Parameter value</param>
        public VarbinaryQueryParameter(string name, byte[] value) : base(name, value)
        {
        }
    }
}
