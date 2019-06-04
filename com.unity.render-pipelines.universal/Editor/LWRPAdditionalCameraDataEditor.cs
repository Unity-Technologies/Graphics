using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Rendering.LWRP
{
    [CanEditMultipleObjects]
    // Disable the GUI for additional camera data
    [CustomEditor(typeof(LWRPAdditionalCameraData))]
    class AdditionalCameraDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
        }
        [MenuItem("CONTEXT/LWRPAdditionalCameraData/Remove Component")]
        static void RemoveComponent(MenuCommand command)
        {
            if (EditorUtility.DisplayDialog("Remove Component?", "Are you sure you want to remove this component? If you do, you will lose some settings.", "Remove", "Cancel"))
            {
                Undo.DestroyObjectImmediate(command.context);
            }
        }
    }
}
