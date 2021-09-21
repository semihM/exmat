using System;

namespace ExMat.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    internal sealed class ExConsoleHelper : Attribute
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
