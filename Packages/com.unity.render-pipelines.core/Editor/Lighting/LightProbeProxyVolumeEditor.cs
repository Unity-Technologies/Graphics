using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
#pragma warning disable CS0618  // Type or member is obsolete
    [CustomEditor(typeof(LightProbeProxyVolume))]
    [CanEditMultipleObjects]
    class LightProbeProxyVolumeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GraphicsSettings.isScriptableRenderPipelineEnabled)
            {
                const int offsetToMatchWarning = 16;
                Rect buttonRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                buttonRect.x -= offsetToMatchWarning;
                buttonRect.width += offsetToMatchWarning;

                if (GUI.Button(buttonRect, "Add Adaptive Probe Volume component") && serializedObject.targetObject is LightProbeProxyVolume proxy)
                {
                    proxy.gameObject.AddComponent<ProbeVolume>();
                    EditorSceneManager.MarkSceneDirty(proxy.gameObject.scene);
                }
            }

            DrawDefaultInspector();
        }
    }
}
