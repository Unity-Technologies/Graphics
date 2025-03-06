using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Internal
{
    /// <summary>
    /// Contains a database of built-in resource GUIds. These are used to load built-in resource files.
    /// </summary>
    public static class ResourceGuid
    {
        /// <summary>
        /// GUId for the <c>ScriptableRendererFeature</c> template file.
        /// </summary>
        public static readonly string rendererTemplate = "51493ed8d97d3c24b94c6cffe834630b";
    }
}

namespace UnityEditor.Rendering.Universal
{
    static partial class EditorUtils
    {
        internal enum Unit { Metric, Percent }

        internal class Styles
        {
            //Measurements
            public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            public static readonly GUIContent alembicMotionVectors = EditorGUIUtility.TrTextContent("Alembic Motion Vectors",
                "When enabled, the material will use motion vectors from the Alembic animation cache. Should not be used on regular meshes or Alembic caches without precomputed motion vectors.");

            public static readonly GUIContent xrMotionVectorsPass = EditorGUIUtility.TrTextContent("XR Motion Vectors Pass (Space Warp)",
                "When enabled, the material will run the XR motion vectors pass to be compatible with Space Warp.");
        }

        internal static void FeatureHelpBox(string message, MessageType type)
        {
            CoreEditorUtils.DrawFixMeBox(message, type, "Open", () =>
            {
                EditorUtility.OpenPropertyEditor(UniversalRenderPipeline.asset.scriptableRendererData);
                GUIUtility.ExitGUI();
            });
        }

        internal static void QualitySettingsHelpBox(string message, MessageType type, UniversalRenderPipelineAssetUI.Expandable expandable, string propertyPath)
        {
            CoreEditorUtils.DrawFixMeBox(message, type, "Open", () =>
            {
                var currentPipeline = UniversalRenderPipeline.asset;

                // Make sure we open a new window if the user has already selected Open
                var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();

                if (windows.Length != 0)
                {
                    foreach (var window in windows)
                    {
                        if (currentPipeline.name.Equals(window.titleContent.text))
                            window.Close();
                    }
                }

                EditorUtility.OpenPropertyEditor(currentPipeline);
                UniversalRenderPipelineAssetUI.Expand(expandable, true);

                EditorApplication.delayCall += () =>
                    EditorApplication.delayCall += () =>
                        CoreEditorUtils.Highlight(currentPipeline.name, propertyPath, HighlightSearchMode.Identifier);
            });
        }
    }
}
