
using System;

namespace UnityEngine.Rendering.UnifiedRayTracing
{
    /// <summary>
    /// Represents errors that can occur during UnifiedRayTracing calls.
    /// </summary>
    public enum UnifiedRayTracingError
    {
        /// <summary>Unknown error.</summary>
        Unknown,
        /// <summary>Graphics Buffer allocation failed. It happens usually when the GPU runs out of memory. You can try to reduce your mesh data or your number of instances.</summary>
        GraphicsBufferAllocationFailed
    }

    /// <summary>
    /// Exception type that can be thrown in case of failure in UnifiedRayTracing calls.
    /// </summary>
    public class UnifiedRayTracingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UnifiedRayTracingException"/>
        /// </summary>
        /// <param name="message">Message describing the error.</param>
        /// <param name="errorCode">The error code.</param>
        public UnifiedRayTracingException(string message, UnifiedRayTracingError errorCode)
            : base(message)
        {
            this.errorCode = errorCode;
        }

        /// <summary>
        /// Gets the <see cref="UnifiedRayTracingError"/> code associated with the exception.
        /// </summary>
        public UnifiedRayTracingError errorCode { get; private set; }
    }

}


