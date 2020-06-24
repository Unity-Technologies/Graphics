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
        MaterialVariant m_Variant;

        public MaterialPropertyScope(/*Rect drawingRect, */MaterialProperty materialProperty/*, GUIContent label*/, MaterialVariant variant)
        {
            //m_DrawingRect = drawingRect;
            m_MaterialProperty = materialProperty;
            //m_Label = label;
            m_Variant = variant;

            //Handling of override and contextual menu on label
            // TO DO

            //Starting registering change
            if (m_Variant != null)
                EditorGUI.BeginChangeCheck();
        }

        void IDisposable.Dispose()
        {
            //Stop registering change
            if (m_Variant != null && EditorGUI.EndChangeCheck())
            {
                System.Collections.Generic.IEnumerable<MaterialPropertyModification> changes = MaterialPropertyModification.CreateMaterialPropertyModifications(m_MaterialProperty);
                m_Variant.TrimPreviousOverridesAndAdd(changes);
            }
        }
    }
}
