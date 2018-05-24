using UnityEditor.AnimatedValues;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<ShadowInitParametersUI, SerializedShadowInitParameters>;

    class ShadowInitParametersUI : BaseUI<SerializedShadowInitParameters>
    {
        public static readonly CED.IDrawer SectionAtlas = CED.Action(Drawer_FieldShadowSize);

        public ShadowInitParametersUI()
            : base(0)
        {
            
        }

        static void Drawer_FieldShadowSize(ShadowInitParametersUI s, SerializedShadowInitParameters d, Editor o)
        {
            EditorGUILayout.LabelField(_.GetContent("Shadow Atlas Settings"), EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(d.shadowAtlasWidth, _.GetContent("Atlas Width"));
            EditorGUILayout.PropertyField(d.shadowAtlasHeight, _.GetContent("Atlas Height"));
            --EditorGUI.indentLevel;
        }
    }
}
