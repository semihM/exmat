using System;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using System.Globalization;
using System.Linq;
using System.Text;
using ExMat.Attributes;

namespace ExMat.Objects
{
    /// <summary>
    /// Native function registery class
    /// </summary>
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    public sealed class ExNativeFunc : ExNativeFuncBase, IDisposable
    {
        /// <summary>
        /// Library of which this function is a part of 
        /// </summary>
        public ExStdLibType Base = ExStdLibType.UNKNOWN;

        /// <summary>
        /// Function reference
        /// </summary>
        public ExMat.StdLibFunction Function;    // Fonksiyon referansı

        /// <summary>
        /// Parameter information
        /// </summary>
        internal List<ExNativeParam> Parameters;

        /// <summary>
        /// Combined parameter type mask string
        /// </summary>
        public string ParameterMask;

        private void SetNumberOfParameterCombinedMask()
        {
            if (Parameters == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(ParameterMask))
            {
                StringBuilder pmask = new(BaseTypeMask.ToString(CultureInfo.CurrentCulture));
                foreach (ExNativeParam p in Parameters)
                {
                    pmask.Append(p.TypeMask);
                }
                ParameterMask = pmask.ToString();
            }
        }

        private void SetNumberOfParameters()
        {
            if (NumberOfParameters == int.MaxValue)
            {
                if (Parameters == null)
                {
                    NumberOfParameters = BaseTypeMask == '.' ? -1 : 1;
                }
                else
                {
                    NumberOfParameters = 1;
                    foreach (ExNativeParam fp in Parameters)
                    {
                        if (fp.HasDefaultValue)
                        {
                            NumberOfParameters *= -1;
                            break;
                        }
                        NumberOfParameters++;
                    }
                }
            }
        }

        /// <summary>
        /// Information string about return type
        /// </summary>
        public string ReturnsType;

        private void SetReturnInfo()
        {
            ReturnsType = API.ExApi.GetTypeMaskInfo((int)Returns);
        }

        private bool disposedValue;

        private void InitializeFromNativeBase(ExNativeFuncBase ex)
        {
            Name = ex.Name;
            NumberOfParameters = ex.NumberOfParameters;
            Description = ex.Description;
            Returns = ex.Returns;
            BaseTypeMask = ex.BaseTypeMask;
            IsDelegateFunction = ex.IsDelegateFunction;
        }

        /// <summary>
        /// Empty constructor
        /// </summary>
        public ExNativeFunc() { }

        /// <summary>
        /// Construct a native function from a standard library method
        /// </summary>
        /// <param name="foo">Standard library method</param>
        public ExNativeFunc(ExMat.StdLibFunction foo)
        {
            Function = foo;

            Parameters = new();

            foreach (Attribute attr in GetCustomAttributes(foo.Method))
            {
                if (attr is ExNativeParamBase exNativeParam)
                {
                    Parameters.Add(new(exNativeParam));
                }
                else if (attr is ExNativeFuncBase exNative)
                {
                    InitializeFromNativeBase(exNative);
                }
            }

            Parameters = NumberOfParameters != int.MaxValue ? null : (new(Parameters.OrderBy(x => x.Index)));

            SetNumberOfParameters();
            SetNumberOfParameterCombinedMask();
            SetReturnInfo();
        }

        /// <summary>
        /// Construct a delegate native function from a standard library method
        /// </summary>
        /// <param name="foo">Standard library method</param>
        /// <param name="baseMask">Delegate base</param>
        public ExNativeFunc(ExMat.StdLibFunction foo, char baseMask)
        {
            Function = foo;

            Parameters = new();

            foreach (Attribute attr in GetCustomAttributes(foo.Method))
            {
                if (attr is ExNativeParamBase exNativeParam)
                {
                    Parameters.Add(new(exNativeParam));
                }
                else if (attr is ExNativeFuncDelegate exNative && exNative.BaseTypeMask == baseMask)
                {
                    InitializeFromNativeBase(exNative);
                }
            }

            Parameters = NumberOfParameters != int.MaxValue ? null : (new(Parameters.OrderBy(x => x.Index)));

            SetNumberOfParameters();
            SetNumberOfParameterCombinedMask();
            SetReturnInfo();
        }

        internal void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Name = null;
                    Function = null;
                    Description = null;

                    ExDisposer.DisposeList(ref Parameters);

                    ReturnsType = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        /// <summary>
        /// Disposer
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

#if DEBUG
        private object GetDebuggerDisplay()
        {
            return string.Format(CultureInfo.CurrentCulture, "RegFunc {0}", Name);
        }
#endif
    }
}
