using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.MaterialVariants
{
    public class HierarchyUI
    {
        public static class Styles
        {
            public static readonly GUIContent parentVariantType = EditorGUIUtility.TrTextContent("Type", "");
            public const string materialVariantHierarchyText = "Material Variant Hierarchy";
        }

        private enum ParentVariantType
        {
            Material = 0,
            Shader = 1
        }

        MaterialVariant m_MatVariant;

        string m_ParentGUID = "";
        Object m_Parent; // This can be Material, Shader or MaterialVariant
        Object m_ParentTarget; // This is the target object Material or Shader

        ParentVariantType m_ParentVariantType = ParentVariantType.Material;

        public HierarchyUI(Object materialEditorTarget)
        {
            m_MatVariant = MaterialVariant.GetMaterialVariantFromObject(materialEditorTarget);
        }

        public void OnGUI()
        {
            if (m_MatVariant.rootGUID != m_ParentGUID)
            {
                m_ParentGUID = m_MatVariant.rootGUID;

                m_Parent = m_MatVariant.GetParent();
                m_ParentTarget = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GetAssetPath(m_Parent));

                if (m_ParentTarget != null)
                    m_ParentVariantType = m_ParentTarget is Shader ? ParentVariantType.Shader : ParentVariantType.Material;
            }

            GUILayout.BeginVertical();

            // Draw ourselves in the hierarchy
            using (new EditorGUI.DisabledScope(true))
            {
                Material currentMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GetAssetPath(m_MatVariant));
                DrawLineageMember("Current", currentMaterial, false);
            }

            // Display the rest of the hierarchy
            Object selectedParentTarget = null;
            if (m_Parent == null)
            {
                selectedParentTarget = DrawLineageMember("Parent", null, true);
            }
            else
            {
                bool isFirstAncestor = true;
                Object nextParent = m_Parent;
                while (nextParent)
                {
                    using (new EditorGUI.DisabledScope(!isFirstAncestor))
                    {
                        Object parentTargetForCurrentAncestor = null;

                        if (nextParent is MaterialVariant nextMatVariant)
                        {
                            Material mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GetAssetPath(nextParent));
                            parentTargetForCurrentAncestor = DrawLineageMember("Parent", mat, isFirstAncestor);
                            nextParent = nextMatVariant.GetParent();
                        }
                        else if (nextParent is Material nextMaterial)
                        {
                            parentTargetForCurrentAncestor = DrawLineageMember("Parent", nextParent, isFirstAncestor);
                            nextParent = nextMaterial.shader;
                        }
                        else if (nextParent is Shader)
                        {
                            parentTargetForCurrentAncestor = DrawLineageMember(isFirstAncestor ? "Parent" : "Root", nextParent, isFirstAncestor);
                            nextParent = null;
                        }

                        if (isFirstAncestor)
                        {
                            selectedParentTarget = parentTargetForCurrentAncestor;
                            isFirstAncestor = false;
                        }
                    }
                }
            }

            GUILayout.EndVertical();

            // Reparenting: when the user selects a new parent, we change the rootGUID property - on the next OnGUI the hierarchy will be regenerated
            if (selectedParentTarget != m_ParentTarget)
            {
                // Validate selectedParentTarget: to avoid a loop, it must not be the current material or have the current material as one of its ancestors
                bool valid = true;
                Object nextParent = MaterialVariant.GetMaterialVariantFromObject(selectedParentTarget);
                while (nextParent != null)
                {
                    if (nextParent == m_MatVariant)
                    {
                        valid = false;
                        break;
                    }

                    MaterialVariant nextMatVariant = nextParent as MaterialVariant;
                    nextParent = nextMatVariant ? nextMatVariant.GetParent() : null;
                }

                if (valid)
                {
                    Undo.RecordObject(m_MatVariant, "Change Parent");
                    m_MatVariant.rootGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(selectedParentTarget));
                }
            }
        }

        Object DrawLineageMember(string label, Object asset, bool showButton)
        {
            Object target;

            if (showButton)
            {
                EditorGUILayout.BeginHorizontal();

                Type type = m_ParentVariantType == ParentVariantType.Shader ? typeof(Shader) : typeof(Material);
                target = EditorGUILayout.ObjectField(label, asset, type, false);

                var oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 45;
                EditorGUI.BeginChangeCheck();
                m_ParentVariantType = (ParentVariantType)EditorGUILayout.EnumPopup(HierarchyUI.Styles.parentVariantType, m_ParentVariantType);
                if (EditorGUI.EndChangeCheck())
                    target = null;
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
