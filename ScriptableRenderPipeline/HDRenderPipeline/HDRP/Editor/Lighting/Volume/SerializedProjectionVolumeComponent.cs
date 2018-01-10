using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedProjectionVolumeComponent
    {
        public SerializedObject serializedObject;

        public SerializedProjectionVolume projectionVolume;

        public SerializedProjectionVolumeComponent(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            projectionVolume = new SerializedProjectionVolume(serializedObject.Find((ProxyVolumeComponent c) => c.projectionVolume));
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
