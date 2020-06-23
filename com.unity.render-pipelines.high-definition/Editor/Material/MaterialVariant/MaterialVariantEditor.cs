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
            targetEditor.OnInspectorGUI();

            ApplyRevertGUI();
        }
    }
}
