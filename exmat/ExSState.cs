﻿using System.Collections.Generic;
using ExMat.Closure;
using ExMat.Objects;
using ExMat.VM;

namespace ExMat.States
{
    public class ExSState
    {
        public Dictionary<string, ExObjectPtr> _strings = new();

        public List<ExObjectPtr> _types = new();

        public List<ExObjectPtr> _metaMethods = new();
        public ExObjectPtr _metaMethodsMap = new(new Dictionary<string, ExObjectPtr>());

        public Dictionary<string, dynamic> _consts = new();

        public ExCollectable _GC_CHAIN = new();

        public ExVM _rootVM;

        public ExObjectPtr _constdict = new(new Dictionary<string, ExObjectPtr>());

        public ExObjectPtr _reg = new(new Dictionary<string, ExObjectPtr>());

        public ExObjectPtr _constructid = new("constructor");

        public ExObjectPtr _class_del = new(new Dictionary<string, ExObjectPtr>());
        public List<ExRegFunc> _class_delF = new();
        public ExObjectPtr _dict_del = new(new Dictionary<string, ExObjectPtr>());
        public List<ExRegFunc> _dict_delF = new();
        public ExObjectPtr _list_del = new(new Dictionary<string, ExObjectPtr>());
        public List<ExRegFunc> _list_delF = new();
        public ExObjectPtr _num_del = new(new Dictionary<string, ExObjectPtr>());
        public List<ExRegFunc> _num_delF = new();
        public ExObjectPtr _str_del = new(new Dictionary<string, ExObjectPtr>());
        public List<ExRegFunc> _str_delF = new();
        public ExObjectPtr _closure_del = new(new Dictionary<string, ExObjectPtr>());
        public List<ExRegFunc> _closure_delF = new();
        public ExObjectPtr _inst_del = new(new Dictionary<string, ExObjectPtr>());
        public List<ExRegFunc> _inst_delF = new();
        public ExObjectPtr _wref_del = new(new Dictionary<string, ExObjectPtr>());
        public List<ExRegFunc> _wref_delF = new();

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

            _dict_del = CreateDefDel(this, _dict_delF);
            _class_del = CreateDefDel(this, _class_delF);
            _list_del = CreateDefDel(this, _list_delF);
            _num_del = CreateDefDel(this, _num_delF);
            _str_del = CreateDefDel(this, _str_delF);
            _closure_del = CreateDefDel(this, _closure_delF);
            _inst_del = CreateDefDel(this, _inst_delF);
            _wref_del = CreateDefDel(this, _wref_delF);
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
                    exs._strings.Add(f[i].name, new());
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