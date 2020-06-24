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
        }

        void IDisposable.Dispose()
        {
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
