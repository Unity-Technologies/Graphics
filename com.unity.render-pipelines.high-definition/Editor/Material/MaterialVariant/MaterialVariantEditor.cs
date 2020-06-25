using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections.Generic;

namespace Unity.Assets.MaterialVariant.Editor
{
    [CustomEditor(typeof(MaterialVariantImporter))]
    public class MaterialVariantEditor : ScriptedImporterEditor
    {
        private UnityEditor.Editor targetEditor = null;

        protected override Type extraDataType => typeof(MaterialVariant);
        protected override bool needsApplyRevert => true;
        public override bool showImportedObject => false;
        
        protected override void InitializeExtraDataInstance(Object extraTarget, int targetIndex)
            => LoadMaterialVariant((MaterialVariant)extraTarget, ((AssetImporter)targets[targetIndex]).assetPath);
        
        void LoadMaterialVariant(MaterialVariant variantTarget, string assetPath)
        {
            var asset = MaterialVariantImporter.GetMaterialVariantFromAssetPath(assetPath);
            if (asset)
            {
                variantTarget.rootGUID = asset.rootGUID;
                variantTarget.overrides = asset.overrides;
            }
        }

        static Dictionary<UnityEditor.Editor, MaterialVariant[]> registeredVariants = new Dictionary<UnityEditor.Editor, MaterialVariant[]>();

        public static MaterialVariant[] GetMaterialVariantsFor(MaterialEditor editor)
        {
            if (!registeredVariants.ContainsKey(editor))
                return null;

            return registeredVariants[editor];
        }

        public override void OnEnable()
        {
            base.OnEnable();
            targetEditor = CreateEditor(assetTarget);
            registeredVariants.Add(targetEditor, extraDataTargets.Cast<MaterialVariant>().ToArray());
        }

        public override void OnDisable()
        {
            registeredVariants.Remove(targetEditor);
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
        
        protected override void Apply()
        {
            base.Apply();
            
            if (assetTarget != null)
            {
                for (int i = 0; i < targets.Length; ++i)
                {
                    InternalEditorUtility.SaveToSerializedFileAndForget(new[] { extraDataTargets[i] }, (targets[i] as MaterialVariantImporter).assetPath, true);
                    AssetDatabase.ImportAsset((targets[i] as MaterialVariantImporter).assetPath);
                }
            }
        }
    }
}
