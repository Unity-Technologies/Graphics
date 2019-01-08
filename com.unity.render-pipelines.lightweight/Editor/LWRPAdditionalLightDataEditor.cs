using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Rendering.LWRP
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(LWRPAdditionalLightData))]
    public class LWRPAdditionLightDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
        }

        [MenuItem("CONTEXT/LWRPAdditionalLightData/Remove Component")]
        static void RemoveComponent(MenuCommand command)
        {
            if (EditorUtility.DisplayDialog("Remove Component?", "Are you sure you want to remove this component? If you do, you will lose some settings.", "Remove", "Cancel"))
            {
                Undo.DestroyObjectImmediate(command.context);
            }
        }
    }
}
