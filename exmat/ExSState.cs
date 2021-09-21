using System;
using System.Collections.Generic;
using System.Linq;
using ExMat.API;
using ExMat.Closure;
using ExMat.Objects;
using ExMat.VM;

namespace ExMat.States
{
    /// <summary>
    /// Shared state model to keep track of common values
    /// </summary>
    public class ExSState : IDisposable
    {
        /// <summary>
        /// Owner VM
        /// </summary>
        // Kullanılan ana sanal makine
        public ExVM Root;

        /// <summary>
        /// List of meta method names
        /// </summary>
        // Meta metotların listesi
        public List<ExObject> MetaMethods = new();

        /// <summary>
        /// Meta method names mapped to <see cref="ExMetaMethod"/> integer values
        /// </summary>
        // Meta metot isimleri tablosu
        public ExObject MetaMethodsMap = new(new Dictionary<string, ExObject>());

        /// <summary>
        /// Class constructor method name
        /// </summary>
        // Sınıfların inşa edici metot ismi
        public ExObject ConstructorID = new(ExMat.ConstructorName);

        /// <summary>
        /// String literals and the objects they are stored in
        /// </summary>
        // Kullanılan yazı dizileri ve değişken isimleri
        public Dictionary<string, ExObject> Strings = new();

        /// <summary>
        /// Constant names and their values
        /// </summary>
        // Sabitler
        public Dictionary<string, ExObject> Consts = new();

        /// <summary>
        /// <see cref="ExObjType.CLASS"/> type object delegates
        /// </summary>
        // Sınıf temisili metotları
        public ExObject ClassDelegate = new(new Dictionary<string, ExObject>());

        /// <summary>
        /// <see cref="ExObjType.DICT"/> type object delegates
        /// </summary>
        // Tablo temisili metotları
        public ExObject DictDelegate = new(new Dictionary<string, ExObject>());

        /// <summary>
        /// <see cref="ExObjType.ARRAY"/> type object delegates
        /// </summary>
        // Liste temisili metotları
        public ExObject ListDelegate = new(new Dictionary<string, ExObject>());

        /// <summary>
        /// <see cref="ExObjType.COMPLEX"/> type object delegates
        /// </summary>
        // Kompleks sayı temisili metotları
        public ExObject ComplexDelegate = new(new Dictionary<string, ExObject>());

        /// <summary>
        /// <see cref="ExObjType.INTEGER"/> or <see cref="ExObjType.FLOAT"/> type object delegates
        /// </summary>
        // Gerçek sayı temisili metotları
        public ExObject NumberDelegate = new(new Dictionary<string, ExObject>());

        /// <summary>
        /// <see cref="ExObjType.STRING"/> type object delegates
        /// </summary>
        // Yazı dizisi temisili metotları
        public ExObject StringDelegate = new(new Dictionary<string, ExObject>());

        /// <summary>
        /// <see cref="ExObjType.CLOSURE"/> type object delegates
        /// </summary>
        // Fonksiyon/kod bloğu temisili metotları
        public ExObject ClosureDelegate = new(new Dictionary<string, ExObject>());

        /// <summary>
        /// <see cref="ExObjType.INSTANCE"/> type object delegates
        /// </summary>
        // Sınıfa ait obje temisili metotları
        public ExObject InstanceDelegate = new(new Dictionary<string, ExObject>());

        /// <summary>
        /// <see cref="ExObjType.WEAKREF"/> type object delegates
        /// </summary>
        // Zayıf referans temisili metotları
        public ExObject WeakRefDelegate = new(new Dictionary<string, ExObject>());

        private bool disposedValue;

        private void CreateMetaMethod(int i)
        {
            string mname = "_" + ((ExMetaMethod)i).ToString();

            MetaMethods.Add(new(mname));
            MetaMethodsMap.GetDict().Add(mname, new(i));
        }

        /// <summary>
        /// Initialize the shared state
        /// </summary>
        public void Initialize()
        {
            MetaMethodsMap.ValueCustom.d_Dict = new();

            for (int i = 0; i < Enum.GetNames(typeof(ExMetaMethod)).Length; i++)
            {
                CreateMetaMethod(i);
            }

            List<ExNativeFunc> delegs = ExApi.FindDelegateNativeFunctions();

            DictDelegate.Assign(GetDelegateDictionary(this, delegs, 'd'));
            ClassDelegate.Assign(GetDelegateDictionary(this, delegs, 'y'));
            ListDelegate.Assign(GetDelegateDictionary(this, delegs, 'a'));
            NumberDelegate.Assign(GetDelegateDictionary(this, delegs, 'r'));
            ComplexDelegate.Assign(GetDelegateDictionary(this, delegs, 'C'));
            StringDelegate.Assign(GetDelegateDictionary(this, delegs, 's'));
            ClosureDelegate.Assign(GetDelegateDictionary(this, delegs, 'c'));
            InstanceDelegate.Assign(GetDelegateDictionary(this, delegs, 'x'));
            WeakRefDelegate.Assign(GetDelegateDictionary(this, delegs, 'w'));
        }

        private static ExObject GetDelegateDictionary(ExSState exS, List<ExNativeFunc> delegs, char baseMask)
        {
            return CreateDefDel(exS, new(delegs.Where(d => d.BaseTypeMask == baseMask)));
        }

        private static void SetNativeClosureDelegateSettings(ExNativeClosure cls, ExNativeFunc f)
        {
            cls.Name = new(f.Name);
            cls.nParameterChecks = f.NumberOfParameters;
            cls.DefaultValues = ExApi.GetDefaultValuesFromParameters(f.Parameters);
            cls.IsDelegateFunction = true;
            cls.Documentation = ExApi.CreateDocStringFromRegFunc(f, false); // TO-DO : Hack, what happens if we want vargs in delegates ?
            cls.Summary = f.Description;
            cls.Returns = f.ReturnsType;
            cls.Base = ((ExBaseType)ExMat.TypeMasks.FirstOrDefault(p => p.Value == f.BaseTypeMask).Key).ToString();
        }

        private static ExObject CreateDefDel(ExSState exs, List<ExNativeFunc> f)
        {
            Dictionary<string, ExObject> d = new();
            foreach (ExNativeFunc func in f)
            {
                func.IsDelegateFunction = true;

                ExNativeClosure cls = ExNativeClosure.Create(exs, func.Function, 0);

                if (!string.IsNullOrEmpty(func.ParameterMask)
                    && !ExApi.CompileTypeMask(func.ParameterMask, cls.TypeMasks))
                {
                    return new(); // Shouldn't happen
                }

                SetNativeClosureDelegateSettings(cls, func);

                if (!exs.Strings.ContainsKey(func.Name))
                {
                    exs.Strings.Add(func.Name, new(func.Name));
                }

                d.Add(func.Name, new(cls));
            }
            return new(d);
        }

        /// <summary>
        /// Get meta method index from it's name
        /// </summary>
        /// <param name="mname">Meta method name</param>
        /// <returns></returns>
        public int GetMetaIdx(string mname)
        {
            return MetaMethodsMap != null && MetaMethodsMap.GetDict() != null && MetaMethodsMap.GetDict().ContainsKey(mname) ? (int)MetaMethodsMap.GetDict()[mname].GetInt() : -1;
        }

        internal virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Root = null;

                    ExDisposer.DisposeObjects(ConstructorID,
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

                    ExDisposer.DisposeDict(ref Strings);

                    ExDisposer.DisposeList(ref MetaMethods);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        /// <summary>
        /// Disposer
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
