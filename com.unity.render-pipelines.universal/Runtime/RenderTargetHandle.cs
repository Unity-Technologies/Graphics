using UnityEngine.Experimental.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
    [MovedFrom("UnityEngine.Rendering.LWRP")] public struct RenderTargetHandle
    {
        public int id { set; get; }

        public AttachmentDescriptor targetDescriptor;

        public static readonly RenderTargetHandle CameraTarget = new RenderTargetHandle {id = -1};

        public RenderTargetIdentifier identifier { set; get; }

        public void Init(string shaderProperty)
        {
            id = Shader.PropertyToID(shaderProperty);
            identifier = new RenderTargetIdentifier(id);
        }

        public void InitDescriptor(GraphicsFormat format)
        {
           // targetDescriptor = new AttachmentDescriptor(, Identifier(), false, true);
        }

        public void InitDescriptor(RenderTextureFormat format)
        {
            targetDescriptor = new AttachmentDescriptor(format);
        }

        public RenderTargetIdentifier Identifier()
        {
            if (id == -1)
            {
                return BuiltinRenderTextureType.CameraTarget;
            }

            return identifier;
        }

        public bool Equals(RenderTargetHandle other)
        {
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

        public static bool operator==(RenderTargetHandle c1, RenderTargetHandle c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator!=(RenderTargetHandle c1, RenderTargetHandle c2)
        {
            return !c1.Equals(c2);
        }
    }
}
