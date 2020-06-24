using System.Runtime.InteropServices;
using Object = UnityEngine.Object;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine;
using UnityEditor;
using System;

namespace Unity.Assets.MaterialVariant.Editor
{
    public struct MaterialPropertyScope : IDisposable
    {
        //Rect m_DrawingRect;
        MaterialProperty m_MaterialProperty;
        //GUIContent m_Label;
        MaterialVariant[] m_Variants;

        float startY;

        public MaterialPropertyScope(/*Rect drawingRect, */MaterialProperty materialProperty/*, GUIContent label*/, MaterialVariant[] variants)
        {
            //m_DrawingRect = drawingRect;
            m_MaterialProperty = materialProperty;
            //m_Label = label;
            m_Variants = variants;

            //Handling of override and contextual menu on label
            // TO DO

            //Starting registering change
            if (m_Variants != null)
                EditorGUI.BeginChangeCheck();

            // Get the current Y coordinate before drawing the property
            startY = GUILayoutUtility.GetLastRect().yMax;
        }
        void ResetOverride()
        {
            m_Variants[0].ResetOverride(m_MaterialProperty);
        }

        void IDisposable.Dispose()
        {
            bool isOverride = m_Variants[0].IsOverriddenProperty(m_MaterialProperty);

            Rect r = GUILayoutUtility.GetLastRect();
            float endY = r.yMax;
            r.xMin = 1;
            r.yMin = startY + 2;
            r.yMax = endY - 2;
            r.width = EditorGUIUtility.labelWidth;

            if ( Event.current.rawType == EventType.ContextClick && r.Contains(Event.current.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Reset Override"), isOverride, ResetOverride);
                menu.ShowAsContext();
            }

            if (isOverride)
            {
                r.width = 3;
                EditorGUI.DrawRect(r, Color.white);
            }

            //Stop registering change
            if (m_Variants != null && EditorGUI.EndChangeCheck())
            {
                System.Collections.Generic.IEnumerable<MaterialPropertyModification> changes = MaterialPropertyModification.CreateMaterialPropertyModifications(m_MaterialProperty);
                foreach (var variant in m_Variants)
                    variant?.TrimPreviousOverridesAndAdd(changes);
            }
        }
    }
}
