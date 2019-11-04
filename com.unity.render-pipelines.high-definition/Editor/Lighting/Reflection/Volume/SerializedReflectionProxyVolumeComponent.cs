using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedReflectionProxyVolumeComponent
    {
        public SerializedObject serializedObject;

        public SerializedProxyVolume proxyVolume;

        public SerializedReflectionProxyVolumeComponent(SerializedObject serializedObject)
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
