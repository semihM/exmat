using System;
using System.Collections.Generic;
using System.Reflection;
using ExMat.Closure;
using ExMat.Lexer;
using ExMat.Objects;
using ExMat.VM;

namespace ExMat.States
{
    public class ExSState : IDisposable
    {
        // Kullanılan ana sanal makine
        public ExVM Root;

        // Meta metotların listesi
        public List<ExObject> MetaMethods = new();

        // Meta metot isimleri tablosu
        public ExObject MetaMethodsMap = new(new Dictionary<string, ExObject>());

        // Sınıfların inşa edici metot ismi
        public ExObject ConstructorID = new(ExMat.ConstructorName);

        // Kullanılan yazı dizileri ve değişken isimleri
        public Dictionary<string, ExObject> Strings = new();

        #region _
        public Dictionary<string, ExObject> Macros = new();
        public Dictionary<string, ExMacro> BlockMacros = new();
        #endregion

        // Sınıf temisili metotları
        public ExObject ClassDelegate = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> ClassDelegateFuncs = new()
        {
            new()
            {
                Name = "has_attr",
                nParameterChecks = 3,
                ParameterMask = "yss",
                Function = new(GetDelegMethod("StdClassHasAttr"))
            },
            new()
            {
                Name = "get_attr",
                nParameterChecks = 3,
                ParameterMask = "yss",
                Function = new(GetDelegMethod("StdClassGetAttr"))
            },
            new()
            {
                Name = "set_attr",
                nParameterChecks = 4,
                ParameterMask = "yss.",
                Function = new(GetDelegMethod("StdClassSetAttr"))
            },
            new()
            {
                Name = "weakref",
                Function = new(GetDelegMethod("StdWeakRef")),
                nParameterChecks = 1,
                ParameterMask = "y"
            },

            new() { Name = string.Empty }
        };

        // Tablo temisili metotları
        public ExObject DictDelegate = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> DictDelegateFuncs = new()
        {
            new()
            {
                Name = "len",
                nParameterChecks = 1,
                ParameterMask = "d",
                Function = new(GetDelegMethod("StdDefaultLength"))
            },
            new()
            {
                Name = "has_key",
                nParameterChecks = 2,
                ParameterMask = "ds",
                Function = new(GetDelegMethod("StdDictHasKey"))
            },
            new()
            {
                Name = "get_keys",
                nParameterChecks = 1,
                ParameterMask = "d",
                Function = new(GetDelegMethod("StdDictKeys"))
            },
            new()
            {
                Name = "get_values",
                nParameterChecks = 1,
                ParameterMask = "d",
                Function = new(GetDelegMethod("StdDictValues"))
            },
            new()
            {
                Name = "weakref",
                Function = new(GetDelegMethod("StdWeakRef")),
                nParameterChecks = 1,
                ParameterMask = "d"
            },
            new() { Name = string.Empty }
        };

