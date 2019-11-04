using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedXRSettings
    {
        public SerializedProperty root;

        public SerializedProperty occlusionMesh;

        public SerializedXRSettings(SerializedProperty root)
        {
            this.root = root;

            occlusionMesh = root.Find((GlobalXRSettings s) => s.occlusionMesh);
        }
    }
}
