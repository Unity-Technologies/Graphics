using UnityEditor.Build;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
    // RenderTargetHandle can be thought of as a kind of ShaderProperty string hash
    [MovedFrom("UnityEngine.Rendering.LWRP")] public struct RenderTargetHandle
    {
        public int id { set; get; }

        public AttachmentDescriptor targetDescriptor;

        public RenderTargetIdentifier identifier { set; get; }
        public static readonly RenderTargetHandle CameraTarget = new RenderTargetHandle {id = -1, identifier = BuiltinRenderTextureType.CameraTarget};

        public void Init(string shaderProperty)
        {
            // Shader.PropertyToID returns what is internally referred to as a "ShaderLab::FastPropertyName".
            // It is a value coming from an internal global std::map<char*,int> that converts shader property strings into unique integer handles (that are faster to work with).
            id = Shader.PropertyToID(shaderProperty);
            identifier = new RenderTargetIdentifier(id);
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
