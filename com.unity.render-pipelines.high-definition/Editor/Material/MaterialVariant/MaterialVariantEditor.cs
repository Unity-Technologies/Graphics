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
        private UnityEditor.Editor targetEditor = null;

        protected override Type extraDataType => typeof(MaterialVariant);
        protected override bool needsApplyRevert => true;
        public override bool showImportedObject => false;

        public override void OnEnable()
        {
            base.OnEnable();

            targetEditor = CreateEditor(assetTarget);
        }

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
          //  extraDataSerializedObject.Update();
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                targetEditor.OnInspectorGUI();
                if (changed.changed)
                {
                    Apply();
                }
            }

            ApplyRevertGUI();
        }

        protected override void InitializeExtraDataInstance(Object extraData, int targetIndex)
        {
            var importer = targets[targetIndex] as MaterialVariantImporter;
            var assets = InternalEditorUtility.LoadSerializedFileAndForget(importer.assetPath);
            EditorUtility.CopySerialized(assets[0], extraData);
        }

        protected override void Apply()
        {
            base.Apply();

            InternalEditorUtility.SaveToSerializedFileAndForget(new[] { extraDataTarget }, (target as MaterialVariantImporter).assetPath, true);
            AssetDatabase.ImportAsset((target as MaterialVariantImporter).assetPath);
        }
    }
}
