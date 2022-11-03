namespace Aronium.Data
{
    public class XmlQueryParameter : QueryParameter
    {
        /// <summary>
        /// Initalizes new instance of XmlQueryParameter class.
        /// </summary>
        public XmlQueryParameter()
        {
        }

        /// <summary>
        /// Initalizes new instance of XmlQueryParameter class.
        /// </summary>
        /// <param name="name">Parameter name</param>
        /// <param name="value">Parameter value</param>
        public XmlQueryParameter(string name, string value) : base(name, value)
        {
        }
    }
}
