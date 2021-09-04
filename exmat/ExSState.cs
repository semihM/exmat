using System;
using System.Collections.Generic;
using ExMat.API;
using ExMat.BaseLib;
using ExMat.Closure;
using ExMat.Objects;
using ExMat.VM;

namespace ExMat.States
{
    public class ExSState : IDisposable
    {
        // Kullanılan ana sanal makine
        public ExVM Root;

        // Meta metotların listesi
        public List<ExObject> MetaMethods = new();

        // Meta metot isimleri tablosu
        public ExObject MetaMethodsMap = new(new Dictionary<string, ExObject>());

        // Sınıfların inşa edici metot ismi
        public ExObject ConstructorID = new(ExMat.ConstructorName);

        // Kullanılan yazı dizileri ve değişken isimleri
        public Dictionary<string, ExObject> Strings = new();

        private static ExRegFunc CreateWeakReferenceDelegate(char basemask)
        {
            return new()
            {
                Name = "weakref",
                Function = ExBaseLib.StdWeakRef,
                Parameters = new(),
                BaseTypeMask = basemask,
                Returns = ExObjType.WEAKREF,
                Description = "Returns a weak reference of the object"
            };
        }
        private static ExRegFunc CreateLengthDelegate(char basemask)
        {
            return new()
            {
                Name = "len",
                Function = ExBaseLib.StdDefaultLength,
                Parameters = new(),
                BaseTypeMask = basemask,
                Returns = ExObjType.INTEGER,
                Description = "Returns the 'length' of the object"
            };
        }
        private static ExRegFunc EndDelegateList()
        {
            return new() { Name = string.Empty };
        }

        // Sınıf temisili metotları
        public ExObject ClassDelegate = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> ClassDelegateFuncs = new()
        {
            new()
            {
                Name = "has_attr",
                Function = ExBaseLib.StdClassHasAttr,
                BaseTypeMask = 'y',
                Parameters = new()
                {
                    new("member_or_method", "s", "Member or method name"),
                    new("attribute", "s", "Attribute name to check")
                },
                Returns = ExObjType.BOOL,
                Description = "Check if an attribute exists for a member or a method"
            },
            new()
            {
                Name = "get_attr",
                Function = ExBaseLib.StdClassGetAttr,
                BaseTypeMask = 'y',
                Parameters = new()
                {
                    new("member_or_method", "s", "Member or method name"),
                    new("attribute", "s", "Attribute name to get")
                },
                Returns = ExObjType.BOOL,
                Description = "Get an attribute of a member or a method"
            },
            new()
            {
                Name = "set_attr",
                Function = ExBaseLib.StdClassSetAttr,
                BaseTypeMask = 'y',
                Parameters = new()
                {
                    new("member_or_method", "s", "Member or method name"),
                    new("attribute", "s", "Attribute name to get"),
                    new("new_value", ".", "New attribute value")
                },
                Returns = ExObjType.BOOL,
                Description = "Set an attribute of a member or a method"
            },

            CreateWeakReferenceDelegate('y'),

            EndDelegateList()
        };

        // Tablo temisili metotları
        public ExObject DictDelegate = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> DictDelegateFuncs = new()
        {
            CreateLengthDelegate('d'),

            new()
            {
                Name = "has_key",
                Function = ExBaseLib.StdDictHasKey,
                BaseTypeMask = 'd',
                Parameters = new()
                {
                    new("key", "s", "Key to check")
                },
                Returns = ExObjType.BOOL,
                Description = "Check if given key exists"
            },
            new()
            {
                Name = "get_keys",
                Function = ExBaseLib.StdDictKeys,
                BaseTypeMask = 'd',
                Parameters = new(),
                Returns = ExObjType.ARRAY,
                Description = "Get a list of the keys"
            },
            new()
            {
                Name = "get_values",
                Function = ExBaseLib.StdDictValues,
                BaseTypeMask = 'd',
                Parameters = new(),
                Returns = ExObjType.ARRAY,
                Description = "Get a list of the values"
            },
            new()
            {
                Name = "random_key",
                Function = ExBaseLib.StdDictRandomKey,
                BaseTypeMask = 'd',
                Parameters = new(),
                Returns = ExObjType.STRING,
                Description = "Get a random key"
            },
            new()
            {
                Name = "random_val",
                Function = ExBaseLib.StdDictRandomVal,
                BaseTypeMask = 'd',
                Parameters = new(),
                Returns = -1,
                Description = "Get a random value"
            },
            CreateWeakReferenceDelegate('d'),

            EndDelegateList()
        };

