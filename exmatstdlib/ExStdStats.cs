using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ExMat.API;
using ExMat.Objects;
using ExMat.Utils;
using ExMat.VM;

namespace ExMat.StdLib
{
    [ExStdLibBase(ExStdLibType.STATISTICS)]
    [ExStdLibName("stats")]
    [ExStdLibRegister(nameof(Registery))]
    public static class ExStdStats
    {
        #region FUNCTIONS
        [ExNativeFuncBase("mean", ExBaseType.FLOAT | ExBaseType.COMPLEX, "Get the mean of a given list of values. Only numeric values are used for calculations.")]
        [ExNativeParamBase(1, "list", "a", "List of numeric values")]
        public static ExFunctionStatus StatsMean(ExVM vm, int nargs)
        {
            double real = 0;
            double img = 0;
            int count = 0;
            List<ExObject> lis = vm.GetArgument(1).GetList();
            foreach (ExObject o in lis)
            {
                switch (o.Type)
                {
                    case ExObjType.FLOAT:
                        {
                            real += o.Value.f_Float;
                            count++;
                            break;
                        }
                    case ExObjType.INTEGER:
                        {
                            real += o.Value.i_Int;
                            count++;
                            break;
                        }
                    case ExObjType.COMPLEX:
                        {
                            real += o.Value.f_Float;
                            img += o.Value.c_Float;
                            count++;
                            break;
                        }
                }
            }

            if (img == 0)
            {
                return vm.CleanReturn(nargs + 2, real / count);
            }

            return vm.CleanReturn(nargs + 2, new Complex(real / count, img / count));
        }

        [ExNativeFuncBase("mode", ExBaseType.DICT, "Get the mode of a given list of values. Returns a dictionary:\n\t'value' = Most repeated value or list of most repeated values\n\t'repeat' = Repeat count of 'value' or items in 'value'\n\t'single_value' = Wheter 'value' is the repeated value itself or list of most repeating objects")]
        [ExNativeParamBase(1, "list", "a", "List of values")]
        public static ExFunctionStatus StatsMode(ExVM vm, int nargs)
        {
            List<ExObject> lis = vm.GetArgument(1).GetList();

            Dictionary<ExObject, int> counts = new();

            for (int i = 0; i < lis.Count; i++)
            {
                ExObject o = lis[i];

                int c = 0;

                bool cfound = false;
                ExObject[] carr = new ExObject[counts.Count];
                counts.Keys.CopyTo(carr, 0);

                foreach (KeyValuePair<ExObject, int> pair in counts)
                {
                    if (ExApi.CheckEqual(pair.Key, o, ref cfound) && cfound)
                    {
                        counts[carr[c]] += 1;
                        break;
                    }
                    c++;
                }
                if (!cfound)
                {
                    counts.Add(lis[c], 1);
                }
            }

            int max = counts.Values.Max();

            List<ExObject> maxrepeats = counts.Where(pair => pair.Value == max).Select(pair => new ExObject(pair.Key)).ToList();

            Dictionary<string, ExObject> res = new(2)
            {
                { "single_value", new(maxrepeats.Count == 1) },
                { "value", maxrepeats.Count == 1 ? new(maxrepeats[0]) : new(maxrepeats) },
                { "repeat", new(max) }
            };

            return vm.CleanReturn(nargs + 2, res);
        }

        [ExNativeFuncBase("median", ExBaseType.INTEGER | ExBaseType.FLOAT | ExBaseType.NULL, "Get the median of a given list of values. Only real numbers are used for ordering. Returns null if there is no real numbers in list.")]
        [ExNativeParamBase(1, "list", "a", "List of real numbers")]
        public static ExFunctionStatus StatsMedian(ExVM vm, int nargs)
        {
            List<ExObject> lis = ExUtils.GetOrderedNumericList(vm.GetArgument(1).GetList());
            int c = lis.Count;
            if (c == 0)
            {
                return vm.CleanReturn(nargs + 2, new ExObject());
            }
            else if (c % 2 == 1)
            {
                return vm.CleanReturn(nargs + 2, lis[c / 2].GetFloat());
            }
            else
            {
                return vm.CleanReturn(nargs + 2, (lis[c / 2].GetFloat() + lis[(c - 1) / 2].GetFloat()) / 2.0);
            }
        }

        #endregion

        // MAIN
        public static ExMat.StdLibRegistery Registery => (ExVM vm) =>
        {
            return true;
        };
    }
}
