using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedProjectionVolume
    {
        public SerializedProperty root;

        public SerializedProperty shapeType;
        public SerializedProperty boxSize;
        public SerializedProperty boxOffset;
        public SerializedProperty boxInfiniteProjection;
        public SerializedProperty sphereRadius;
        public SerializedProperty sphereOffset;
        public SerializedProperty sphereInfiniteProjection;

        public SerializedProjectionVolume(SerializedProperty root)
        {
            this.root = root;

            shapeType = root.Find((ProjectionVolume p) => p.shapeType);
            boxSize = root.Find((ProjectionVolume p) => p.boxSize);
            boxOffset = root.Find((ProjectionVolume p) => p.boxOffset);
            boxInfiniteProjection = root.Find((ProjectionVolume p) => p.boxInfiniteProjection);
            sphereRadius = root.Find((ProjectionVolume p) => p.sphereRadius);
            sphereOffset = root.Find((ProjectionVolume p) => p.sphereOffset);
            sphereInfiniteProjection = root.Find((ProjectionVolume p) => p.sphereInfiniteProjection);
        }
    }
}