        // Liste temisili metotları
        public ExObject ListDelegate = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> ListDelegateFuncs = new()
        {
            CreateLengthDelegate('a'),
            new()
            {
                Name = "append",
                Function = ExBaseLib.StdArrayAppend,
                BaseTypeMask = 'a',
                Parameters = new()
                {
                    new("object", ".", "Object to append")
                },
                Returns = ExObjType.ARRAY,
                Description = "Return a new list with given item appended"
            },
            new()
            {
                Name = "remove_at",
                Function = ExBaseLib.StdArrayRemoveAt,
                BaseTypeMask = 'a',
                Parameters = new()
                {
                    new("index", "r", "Index of the item to remove")
                },
                Returns = ExObjType.ARRAY,
                Description = "Return a new list with the item at given index removed"
            },
            new()
            {
                Name = "expand",
                Function = ExBaseLib.StdArrayExpand,
                BaseTypeMask = 'a',
                Parameters = new()
                {
                    new("list", "a", "List of items to append")
                },
                Returns = ExObjType.ARRAY,
                Description = "Return a new list with given list of objects appended"
            },
            new()
            {
                Name = "push",
                Function = ExBaseLib.StdArrayPush,
                BaseTypeMask = 'a',
                Parameters = new()
                {
                    new("object", ".", "Object to push to end")
                },
                Returns = ExObjType.ARRAY,
                Description = "Return the original list with given item appended"
            },
            new()
            {
                Name = "pop",
                Function = ExBaseLib.StdArrayPop,
                BaseTypeMask = 'a',
                Parameters = new()
                {
                    new("count", "r", "Amount of items to pop", new(1))
                },
                Returns = ExObjType.ARRAY,
                Description = "Return the original list with given amount of items popped"
            },
            new()
            {
                Name = "extend",
                Function = ExBaseLib.StdArrayExtend,
                BaseTypeMask = 'a',
                Parameters = new()
                {
                    new("list", "a", "List of items to append")
                },
                Returns = ExObjType.ARRAY,
                Description = "Return the original list with given list of objects appended"
            },
            new()
            {
                Name = "resize",
                Function = ExBaseLib.StdArrayResize,
                BaseTypeMask = 'a',
                Parameters = new()
                {
                    new("new_size", "r", "New size for the list"),
                    new("filler", ".", "Filler object if new size is bigger than current size", new())
                },
                Returns = ExObjType.ARRAY,
                Description = "Return the original list resized"
            },
            new()
            {
                Name = "index_of",
                Function = ExBaseLib.StdArrayIndexOf,
                BaseTypeMask = 'a',
                Parameters = new()
                {
                    new("object", ".", "Object to search for")
                },
                Returns = ExObjType.INTEGER,
                Description = "Return the index of an object or -1 if nothing found"
            },
            new()
            {
                Name = "count",
                Function = ExBaseLib.StdArrayCount,
                BaseTypeMask = 'a',
                Parameters = new()
                {
                    new("object", ".", "Object to search for")
                },
                Returns = ExObjType.INTEGER,
                Description = "Count how many times given object appears in the list"
            },
            new()
            {
                Name = "reverse",
                Function = ExBaseLib.StdArrayReverse,
                BaseTypeMask = 'a',
                Parameters = new(),
                Returns = ExObjType.ARRAY,
                Description = "Return a new list with the order of items reversed"
            },
            new()
            {
                Name = "slice",
                Function = ExBaseLib.StdArraySlice,
                BaseTypeMask = 'a',
                Parameters = new()
                {
                    new("index1", "r", "If used alone: [0,index1), otherwise: [index1,index2)"),
                    new("index2", "r", "Ending index, returned list length == index2 - index1", new(-1))
                },
                Returns = ExObjType.ARRAY,
                Description = "Return a new list with items picked from given range. Negative indices gets incremented by list length"
            },
            new()
            {
                Name = "copy",
                Function = ExBaseLib.StdArrayCopy,
                BaseTypeMask = 'a',
                Parameters = new(),
                Returns = ExObjType.ARRAY,
                Description = "Return a copy of the list."
            },
            new()
            {
                Name = "transpose",
                Function = ExBaseLib.StdArrayTranspose,
                BaseTypeMask = 'a',
                Parameters = new(),
                Returns = ExObjType.ARRAY,
                Description = "Return the transposed form of given matrix. Not usable for non-matrix formats."
            },
            new()
            {
                Name = "random",
                Function = ExBaseLib.StdArrayRandom,
                BaseTypeMask = 'a',
                Parameters = new()
                {
                    new("count", "n", "Amount of random values to return", new(1))
                },
                Returns = -1,
                Description = "Return a random item or a list of given amount of random items. If 'count' > 1, a list of unique item picks is returned."
            },
            new()
            {
                Name = "shuffle",
                Function = ExBaseLib.StdArrayShuffle,
                BaseTypeMask = 'a',
                Parameters = new(),
                Returns = ExObjType.ARRAY,
                Description = "Return a new shuffled list"
            },

            CreateWeakReferenceDelegate('a'),

            EndDelegateList()
        };

