namespace Aronium.Data
{
    public class ImageQueryParameter : QueryParameter
    {
        /// <summary>
        /// Initalizes new instance of ImageQueryParameter class.
        /// </summary>
        public ImageQueryParameter()
        {
        }

        /// <summary>
        /// Initalizes new instance of ImageQueryParameter class.
        /// </summary>
        /// <param name="name">Parameter name</param>
        /// <param name="value">Parameter value</param>
        public ImageQueryParameter(string name, byte[] value) : base(name, value)
        {
        }
    }
}
