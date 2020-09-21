using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(ForwardRendererData), true)]
    [MovedFrom("UnityEditor.Rendering.LWRP")] public class ForwardRendererDataEditor : ScriptableRendererDataEditor
    {
        private static class Styles
        {
            public static readonly GUIContent RendererTitle = new GUIContent("Forward Renderer", "Custom Forward Renderer for Universal RP.");
            public static readonly GUIContent PostProcessLabel = new GUIContent("Post Process Data", "The asset containing references to shaders and Textures that the Renderer uses for post-processing.");
            public static readonly GUIContent FilteringLabel = new GUIContent("Filtering", "Controls filter rendering settings for this renderer.");
            public static readonly GUIContent OpaqueLabel = new GUIContent("Opaque", "");
            public static readonly GUIContent OpaqueMask = new GUIContent("Layer Mask", "Controls which opaque layers this renderer draws.");
            public static readonly GUIContent OpaqueRenderingLayerMask = new GUIContent("Rendering Layer Mask", "Controls which opaque rendering layers this renderer draws.");
            public static readonly GUIContent TransparentLabel = new GUIContent("Transparent", "");
            public static readonly GUIContent TransparentMask = new GUIContent("Layer Mask", "Controls which transparent rendering layers this renderer draws.");
            public static readonly GUIContent TransparentRenderingLayerMask = new GUIContent("Rendering Layer Mask", "Controls which transparent layers this renderer draws.");
            public static readonly GUIContent LightingLabel = new GUIContent("Lighting", "Settings related to lighting and rendering paths.");
            public static readonly GUIContent RenderingModeLabel = new GUIContent("Rendering Path", "Select a rendering path.");
            public static readonly GUIContent accurateGbufferNormalsLabel = EditorGUIUtility.TrTextContent("Accurate G-buffer normals", "Normals in G-buffer use octahedron encoding/decoding. This improves visual quality but might reduce performance.");
            //public static readonly GUIContent tiledDeferredShadingLabel = EditorGUIUtility.TrTextContent("Tiled Deferred Shading (Experimental)", "Allows Tiled Deferred Shading on appropriate lights");
            public static readonly GUIContent defaultStencilStateLabel = EditorGUIUtility.TrTextContent("Default Stencil State", "Configure the stencil state for the opaque and transparent render passes.");
            public static readonly GUIContent shadowTransparentReceiveLabel = EditorGUIUtility.TrTextContent("Transparent Receive Shadows", "When disabled, none of the transparent objects will receive shadows.");
            public static readonly GUIContent invalidStencilOverride = EditorGUIUtility.TrTextContent("Error: When using the deferred rendering path, the Renderer requires the control over the 4 highest bits of the stencil buffer to store Material types. The current combination of the stencil override options prevents the Renderer from controlling the required bits. Try changing one of the options to Replace.");
        }

        SerializedProperty m_OpaqueLayerMask;
        SerializedProperty m_OpaqueRenderingLayerMask;
        SerializedProperty m_TransparentLayerMask;
        SerializedProperty m_TransparentRenderingLayerMask;
#if ENABLE_RENDERING_PATH_UI
        SerializedProperty m_RenderingMode;
        SerializedProperty m_AccurateGbufferNormals;
        //SerializedProperty m_TiledDeferredShading;
#endif
        SerializedProperty m_DefaultStencilState;
        SerializedProperty m_PostProcessData;
        SerializedProperty m_Shaders;
        SerializedProperty m_ShadowTransparentReceiveProp;

        private void OnEnable()
        {
            m_OpaqueLayerMask = serializedObject.FindProperty("m_OpaqueLayerMask");
            m_OpaqueRenderingLayerMask = serializedObject.FindProperty("m_OpaqueRenderingLayerMask");
            m_TransparentLayerMask = serializedObject.FindProperty("m_TransparentLayerMask");
            m_TransparentRenderingLayerMask = serializedObject.FindProperty("m_TransparentRenderingLayerMask");
#if ENABLE_RENDERING_PATH_UI
            m_RenderingMode = serializedObject.FindProperty("m_RenderingMode");
            m_AccurateGbufferNormals = serializedObject.FindProperty("m_AccurateGbufferNormals");
            // Not exposed yet.
            //m_TiledDeferredShading = serializedObject.FindProperty("m_TiledDeferredShading");
#endif
            m_DefaultStencilState = serializedObject.FindProperty("m_DefaultStencilState");
            m_PostProcessData = serializedObject.FindProperty("postProcessData");
            m_Shaders = serializedObject.FindProperty("shaders");
            m_ShadowTransparentReceiveProp = serializedObject.FindProperty("m_ShadowTransparentReceive");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            RenderPipelineAsset asset = UniversalRenderPipeline.asset;
            string[] layerNames = asset != null ? asset.renderingLayerMaskNames : EditorUtils.defaultRenderingLayerNames;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.RendererTitle, EditorStyles.boldLabel); // Title
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_PostProcessData, Styles.PostProcessLabel);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Styles.FilteringLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField(Styles.OpaqueLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_OpaqueLayerMask, Styles.OpaqueMask);
            m_OpaqueRenderingLayerMask.longValue = (uint) EditorGUILayout.MaskField(Styles.OpaqueRenderingLayerMask, m_OpaqueRenderingLayerMask.intValue, layerNames);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.TransparentLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_TransparentLayerMask, Styles.TransparentMask);
            m_TransparentRenderingLayerMask.longValue = (uint)EditorGUILayout.MaskField(Styles.TransparentRenderingLayerMask, m_TransparentRenderingLayerMask.intValue, layerNames);
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

