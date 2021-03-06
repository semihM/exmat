namespace ExMat.Objects
{
    /// <summary>
    /// Weak reference object
    /// </summary>
    public sealed class ExWeakRef : ExRefC
    {
        /// <summary>
        /// Weakly refernced object
        /// </summary>
        public ExObject ReferencedObject;   // Zayıf referans edilen obje
        private bool disposedValue;

        /// <summary>
        /// Disposer
        /// </summary>
        /// <param name="disposing"></param>
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
