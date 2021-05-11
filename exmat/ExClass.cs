using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExMat.Objects;
using ExMat.States;

namespace ExMat.Class
{
    public class ExClassMem
    {
        public ExObjectPtr val;
        public ExObjectPtr attrs;

        public void Nullify()
        {
            val.Nullify();
            attrs.Nullify();
        }
    }
    public class ExClass : ExCollectable
    {
        public ExClass _base;
        public Dictionary<string, ExObjectPtr> _members;
        public List<ExObjectPtr> _metas;
        public List<ExClassMem> _defvals;
        public List<ExClassMem> _methods;
        public ExObjectPtr _attrs;
        public ExObjectPtr _hook;
        public dynamic _typetag;
        public bool _islocked;
        public int _constridx;
        public int _udsize;

        public ExClass()
        {
            _type = ExObjType.CLASS;
        }

        public new ExObjType GetType()
        {
            return ExObjType.CLASS;
        }

        public void LockCls()
        {
            _islocked = true;
            if (_base != null && _base._type != ExObjType.NULL)
            {
                _base.LockCls();
            }
        }
        public ExInstance CreateInstance()
        {
            if (!_islocked)
            {
                LockCls();
            }
            return ExInstance.Create(_sState, this);
        }

        public bool GetConstructor(ref ExObjectPtr o)
        {
            if (_constridx != -1)
            {
                o.Assign(_methods[_constridx].val);
                return true;
            }
            return false;
        }
    }

    public class ExInstance : ExDeleg
    {
        public ExClass _class;
        public ExUserP _up;
        public ExInt _hook;
        public List<ExObjectPtr> _values;

        public ExInstance()
        {
            _type = ExObjType.INSTANCE;
        }

        public new ExObjType GetType()
        {
            return ExObjType.INSTANCE;
        }

        public static ExInstance Create(ExSState exS, ExClass cls)
        {
            ExInstance ins = new() { _sState = exS, _class = cls };
            return ins;
        }

        public bool GetMeta(int midx, ref ExObjectPtr res)
        {
            if (_class._metas[midx]._type != ExObjType.NULL)
            {
                res = _class._metas[midx];
                return true;
            }
            return false;
        }
    }
}