        // Liste temisili metotları
        public ExObject ListDelegate = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> ListDelegateFuncs = new()
        {
            new()
            {
                Name = "len",
                nParameterChecks = 1,
                ParameterMask = "a",
                Function = new(GetDelegMethod("StdDefaultLength"))
            },
            new()
            {
                Name = "append",
                nParameterChecks = 2,
                ParameterMask = "a.",
                Function = new(GetDelegMethod("StdArrayAppend"))
            },
            new()
            {
                Name = "extend",
                nParameterChecks = 2,
                ParameterMask = "aa",
                Function = new(GetDelegMethod("StdArrayExtend"))
            },
            new()
            {
                Name = "push",
                nParameterChecks = 2,
                ParameterMask = "a.",
                Function = new(GetDelegMethod("StdArrayAppend"))
            },
            new()
            {
                Name = "pop",
                nParameterChecks = 1,
                ParameterMask = "a",
                Function = new(GetDelegMethod("StdArrayPop"))
            },
            new()
            {
                Name = "resize",
                nParameterChecks = 2,
                ParameterMask = "ai",
                Function = new(GetDelegMethod("StdArrayResize"))
            },
            new()
            {
                Name = "index_of",
                nParameterChecks = 2,
                ParameterMask = "a.",
                Function = new(GetDelegMethod("StdArrayIndexOf"))
            },
            new()
            {
                Name = "count",
                nParameterChecks = 2,
                ParameterMask = "a.",
                Function = new(GetDelegMethod("StdArrayCount"))
            },
            new()
            {
                Name = "reverse",
                nParameterChecks = 1,
                ParameterMask = "a",
                Function = new(GetDelegMethod("StdArrayReverse"))
            },
            new()
            {
                Name = "slice",
                nParameterChecks = -2,
                ParameterMask = "aii",
                Function = new(GetDelegMethod("StdArraySlice"))
            },
            new()
            {
                Name = "copy",
                nParameterChecks = 1,
                ParameterMask = "a",
                Function = new(GetDelegMethod("StdArrayCopy"))
            },
            new()
            {
                Name = "transpose",
                nParameterChecks = 1,
                ParameterMask = "a",
                Function = new(GetDelegMethod("StdArrayTranspose"))
            },
            new()
            {
                Name = "weakref",
                Function = new(GetDelegMethod("StdWeakRef")),
                nParameterChecks = 1,
                ParameterMask = "a"
            },

            new() { Name = string.Empty }
        };

        // Kompleks sayı temisili metotları
        public ExObject ComplexDelegate = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> ComplexDelegateFuncs = new()
        {
            new()
            {
                Name = "abs",
                nParameterChecks = 1,
                ParameterMask = "C",
                Function = new(GetDelegMethod("StdComplexMagnitude"))
            },
            new()
            {
                Name = "phase",
                nParameterChecks = 1,
                ParameterMask = "C",
                Function = new(GetDelegMethod("StdComplexPhase"))
            },
            new()
            {
                Name = "img",
                nParameterChecks = 1,
                ParameterMask = "C",
                Function = new(GetDelegMethod("StdComplexImg"))
            },
            new()
            {
                Name = "real",
                nParameterChecks = 1,
                ParameterMask = "C",
                Function = new(GetDelegMethod("StdComplexReal"))
            },
            new()
            {
                Name = "conj",
                nParameterChecks = 1,
                ParameterMask = "C",
                Function = new(GetDelegMethod("StdComplexConjugate"))
            },
            new()
            {
                Name = "weakref",
                Function = new(GetDelegMethod("StdWeakRef")),
                nParameterChecks = 1,
                ParameterMask = "C"
            },

            new() { Name = string.Empty }
        };

        // Gerçek sayı temisili metotları
        public ExObject NumberDelegate = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> NumberDelegateFuncs = new()
        {
            new()
            {
                Name = "weakref",
                Function = new(GetDelegMethod("StdWeakRef")),
                nParameterChecks = 1,
                ParameterMask = "n"
            },
            new() { Name = string.Empty }
        };

