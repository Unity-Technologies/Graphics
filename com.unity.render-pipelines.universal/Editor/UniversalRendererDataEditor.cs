using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(UniversalRendererData), true)]
    public class UniversalRendererDataEditor : ScriptableRendererDataEditor
    {
        private static class Styles
        {
            public static readonly GUIContent RendererTitle = new GUIContent("Universal Renderer", "Custom Universal Renderer for Universal RP.");
            public static readonly GUIContent PostProcessIncluded = EditorGUIUtility.TrTextContent("Enabled", "Turns post-processing on (check box selected) or off (check box cleared). If you clear this check box, Unity excludes post-processing render Passes, shaders, and textures from the build.");
            public static readonly GUIContent PostProcessLabel = new GUIContent("Data", "The asset containing references to shaders and Textures that the Renderer uses for post-processing.");
            public static readonly GUIContent FilteringLabel = new GUIContent("Filtering", "Controls filter rendering settings for this renderer.");
            public static readonly GUIContent OpaqueMask = new GUIContent("Opaque Layer Mask", "Controls which opaque layers this renderer draws.");
            public static readonly GUIContent TransparentMask = new GUIContent("Transparent Layer Mask", "Controls which transparent layers this renderer draws.");
            public static readonly GUIContent RenderingLabel = new GUIContent("Rendering", "Settings related to rendering and lighting.");
            public static readonly GUIContent RenderingModeLabel = new GUIContent("Rendering Path", "Select a rendering path.");
            public static readonly GUIContent DepthPrimingModeLabel = new GUIContent("Depth Priming Mode", "With depth priming enabled, Unity uses the depth buffer generated in the depth prepass to determine if a fragment should be rendered or skipped during the Base Camera opaque pass. Disabled: Unity does not perform depth priming. Auto: If there is a Render Pass that requires a depth prepass, Unity performs the depth prepass and depth priming. Forced: Unity performs the depth prepass and depth priming.");
            public static readonly GUIContent DepthPrimingModeInfo = new GUIContent("On Android, iOS, and Apple TV, Unity performs depth priming only in the Forced mode. On tiled GPUs, which are common to those platforms, depth priming might reduce performance when combined with MSAA.");
            public static readonly GUIContent RenderPassLabel = new GUIContent("Native RenderPass", "Enables URP to use RenderPass API. Has no effect on OpenGLES2");
            public static readonly GUIContent accurateGbufferNormalsLabel = EditorGUIUtility.TrTextContent("Accurate G-buffer normals", "Normals in G-buffer use octahedron encoding/decoding. This improves visual quality but might reduce performance.");
            //public static readonly GUIContent tiledDeferredShadingLabel = EditorGUIUtility.TrTextContent("Tiled Deferred Shading (Experimental)", "Allows Tiled Deferred Shading on appropriate lights");
            public static readonly GUIContent defaultStencilStateLabel = EditorGUIUtility.TrTextContent("Default Stencil State", "Configure the stencil state for the opaque and transparent render passes.");
            public static readonly GUIContent shadowTransparentReceiveLabel = EditorGUIUtility.TrTextContent("Transparent Receive Shadows", "When disabled, none of the transparent objects will receive shadows.");
            public static readonly GUIContent invalidStencilOverride = EditorGUIUtility.TrTextContent("Error: When using the deferred rendering path, the Renderer requires the control over the 4 highest bits of the stencil buffer to store Material types. The current combination of the stencil override options prevents the Renderer from controlling the required bits. Try changing one of the options to Replace.");
            public static readonly GUIContent clusteredRenderingLabel = EditorGUIUtility.TrTextContent("Clustered (experimental)", "(Experimental) Enables clustered rendering, allowing for more lights per object and more accurate light cullling.");
        }

        SerializedProperty m_OpaqueLayerMask;
        SerializedProperty m_TransparentLayerMask;
        SerializedProperty m_RenderingMode;
        SerializedProperty m_DepthPrimingMode;
        SerializedProperty m_AccurateGbufferNormals;
        //SerializedProperty m_TiledDeferredShading;
        SerializedProperty m_ClusteredRendering;
        SerializedProperty m_TileSize;
        SerializedProperty m_UseNativeRenderPass;
        SerializedProperty m_DefaultStencilState;
        SerializedProperty m_PostProcessData;
        SerializedProperty m_Shaders;
        SerializedProperty m_ShadowTransparentReceiveProp;

#if URP_ENABLE_CLUSTERED_UI
        static bool s_EnableClusteredUI => true;
#else
        static bool s_EnableClusteredUI => false;
#endif

        private void OnEnable()
        {
            m_OpaqueLayerMask = serializedObject.FindProperty("m_OpaqueLayerMask");
            m_TransparentLayerMask = serializedObject.FindProperty("m_TransparentLayerMask");
            m_RenderingMode = serializedObject.FindProperty("m_RenderingMode");
            m_DepthPrimingMode = serializedObject.FindProperty("m_DepthPrimingMode");
            m_AccurateGbufferNormals = serializedObject.FindProperty("m_AccurateGbufferNormals");
            // Not exposed yet.
            //m_TiledDeferredShading = serializedObject.FindProperty("m_TiledDeferredShading");
            m_ClusteredRendering = serializedObject.FindProperty("m_ClusteredRendering");
            m_TileSize = serializedObject.FindProperty("m_TileSize");
            m_UseNativeRenderPass = serializedObject.FindProperty("m_UseNativeRenderPass");
            m_DefaultStencilState = serializedObject.FindProperty("m_DefaultStencilState");
            m_PostProcessData = serializedObject.FindProperty("postProcessData");
            m_Shaders = serializedObject.FindProperty("shaders");
            m_ShadowTransparentReceiveProp = serializedObject.FindProperty("m_ShadowTransparentReceive");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Styles.FilteringLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_OpaqueLayerMask, Styles.OpaqueMask);
            EditorGUILayout.PropertyField(m_TransparentLayerMask, Styles.TransparentMask);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Styles.RenderingLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_RenderingMode, Styles.RenderingModeLabel);
            if (m_RenderingMode.intValue == (int)RenderingMode.Deferred)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_AccurateGbufferNormals, Styles.accurateGbufferNormalsLabel, true);
                //EditorGUILayout.PropertyField(m_TiledDeferredShading, Styles.tiledDeferredShadingLabel, true);
                EditorGUI.indentLevel--;
            }

            if (m_RenderingMode.intValue == (int)RenderingMode.Forward)
            {
                EditorGUI.indentLevel++;

                if (s_EnableClusteredUI)
                {
                    EditorGUILayout.PropertyField(m_ClusteredRendering, Styles.clusteredRenderingLabel);
                    EditorGUI.BeginDisabledGroup(!m_ClusteredRendering.boolValue);
                    EditorGUILayout.PropertyField(m_TileSize);
                    EditorGUI.EndDisabledGroup();
                }

                EditorGUILayout.PropertyField(m_DepthPrimingMode, Styles.DepthPrimingModeLabel);
                if (m_DepthPrimingMode.intValue != (int)DepthPrimingMode.Disabled)
                {
                    EditorGUILayout.HelpBox(Styles.DepthPrimingModeInfo.text, MessageType.Info);
                }

                EditorGUI.indentLevel--;
            }


            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("RenderPass", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_UseNativeRenderPass, Styles.RenderPassLabel);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shadows", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_ShadowTransparentReceiveProp, Styles.shadowTransparentReceiveLabel);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Post-processing", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            var postProcessIncluded = EditorGUILayout.Toggle(Styles.PostProcessIncluded, m_PostProcessData.objectReferenceValue != null);
            if (EditorGUI.EndChangeCheck())
            {
                m_PostProcessData.objectReferenceValue = postProcessIncluded ? PostProcessData.GetDefaultPostProcessData() : null;
            }
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_PostProcessData, Styles.PostProcessLabel);
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Overrides", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_DefaultStencilState, Styles.defaultStencilStateLabel, true);
            SerializedProperty overrideStencil = m_DefaultStencilState.FindPropertyRelative("overrideStencilState");

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
                    var resources = target as UniversalRendererData;
                    resources.shaders = null;
                    ResourceReloader.ReloadAllNullIn(target, UniversalRenderPipelineAsset.packagePath);
                }
            }
        }
    }
}
