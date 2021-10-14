#if VFX_HAS_TIMELINE
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    //[CustomEditor(typeof(VisualEffectControlPlayableAsset))]
    class VisualEffectControlPlayableAssetInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Here, below, there will be settings of events...", MessageType.Warning);
        }
    }
}
#endif
