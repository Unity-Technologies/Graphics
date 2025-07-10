
using System;

namespace UnityEngine.Rendering.UnifiedRayTracing
{

    internal enum UnifiedRayTracingError
    {
        Unknown,
        GraphicsBufferAllocationFailed
    }

    internal class UnifiedRayTracingException : Exception
    {
        public UnifiedRayTracingException(string message, UnifiedRayTracingError errorCode)
            : base(message)
        {
            this.errorCode = errorCode;
        }

        public UnifiedRayTracingError errorCode { get; private set; }
    }

}


