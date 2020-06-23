using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Assets.MaterialVariant.Editor
{
    [CustomEditor(typeof(MaterialVariantImporter))]
    public class MaterialVariantEditor : ScriptedImporterEditor
    {
        private static Color k_OverrideMarginColor = new Color(1f / 255f, 153f / 255f, 235f / 255f, 0.75f);

        private UnityEditor.Editor targetEditor = null;
        private Object aTarget = null;

        private void InitEditor()
        {
            targetEditor = CreateEditor(aTarget);
            //targetEditor.firstInspectedEditor = true;
            //targetEditor.rootOverride = ((AssetVariant)extraDataTarget).root;
            //targetEditor.onHeaderControlsGUIOverride += MyControlGUI;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            aTarget = AssetDatabase.LoadAssetAtPath<Material>(((AssetImporter)target).assetPath);

            InitEditor();
        }

        /*
        private void EditorGuiUtilityOnBeginProperty(Rect position, SerializedProperty property)
        {
            if (Event.current.type == EventType.Repaint && property.prefabOverride)
            {
                Rect highlightRect = position;
                highlightRect.xMin += EditorGUI.indentLevel * 15f;
                Color oldColor = GUI.backgroundColor;
                bool oldEnabled = GUI.enabled;
                GUI.enabled = true;

                GUI.backgroundColor = k_OverrideMarginColor;
                highlightRect.x = 0;
                highlightRect.width = 2;
                var style = GUI.skin.FindStyle("OverrideMargin");
                style.Draw(highlightRect, false, false, false, false);

                GUI.enabled = oldEnabled;
                GUI.backgroundColor = oldColor;
                //EditorGUIUtility.SetBoldDefaultFont(true);
            }
        }
        */

        public override void OnDisable()
        {
            DestroyImmediate(targetEditor);
            base.OnDisable();
        }

        protected override void OnHeaderGUI()
        {
            targetEditor.DrawHeader();
        }

        public override void OnInspectorGUI()
        {
            targetEditor.OnInspectorGUI();
        }
    }
}
