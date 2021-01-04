using System;
using System.Linq;
using UnityEditor;

using UnityEditor.Rendering;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace Unity.Assets.MaterialVariant.Editor
{
    [CustomEditor(typeof(MaterialVariantImporter))]
    public class MaterialVariantEditor : UnityEditor.AssetImporters.ScriptedImporterEditor
    {
        private UnityEditor.Editor targetEditor = null;

        protected override Type extraDataType => typeof(MaterialVariant);
        protected override bool needsApplyRevert => true;
        public override bool showImportedObject => false;

        ParentVariantType m_ParentVariantType;
        MaterialVariant m_MatVariant;
        Object m_Parent; // This can be Material, Shader or MaterialVariant
        Object m_ParentTarget; // This is the target object Material or Shader

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

            m_MatVariant = extraDataTarget as MaterialVariant;
            m_Parent = m_MatVariant.GetParent();
            m_ParentTarget = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GetAssetPath(m_Parent));
            // if m_ParentTarget is null this will setup Material by default
            m_ParentVariantType = m_ParentTarget is Shader ? ParentVariantType.Shader : ParentVariantType.Material;
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

        static class Styles
        {
            public static readonly GUIContent parentVariantType = EditorGUIUtility.TrTextContent("Type", "");
            public const string materialVariantHierarchyText = "Material Variant Hierarchy";
        }


        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            extraDataSerializedObject.UpdateIfRequiredOrScript();

            if (targetEditor is MaterialEditor)
            {
                using (var header = new UnityEditor.Rendering.HighDefinition.MaterialHeaderScope(Styles.materialVariantHierarchyText, (uint)1, targetEditor as MaterialEditor))
                {
                    if (header.expanded)
                        DrawLineageGUI();
                }

                targetEditor.OnInspectorGUI();
            }
            else
            {
                DrawLineageGUI();
            }

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
            GUILayout.BeginVertical();

            Object parent = m_Parent;
            Object selectedTarget = null;
            bool first = true;

            // Draw ourselves in the hierarchy
            using (new EditorGUI.DisabledScope(true))
            {
                DrawLineageMember("Current", assetTarget, false);
            }

            if (parent == null)
            {
                selectedTarget = DrawLineageMember("Parent", null, first);
            }
            else
            {
                Object localSelectedTarget = null;
                while (parent)
                {
                    // Display remaining of the hierarchy
                    using (new EditorGUI.DisabledScope(!first))
                    {
                        if (parent is MaterialVariant)
                        {
                            Material mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GetAssetPath(parent));
                            localSelectedTarget = DrawLineageMember(first ? "Parent" : " ", mat, first);
                            parent = (parent as MaterialVariant).GetParent();
                        }
                        else if (parent is Material)
                        {
                            localSelectedTarget = DrawLineageMember(first ? "Parent" : "Root", parent, first);
                            parent = (parent as Material).shader;
                        }
                        else if (parent is Shader)
                        {
                            localSelectedTarget = DrawLineageMember(first ? "Parent" : "Root", parent, first);
                            parent = null;
                        }

                        if (first)
                        {
                            selectedTarget = localSelectedTarget;
                            first = false;
                        }
                    }
                }
            }

            GUILayout.EndVertical();

            // We need to compare the selected object (if any) with the current asset reference by Parent
            // to see if anything have change
            if (selectedTarget != m_ParentTarget)
            {
                m_MatVariant.rootGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(selectedTarget));

                // Now re-update the other field
                m_Parent = m_MatVariant.GetParent();
                m_ParentTarget = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GetAssetPath(m_Parent));
                // m_ParentVariantType is already on the good type
            }
        }

        private enum ParentVariantType
        {
            Material = 0,
            Shader = 1
        }

        Object DrawLineageMember(string label, Object asset, bool showButton)
        {
            Object target;

            if (showButton)
            {
                EditorGUILayout.BeginHorizontal();

                Type type = m_ParentVariantType == ParentVariantType.Shader ? typeof(Shader) : typeof(Material);
                // If m_ParentTarget is null we favor Material
                if (m_ParentTarget is Material && m_ParentVariantType != ParentVariantType.Material)
                    target = EditorGUILayout.ObjectField(label, null, type, false);
                else if (m_ParentTarget is Shader && m_ParentVariantType != ParentVariantType.Shader)
                    target = EditorGUILayout.ObjectField(label, null, type, false);
                else
                    target = EditorGUILayout.ObjectField(label, asset, type, false);

                var oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 45;
                m_ParentVariantType = (ParentVariantType)EditorGUILayout.EnumPopup(Styles.parentVariantType, m_ParentVariantType);
                EditorGUIUtility.labelWidth = oldWidth;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                Type type = asset is Shader ? typeof(Shader) : typeof(Material);
                target = EditorGUILayout.ObjectField(label, asset, type, false);
            }

            // We could use this to start a Horizontal and add inline icons and toggles to show overridden/locked
            return target;
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

            if (isBlocked || matVariant.IsPropertyBlockedInCurrent(propertyName))
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