        // Yazı dizisi temisili metotları
        public ExObject StringDelegate = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> StringDelegateFuncs = new()
        {
            new()
            {
                Name = "len",
                nParameterChecks = 1,
                ParameterMask = "s",
                Function = new(GetDelegMethod("StdDefaultLength"))
            },
            new()
            {
                Name = "index_of",
                nParameterChecks = 2,
                ParameterMask = "ss",
                Function = new(GetDelegMethod("StdStringIndexOf"))
            },
            new()
            {
                Name = "to_upper",
                nParameterChecks = 1,
                ParameterMask = "s",
                Function = new(GetDelegMethod("StdStringToupper"))
            },
            new()
            {
                Name = "to_lower",
                nParameterChecks = 1,
                ParameterMask = "s",
                Function = new(GetDelegMethod("StdStringToLower"))
            },
            new()
            {
                Name = "reverse",
                nParameterChecks = 1,
                ParameterMask = "s",
                Function = new(GetDelegMethod("StdStringReverse"))
            },
            new()
            {
                Name = "replace",
                nParameterChecks = 3,
                ParameterMask = "sss",
                Function = new(GetDelegMethod("StdStringReplace"))
            },
            new()
            {
                Name = "repeat",
                nParameterChecks = 2,
                ParameterMask = "si",
                Function = new(GetDelegMethod("StdStringRepeat"))
            },

            new()
            {
                Name = "isAlphabetic",
                nParameterChecks = -1,
                ParameterMask = "si",
                Function = new(GetDelegMethod("StdStringAlphabetic"))
            },
            new()
            {
                Name = "isNumeric",
                nParameterChecks = -1,
                ParameterMask = "si",
                Function = new(GetDelegMethod("StdStringNumeric"))
            },
            new()
            {
                Name = "isAlphaNumeric",
                nParameterChecks = -1,
                ParameterMask = "si",
                Function = new(GetDelegMethod("StdStringAlphaNumeric"))
            },
            new()
            {
                Name = "isLower",
                nParameterChecks = -1,
                ParameterMask = "si",
                Function = new(GetDelegMethod("StdStringLower"))
            },
            new()
            {
                Name = "isUpper",
                nParameterChecks = -1,
                ParameterMask = "si",
                Function = new(GetDelegMethod("StdStringUpper"))
            },
            new()
            {
                Name = "isWhitespace",
                nParameterChecks = -1,
                ParameterMask = "si",
                Function = new(GetDelegMethod("StdStringWhitespace"))
            },
            new()
            {
                Name = "isSymbol",
                nParameterChecks = -1,
                ParameterMask = "si",
                Function = new(GetDelegMethod("StdStringSymbol"))
            },
            new()
            {
                Name = "slice",
                nParameterChecks = -2,
                ParameterMask = "sii",
                Function = new(GetDelegMethod("StdStringSlice"))
            },
            new()
            {
                Name = "weakref",
                Function = new(GetDelegMethod("StdWeakRef")),
                nParameterChecks = 1,
                ParameterMask = "s"
            },

            new() { Name = string.Empty }
        };

        // Fonksiyon/kod bloğu temisili metotları
        public ExObject ClosureDelegate = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> ClosureDelegateFuncs = new()
        {
            new()
            {
                Name = "weakref",
                Function = new(GetDelegMethod("StdWeakRef")),
                nParameterChecks = 1,
                ParameterMask = "c"
            },
            new() { Name = string.Empty }
        };

        // Sınıfa ait obje temisili metotları
        public ExObject InstanceDelegate = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> InstanceDelegateFuncs = new()
        {
            new()
            {
                Name = "has_attr",
                nParameterChecks = 3,
                ParameterMask = "xss",
                Function = new(GetDelegMethod("StdInstanceHasAttr"))
            },
            new()
            {
                Name = "get_attr",
                nParameterChecks = 3,
                ParameterMask = "xss",
                Function = new(GetDelegMethod("StdInstanceGetAttr"))
            },
            new()
            {
                Name = "set_attr",
                nParameterChecks = 4,
                ParameterMask = "xss.",
                Function = new(GetDelegMethod("StdInstanceSetAttr"))
            },
            new()
            {
                Name = "weakref",
                Function = new(GetDelegMethod("StdWeakRef")),
                nParameterChecks = 1,
                ParameterMask = "x"
            },

            new() { Name = string.Empty }
        };

        // Zayıf referans temisili metotları
        public ExObject WeakRefDelegate = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> WeakRefDelegateFuncs = new()
        {
            new()
            {
                Name = "ref",
                Function = new(GetDelegMethod("StdWeakRefValue")),
                nParameterChecks = 1,
                ParameterMask = "w"
            },
            new()
            {
                Name = "weakref",
                Function = new(GetDelegMethod("StdWeakRef")),
                nParameterChecks = 1,
                ParameterMask = "w"
            },
            new() { Name = string.Empty }
        };

