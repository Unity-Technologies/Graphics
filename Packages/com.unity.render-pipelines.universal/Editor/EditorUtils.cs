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
        }

        internal static void FeatureHelpBox(string message, MessageType type)
        {
            CoreEditorUtils.DrawFixMeBox(message, type, "Open", () =>
            {
                EditorUtility.OpenPropertyEditor(UniversalRenderPipeline.asset.scriptableRendererData);
                GUIUtility.ExitGUI();
            });
        }

        internal static void QualitySettingsHelpBox(string message, MessageType type, string propertyPath)
        {
            CoreEditorUtils.DrawFixMeBox(message, type, "Open", () =>
            {
                var currentPipeline = UniversalRenderPipeline.asset;
                EditorUtility.OpenPropertyEditor(currentPipeline);

                CoreEditorUtils.Highlight(currentPipeline.name, propertyPath, HighlightSearchMode.Identifier);
                GUIUtility.ExitGUI();
            });
        }

        internal static void DrawRenderingLayerMask(SerializedProperty property, GUIContent style)
        {
            var renderingLayer = property.uintValue;

            EditorGUI.BeginChangeCheck();
            renderingLayer = EditorGUILayout.RenderingLayerMaskField(style, renderingLayer);

            if (EditorGUI.EndChangeCheck())
                property.uintValue = renderingLayer;
        }
    }
}
