using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering
{
#pragma warning disable CS0618  // Type or member is obsolete
    [CustomEditor(typeof(Projector))]
    [CanEditMultipleObjects]
    class ProjectorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GraphicsSettings.currentRenderPipelineAssetType == typeof(HDRenderPipelineAsset))
            {
                const int offsetToMatchWarning = 16;
                Rect buttonRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                buttonRect.x -= offsetToMatchWarning;
                buttonRect.width += offsetToMatchWarning;

                if (GUI.Button(buttonRect,"Add HDRP Decal Projector component") && serializedObject.targetObject is Projector lensFlare)
                {
                    lensFlare.gameObject.AddComponent<DecalProjector>();
                    EditorSceneManager.MarkSceneDirty(lensFlare.gameObject.scene);
                }
            }

            DrawDefaultInspector();
        }
    }
}
