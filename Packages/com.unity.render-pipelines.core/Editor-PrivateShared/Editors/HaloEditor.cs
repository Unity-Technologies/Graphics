using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
#pragma warning disable CS0618  // Type or member is obsolete
    [CustomEditor(typeof(Halo))]
    [CanEditMultipleObjects]
    class HaloEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GraphicsSettings.isScriptableRenderPipelineEnabled)
            {
                const int offsetToMatchWarning = 16;
                Rect buttonRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                buttonRect.x -= offsetToMatchWarning;
                buttonRect.width += offsetToMatchWarning;

                if (GUI.Button(buttonRect, "Add Lens Flare (SRP) component") && serializedObject.targetObject is Halo halo)
                {
                    halo.gameObject.AddComponent<LensFlareComponentSRP>();
                    EditorSceneManager.MarkSceneDirty(halo.gameObject.scene);
                }
            }

            DrawDefaultInspector();
        }
    }
}
