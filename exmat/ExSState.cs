using System.Collections.Generic;
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
        }

        public int GetMetaIdx(string mname)
        {
            return _metaMethodsMap != null && _metaMethodsMap.GetDict() != null && _metaMethodsMap.GetDict().ContainsKey(mname) ? _metaMethodsMap.GetDict()[mname].GetInt() : -1;
        }
    }
}
