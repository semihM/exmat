using System;

namespace Exmat.Exceptions
{
    public class ExException : Exception
    {
        public ExException()
        {
        }

        public ExException(string message)
            : base(message)
        {
        }

        public ExException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
