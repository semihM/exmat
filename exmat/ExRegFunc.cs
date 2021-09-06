using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ExMat.VM;

namespace ExMat.Objects
{
    /// <summary>
    /// Native function registery class
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExRegFunc : IDisposable
    {
        /// <summary>
        /// Library of which this function is a part of 
        /// </summary>
        public ExStdLibType Base;

        /// <summary>
        /// Function name
        /// </summary>
        public string Name;             // Fonksiyon ismi

        /// <summary>
        /// Delegate, a native function template
        /// </summary>
        /// <param name="vm">VM to use the stack of</param>
        /// <param name="nargs">Number of arguments passed to the function</param>
        /// <returns>If a value was pushed to stack: <see cref="ExFunctionStatus.SUCCESS"/>
        /// <para>If nothing was pushed to stack: <see cref="ExFunctionStatus.VOID"/></para>
        /// <para>If there was an error: <see cref="ExFunctionStatus.ERROR"/></para>
        /// <para>In the special case of 'exit': <see cref="ExFunctionStatus.EXIT"/></para></returns>
        public delegate ExFunctionStatus FunctionRef(ExVM vm, int nargs);
        /// <summary>
        /// Function reference
        /// </summary>
        public FunctionRef Function;    // Fonksiyon referansı

        private List<ExFuncParameter> _Parameters;
        /// <summary>
        /// Parameter information
        /// </summary>
        public List<ExFuncParameter> Parameters
        {
            get => _Parameters;
            set
            {
                if (value != null)
                {
                    for (int i = 0; i < value.Count; i++)
                    {
                        value[i].Index = i + 1;
                    }

                    _Parameters = value;
                }
            }
        }

        /// <summary>
        /// Base object type mask. Non-delegate functions should have <c>'.'</c> as the base. Delegate base type should be updated.
        /// </summary>
        public char BaseTypeMask = '.';

        private string _parameterMask;
        /// <summary>
        /// Combined parameter type mask string
        /// </summary>
        public string ParameterMask // Parameter tipleri maskesi
        {
            get
            {
                if (Parameters == null)
                {
                    return null;
                }

                if (string.IsNullOrEmpty(_parameterMask))
                {
                    StringBuilder pmask = new(BaseTypeMask.ToString());
                    foreach (ExFuncParameter p in Parameters)
                    {
                        pmask.Append(p.TypeMask);
                    }
                    _parameterMask = pmask.ToString();
                }

                return _parameterMask;
            }
        }

        private int _nParameterChecks = int.MaxValue;
        /// <summary>
        /// Argument requirement information
        /// <para>Positive 'n': n - 1 parameters == n - 1 arguments</para>
        /// <para>Negative 'n': -n parameters == -n - 1 arguments minimum</para>
        /// <para>Setter should only be used for vargs functions with parameter definitions</para>
        /// </summary>
        public int NumberOfParameters    // Argüman sayısı kontrolü
        {
            get
            {
                if (_nParameterChecks == int.MaxValue)
                {
                    if (Parameters == null)
                    {
                        _nParameterChecks = BaseTypeMask == '.' ? -1 : 1;
                    }
                    else
                    {
                        _nParameterChecks = 1;
                        foreach (ExFuncParameter fp in Parameters)
                        {
                            if (fp.HasDefaultValue)
                            {
                                _nParameterChecks *= -1;
                                break;
                            }
                            _nParameterChecks++;
                        }
                    }
                }

                return _nParameterChecks;
            }
            set => _nParameterChecks = value;
        }

        /// <summary>
        /// Is this a delegate function?
        /// </summary>
        public bool IsDelegateFunction; // Temsili(delegate) fonksiyon ?

        /// <summary>
        /// Documentation
        /// </summary>
        public string Description;

        /// <summary>
        /// Is this function safe ? Does it ever throw exceptions ?
        /// </summary>
        public bool Safe;

        private string _returns;

        /// <summary>
        /// Object type(s) this function returns. Integer parseable values must be used with setter. Returns <see langword="string"></see>
        /// </summary>
        public dynamic Returns
        {
            get
            {
                if (string.IsNullOrEmpty(_returns))
                {
                    _returns = ExObjType.NULL.ToString();
                }

                return _returns;
            }
            set => _returns = API.ExApi.GetTypeMaskInfo((int)value);
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Name = null;
                    Function = null;
                    Description = null;

                    Disposer.DisposeList(ref _Parameters);
                    _parameterMask = null;

                    Returns = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private object GetDebuggerDisplay()
        {
            return string.Format("RegFunc {0}", Name);
        }
    }
}
