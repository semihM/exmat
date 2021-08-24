namespace ExMat.Objects
{
    public class ExWeakRef : ExRefC
    {
        public ExObject ReferencedObject;   // Zayıf referans edilen obje
        private bool disposedValue;

        public virtual void Release()
        {
            if (((int)ReferencedObject.Type & (int)ExObjFlag.COUNTREFERENCES) != 0)
            {
                ReferencedObject.Value._WeakRef = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disposer.DisposeObjects(ReferencedObject);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

    }
}
