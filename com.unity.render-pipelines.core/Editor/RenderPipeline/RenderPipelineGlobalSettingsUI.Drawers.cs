namespace UnityEditor.Rendering
{
    /// <summary>
    /// UI for global settings
    /// </summary>
    public static partial class RenderPipelineGlobalSettingsUI
    {
        /// <summary>
        /// Draws the shader stripping settinsg
        /// </summary>
        /// <param name="serialized">The serialized global settings</param>
        /// <param name="owner">The owner editor</param>
        /// <param name="additionalShaderStrippingSettings">Pass another drawer if you want to specify additional shader stripping settings</param>
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
                additionalShaderStrippingSettings?.Draw(serialized, owner);
            }
            EditorGUILayout.Space();
            EditorGUIUtility.labelWidth = oldWidth;
        }
    }
}
