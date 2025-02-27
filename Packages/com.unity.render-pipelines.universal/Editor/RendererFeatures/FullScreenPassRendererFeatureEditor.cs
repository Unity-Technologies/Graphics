using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Custom editor for FullScreenPassRendererFeature class responsible for drawing unavailable by default properties
    /// such as custom drop down items and additional properties.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom("")]
    [CustomEditor(typeof(FullScreenPassRendererFeature))]
    public class FullScreenPassRendererFeatureEditor : Editor
    {
        private SerializedProperty m_InjectionPointProperty;
        private SerializedProperty m_RequirementsProperty;
        private SerializedProperty m_FetchColorBufferProperty;
        private SerializedProperty m_BindDepthStencilAttachmentProperty;
        private SerializedProperty m_PassMaterialProperty;
        private SerializedProperty m_PassIndexProperty;

        private static readonly GUIContent k_InjectionPointGuiContent = new GUIContent("Injection Point", "Specifies where in the frame this pass will be injected.");
        private static readonly GUIContent k_RequirementsGuiContent = new GUIContent("Requirements", "A mask of URP internal textures that will need to be generated and bound for sampling.\n\nNote that 'Color' here corresponds to '_CameraOpaqueTexture' so most of the time you will want to use the 'Fetch Color Buffer' option instead.");
        private static readonly GUIContent k_FetchColorBufferGuiContent = new GUIContent("Fetch Color Buffer", "Enable this if the assigned material will need to sample the active color target. The active color will be bound to the '_BlitTexture' shader property for sampling. Note that this will introduce an internal color copy pass.");
        private static readonly GUIContent k_BindDepthStencilAttachmentGuiContent = new GUIContent("Bind Depth-Stencil", "Enable this to bind the active camera's depth-stencil attachment to the framebuffer (only use this if depth-stencil ops are used by the assigned material as this could have a performance impact).");
        private static readonly GUIContent k_PassMaterialGuiContent = new GUIContent("Pass Material", "The material used to render the full screen pass.");
        private static readonly GUIContent k_PassGuiContent = new GUIContent("Pass", "The name of the shader pass to use from the assigned material.");

        private void OnEnable()
        {
            m_InjectionPointProperty = serializedObject.FindProperty("injectionPoint");
            m_RequirementsProperty = serializedObject.FindProperty("requirements");
            m_FetchColorBufferProperty = serializedObject.FindProperty("fetchColorBuffer");
            m_BindDepthStencilAttachmentProperty = serializedObject.FindProperty("bindDepthStencilAttachment");
            m_PassMaterialProperty = serializedObject.FindProperty("passMaterial");
            m_PassIndexProperty = serializedObject.FindProperty("passIndex");
        }

        /// <summary>
        /// Implementation for a custom inspector
        /// </summary>
        public override void OnInspectorGUI()
        {
            var currentFeature = target as FullScreenPassRendererFeature;

            if (currentFeature.passMaterial == null || currentFeature.passIndex >= currentFeature.passMaterial.passCount)
                currentFeature.passIndex = 0;

            EditorGUILayout.PropertyField(m_InjectionPointProperty, k_InjectionPointGuiContent);
            EditorGUILayout.PropertyField(m_RequirementsProperty, k_RequirementsGuiContent);
            EditorGUILayout.PropertyField(m_FetchColorBufferProperty, k_FetchColorBufferGuiContent);
            EditorGUILayout.PropertyField(m_BindDepthStencilAttachmentProperty, k_BindDepthStencilAttachmentGuiContent);
            EditorGUILayout.PropertyField(m_PassMaterialProperty, k_PassMaterialGuiContent);

            if (AdvancedProperties.BeginGroup())
            {
                DrawMaterialPassProperty(currentFeature);
            }
            AdvancedProperties.EndGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMaterialPassProperty(FullScreenPassRendererFeature feature)
        {
            List<string> selectablePasses;
            bool isMaterialValid = feature.passMaterial != null;
            selectablePasses = isMaterialValid ? GetPassIndexStringEntries(feature) : new List<string>() {"No material"};

            // If material is invalid 0'th index is selected automatically, so it stays on "No material" entry
            // It is invalid index, but FullScreenPassRendererFeature wont execute until material is valid
            m_PassIndexProperty.intValue = EditorGUILayout.Popup(k_PassGuiContent, m_PassIndexProperty.intValue, selectablePasses.ToArray());
        }

        private static List<string> GetPassIndexStringEntries(FullScreenPassRendererFeature component)
        {
            List<string> passIndexEntries = new List<string>();
            for (int i = 0; i < component.passMaterial.passCount; ++i)
            {
                // "Name of a pass (index)" - "PassAlpha (1)"
                string entry = $"{component.passMaterial.GetPassName(i)} ({i})";
                passIndexEntries.Add(entry);
            }

            return passIndexEntries;
        }
    }
}
