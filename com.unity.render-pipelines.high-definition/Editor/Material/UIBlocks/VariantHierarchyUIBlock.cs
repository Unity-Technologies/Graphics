using System;
using Unity.Assets.MaterialVariant.Editor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Represents an advanced options material UI block.
    /// </summary>
    public class VariantHierarchyUIBlock : MaterialUIBlock
    {
        private enum ParentVariantType
        {
            Material = 0,
            Shader = 1
        }

        ParentVariantType m_ParentVariantType;
        MaterialVariant m_MatVariant;
        Object m_Parent; // This can be Material, Shader or MaterialVariant
        Object m_ParentTarget; // This is the target object Material or Shader

        static class Styles
        {
            public static readonly GUIContent parentVariantType = EditorGUIUtility.TrTextContent("Type", "");
            public const string materialVariantHierarchyText = "Material Variant Hierarchy";
        }

        /// <summary>
        /// Loads the material properties for the block.
        /// </summary>
        public override void LoadMaterialProperties()
        {
            if (m_MatVariant != null)
                return;

            m_MatVariant = MaterialVariantImporter.GetMaterialVariantFromObject(materialEditor.target);
            if (m_MatVariant == null)
                m_Parent = materials[0].shader;
            else
                m_Parent = m_MatVariant.GetParent();

            m_ParentTarget = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GetAssetPath(m_Parent));
            // if m_ParentTarget is null this will setup Material by default
            m_ParentVariantType = m_ParentTarget is Shader ? ParentVariantType.Shader : ParentVariantType.Material;
        }

        /// <summary>
        /// Renders the properties in the block.
        /// </summary>
        public override void OnGUI()
        {
            if (materials.Length != 1) // No multiediting of hierarchy
                return;

            using (var header = new MaterialHeaderScope(Styles.materialVariantHierarchyText, (uint)1, materialEditor))
            {
                if (header.expanded)
                    DrawLineageGUI();
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
                DrawLineageMember("Current", materials[0], false);
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
    }
}
