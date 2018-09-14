namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<GlobalDecalSettingsUI, SerializedGlobalDecalSettings>;

    class GlobalDecalSettingsUI : BaseUI<SerializedGlobalDecalSettings>
    {
        static GlobalDecalSettingsUI()
        {
            Inspector = CED.Group(SectionDecalSettings);
        }

        public static readonly CED.IDrawer Inspector;

        public static readonly CED.IDrawer SectionDecalSettings = CED.Action(Drawer_SectionDecalSettings);

        public GlobalDecalSettingsUI()
            : base(0)
        {
        }

        static void Drawer_SectionDecalSettings(GlobalDecalSettingsUI s, SerializedGlobalDecalSettings d, Editor o)
        {
            EditorGUILayout.LabelField(_.GetContent("Decals"), EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(d.drawDistance, _.GetContent("Draw Distance"));
            EditorGUILayout.PropertyField(d.atlasWidth, _.GetContent("Atlas Width"));
            EditorGUILayout.PropertyField(d.atlasHeight, _.GetContent("Atlas Height"));
            EditorGUILayout.PropertyField(d.perChannelMask, _.GetContent("Enable Metal and AO properties"));
            --EditorGUI.indentLevel;
        }
    }
}
