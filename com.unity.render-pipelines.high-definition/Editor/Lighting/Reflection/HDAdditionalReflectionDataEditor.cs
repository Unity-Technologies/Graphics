using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(HDAdditionalReflectionData))]
    [CanEditMultipleObjects]
    partial class HDAdditionalReflectionDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
        }

        [MenuItem("CONTEXT/HDAdditionalReflectionData/Remove Component")]
        static void RemoveComponent(MenuCommand command)
        {
            RemoveAdditionalDataUtils.RemoveAdditionalData<HDAdditionalReflectionData>(command);
        }
    }
}
