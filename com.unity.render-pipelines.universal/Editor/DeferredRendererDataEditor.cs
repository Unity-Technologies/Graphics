using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(DeferredRendererData), true)]
    [MovedFrom("UnityEditor.Rendering.LWRP")] public class DeferredRendererDataEditor : ScriptableRendererDataEditor
    {
        private static class Styles
        {
            public static readonly GUIContent RendererTitle = new GUIContent("Deferred Renderer", "Custom Deferred Renderer for UniversalRP.");
            public static readonly GUIContent FilteringLabel = new GUIContent("Filtering", "Controls filter rendering settings for this renderer.");
            public static readonly GUIContent OpaqueMask = new GUIContent("Opaque Layer Mask", "Controls which opaque layers this renderer draws.");
            public static readonly GUIContent TransparentMask = new GUIContent("Transparent Layer Mask", "Controls which transparent layers this renderer draws.");
            public static readonly GUIContent defaultStencilStateLabel = EditorGUIUtility.TrTextContent("Default Stencil State", "Configure stencil state for the opaque and transparent render passes.");
            public static readonly GUIContent shadowTransparentReceiveLabel = EditorGUIUtility.TrTextContent("Transparent Receive Shadows", "When disabled, none of the transparent objects will receive shadows.");
            public static readonly GUIContent preferDepthPrepassLabel = EditorGUIUtility.TrTextContent("Prefer Depth Prepass", "Some platform configurations may still enable depth-prepass even when this checkbox is disabled.");
            public static readonly GUIContent accurateGbufferNormalsLabel = EditorGUIUtility.TrTextContent("Accurate G-buffer normals", "normals in G-buffer use octaedron encoding/decoding (expensive)");
            public static readonly GUIContent tiledDeferredShadingLabel = EditorGUIUtility.TrTextContent("Tiled Deferred Shading (Experimental)", "Allows Tiled Deferred Shading on appropriate lights");
            public static readonly GUIContent invalidStencilOverride = EditorGUIUtility.TrTextContent("Error: Deferred Renderer requires the control over the 4 highest bits of the stencil buffer to store Material types. The current combination of the stencil override options prevents the Renderer from controlling the required bits. Try changing one of the options to Replace.");
        }

        SerializedProperty m_OpaqueLayerMask;
        SerializedProperty m_TransparentLayerMask;
        SerializedProperty m_DefaultStencilState;
        SerializedProperty m_PostProcessData;
        SerializedProperty m_Shaders;
        SerializedProperty m_ShadowTransparentReceiveProp;
        SerializedProperty m_PreferDepthPrepass;
        SerializedProperty m_AccurateGbufferNormals;
        //SerializedProperty m_TiledDeferredShading;

        private void OnEnable()
        {
            m_OpaqueLayerMask = serializedObject.FindProperty("m_OpaqueLayerMask");
            m_TransparentLayerMask = serializedObject.FindProperty("m_TransparentLayerMask");
            m_DefaultStencilState = serializedObject.FindProperty("m_DefaultStencilState");
            m_PostProcessData = serializedObject.FindProperty("postProcessData");
            m_Shaders = serializedObject.FindProperty("shaders");
            m_ShadowTransparentReceiveProp = serializedObject.FindProperty("m_ShadowTransparentReceive");
            m_PreferDepthPrepass = serializedObject.FindProperty("m_PreferDepthPrepass");
            m_AccurateGbufferNormals = serializedObject.FindProperty("m_AccurateGbufferNormals");
            // Not exposed yet.
            //m_TiledDeferredShading = serializedObject.FindProperty("m_TiledDeferredShading");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.RendererTitle, EditorStyles.boldLabel); // Title
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_PostProcessData);
            EditorGUILayout.PropertyField(m_PreferDepthPrepass, Styles.preferDepthPrepassLabel, true);
            EditorGUILayout.PropertyField(m_AccurateGbufferNormals, Styles.accurateGbufferNormalsLabel, true);
            //EditorGUILayout.PropertyField(m_TiledDeferredShading, Styles.tiledDeferredShadingLabel, true);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Styles.FilteringLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_OpaqueLayerMask, Styles.OpaqueMask);
            EditorGUILayout.PropertyField(m_TransparentLayerMask, Styles.TransparentMask);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Shadows", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_ShadowTransparentReceiveProp, Styles.shadowTransparentReceiveLabel);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Overrides", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_DefaultStencilState, Styles.defaultStencilStateLabel, true);
            SerializedProperty overrideStencil = m_DefaultStencilState.FindPropertyRelative("overrideStencilState");
            if (overrideStencil.boolValue)
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
                    var resources = target as ForwardRendererData;
                    resources.shaders = null;
                    ResourceReloader.ReloadAllNullIn(target, UniversalRenderPipelineAsset.packagePath);
                }
            }
        }
    }
}
