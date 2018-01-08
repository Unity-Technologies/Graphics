using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class SerializedInfluenceVolume
    {
        public SerializedProperty root;

        public SerializedProperty shapeType;
        public SerializedProperty boxBaseSize;
        public SerializedProperty boxBaseOffset;
        public SerializedProperty boxInfluencePositiveFade;
        public SerializedProperty boxInfluenceNegativeFade;
        public SerializedProperty boxInfluenceNormalPositiveFade;
        public SerializedProperty boxInfluenceNormalNegativeFade;
        public SerializedProperty sphereBaseRadius;
        public SerializedProperty sphereBaseOffset;
        public SerializedProperty sphereInfluenceRadius;
        public SerializedProperty sphereInfluenceNormalRadius;

        public SerializedInfluenceVolume(SerializedProperty root)
        {
            this.root = root;

            shapeType = root.Find((InfluenceVolume i) => i.shapeType);
            boxBaseSize = root.Find((InfluenceVolume i) => i.boxBaseSize);
            boxBaseOffset = root.Find((InfluenceVolume i) => i.boxBaseOffset);
            boxInfluencePositiveFade = root.Find((InfluenceVolume i) => i.boxInfluencePositiveFade);
            boxInfluenceNegativeFade = root.Find((InfluenceVolume i) => i.boxInfluenceNegativeFade);
            boxInfluenceNormalPositiveFade = root.Find((InfluenceVolume i) => i.boxInfluenceNormalPositiveFade);
            boxInfluenceNormalNegativeFade = root.Find((InfluenceVolume i) => i.boxInfluenceNormalNegativeFade);
            sphereBaseRadius = root.Find((InfluenceVolume i) => i.sphereBaseRadius);
            sphereBaseOffset = root.Find((InfluenceVolume i) => i.sphereBaseOffset);
            sphereInfluenceRadius = root.Find((InfluenceVolume i) => i.sphereInfluenceRadius);
            sphereInfluenceNormalRadius = root.Find((InfluenceVolume i) => i.sphereInfluenceNormalRadius);
        }
    }
}
