using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

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
    /// This attribute tells the <see cref="VolumeComponentEditor"/> class which type of
    /// <see cref="VolumeComponent"/> it is an editor for. It is used to associate a custom editor
    /// with a specific volume component, enabling the editor to handle its custom properties and settings.
    /// </summary>
    /// <remarks>
    /// When creating a custom editor for a <see cref="VolumeComponent"/>, this attribute must be applied
    /// to the editor class to ensure it targets the appropriate component. This functionality has been deprecated,
    /// and developers are encouraged to use the <see cref="CustomEditor"/> attribute instead for newer versions.
    ///
    /// The attribute specifies which <see cref="VolumeComponent"/> type the editor class is responsible for.
    /// Typically, it is used in conjunction with custom editor UI drawing and handling logic for the specified volume component.
    /// This provides a way for developers to create custom editing tools for their volume components in the Unity Inspector.
    ///
    /// Since Unity 2022.2, this functionality has been replaced by the <see cref="CustomEditor"/> attribute, and as such,
    /// it is advised to update any existing custom editors to use the newer approach.
    /// </remarks>
    /// <seealso cref="VolumeComponentEditor"/>
    /// <seealso cref="CustomEditor"/>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    [Obsolete("VolumeComponentEditor property has been deprecated. Please use CustomEditor. #from(2022.2)")]
    public sealed class VolumeComponentEditorAttribute : CustomEditor
    {
        /// <summary>
        /// A type derived from <see cref="VolumeComponent"/> that this editor is responsible for.
        /// </summary>
        /// <remarks>
        /// This field holds the type of the volume component that the editor class will handle.
        /// The type should be a subclass of <see cref="VolumeComponent"/> and is used to associate the editor
        /// with the specific component type.
        /// </remarks>
        public readonly Type componentType;

        /// <summary>
        /// Creates a new <see cref="VolumeComponentEditorAttribute"/> instance.
        /// </summary>
        /// <param name="componentType">A type derived from <see cref="VolumeComponent"/> that the editor is responsible for.</param>
        /// <remarks>
        /// This constructor initializes the attribute with the component type that the editor will target.
        /// The component type is a subclass of <see cref="VolumeComponent"/> and provides the necessary
        /// context for the editor class to function properly within the Unity Editor.
        /// </remarks>
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

    public abstract partial class DefaultVolumeProfileSettingsPropertyDrawer
    {        
        /// <summary>
        /// Context menu implementation for Default Volume Profile.
        /// </summary>
        /// <typeparam name="TSetting">Default Volume Profile Settings type</typeparam>
        /// <typeparam name="TRenderPipeline">Render Pipeline type</typeparam>
        [Obsolete("Use DefaultVolumeProfileSettingsPropertyDrawer<T>.DefaultVolumeProfileSettingsContextMenu2<TSetting, TRenderPipeline> instead #from(6000.0)")]
        public abstract class DefaultVolumeProfileSettingsContextMenu<TSetting, TRenderPipeline> : IRenderPipelineGraphicsSettingsContextMenu<TSetting>
            where TSetting : class, IDefaultVolumeProfileSettings
            where TRenderPipeline : RenderPipeline
        {
            /// <summary>
            /// Path where new Default Volume Profile will be created.
            /// </summary>
            [Obsolete("Not used anymore. #from(6000.0)")]
            protected abstract string defaultVolumeProfilePath { get; }

            [Obsolete("Not used anymore. #from(6000.0)")]
            void IRenderPipelineGraphicsSettingsContextMenu<TSetting>.PopulateContextMenu(TSetting setting, PropertyDrawer property, ref GenericMenu menu){ }
        }
    }

    /// <summary>
    /// Builtin Drawer for Maskfield Debug Items.
    /// </summary>
    [DebugUIDrawer(typeof(DebugUI.MaskField))]
    [Obsolete("DebugUI.MaskField has been deprecated and is not longer supported, please use BitField instead. #from(6000.2)", false)]
    public sealed class DebugUIDrawerMaskField : DebugUIFieldDrawer<uint, DebugUI.MaskField, DebugStateUInt>
    {
        /// <summary>
        /// Does the field of the given type
        /// </summary>
        /// <param name="rect">The rect to draw the field</param>
        /// <param name="label">The label for the field</param>
        /// <param name="field">The field</param>
        /// <param name="state">The state</param>
        /// <returns>The current value from the UI</returns>
        protected override uint DoGUI(Rect rect, GUIContent label, DebugUI.MaskField field, DebugStateUInt state)
        {
            uint value = field.GetValue();

            var enumNames = new string[field.enumNames.Length];
            for (int i = 0; i < enumNames.Length; i++)
                enumNames[i] = field.enumNames[i].text;
            var mask = EditorGUI.MaskField(rect, label, (int)value, enumNames);

            return (uint)mask;
        }
    }
}
