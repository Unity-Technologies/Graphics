using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class SerializedPlanarReflectionProbe
    {
        public SerializedObject serializedObject;

        public SerializedProperty proxyVolumeReference;
        public SerializedInfluenceVolume influenceVolume;

        public SerializedProperty captureOffset;
        public SerializedProperty dimmer;
        public SerializedProperty mode;
        public SerializedProperty refreshMode;
        public SerializedProperty customTexture;

        public PlanarReflectionProbe target { get { return serializedObject.targetObject as PlanarReflectionProbe; } }

        public SerializedPlanarReflectionProbe(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            proxyVolumeReference = serializedObject.Find((PlanarReflectionProbe p) => p.proxyVolumeReference);
            influenceVolume = new SerializedInfluenceVolume(serializedObject.Find((PlanarReflectionProbe p) => p.influenceVolume));

            captureOffset = serializedObject.Find((PlanarReflectionProbe p) => p.centerOffset);
            dimmer = serializedObject.Find((PlanarReflectionProbe p) => p.dimmer);
            mode = serializedObject.Find((PlanarReflectionProbe p) => p.mode);
            refreshMode = serializedObject.Find((PlanarReflectionProbe p) => p.refreshMode);
            customTexture = serializedObject.Find((PlanarReflectionProbe p) => p.customTexture);
        }

        public void Update()
        {
            serializedObject.Update();
        }

        public void Apply()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
