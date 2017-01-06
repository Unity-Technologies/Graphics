using System;

namespace UnityEngine.Experimental.Rendering
{
    public abstract class RenderPipeline : IRenderPipeline
    {
        public virtual void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            if (disposed)
                throw new ObjectDisposedException(string.Format("{0} has been disposed. Do not call Render on disposed RenderLoops.", this));
        }
        
        public bool disposed { get; private set; }
        
        public virtual void Dispose()
        {
            disposed = true;
        }
    }
}
