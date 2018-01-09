using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class SerializedPlanarReflectionProbe
    {
        public SerializedObject serializedObject;

        public SerializedProperty projectionVolumeReference;
        public SerializedInfluenceVolume influenceVolume;

        public SerializedPlanarReflectionProbe(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            projectionVolumeReference = serializedObject.Find((PlanarReflectionProbe p) => p.projectionVolumeReference);
            influenceVolume = new SerializedInfluenceVolume(serializedObject.Find((PlanarReflectionProbe p) => p.influenceVolume));
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
