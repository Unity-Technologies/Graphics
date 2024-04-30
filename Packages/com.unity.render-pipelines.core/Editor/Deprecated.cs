using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Callback method that will be called when the Global Preferences for Additional Properties is changed
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [Obsolete("This attribute is not handled anymore. Use Advanced Properties. #from(6000.0)", false)]
    public sealed class SetAdditionalPropertiesVisibilityAttribute : Attribute
    {
    }

    /// <summary>
    /// This attributes tells a <see cref="VolumeComponentEditor"/> class which type of
    /// <see cref="VolumeComponent"/> it's an editor for.
    /// When you make a custom editor for a component, you need put this attribute on the editor
    /// class.
    /// </summary>
    /// <seealso cref="VolumeComponentEditor"/>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    [Obsolete("VolumeComponentEditor property has been deprecated. Please use CustomEditor. #from(2022.2)")]
    public sealed class VolumeComponentEditorAttribute : CustomEditor
    {
        /// <summary>
        /// A type derived from <see cref="VolumeComponent"/>.
        /// </summary>
        public readonly Type componentType;

        /// <summary>
        /// Creates a new <see cref="VolumeComponentEditorAttribute"/> instance.
        /// </summary>
        /// <param name="componentType">A type derived from <see cref="VolumeComponent"/></param>
        public VolumeComponentEditorAttribute(Type componentType)
            : base(componentType, true)
        {
            this.componentType = componentType;
        }
    }

    /// <summary>
    /// Interface that should be used with [ScriptableRenderPipelineExtension(type))] attribute to dispatch ContextualMenu calls on the different SRPs
    /// </summary>
    /// <typeparam name="T">This must be a component that require AdditionalData in your SRP</typeparam>
    [Obsolete("The menu items are handled automatically for components with the AdditionalComponentData attribute. #from(2022.2)", false)]
    public interface IRemoveAdditionalDataContextualMenu<T>
        where T : Component
    {
        /// <summary>
        /// Remove the given component
        /// </summary>
        /// <param name="component">The component to remove</param>
        /// <param name="dependencies">Dependencies.</param>
        void RemoveComponent(T component, IEnumerable<Component> dependencies);
    }

    public static partial class RenderPipelineGlobalSettingsUI
    {
        /// <summary>A collection of GUIContent for use in the inspector</summary>
        [Obsolete("Use ShaderStrippingSettings instead. #from(23.2).")]
        public static class Styles
        {
            /// <summary>
            /// Global label width
            /// </summary>
            public const int labelWidth = 250;

            /// <summary>
            /// Shader Stripping
            /// </summary>
            public static readonly GUIContent shaderStrippingSettingsLabel = EditorGUIUtility.TrTextContent("Shader Stripping", "Shader Stripping settings");

            /// <summary>
            /// Shader Variant Log Level
            /// </summary>
            public static readonly GUIContent shaderVariantLogLevelLabel = EditorGUIUtility.TrTextContent("Shader Variant Log Level", "Controls the level of logging of shader variant information outputted during the build process. Information appears in the Unity Console when the build finishes.");

            /// <summary>
            /// Export Shader Variants
            /// </summary>
            public static readonly GUIContent exportShaderVariantsLabel = EditorGUIUtility.TrTextContent("Export Shader Variants", "Controls whether to output shader variant information to a file.");

            /// <summary>
            /// Stripping Of Rendering Debugger Shader Variants is enabled
            /// </summary>
            public static readonly GUIContent stripRuntimeDebugShadersLabel = EditorGUIUtility.TrTextContent("Strip Runtime Debug Shaders", "When enabled, all debug display shader variants are removed when you build for the Unity Player. This decreases build time, but disables some features of Rendering Debugger in Player builds.");
        }

        /// <summary>
        /// Draws the shader stripping settinsg
        /// </summary>
        /// <param name="serialized">The serialized global settings</param>
        /// <param name="owner">The owner editor</param>
        /// <param name="additionalShaderStrippingSettings">Pass another drawer if you want to specify additional shader stripping settings</param>
        [Obsolete("Use ShaderStrippingSettings instead. #from(23.2).")]
        public static void DrawShaderStrippingSettings(ISerializedRenderPipelineGlobalSettings serialized, Editor owner, CoreEditorDrawer<ISerializedRenderPipelineGlobalSettings>.IDrawer additionalShaderStrippingSettings = null)
        {
            CoreEditorUtils.DrawSectionHeader(Styles.shaderStrippingSettingsLabel);

            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;

            EditorGUILayout.Space();
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serialized.shaderVariantLogLevel, Styles.shaderVariantLogLevelLabel);
                EditorGUILayout.PropertyField(serialized.exportShaderVariants, Styles.exportShaderVariantsLabel);
                EditorGUILayout.PropertyField(serialized.stripDebugVariants, Styles.stripRuntimeDebugShadersLabel);

                additionalShaderStrippingSettings?.Draw(serialized, owner);
            }
            EditorGUILayout.Space();
            EditorGUIUtility.labelWidth = oldWidth;
        }
    }

    /// <summary>
    /// Public interface for handling a serialized object of <see cref="UnityEngine.Rendering.RenderPipelineGlobalSettings"/>
    /// </summary>
    [Obsolete("Use ShaderStrippingSettings instead. #from(23.2).")]
    public interface ISerializedRenderPipelineGlobalSettings
    {
        /// <summary>
        /// The <see cref="SerializedObject"/>
        /// </summary>
        SerializedObject serializedObject { get; }

        /// <summary>
        /// The shader variant log level
        /// </summary>
        SerializedProperty shaderVariantLogLevel { get; }

        /// <summary>
        /// If the shader variants needs to be exported
        /// </summary>
        SerializedProperty exportShaderVariants { get; }

        /// <summary>
        /// If the Runtime Rendering Debugger Debug Variants should be stripped
        /// </summary>
        SerializedProperty stripDebugVariants { get => null; }
    }

    public sealed partial class DefaultVolumeProfileEditor
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="baseEditor">Editor that displays the content of this class</param>
        /// <param name="profile">VolumeProfile to display</param>
        [Obsolete("Use DefaultVolumeProfileEditor(VolumeProfile, SerializedObject) instead. #from(23.3)")]
        public DefaultVolumeProfileEditor(Editor baseEditor, VolumeProfile profile)
        {
            m_Profile = profile;
            m_TargetSerializedObject = baseEditor.serializedObject;
        }
    }
}
