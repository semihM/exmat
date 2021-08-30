using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.Serialization;
using ExMat.API;
using ExMat.VM;

namespace ExMat.Exceptions
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExException : Exception
    {
        public ExException()
        {
        }

        public ExException(string message)
            : base(message)
        {
            Console.WriteLine(string.Format("\n\nFATAL ERROR: {0}\n\n", message));
        }

        public ExException(ExVM vm, string message)
            : base(message)
        {
            WriteErrorMessagesToVM(vm, message);
        }

        public ExException(ExVM vm, string message, Exception innerException) : base(message, innerException)
        {
            WriteErrorMessagesToVM(vm, string.Format("Inner Exception{0}: {1}\nMessage: {2}", innerException.GetType().Name, innerException.Message, message));
        }

        protected ExException(ExVM vm, SerializationInfo info, StreamingContext context) : base(info, context)
        {
            WriteErrorMessagesToVM(vm, "");
        }

        protected ExException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public ExException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public static void WriteErrorMessagesToVM(ExVM vm, string message)
        {
            vm.AddToErrorMessage(message);
            ExApi.WriteErrorMessages(vm, ExErrorType.INTERNAL);
        }

        public override IDictionary Data => base.Data;

        public override string HelpLink { get => base.HelpLink; set => base.HelpLink = value; }

        public override string Message => base.Message;

        public override string Source { get => base.Source; set => base.Source = value; }

        public override string StackTrace => base.StackTrace;

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override Exception GetBaseException()
        {
            return base.GetBaseException();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        private string GetDebuggerDisplay()
        {
            return ToString();
        }
    }
}
