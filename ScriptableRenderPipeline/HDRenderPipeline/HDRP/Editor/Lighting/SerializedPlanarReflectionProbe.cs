using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

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

        public SerializedPlanarReflectionProbe(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            proxyVolumeReference = serializedObject.Find((PlanarReflectionProbe p) => p.proxyVolumeReference);
            influenceVolume = new SerializedInfluenceVolume(serializedObject.Find((PlanarReflectionProbe p) => p.influenceVolume));

            captureOffset = serializedObject.Find((PlanarReflectionProbe p) => p.centerOffset);
            dimmer = serializedObject.Find((PlanarReflectionProbe p) => p.dimmer);
            mode = serializedObject.Find((PlanarReflectionProbe p) => p.mode);
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