#if ENABLE_RENDERING_PATH_UI
            EditorGUILayout.LabelField(Styles.LightingLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_RenderingMode, Styles.RenderingModeLabel);
            if (m_RenderingMode.intValue == (int)RenderingMode.Deferred)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_AccurateGbufferNormals, Styles.accurateGbufferNormalsLabel, true);
                //EditorGUILayout.PropertyField(m_TiledDeferredShading, Styles.tiledDeferredShadingLabel, true);
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
#endif
            EditorGUILayout.LabelField("Shadows", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_ShadowTransparentReceiveProp, Styles.shadowTransparentReceiveLabel);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Overrides", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_DefaultStencilState, Styles.defaultStencilStateLabel, true);
            SerializedProperty overrideStencil = m_DefaultStencilState.FindPropertyRelative("overrideStencilState");
#if ENABLE_RENDERING_PATH_UI
            if (overrideStencil.boolValue && m_RenderingMode.intValue == (int)RenderingMode.Deferred)
            {
                CompareFunction stencilFunction = (CompareFunction)m_DefaultStencilState.FindPropertyRelative("stencilCompareFunction").enumValueIndex;
                StencilOp stencilPass = (StencilOp)m_DefaultStencilState.FindPropertyRelative("passOperation").enumValueIndex;
                StencilOp stencilFail = (StencilOp)m_DefaultStencilState.FindPropertyRelative("failOperation").enumValueIndex;
                StencilOp stencilZFail = (StencilOp)m_DefaultStencilState.FindPropertyRelative("zFailOperation").enumValueIndex;
                bool invalidFunction = stencilFunction == CompareFunction.Disabled || stencilFunction == CompareFunction.Never;
                bool invalidOp = stencilPass != StencilOp.Replace && stencilFail != StencilOp.Replace && stencilZFail != StencilOp.Replace;

                if (invalidFunction || invalidOp)
                    EditorGUILayout.HelpBox(Styles.invalidStencilOverride.text, MessageType.Error, true);
            }
#endif
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();

            base.OnInspectorGUI(); // Draw the base UI, contains ScriptableRenderFeatures list

            // Add a "Reload All" button in inspector when we are in developer's mode
            if (EditorPrefs.GetBool("DeveloperMode"))
            {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(m_Shaders, true);

                if (GUILayout.Button("Reload All"))
                {
                    var resources = target as ForwardRendererData;
                    resources.shaders = null;
                    ResourceReloader.ReloadAllNullIn(target, UniversalRenderPipelineAsset.packagePath);
                }
            }
        }
    }
}
