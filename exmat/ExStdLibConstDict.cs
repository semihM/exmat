using System;

namespace ExMat
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ExStdLibConstDict : Attribute
    {
        public string Name = string.Empty;

        public ExStdLibConstDict()
        {

        }

        public ExStdLibConstDict(string name)
        {
            Name = name;
        }
    }
}
