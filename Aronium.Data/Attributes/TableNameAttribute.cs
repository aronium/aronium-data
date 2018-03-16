using System;

namespace Aronium.Data
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TableNameAttribute : Attribute
    {
        public TableNameAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
    }
}
