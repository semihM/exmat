using System.Collections.Generic;

namespace ExMat.Objects
{
    public class ExList : ExObject
    {
        public ExList()
        {
            Type = ExObjType.ARRAY;
            Value.l_List = new();
            Value._RefC = new();
        }

        public ExList(bool n)
        {
            Type = ExObjType.ARRAY;
            Value.l_List = n ? new() : null;
            Value._RefC = new();
        }
        public ExList(char c)
        {
            Type = ExObjType.ARRAY;
            Value.l_List = c != '0' ? new() : null;
            Value._RefC = new();
        }
        public ExList(ExList e)
        {
            Type = ExObjType.ARRAY;
            Value.l_List = e.Value.l_List;
            Value._RefC = new();
        }
        public ExList(List<ExObject> e)
        {
            Type = ExObjType.ARRAY;
            Value.l_List = e;
            Value._RefC = new();
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
        }
    }
}
