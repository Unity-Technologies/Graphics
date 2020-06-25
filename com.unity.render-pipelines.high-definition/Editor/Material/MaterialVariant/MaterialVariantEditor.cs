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
                variantTarget.blocks = asset.blocks;
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
            InternalEditorUtility.SetIsInspectorExpanded(assetTarget, true);
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
            serializedObject.UpdateIfRequiredOrScript();
            extraDataSerializedObject.UpdateIfRequiredOrScript();

            targetEditor.OnInspectorGUI();

            serializedObject.ApplyModifiedProperties();
            extraDataSerializedObject.ApplyModifiedProperties();

            DrawLineageGUI();

            ApplyRevertGUI();
        }

        protected override void Apply()
        {
            base.Apply();
            RefreshTargets(true);
        }

        protected override void ResetValues()
        {
            base.ResetValues();
            RefreshTargets(false);
        }

        private void RefreshTargets(bool save)
        {
            if (assetTarget != null)
            {
                for (int i = 0; i < targets.Length; ++i)
                {
                    if (save)
                    {
                        InternalEditorUtility.SaveToSerializedFileAndForget(new[] { extraDataTargets[i] }, (targets[i] as MaterialVariantImporter).assetPath, true);
                    }

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

        internal static void DrawPropertyScopeContextMenuAndIcons(MaterialVariant matVariant, string propertyName, bool isOverride, Rect labelRect, GenericMenu.MenuFunction resetFunction, GenericMenu.MenuFunction blockFunction)
        {
            if (Event.current.rawType == EventType.ContextClick && labelRect.Contains(Event.current.mousePosition))
            {
                GenericMenu menu = new GenericMenu();

                var resetGUIContent = new GUIContent("Reset Override");
                if (isOverride)
                {
                    menu.AddItem(resetGUIContent, false, resetFunction);
                }
                else
                {
                    menu.AddDisabledItem(resetGUIContent);
                }

                var blockGUIContent = new GUIContent("Lock in children");
                if (matVariant.IsPropertyBlockedInAncestors(propertyName))
                {
                    menu.AddDisabledItem(blockGUIContent, true);
                }
                else
                {
                    menu.AddItem(blockGUIContent, matVariant.IsPropertyBlockedInCurrent(propertyName), blockFunction);
                }

                menu.ShowAsContext();
            }

            if (isOverride)
            {
                labelRect.width = 3;
                EditorGUI.DrawRect(labelRect, Color.white);
            }
        }
    }
}
