using System;
using System.Collections.Generic;
using System.Linq;
using ExMat.API;
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

        // Sabitler
        public Dictionary<string, ExObject> Consts = new();

        // Sınıf temisili metotları
        public ExObject ClassDelegate = new(new Dictionary<string, ExObject>());

        // Tablo temisili metotları
        public ExObject DictDelegate = new(new Dictionary<string, ExObject>());

        // Liste temisili metotları
        public ExObject ListDelegate = new(new Dictionary<string, ExObject>());

        // Kompleks sayı temisili metotları
        public ExObject ComplexDelegate = new(new Dictionary<string, ExObject>());

        // Gerçek sayı temisili metotları
        public ExObject NumberDelegate = new(new Dictionary<string, ExObject>());

        // Yazı dizisi temisili metotları
        public ExObject StringDelegate = new(new Dictionary<string, ExObject>());

        // Fonksiyon/kod bloğu temisili metotları
        public ExObject ClosureDelegate = new(new Dictionary<string, ExObject>());

        // Sınıfa ait obje temisili metotları
        public ExObject InstanceDelegate = new(new Dictionary<string, ExObject>());

        // Zayıf referans temisili metotları
        public ExObject WeakRefDelegate = new(new Dictionary<string, ExObject>());

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
        }

        public static ExObject CreateDefDel(ExSState exs, List<ExNativeFunc> f)
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
