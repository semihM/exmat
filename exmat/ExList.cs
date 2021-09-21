using System.Collections.Generic;

namespace ExMat.Objects
{
    /// <summary>
    /// Class to create <see cref="ExObject(List{ExObject})"/>
    /// </summary>
    public class ExList : ExObject
    {
        /// <summary>
        /// Empty list constructor
        /// </summary>
        public ExList() : base(new List<ExObject>())
        {

        }

        /// <summary>
        /// List constructor
        /// </summary>
        /// <param name="e">List of objects</param>
        public ExList(List<ExObject> e) : base(e)
        {

        }

        /// <summary>
        /// Character array to object list
        /// </summary>
        /// <param name="e">Char array to store</param>
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

        /// <summary>
        /// String array to object list
        /// </summary>
        /// <param name="e">String array to store</param>
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

        /// <summary>
        /// String list to object list
        /// </summary>
        /// <param name="e">String list to store</param>
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
