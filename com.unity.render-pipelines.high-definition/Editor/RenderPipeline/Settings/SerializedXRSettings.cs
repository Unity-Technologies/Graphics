using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedXRSettings
    {
        public SerializedProperty root;

        public SerializedProperty singlePass;
        public SerializedProperty occlusionMesh;
        public SerializedProperty cameraJitter;

        public SerializedXRSettings(SerializedProperty root)
        {
            this.root = root;

            singlePass = root.Find((GlobalXRSettings s) => s.singlePass);
            occlusionMesh = root.Find((GlobalXRSettings s) => s.occlusionMesh);
            cameraJitter = root.Find((GlobalXRSettings s) => s.cameraJitter);
        }
    }
}