        private bool disposedValue;

        public static MethodInfo GetDelegMethod(string name)
        {
            return Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod(name);
        }

        public void Initialize()
        {
            MetaMethodsMap.Value.d_Dict = new();

            for (int i = 0; i < (int)ExMetaM.LAST; i++)
            {
                string mname = "_" + ((ExMetaM)i).ToString();

                MetaMethods.Add(new(mname));
                MetaMethodsMap.Value.d_Dict.Add(mname, new(i));
            }

            DictDelegate.Assign(CreateDefDel(this, DictDelegateFuncs));
            ClassDelegate.Assign(CreateDefDel(this, ClassDelegateFuncs));
            ListDelegate.Assign(CreateDefDel(this, ListDelegateFuncs));
            NumberDelegate.Assign(CreateDefDel(this, NumberDelegateFuncs));
            ComplexDelegate.Assign(CreateDefDel(this, ComplexDelegateFuncs));
            StringDelegate.Assign(CreateDefDel(this, StringDelegateFuncs));
            ClosureDelegate.Assign(CreateDefDel(this, ClosureDelegateFuncs));
            InstanceDelegate.Assign(CreateDefDel(this, InstanceDelegateFuncs));
            WeakRefDelegate.Assign(CreateDefDel(this, WeakRefDelegateFuncs));
        }

        public static ExObject CreateDefDel(ExSState exs, List<ExRegFunc> f)
        {
            int i = 0;
            Dictionary<string, ExObject> d = new();
            while (f[i].Name != string.Empty)
            {
                f[i].IsDelegateFunction = true;
                ExNativeClosure cls = ExNativeClosure.Create(exs, f[i].Function, 0);
                cls.nParameterChecks = f[i].nParameterChecks;
                cls.IsDelegateFunction = true;

                if (!exs.Strings.ContainsKey(f[i].Name))
                {
                    exs.Strings.Add(f[i].Name, new(f[i].Name));
                }
                cls.Name = new(f[i].Name);

                if (!string.IsNullOrEmpty(f[i].ParameterMask) && !API.ExAPI.CompileTypeMask(cls.TypeMasks, f[i].ParameterMask))
                {
                    return new();
                }
                d.Add(f[i].Name, new(cls));
                i++;
            }
            return new(d);
        }

        public int GetMetaIdx(string mname)
        {
            return MetaMethodsMap != null && MetaMethodsMap.GetDict() != null && MetaMethodsMap.GetDict().ContainsKey(mname) ? (int)MetaMethodsMap.GetDict()[mname].GetInt() : -1;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Root = null;

                    Disposer.DisposeObjects(ConstructorID,
                                            MetaMethodsMap,
                                            WeakRefDelegate,
                                            InstanceDelegate,
                                            ClosureDelegate,
                                            StringDelegate,
                                            NumberDelegate,
                                            ComplexDelegate,
                                            ListDelegate,
                                            DictDelegate,
                                            ClassDelegate);

                    Disposer.DisposeDict(ref BlockMacros);
                    Disposer.DisposeDict(ref Macros);
                    Disposer.DisposeDict(ref Strings);

                    Disposer.DisposeList(ref MetaMethods);
                    Disposer.DisposeList(ref WeakRefDelegateFuncs);
                    Disposer.DisposeList(ref InstanceDelegateFuncs);
                    Disposer.DisposeList(ref ClosureDelegateFuncs);
                    Disposer.DisposeList(ref StringDelegateFuncs);
                    Disposer.DisposeList(ref NumberDelegateFuncs);
                    Disposer.DisposeList(ref ComplexDelegateFuncs);
                    Disposer.DisposeList(ref ListDelegateFuncs);
                    Disposer.DisposeList(ref DictDelegateFuncs);
                    Disposer.DisposeList(ref ClassDelegateFuncs);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ExSState()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
