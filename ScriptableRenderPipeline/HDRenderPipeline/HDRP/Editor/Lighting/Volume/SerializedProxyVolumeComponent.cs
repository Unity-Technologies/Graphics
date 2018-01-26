using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class SerializedProxyVolumeComponent
    {
        public SerializedObject serializedObject;

        public SerializedProxyVolume proxyVolume;

        public SerializedProxyVolumeComponent(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            proxyVolume = new SerializedProxyVolume(serializedObject.Find((ReflectionProxyVolumeComponent c) => c.proxyVolume));
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
