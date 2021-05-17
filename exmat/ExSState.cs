﻿using System;
using System.Collections.Generic;
using ExMat.Closure;
using ExMat.Objects;
using ExMat.VM;

namespace ExMat.States
{
    public class ExSState
    {
        public Dictionary<string, ExObjectPtr> _strings = new();
        public Dictionary<string, ExObjectPtr> _spaces = new();
        public Dictionary<string, ExObjectPtr> _macros = new();

        public List<ExObjectPtr> _types = new();

        public List<ExObjectPtr> _metaMethods = new();
        public ExObjectPtr _metaMethodsMap = new(new Dictionary<string, ExObjectPtr>());

        public Dictionary<string, ExObjectPtr> _consts = new();

        public ExCollectable _GC_CHAIN = new();

        public ExVM _rootVM;

        public ExObjectPtr _constdict = new(new Dictionary<string, ExObjectPtr>());

        public ExObjectPtr _reg = new(new Dictionary<string, ExObjectPtr>());

        public ExObjectPtr _constructid = new("constructor");

        public ExObjectPtr _class_del = new(new Dictionary<string, ExObjectPtr>());
        public List<ExRegFunc> _class_delF = new()
        {
            new() { name = string.Empty }
        };

        public ExObjectPtr _dict_del = new(new Dictionary<string, ExObjectPtr>());
        public List<ExRegFunc> _dict_delF = new()
        {
            new() { name = "len", n_pchecks = 1, mask = "d", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_default_length")) },
            new() { name = "has_key", n_pchecks = 2, mask = "ds", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_dict_has_key")) },
            new() { name = string.Empty }
        };

        public ExObjectPtr _list_del = new(new Dictionary<string, ExObjectPtr>());
        public List<ExRegFunc> _list_delF = new()
        {
            new() { name = "len", n_pchecks = 1, mask = "a", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_default_length")) },
            new() { name = "append", n_pchecks = 2, mask = "a.", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_array_append")) },
            new() { name = "extend", n_pchecks = 2, mask = "aa", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_array_extend")) },
            new() { name = "push", n_pchecks = 2, mask = "a.", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_array_append")) },
            new() { name = "pop", n_pchecks = 1, mask = "a", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_array_pop")) },
            new() { name = "resize", n_pchecks = 2, mask = "ai", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_array_resize")) },
            new() { name = "index_of", n_pchecks = 2, mask = "a.", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_array_index_of")) },

            new() { name = string.Empty }
        };

        public ExObjectPtr _num_del = new(new Dictionary<string, ExObjectPtr>());
        public List<ExRegFunc> _num_delF = new()
        {
            new() { name = string.Empty }
        };

        public ExObjectPtr _str_del = new(new Dictionary<string, ExObjectPtr>());
        public List<ExRegFunc> _str_delF = new()
        {
            new() { name = "len", n_pchecks = 1, mask = "s", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_default_length")) },
            new() { name = "index_of", n_pchecks = 2, mask = "ss", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_string_index_of")) },
            new() { name = string.Empty }
        };

        public ExObjectPtr _closure_del = new(new Dictionary<string, ExObjectPtr>());
        public List<ExRegFunc> _closure_delF = new()
        {
            new() { name = string.Empty }
        };

        public ExObjectPtr _inst_del = new(new Dictionary<string, ExObjectPtr>());
        public List<ExRegFunc> _inst_delF = new()
        {
            new() { name = string.Empty }
        };

        public ExObjectPtr _wref_del = new(new Dictionary<string, ExObjectPtr>());
        public List<ExRegFunc> _wref_delF = new()
        {
            new() { name = string.Empty }
        };

        public void Initialize()
        {
            _metaMethods.Add(new("_ADD"));
            _metaMethods.Add(new("_SUB"));
            _metaMethods.Add(new("_MLT"));
            _metaMethods.Add(new("_DIV"));
            _metaMethods.Add(new("_MOD"));
            _metaMethods.Add(new("_NEG"));
            _metaMethods.Add(new("_SET"));
            _metaMethods.Add(new("_GET"));
            _metaMethods.Add(new("_TYPEOF"));
            _metaMethods.Add(new("_NEXT"));
            _metaMethods.Add(new("_CMP"));
            _metaMethods.Add(new("_CALL"));
            _metaMethods.Add(new("_NEWSLOT"));
            _metaMethods.Add(new("_DELSLOT"));
            _metaMethods.Add(new("_NEWMEM"));
            _metaMethods.Add(new("_INHERIT"));

            _metaMethodsMap._val.d_Dict = new();
            for (int i = 0; i < _metaMethods.Count; i++)
            {
                _metaMethodsMap._val.d_Dict.Add(_metaMethods[i].GetString(), new(i));
            }

            _dict_del.Assign(CreateDefDel(this, _dict_delF));
            _class_del.Assign(CreateDefDel(this, _class_delF));
            _list_del.Assign(CreateDefDel(this, _list_delF));
            _num_del.Assign(CreateDefDel(this, _num_delF));
            _str_del.Assign(CreateDefDel(this, _str_delF));
            _closure_del.Assign(CreateDefDel(this, _closure_delF));
            _inst_del.Assign(CreateDefDel(this, _inst_delF));
            _wref_del.Assign(CreateDefDel(this, _wref_delF));
        }

        public static ExObjectPtr CreateDefDel(ExSState exs, List<ExRegFunc> f)
        {
            int i = 0;
            Dictionary<string, ExObjectPtr> d = new();
            while (f[i].name != string.Empty)
            {
                ExNativeClosure cls = ExNativeClosure.Create(exs, f[i].func, 0);
                cls.n_paramscheck = f[i].n_pchecks;
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
            return _metaMethodsMap != null && _metaMethodsMap.GetDict() != null && _metaMethodsMap.GetDict().ContainsKey(mname) ? _metaMethodsMap.GetDict()[mname].GetInt() : -1;
        }
    }
}
