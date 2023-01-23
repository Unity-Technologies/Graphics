using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class for render target handles in URP.
    /// Deprecated in favor of RTHandle.
    /// </summary>
    // RenderTargetHandle can be thought of as a kind of ShaderProperty string hash
    [Obsolete("Deprecated in favor of RTHandle")] // TODO OBSOLETE: need to fix the URP test failures when bumping
    public struct RenderTargetHandle
    {
        /// <summary>
        /// The ID of the handle for the handle.
        /// </summary>
        public int id { set; get; }

        /// <summary>
        /// The render target ID for the handle.
        /// </summary>
        private RenderTargetIdentifier rtid { set; get; }

        /// <summary>
        /// The render target handle for the Camera target.
        /// </summary>
        public static readonly RenderTargetHandle CameraTarget = new RenderTargetHandle { id = -1 };

        /// <summary>
        /// Constructor for a render target handle.
        /// </summary>
        /// <param name="renderTargetIdentifier">The render target ID for the new handle.</param>
        public RenderTargetHandle(RenderTargetIdentifier renderTargetIdentifier)
        {
            id = -2;
            rtid = renderTargetIdentifier;
        }

        /// <summary>
        /// Constructor for a render target handle.
        /// </summary>
        /// <param name="rtHandle">The rt handle for the new handle.</param>
        public RenderTargetHandle(RTHandle rtHandle)
        {
            if (rtHandle.nameID == BuiltinRenderTextureType.CameraTarget)
                id = -1;
            else if (rtHandle.name.Length == 0)
                id = -2;
            else
                id = Shader.PropertyToID(rtHandle.name);
            rtid = rtHandle.nameID;
            if (rtHandle.rt != null && id != rtid)
                id = -2;
        }

        internal static RenderTargetHandle GetCameraTarget(ref CameraData cameraData)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
                return new RenderTargetHandle(cameraData.xr.renderTarget);
#endif

            return CameraTarget;
        }

        /// <summary>
        /// Initializes the ID for the handle.
        /// </summary>
        /// <param name="shaderProperty">The shader property to initialize with.</param>
        public void Init(string shaderProperty)
        {
            // Shader.PropertyToID returns what is internally referred to as a "ShaderLab::FastPropertyName".
            // It is a value coming from an internal global std::map<char*,int> that converts shader property strings into unique integer handles (that are faster to work with).
            id = Shader.PropertyToID(shaderProperty);
        }

        /// <summary>
        /// Initializes the render target ID for the handle.
        /// </summary>
        /// <param name="renderTargetIdentifier">The render target ID to initialize with.</param>
        public void Init(RenderTargetIdentifier renderTargetIdentifier)
        {
            id = -2;
            rtid = renderTargetIdentifier;
        }

        /// <summary>
        /// The render target ID for this render target handle.
        /// </summary>
        /// <returns>The render target ID for this render target handle.</returns>
        public RenderTargetIdentifier Identifier()
        {
            if (id == -1)
            {
                return BuiltinRenderTextureType.CameraTarget;
            }
            if (id == -2)
            {
                return rtid;
            }
            return new RenderTargetIdentifier(id, 0, CubemapFace.Unknown, -1);
        }

        /// <summary>
        /// Does this handle have internal render target ID?
        /// </summary>
        /// <returns>True if it has internal render target ID.</returns>
        public bool HasInternalRenderTargetId()
        {
            return id == -2;
        }

        /// <summary>
        /// Equality check with another render target handle.
        /// </summary>
        /// <param name="other">Other render target handle to compare with.</param>
        /// <returns>True if the handles have the same ID, otherwise false.</returns>
        public bool Equals(RenderTargetHandle other)
        {
            if (id == -2 || other.id == -2)
                return Identifier() == other.Identifier();
            return id == other.id;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is RenderTargetHandle && Equals((RenderTargetHandle)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return id;
        }

        /// <summary>
        /// Equality check between two render target handles.
        /// </summary>
        /// <param name="c1">First handle for the check.</param>
        /// <param name="c2">Second handle for the check.</param>
        /// <returns>True if the handles have the same ID, otherwise false.</returns>
        public static bool operator ==(RenderTargetHandle c1, RenderTargetHandle c2)
        {
            return c1.Equals(c2);
        }

        /// <summary>
        /// Equality check between two render target handles.
        /// </summary>
        /// <param name="c1">First handle for the check.</param>
        /// <param name="c2">Second handle for the check.</param>
        /// <returns>True if the handles do not have the same ID, otherwise false.</returns>
        public static bool operator !=(RenderTargetHandle c1, RenderTargetHandle c2)
        {
            return !c1.Equals(c2);
        }
    }
}
