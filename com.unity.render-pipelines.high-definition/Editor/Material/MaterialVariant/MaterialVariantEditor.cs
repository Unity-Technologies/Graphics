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
            if (assetTarget is MaterialVariant)
            {
                serializedObject.UpdateIfRequiredOrScript();
                extraDataSerializedObject.UpdateIfRequiredOrScript();

                var oldLabelWidth = EditorGUIUtility.labelWidth;

                //using (new EditorGUI.DisabledScope(!IsEnabled()))
                {
                    EditorGUIUtility.labelWidth = 50;

                    // Shader selection dropdown
                    ShaderPopup("MiniPulldown", null);

                    // Edit button for custom shaders
                  //  if (m_Shader != null && !HasMultipleMixedShaderValues() && (m_Shader.hideFlags & HideFlags.DontSave) == 0)
                  //  {
                   //     if (GUILayout.Button("Edit...", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                   //         AssetDatabase.OpenAsset(m_Shader);
                   // }
                }

                EditorGUIUtility.labelWidth = oldLabelWidth;

                serializedObject.ApplyModifiedProperties();
                extraDataSerializedObject.ApplyModifiedProperties();
            }
            else
            {
                targetEditor.DrawHeader();
            }            
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

        // Note: this is called from native code.
        internal void OnSelectedShaderPopup(object shaderNameObj)
        {
            serializedObject.Update();
            var shaderName = (string)shaderNameObj;
            if (!string.IsNullOrEmpty(shaderName))
            {
                var shader = Shader.Find(shaderName);
                if (shader != null)
                {
                    // TODO
                    //extraDataTargets.Cast<MaterialVariant>();

                    MaterialVariant matVariant = extraDataTargets[0] as MaterialVariant;
                    matVariant.rootGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(shader));
                }
            }
        }

        private void ShaderPopup(GUIStyle style, Shader shader)
        {
            bool wasEnabled = GUI.enabled;

            Rect position = EditorGUILayout.GetControlRect();
            position = EditorGUI.PrefixLabel(position, 47385, EditorGUIUtility.TempContent("Shader"));
            EditorGUI.showMixedValue = false; // HasMultipleMixedShaderValues();

            var buttonContent = EditorGUIUtility.TempContent(shader != null ? shader.name : "No Shader Selected");
            if (EditorGUI.DropdownButton(position, buttonContent, FocusType.Keyboard, style))
            {
                var dropdown = new ShaderSelectionDropdown(shader, OnSelectedShaderPopup);
                dropdown.Show(position);
            }

            EditorGUI.showMixedValue = false;
            GUI.enabled = wasEnabled;
        }

        private class ShaderSelectionDropdown : AdvancedDropdown
        {
            Action<object> m_OnSelectedShaderPopup;
            Shader m_CurrentShader;

            public ShaderSelectionDropdown(Shader shader, Action<object> onSelectedShaderPopup)
                : base(new AdvancedDropdownState())
            {
                minimumSize = new Vector2(270, 308);
                m_CurrentShader = shader;
                m_OnSelectedShaderPopup = onSelectedShaderPopup;
                m_DataSource = new CallbackDataSource(BuildRoot);
                m_Gui = new MaterialVariantDropdownGUI(m_DataSource);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                var root = new AdvancedDropdownItem("Shaders");

                var shaders = ShaderUtil.GetAllShaderInfo();
                var shaderList = new List<string>();

                foreach (var shader in shaders)
                {
                    // Only HDRP and HDRP shader graph shader are supported currently
                    //if (shader.IsShaderGraph())
                    //{
                    //    if (!shader.TryGetMetadataOfType<HDMetadata>(out _))
                    //        continue;
                    //}
                    if (!shader.name.Contains("HDRP") || shader.name.StartsWith("Hidden"))
                    {
                        continue;
                    }

                    shaderList.Add(shader.name);
                }

                shaderList.Sort((s1, s2) =>
                {
                    var order = s2.Count(c => c == '/') - s1.Count(c => c == '/');
                    if (order == 0)
                    {
                        order = s1.CompareTo(s2);
                    }

                    return order;
                });

                shaderList.ForEach(s => AddShaderToMenu("", root, s, s));

                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                m_OnSelectedShaderPopup(((ShaderDropdownItem)item).fullName);
            }

            private void AddShaderToMenu(string prefix, AdvancedDropdownItem parent, string fullShaderName, string shaderName)
            {
                var shaderNameParts = shaderName.Split('/');
                if (shaderNameParts.Length > 1)
                {
                    AddShaderToMenu(prefix, FindOrCreateChild(parent, shaderName), fullShaderName, shaderName.Substring(shaderNameParts[0].Length + 1));
                }
                else
                {
                    var item = new ShaderDropdownItem(prefix, fullShaderName, shaderName);
                    parent.AddChild(item);
                    if (m_CurrentShader != null && m_CurrentShader.name == fullShaderName)
                    {
                        m_DataSource.selectedIDs.Add(item.id);
                    }
                }
            }

            private AdvancedDropdownItem FindOrCreateChild(AdvancedDropdownItem parent, string path)
            {
                var shaderNameParts = path.Split('/');
                var group = shaderNameParts[0];
                foreach (var child in parent.children)
                {
                    if (child.name == group)
                        return child;
                }

                var item = new AdvancedDropdownItem(group);
                parent.AddChild(item);
                return item;
            }

            private class ShaderDropdownItem : AdvancedDropdownItem
            {
                string m_FullName;
                string m_Prefix;
                public string fullName => m_FullName;
                public string prefix => m_Prefix;

                public ShaderDropdownItem(string prefix, string fullName, string shaderName)
                    : base(shaderName)
                {
                    m_FullName = fullName;
                    m_Prefix = prefix;
                    id = (prefix + fullName + shaderName).GetHashCode();
                }
            }

            private class MaterialVariantDropdownGUI : AdvancedDropdownGUI
            {
                public MaterialVariantDropdownGUI(AdvancedDropdownDataSource dataSource)
                    : base(dataSource) { }

                internal override void DrawItem(AdvancedDropdownItem item, string name, Texture2D icon, bool enabled, bool drawArrow, bool selected, bool hasSearch)
                {
                    var newScriptItem = item as ShaderDropdownItem;
                    if (hasSearch && newScriptItem != null)
                    {
                        name = string.Format("{0} ({1})", newScriptItem.name, newScriptItem.prefix + newScriptItem.fullName);
                    }
                    base.DrawItem(item, name, icon, enabled, drawArrow, selected, hasSearch);
                }
            }
        } // ShaderSelectionDropdown
    }
}