        // Kompleks sayı temisili metotları
        public ExObject ComplexDelegate = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> ComplexDelegateFuncs = new()
        {
            new()
            {
                Name = "abs",
                Function = ExBaseLib.StdComplexMagnitude,
                BaseTypeMask = 'C',
                Parameters = new(),
                Returns = ExObjType.FLOAT,
                Description = "Return the magnitute of the value"
            },
            new()
            {
                Name = "phase",
                Function = ExBaseLib.StdComplexPhase,
                BaseTypeMask = 'C',
                Parameters = new(),
                Returns = ExObjType.FLOAT,
                Description = "Return the phase of the value"
            },
            new()
            {
                Name = "img",
                Function = ExBaseLib.StdComplexImg,
                BaseTypeMask = 'C',
                Parameters = new(),
                Returns = ExObjType.FLOAT,
                Description = "Return the imaginary part of the value"
            },
            new()
            {
                Name = "real",
                Function = ExBaseLib.StdComplexReal,
                BaseTypeMask = 'C',
                Parameters = new(),
                Returns = ExObjType.FLOAT,
                Description = "Return the real part of the value"
            },
            new()
            {
                Name = "conj",
                Function = ExBaseLib.StdComplexConjugate,
                BaseTypeMask = 'C',
                Parameters = new(),
                Returns = ExObjType.COMPLEX,
                Description = "Return the complex conjugate of the value"
            },
            CreateWeakReferenceDelegate('C'),

            EndDelegateList()
        };

        // Gerçek sayı temisili metotları
        public ExObject NumberDelegate = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> NumberDelegateFuncs = new()
        {
            new()
            {
                Name = "real",
                Function = ExBaseLib.StdNumericReal,
                BaseTypeMask = 'r',
                Parameters = new(),
                Returns = ExObjType.INTEGER | ExObjType.FLOAT,
                Description = "Return the real part of the value. This delegate always returns the value itself"
            },
            new()
            {
                Name = "img",
                Function = ExBaseLib.StdNumericImage,
                BaseTypeMask = 'r',
                Parameters = new(),
                Returns = ExObjType.INTEGER | ExObjType.FLOAT,
                Description = "Return the imaginary part of the value. This delegate always returns 0"
            },
            CreateWeakReferenceDelegate('r'),

            EndDelegateList()
        };

