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

            Value.l_List = new(e.Length);

            Value._RefC = new();

            foreach (char s in e)
            {
                Value.l_List.Add(new(s.ToString()));
            }

            AddReference(Type, Value, true);
        }

        public ExList(string[] e)
        {
            Type = ExObjType.ARRAY;

            Value.l_List = new(e.Length);

            Value._RefC = new();

            foreach (string s in e)
            {
                Value.l_List.Add(new(s));
            }

            AddReference(Type, Value, true);
        }

        public ExList(List<string> e)
        {
            Type = ExObjType.ARRAY;

            Value.l_List = new(e.Count);

            Value._RefC = new();

            foreach (string s in e)
            {
                Value.l_List.Add(new(s));
            }

            AddReference(Type, Value, true);
        }
    }
}
