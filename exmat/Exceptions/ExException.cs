﻿using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.Serialization;
using ExMat.VM;

namespace ExMat.Exceptions
{
    public enum ExExceptionType
    {
        BASE,

        COMPILER,

        RUNTIME
    }

    /// <summary>
    /// Base class for exceptions
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExException : Exception
    {
        public virtual ExExceptionType Type => ExExceptionType.BASE;

        public ExException()
        {
        }

        public ExException(string message)
            : base(message)
        {
        }

        public ExException(ExVM vm, string message)
            : base(message)
        {
        }

        public ExException(ExVM vm, string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ExException(ExVM vm, SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        protected ExException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public ExException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public static void WriteErrorMessagesToVM(ExVM vm, string message)
        {
        }

        public override IDictionary Data => base.Data;

        public override string HelpLink { get => base.HelpLink; set => base.HelpLink = value; }

        public override string Message => base.Message;

        public override string Source { get => base.Source; set => base.Source = value; }

        public override string StackTrace => base.StackTrace;

        public override Exception GetBaseException()
        {
            return base.GetBaseException();
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public string GetDebuggerDisplay()
        {
            return "Exception: " + Type.ToString();
        }
    }
}
