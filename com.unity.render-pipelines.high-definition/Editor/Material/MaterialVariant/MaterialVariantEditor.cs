using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.Rendering;
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

            // We want to allow users to do a re-parenting so we allow to edit parent
            assetTarget.hideFlags &= ~HideFlags.NotEditable; // Be sure we can edit this material

            targetEditor = CreateEditor(assetTarget);
            //targetEditor.firstInspectedEditor = true; // This line allow to remove the small extra arrow in the header, but require to have access to internal of Editor
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

            DrawLineageGUI();
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

        private void DrawLineageGUI()
        {
            GUILayout.Space(10);
            GUILayout.BeginVertical("Bloodline", "window"); // TODO Find a better style
            GUILayout.Space(4);

            using (new EditorGUI.DisabledScope(true))
            {
                DrawLineageMember(assetTarget, typeof(Material));

                Object nextAncestor = (extraDataTarget as MaterialVariant).GetParent();
                while (nextAncestor)
                {
                    if (nextAncestor is MaterialVariant)
                    {
                        Material mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GetAssetPath(nextAncestor));
                        DrawLineageMember(mat, typeof(Material));
                        nextAncestor = (nextAncestor as MaterialVariant).GetParent();
                    }
                    else if (nextAncestor is Material)
                    {
                        DrawLineageMember(nextAncestor, typeof(Material));
                        nextAncestor = (nextAncestor as Material).shader;
                    }
                    else if (nextAncestor is Shader)
                    {
                        DrawLineageMember(nextAncestor, typeof(Shader));
                        nextAncestor = null;
                    }
                }
            }

            GUILayout.Space(4);
            GUILayout.EndVertical();
        }

        void DrawLineageMember(Object asset, Type assetType)
        {
            // We could use this to start a Horizontal and add inline icons and toggles to show overridden/locked
            EditorGUILayout.ObjectField("", asset, assetType, false);
        }
    }
}
