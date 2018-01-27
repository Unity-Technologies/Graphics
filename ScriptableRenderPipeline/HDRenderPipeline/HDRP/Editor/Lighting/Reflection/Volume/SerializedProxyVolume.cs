using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class SerializedProxyVolume
    {
        public SerializedProperty root;

        public SerializedProperty shapeType;
        public SerializedProperty boxSize;
        public SerializedProperty boxOffset;
        public SerializedProperty boxInfiniteProjection;
        public SerializedProperty sphereRadius;
        public SerializedProperty sphereOffset;
        public SerializedProperty sphereInfiniteProjection;

        public SerializedProxyVolume(SerializedProperty root)
        {
            this.root = root;

            shapeType = root.Find((ProxyVolume p) => p.shapeType);
            boxSize = root.Find((ProxyVolume p) => p.boxSize);
            boxOffset = root.Find((ProxyVolume p) => p.boxOffset);
            boxInfiniteProjection = root.Find((ProxyVolume p) => p.boxInfiniteProjection);
            sphereRadius = root.Find((ProxyVolume p) => p.sphereRadius);
            sphereOffset = root.Find((ProxyVolume p) => p.sphereOffset);
            sphereInfiniteProjection = root.Find((ProxyVolume p) => p.sphereInfiniteProjection);
        }
    }
}
