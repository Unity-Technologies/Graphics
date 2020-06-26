using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.Rendering;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

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

            DrawLineageGUI();

            serializedObject.ApplyModifiedProperties();
            extraDataSerializedObject.ApplyModifiedProperties();

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
            GUILayout.BeginVertical("Material Variant Hierarchy", "window"); // TODO Find a better style
            GUILayout.Space(4);

            MaterialVariant matVariant = extraDataTarget as MaterialVariant;
            Object parent = matVariant.GetParent();
            Object selectedObj = null;

            // Draw ourselve in the hierarchy
            using (new EditorGUI.DisabledScope(true))
            {
                DrawLineageMember(assetTarget, typeof(Material));
            }

            if (parent == null)
            {
                Object selectedMaterial = DrawLineageMember(null, typeof(Material));
                Object selectedShader = DrawLineageMember(null, typeof(Shader));

                selectedObj = selectedMaterial != null ? selectedMaterial : selectedShader;
            }
            else
            {
                bool first = true;
                Object localSelectedObj = null;
                while (parent)
                {
                    // Display remaining of the hierarchy
                    using (new EditorGUI.DisabledScope(!first))
                    {
                        if (parent is MaterialVariant)
                        {
                            Material mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GetAssetPath(parent));
                            localSelectedObj = DrawLineageMember(mat, typeof(Material));
                            if (first)
                            {

                            }
                            parent = (parent as MaterialVariant).GetParent();
                        }
                        else if (parent is Material)
                        {
                            localSelectedObj = DrawLineageMember(parent, typeof(Material));
                            parent = (parent as Material).shader;
                        }
                        else if (parent is Shader)
                        {
                            localSelectedObj = DrawLineageMember(parent, typeof(Shader));
                            parent = null;
                        }

                        if (first)
                        {
                            selectedObj = localSelectedObj;
                            first = false;
                        }
                    }
                }
            }

            GUILayout.Space(4);
            GUILayout.EndVertical();

            // We need to compare the selected object (if any) with the current asset reference by Parent
            // to see if anything have change
            Object initialObj = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GetAssetPath(matVariant.GetParent()));
            if (selectedObj != initialObj)
            {
                matVariant.rootGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(selectedObj));
            }
        }

        Object DrawLineageMember(Object asset, Type assetType)
        {
            // We could use this to start a Horizontal and add inline icons and toggles to show overridden/locked
            return EditorGUILayout.ObjectField("", asset, assetType, false);
        }

        internal static void DrawPropertyScopeContextMenuAndIcons(MaterialVariant matVariant, string propertyName, bool isOverride, bool isBlocked, Rect labelRect, GenericMenu.MenuFunction resetFunction, GenericMenu.MenuFunction blockFunction)
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

            if(isBlocked || matVariant.IsPropertyBlockedInCurrent(propertyName))
            {
                labelRect.xMin = 8;
                labelRect.width = 32;
                EditorGUI.BeginDisabledGroup(isBlocked);
                GUI.Label(labelRect, EditorGUIUtility.IconContent("AssemblyLock"));
                EditorGUI.EndDisabledGroup();
            }
        }
    }
}
