using System;

namespace ExMat.Objects
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class ExConsoleHelper : Attribute
    {
        public string Source = string.Empty;
        public string Help = string.Empty;

        public ExConsoleHelper(string name, string info)
        {
            Source = name;
            Help = info;
        }
    }
}
