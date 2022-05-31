using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
    // RenderTargetHandle can be thought of as a kind of ShaderProperty string hash
    [Obsolete("Deprecated in favor of RTHandle")]
    public struct RenderTargetHandle
    {
        public int id { set; get; }
        private RenderTargetIdentifier rtid { set; get; }

        public static readonly RenderTargetHandle CameraTarget = new RenderTargetHandle { id = -1 };

        public RenderTargetHandle(RenderTargetIdentifier renderTargetIdentifier)
        {
            id = -2;
            rtid = renderTargetIdentifier;
        }

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

        public void Init(string shaderProperty)
        {
            // Shader.PropertyToID returns what is internally referred to as a "ShaderLab::FastPropertyName".
            // It is a value coming from an internal global std::map<char*,int> that converts shader property strings into unique integer handles (that are faster to work with).
            id = Shader.PropertyToID(shaderProperty);
        }

        public void Init(RenderTargetIdentifier renderTargetIdentifier)
        {
            id = -2;
            rtid = renderTargetIdentifier;
        }

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

        public bool HasInternalRenderTargetId()
        {
            return id == -2;
        }

        public bool Equals(RenderTargetHandle other)
        {
            if (id == -2 || other.id == -2)
                return Identifier() == other.Identifier();
            return id == other.id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is RenderTargetHandle && Equals((RenderTargetHandle)obj);
        }

        public override int GetHashCode()
        {
            return id;
        }

        public static bool operator ==(RenderTargetHandle c1, RenderTargetHandle c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(RenderTargetHandle c1, RenderTargetHandle c2)
        {
            return !c1.Equals(c2);
        }
    }
}