        // Yazı dizisi temisili metotları
        public ExObject StringDelegate = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> StringDelegateFuncs = new()
        {
            CreateLengthDelegate('s'),
            new()
            {
                Name = "index_of",
                Function = ExBaseLib.StdStringIndexOf,
                BaseTypeMask = 's',
                Parameters = new()
                {
                    new("substring", "s", "Substring to search for")
                },
                Returns = ExObjType.INTEGER,
                Description = "Return the index of given substring or -1"
            },
            new()
            {
                Name = "to_upper",
                Function = ExBaseLib.StdStringToUpper,
                BaseTypeMask = 's',
                Parameters = new(),
                Returns = ExObjType.STRING,
                Description = "Return a new string with characters capitalized"
            },
            new()
            {
                Name = "to_lower",
                Function = ExBaseLib.StdStringToLower,
                BaseTypeMask = 's',
                Parameters = new(),
                Returns = ExObjType.STRING,
                Description = "Return a new string with characters uncapitalized"
            },
            new()
            {
                Name = "reverse",
                Function = ExBaseLib.StdStringReverse,
                BaseTypeMask = 's',
                Parameters = new(),
                Returns = ExObjType.STRING,
                Description = "Return a new string with character order reversed"
            },
            new()
            {
                Name = "replace",
                Function = ExBaseLib.StdStringReplace,
                BaseTypeMask = 's',
                Parameters = new()
                {
                    new("old", "s", "Value to be replaced"),
                    new("new", "s", "Value to use for replacing")
                },
                Returns = ExObjType.STRING,
                Description = "Return a new string with given substrings replaced with given new string"
            },
            new()
            {
                Name = "repeat",
                Function = ExBaseLib.StdStringRepeat,
                BaseTypeMask = 's',
                Parameters = new()
                {
                    new("repeat", "r", "Times to repeat the string")
                },
                Returns = ExObjType.STRING,
                Description = "Return a new string with the original string repeat given times"
            },

            new()
            {
                Name = "isAlphabetic",
                Function = ExBaseLib.StdStringAlphabetic,
                BaseTypeMask = 's',
                Parameters = new()
                {
                    new("index", "r", "Character index to check instead of the whole string", new(0))
                },
                Returns = ExObjType.BOOL,
                Description = "Check if the string or a character at given index is alphabetic"
            },
            new()
            {
                Name = "isNumeric",
                Function = ExBaseLib.StdStringNumeric,
                BaseTypeMask = 's',
                Parameters = new()
                {
                    new("index", "r", "Character index to check instead of the whole string", new(0))
                },
                Returns = ExObjType.BOOL,
                Description = "Check if the string or a character at given index is numeric"
            },
            new()
            {
                Name = "isAlphaNumeric",
                Function = ExBaseLib.StdStringAlphaNumeric,
                BaseTypeMask = 's',
                Parameters = new()
                {
                    new("index", "r", "Character index to check instead of the whole string", new(0))
                },
                Returns = ExObjType.BOOL,
                Description = "Check if the string or a character at given index is alphabetic or numeric"
            },
            new()
            {
                Name = "isLower",
                Function = ExBaseLib.StdStringLower,
                BaseTypeMask = 's',
                Parameters = new()
                {
                    new("index", "r", "Character index to check instead of the whole string", new(0))
                },
                Returns = ExObjType.BOOL,
                Description = "Check if the string or a character at given index is lower case"
            },
            new()
            {
                Name = "isUpper",
                Function = ExBaseLib.StdStringUpper,
                BaseTypeMask = 's',
                Parameters = new()
                {
                    new("index", "r", "Character index to check instead of the whole string", new(0))
                },
                Returns = ExObjType.BOOL,
                Description = "Check if the string or a character at given index is upper case"
            },
            new()
            {
                Name = "isWhitespace",
                Function = ExBaseLib.StdStringWhitespace,
                BaseTypeMask = 's',
                Parameters = new()
                {
                    new("index", "r", "Character index to check instead of the whole string", new(0))
                },
                Returns = ExObjType.BOOL,
                Description = "Check if the string or a character at given index is whitespace"
            },
            new()
            {
                Name = "isSymbol",
                Function = ExBaseLib.StdStringSymbol,
                BaseTypeMask = 's',
                Parameters = new()
                {
                    new("index", "r", "Character index to check instead of the whole string", new(0))
                },
                Returns = ExObjType.BOOL,
                Description = "Check if the string or a character at given index is symbolic"
            },
            new()
            {
                Name = "slice",
                Function = ExBaseLib.StdStringSlice,
                BaseTypeMask = 'a',
                Parameters = new()
                {
                    new("index1", "r", "If used alone: [0,index1), otherwise: [index1,index2)"),
                    new("index2", "r", "Ending index, returned list length == index2 - index1", new(-1))
                },
                Returns = ExObjType.ARRAY,
                Description = "Return a new string with characters picked from given range. Negative indices gets incremented by string length"
            },
            CreateWeakReferenceDelegate('s'),

            EndDelegateList()
        };

