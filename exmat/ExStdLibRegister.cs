using System;

namespace ExMat
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ExStdLibRegister : Attribute
    {
        public string RegisterMethodName;

        public ExStdLibRegister(string name)
        {
            RegisterMethodName = name;
        }
    }
}
