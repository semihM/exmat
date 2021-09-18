using System.Collections.Generic;

namespace ExMat.Objects
{
    public class ExList : ExObject
    {
        public ExList() : base(new List<ExObject>())
        {

        }

        public ExList(List<ExObject> e) : base(e)
        {

        }

        public ExList(char[] e)
        {
            Type = ExObjType.ARRAY;

            ValueCustom.l_List = new(e.Length);

            ValueCustom._RefC = new();

            foreach (char s in e)
            {
                ValueCustom.l_List.Add(new(s.ToString()));
            }

            AddReference(Type, ValueCustom, true);
        }

        public ExList(string[] e)
        {
            Type = ExObjType.ARRAY;

            ValueCustom.l_List = new(e.Length);

            ValueCustom._RefC = new();

            foreach (string s in e)
            {
                ValueCustom.l_List.Add(new(s));
            }

            AddReference(Type, ValueCustom, true);
        }

        public ExList(List<string> e)
        {
            Type = ExObjType.ARRAY;

            ValueCustom.l_List = new(e.Count);

            ValueCustom._RefC = new();

            foreach (string s in e)
            {
                ValueCustom.l_List.Add(new(s));
            }

            AddReference(Type, ValueCustom, true);
        }
    }
}
