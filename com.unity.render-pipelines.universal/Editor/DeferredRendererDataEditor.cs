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
            public static readonly GUIContent OpaqueMask = new GUIContent("Default Layer Mask", "Controls which layers to globally include in the Custom Deferred Renderer.");
            public static readonly GUIContent defaultStencilStateLabel = EditorGUIUtility.TrTextContent("Default Stencil State", "Configure stencil state for the opaque and transparent render passes.");
            public static readonly GUIContent shadowTransparentReceiveLabel = EditorGUIUtility.TrTextContent("Transparent Receive Shadows", "When disabled, none of the transparent objects will receive shadows.");
            public static readonly GUIContent accurateGbufferNormalsLabel = EditorGUIUtility.TrTextContent("Accurate G-buffer normals", "normals in G-buffer use octaedron encoding/decoding (expensive)");
            public static readonly GUIContent tiledDeferredShadingLabel = EditorGUIUtility.TrTextContent("Tiled Deferred Shading", "Allows Tiled Deferred Shading on appropriate lights");
        }

        SerializedProperty m_OpaqueLayerMask;
        SerializedProperty m_TransparentLayerMask;
        SerializedProperty m_DefaultStencilState;
        SerializedProperty m_PostProcessData;
        SerializedProperty m_Shaders;
        SerializedProperty m_ShadowTransparentReceiveProp;
        SerializedProperty m_AccurateGbufferNormals;
        SerializedProperty m_TiledDeferredShading;

        private void OnEnable()
        {
            m_OpaqueLayerMask = serializedObject.FindProperty("m_OpaqueLayerMask");
            m_TransparentLayerMask = serializedObject.FindProperty("m_TransparentLayerMask");
            m_DefaultStencilState = serializedObject.FindProperty("m_DefaultStencilState");
            m_PostProcessData = serializedObject.FindProperty("postProcessData");
            m_Shaders = serializedObject.FindProperty("shaders");
            m_ShadowTransparentReceiveProp = serializedObject.FindProperty("m_ShadowTransparentReceive");
            m_AccurateGbufferNormals = serializedObject.FindProperty("m_AccurateGbufferNormals");
            m_TiledDeferredShading = serializedObject.FindProperty("m_TiledDeferredShading");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.RendererTitle, EditorStyles.boldLabel); // Title
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_OpaqueLayerMask, Styles.OpaqueMask);
            if (EditorGUI.EndChangeCheck()) // We copy the opaque mask to the transparent mask, later we might expose both
                m_TransparentLayerMask.intValue = m_OpaqueLayerMask.intValue;
            EditorGUILayout.PropertyField(m_PostProcessData);
            EditorGUILayout.PropertyField(m_AccurateGbufferNormals, Styles.accurateGbufferNormalsLabel, true);
            EditorGUILayout.PropertyField(m_TiledDeferredShading, Styles.tiledDeferredShadingLabel, true);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Shadows", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_ShadowTransparentReceiveProp, Styles.shadowTransparentReceiveLabel);
            EditorGUILayout.Space();

            /* // Currently not supported for deferred renderer.
            EditorGUILayout.LabelField("Overrides", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_DefaultStencilState, Styles.defaultStencilStateLabel, true);
            EditorGUILayout.Space();
            */

            serializedObject.ApplyModifiedProperties();

            base.OnInspectorGUI(); // Draw the base UI, contains ScriptableRenderFeatures list

            // Add a "Reload All" button in inspector when we are in developer's mode
            if (EditorPrefs.GetBool("DeveloperMode"))
            {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(m_Shaders, true);

                if (GUILayout.Button("Reload All"))
                {
                    var resources = target as DeferredRendererData;
                    resources.shaders = null;
                    ResourceReloader.ReloadAllNullIn(target, UniversalRenderPipelineAsset.packagePath);
                }
            }
        }
    }
}