        // Fonksiyon/kod bloğu temisili metotları
        public ExObject ClosureDelegate = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> ClosureDelegateFuncs = new()
        {
            CreateWeakReferenceDelegate('c'),
            EndDelegateList()
        };

        // Sınıfa ait obje temisili metotları
        public ExObject InstanceDelegate = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> InstanceDelegateFuncs = new()
        {
            new()
            {
                Name = "has_attr",
                Function = ExBaseLib.StdInstanceHasAttr,
                BaseTypeMask = 'x',
                Parameters = new()
                {
                    new("member_or_method", "s", "Member or method name"),
                    new("attribute", "s", "Attribute name to check")
                },
                Returns = ExObjType.BOOL,
                Description = "Check if an attribute exists for a member or a method"
            },
            new()
            {
                Name = "get_attr",
                Function = ExBaseLib.StdInstanceGetAttr,
                BaseTypeMask = 'x',
                Parameters = new()
                {
                    new("member_or_method", "s", "Member or method name"),
                    new("attribute", "s", "Attribute name to get")
                },
                Returns = ExObjType.BOOL,
                Description = "Get an attribute of a member or a method"
            },
            new()
            {
                Name = "set_attr",
                Function = ExBaseLib.StdInstanceSetAttr,
                BaseTypeMask = 'x',
                Parameters = new()
                {
                    new("member_or_method", "s", "Member or method name"),
                    new("attribute", "s", "Attribute name to get"),
                    new("new_value", ".", "New attribute value")
                },
                Returns = ExObjType.BOOL,
                Description = "Set an attribute of a member or a method"
            },
            CreateWeakReferenceDelegate('x'),

            EndDelegateList()
        };

        // Zayıf referans temisili metotları
        public ExObject WeakRefDelegate = new(new Dictionary<string, ExObject>());
        public List<ExRegFunc> WeakRefDelegateFuncs = new()
        {
            new()
            {
                Name = "ref",
                Function = ExBaseLib.StdWeakRefValue,
                BaseTypeMask = 'w',
                Parameters = new(),
                Returns = -1,
                Description = "Return the referenced object"
            },
            CreateWeakReferenceDelegate('w'),
            EndDelegateList()
        };

        private bool disposedValue;

        private void CreateMetaMethod(int i)
        {
            string mname = "_" + ((ExMetaMethod)i).ToString();

            MetaMethods.Add(new(mname));
            MetaMethodsMap.GetDict().Add(mname, new(i));
        }

