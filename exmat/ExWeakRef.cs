namespace ExMat.Objects
{
    public class ExWeakRef : ExRefC
    {
        public ExObject ReferencedObject;   // Zayıf referans edilen obje
        private bool disposedValue;

        protected override void Dispose(bool disposing)
        {
            if (ReferenceCount > 0)
            {
                return;
            }
            base.Dispose(disposing);
            if (!disposedValue)
            {
                if (disposing)
                {
                    ExDisposer.DisposeObjects(ReferencedObject);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

    }
}
