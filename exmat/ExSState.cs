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
        public Dictionary<string, ExObject> _strings = new();
        public Dictionary<string, ExObject> _macros = new();
        public Dictionary<string, ExMacro> _blockmacros = new();

        public List<ExObject> _metaMethods = new();
        public ExObject _metaMethodsMap = new(new Dictionary<string, ExObject>());

        public Dictionary<string, ExObject> _consts = new();

        public ExVM _rootVM;

        public ExObject _constdict = new(new Dictionary<string, ExObject>());

        public ExObject _reg = new(new Dictionary<string, ExObject>());

        public ExObject _constructid = new(ExMat._CONSTRUCTOR);

        public ExObject _class_del = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> _class_delF = new()
        {
            new()
            {
                name = "has_attr",
                n_pchecks = 3,
                mask = "yss",
                func = new(GetDelegMethod("BASE_class_hasattr"))
            },
            new()
            {
                name = "get_attr",
                n_pchecks = 3,
                mask = "yss",
                func = new(GetDelegMethod("BASE_class_getattr"))
            },
            new()
            {
                name = "set_attr",
                n_pchecks = 4,
                mask = "yss.",
                func = new(GetDelegMethod("BASE_class_setattr"))
            },

            new() { name = string.Empty }
        };

        public ExObject _dict_del = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> _dict_delF = new()
        {
            new()
            {
                name = "len",
                n_pchecks = 1,
                mask = "d",
                func = new(GetDelegMethod("BASE_default_length"))
            },
            new()
            {
                name = "has_key",
                n_pchecks = 2,
                mask = "ds",
                func = new(GetDelegMethod("BASE_dict_has_key"))
            },
            new()
            {
                name = "get_keys",
                n_pchecks = 1,
                mask = "d",
                func = new(GetDelegMethod("BASE_dict_keys"))
            },
            new()
            {
                name = "get_values",
                n_pchecks = 1,
                mask = "d",
                func = new(GetDelegMethod("BASE_dict_values"))
            },
            new() { name = string.Empty }
        };

        public ExObject _list_del = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> _list_delF = new()
        {
            new()
            {
                name = "len",
                n_pchecks = 1,
                mask = "a",
                func = new(GetDelegMethod("BASE_default_length"))
            },
            new()
            {
                name = "append",
                n_pchecks = 2,
                mask = "a.",
                func = new(GetDelegMethod("BASE_array_append"))
            },
            new()
            {
                name = "extend",
                n_pchecks = 2,
                mask = "aa",
                func = new(GetDelegMethod("BASE_array_extend"))
            },
            new()
            {
                name = "push",
                n_pchecks = 2,
                mask = "a.",
                func = new(GetDelegMethod("BASE_array_append"))
            },
            new()
            {
                name = "pop",
                n_pchecks = 1,
                mask = "a",
                func = new(GetDelegMethod("BASE_array_pop"))
            },
            new()
            {
                name = "resize",
                n_pchecks = 2,
                mask = "ai",
                func = new(GetDelegMethod("BASE_array_resize"))
            },
            new()
            {
                name = "index_of",
                n_pchecks = 2,
                mask = "a.",
                func = new(GetDelegMethod("BASE_array_index_of"))
            },
            new()
            {
                name = "reverse",
                n_pchecks = 1,
                mask = "a",
                func = new(GetDelegMethod("BASE_array_reverse"))
            },
            new()
            {
                name = "slice",
                n_pchecks = -2,
                mask = "aii",
                func = new(GetDelegMethod("BASE_array_slice"))
            },
            new()
            {
                name = "transpose",
                n_pchecks = 1,
                mask = "a",
                func = new(GetDelegMethod("BASE_array_transpose"))
            },

            new() { name = string.Empty }
        };

        public ExObject _complex_del = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> _complex_delF = new()
        {
            new()
            {
                name = "abs",
                n_pchecks = 1,
                mask = "C",
                func = new(GetDelegMethod("BASE_complex_magnitude"))
            },
            new()
            {
                name = "phase",
                n_pchecks = 1,
                mask = "C",
                func = new(GetDelegMethod("BASE_complex_phase"))
            },
            new()
            {
                name = "img",
                n_pchecks = 1,
                mask = "C",
                func = new(GetDelegMethod("BASE_complex_img"))
            },
            new()
            {
                name = "real",
                n_pchecks = 1,
                mask = "C",
                func = new(GetDelegMethod("BASE_complex_real"))
            },
            new()
            {
                name = "conj",
                n_pchecks = 1,
                mask = "C",
                func = new(GetDelegMethod("BASE_complex_conjugate"))
            },

            new() { name = string.Empty }
        };

        public ExObject _num_del = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> _num_delF = new()
        {
            new() { name = string.Empty }
        };

        public ExObject _str_del = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> _str_delF = new()
        {
            new()
            {
                name = "len",
                n_pchecks = 1,
                mask = "s",
                func = new(GetDelegMethod("BASE_default_length"))
            },
            new()
            {
                name = "index_of",
                n_pchecks = 2,
                mask = "ss",
                func = new(GetDelegMethod("BASE_string_index_of"))
            },
            new()
            {
                name = "to_upper",
                n_pchecks = 1,
                mask = "s",
                func = new(GetDelegMethod("BASE_string_toupper"))
            },
            new()
            {
                name = "to_lower",
                n_pchecks = 1,
                mask = "s",
                func = new(GetDelegMethod("BASE_string_tolower"))
            },
            new()
            {
                name = "reverse",
                n_pchecks = 1,
                mask = "s",
                func = new(GetDelegMethod("BASE_string_reverse"))
            },
            new()
            {
                name = "replace",
                n_pchecks = 3,
                mask = "sss",
                func = new(GetDelegMethod("BASE_string_replace"))
            },
            new()
            {
                name = "repeat",
                n_pchecks = 2,
                mask = "si",
                func = new(GetDelegMethod("BASE_string_repeat"))
            },

            new()
            {
                name = "isAlphabetic",
                n_pchecks = -1,
                mask = "si",
                func = new(GetDelegMethod("BASE_string_isAlphabetic"))
            },
            new()
            {
                name = "isNumeric",
                n_pchecks = -1,
                mask = "si",
                func = new(GetDelegMethod("BASE_string_isNumeric"))
            },
            new()
            {
                name = "isAlphaNumeric",
                n_pchecks = -1,
                mask = "si",
                func = new(GetDelegMethod("BASE_string_isAlphaNumeric"))
            },
            new()
            {
                name = "isLower",
                n_pchecks = -1,
                mask = "si",
                func = new(GetDelegMethod("BASE_string_isLower"))
            },
            new()
            {
                name = "isUpper",
                n_pchecks = -1,
                mask = "si",
                func = new(GetDelegMethod("BASE_string_isUpper"))
            },
            new()
            {
                name = "isWhitespace",
                n_pchecks = -1,
                mask = "si",
                func = new(GetDelegMethod("BASE_string_isWhitespace"))
            },
            new()
            {
                name = "isSymbol",
                n_pchecks = -1,
                mask = "si",
                func = new(GetDelegMethod("BASE_string_isSymbol"))
            },

            new() { name = string.Empty }
        };

        public ExObject _closure_del = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> _closure_delF = new()
        {
            new() { name = string.Empty }
        };

        public ExObject _inst_del = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> _inst_delF = new()
        {
            new()
            {
                name = "has_attr",
                n_pchecks = 3,
                mask = "xss",
                func = new(GetDelegMethod("BASE_instance_hasattr"))
            },
            new()
            {
                name = "get_attr",
                n_pchecks = 3,
                mask = "xss",
                func = new(GetDelegMethod("BASE_instance_getattr"))
            },
            new()
            {
                name = "set_attr",
                n_pchecks = 4,
                mask = "xss.",
                func = new(GetDelegMethod("BASE_instance_setattr"))
            },

            new() { name = string.Empty }
        };

        public ExObject _wref_del = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> _wref_delF = new()
        {
            new() { name = string.Empty }
        };
        private bool disposedValue;

        public static MethodInfo GetDelegMethod(string name)
        {
            return Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod(name);
        }

        public void Initialize()
        {
            _metaMethodsMap._val.d_Dict = new();

            for (int i = 0; i < (int)ExMetaM._LAST; i++)
            {
                string mname = "_" + ((ExMetaM)i).ToString();

                _metaMethods.Add(new(mname));
                _metaMethodsMap._val.d_Dict.Add(mname, new(i));
            }

            _dict_del.Assign(CreateDefDel(this, _dict_delF));
            _class_del.Assign(CreateDefDel(this, _class_delF));
            _list_del.Assign(CreateDefDel(this, _list_delF));
            _num_del.Assign(CreateDefDel(this, _num_delF));
            _complex_del.Assign(CreateDefDel(this, _complex_delF));
            _str_del.Assign(CreateDefDel(this, _str_delF));
            _closure_del.Assign(CreateDefDel(this, _closure_delF));
            _inst_del.Assign(CreateDefDel(this, _inst_delF));
            _wref_del.Assign(CreateDefDel(this, _wref_delF));
        }

        public static ExObject CreateDefDel(ExSState exs, List<ExRegFunc> f)
        {
            int i = 0;
            Dictionary<string, ExObject> d = new();
            while (f[i].name != string.Empty)
            {
                f[i].b_isdeleg = true;
                ExNativeClosure cls = ExNativeClosure.Create(exs, f[i].func, 0);
                cls.n_paramscheck = f[i].n_pchecks;
                cls.b_deleg = true;

                if (!exs._strings.ContainsKey(f[i].name))
                {
                    exs._strings.Add(f[i].name, new(f[i].name));
                }
                cls._name = new(f[i].name);

                if (!string.IsNullOrEmpty(f[i].mask) && !API.ExAPI.CompileTypeMask(cls._typecheck, f[i].mask))
                {
                    return new();
                }
                d.Add(f[i].name, cls);
                i++;
            }
            return new(d);
        }

        public int GetMetaIdx(string mname)
        {
            return _metaMethodsMap != null && _metaMethodsMap.GetDict() != null && _metaMethodsMap.GetDict().ContainsKey(mname) ? (int)_metaMethodsMap.GetDict()[mname].GetInt() : -1;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _rootVM = null;

                    Disposer.DisposeObjects(_constructid,
                                            _reg,
                                            _constdict,
                                            _metaMethodsMap,
                                            _wref_del,
                                            _inst_del,
                                            _closure_del,
                                            _str_del,
                                            _num_del,
                                            _complex_del,
                                            _list_del,
                                            _dict_del,
                                            _class_del);

                    Disposer.DisposeDict(ref _consts);
                    Disposer.DisposeDict(ref _blockmacros);
                    Disposer.DisposeDict(ref _macros);
                    Disposer.DisposeDict(ref _strings);

                    Disposer.DisposeList(ref _metaMethods);
                    Disposer.DisposeList(ref _wref_delF);
                    Disposer.DisposeList(ref _inst_delF);
                    Disposer.DisposeList(ref _closure_delF);
                    Disposer.DisposeList(ref _str_delF);
                    Disposer.DisposeList(ref _num_delF);
                    Disposer.DisposeList(ref _complex_delF);
                    Disposer.DisposeList(ref _list_delF);
                    Disposer.DisposeList(ref _dict_delF);
                    Disposer.DisposeList(ref _class_delF);
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