        public void Initialize()
        {
            MetaMethodsMap.Value.d_Dict = new();

            for (int i = 0; i < (int)ExMetaMethod.LAST; i++)
            {
                CreateMetaMethod(i);
            }

            DictDelegate.Assign(CreateDefDel(this, DictDelegateFuncs));
            ClassDelegate.Assign(CreateDefDel(this, ClassDelegateFuncs));
            ListDelegate.Assign(CreateDefDel(this, ListDelegateFuncs));
            NumberDelegate.Assign(CreateDefDel(this, NumberDelegateFuncs));
            ComplexDelegate.Assign(CreateDefDel(this, ComplexDelegateFuncs));
            StringDelegate.Assign(CreateDefDel(this, StringDelegateFuncs));
            ClosureDelegate.Assign(CreateDefDel(this, ClosureDelegateFuncs));
            InstanceDelegate.Assign(CreateDefDel(this, InstanceDelegateFuncs));
            WeakRefDelegate.Assign(CreateDefDel(this, WeakRefDelegateFuncs));
        }

        private static void SetNativeClosureDelegateSettings(ExNativeClosure cls, ExRegFunc f)
        {
            cls.Name = new(f.Name);
            cls.nParameterChecks = f.NumberOfParameters;
            cls.DefaultValues = ExApi.GetDefaultValuesFromParameters(f.Parameters);
            cls.IsDelegateFunction = true;
            cls.Documentation = ExApi.CreateDocStringFromRegFunc(f, false); // TO-DO : Hack, what happens if we want vargs in delegates ?
            cls.Summary = f.Description;
            cls.Returns = f.Returns;
        }

        public static ExObject CreateDefDel(ExSState exs, List<ExRegFunc> f)
        {
            int i = 0;
            Dictionary<string, ExObject> d = new();
            while (f[i].Name != string.Empty)
            {
                f[i].IsDelegateFunction = true;

                ExNativeClosure cls = ExNativeClosure.Create(exs, f[i].Function, 0);

                if (!string.IsNullOrEmpty(f[i].ParameterMask)
                    && !ExApi.CompileTypeMask(f[i].ParameterMask, cls.TypeMasks))
                {
                    return new(); // Shouldn't happen
                }

                SetNativeClosureDelegateSettings(cls, f[i]);

                if (!exs.Strings.ContainsKey(f[i].Name))
                {
                    exs.Strings.Add(f[i].Name, new(f[i].Name));
                }

                d.Add(f[i++].Name, new(cls));
            }
            return new(d);
        }

        public int GetMetaIdx(string mname)
        {
            return MetaMethodsMap != null && MetaMethodsMap.GetDict() != null && MetaMethodsMap.GetDict().ContainsKey(mname) ? (int)MetaMethodsMap.GetDict()[mname].GetInt() : -1;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Root = null;

                    Disposer.DisposeObjects(ConstructorID,
                                            MetaMethodsMap,
                                            WeakRefDelegate,
                                            InstanceDelegate,
                                            ClosureDelegate,
                                            StringDelegate,
                                            NumberDelegate,
                                            ComplexDelegate,
                                            ListDelegate,
                                            DictDelegate,
                                            ClassDelegate);

                    Disposer.DisposeDict(ref Strings);

                    Disposer.DisposeList(ref MetaMethods);
                    Disposer.DisposeList(ref WeakRefDelegateFuncs);
                    Disposer.DisposeList(ref InstanceDelegateFuncs);
                    Disposer.DisposeList(ref ClosureDelegateFuncs);
                    Disposer.DisposeList(ref StringDelegateFuncs);
                    Disposer.DisposeList(ref NumberDelegateFuncs);
                    Disposer.DisposeList(ref ComplexDelegateFuncs);
                    Disposer.DisposeList(ref ListDelegateFuncs);
                    Disposer.DisposeList(ref DictDelegateFuncs);
                    Disposer.DisposeList(ref ClassDelegateFuncs);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
