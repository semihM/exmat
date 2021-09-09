using System;

namespace ExMat
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ExStdLibName : Attribute
    {
        public string Name;

        public ExStdLibName(string name)
        {
            Name = name;
        }
    }
}
