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
        MaterialProperty m_MaterialProperty;
        MaterialVariant[] m_Variants;
        bool m_HaveDelayedRegisterer;
        bool m_Force;
        float m_StartY;

        /// <summary>
        /// MaterialPropertyScope are used to handle MaterialPropertyModification in material instances.
        /// This will do the registration of any new override but also this will do the UI (contextual menu and left bar displayed when there is an override).
        /// </summary>
        /// <param name="materialProperty">The materialProperty that we need to register</param>
        /// <param name="variants">The list of MaterialVariant should have the same size than elements in selection.</param>
        /// <param name="force">
        /// The force registration is for MaterialProperty that are changed at inspector frame without change from the user.
        /// In this case, we skip the UI part (contextual menu and left bar displayed when there is an override).
        /// </param>
        public MaterialPropertyScope(MaterialProperty materialProperty, MaterialVariant[] variants, bool force = false)
        {
            m_MaterialProperty = materialProperty;
            m_Variants = variants;
            m_HaveDelayedRegisterer = false;
            m_Force = force;

            //Starting registering change
            if (!m_Force && m_Variants != null)
                EditorGUI.BeginChangeCheck();

            // Get the current Y coordinate before drawing the property
            m_StartY = GUILayoutUtility.GetLastRect().yMax;
        }
        void ResetOverride()
        {
            m_Variants[0].ResetOverride(m_MaterialProperty);
        }

        void IDisposable.Dispose()
        {
            // force registration is for MaterialProperty that are changed at inspector frame without change from the user
            if (!m_Force)
            {
                bool isOverride = (m_Variants != null) ? m_Variants[0].IsOverriddenProperty(m_MaterialProperty) : false;

                Rect r = GUILayoutUtility.GetLastRect();
                float endY = r.yMax;
                r.xMin = 1;
                r.yMin = m_StartY + 2;
                r.yMax = endY - 2;
                r.width = EditorGUIUtility.labelWidth;

                if (Event.current.rawType == EventType.ContextClick && r.Contains(Event.current.mousePosition))
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
            }

            //Stop registering change
            // EditorGUI.EndChangeCheck() must be first to not break balance of BeginChangeCheck and EndChangeCheck if we ar not force registering
            if ((m_Force || EditorGUI.EndChangeCheck()) && !m_HaveDelayedRegisterer && m_Variants != null)
            {
                System.Collections.Generic.IEnumerable<MaterialPropertyModification> changes = MaterialPropertyModification.CreateMaterialPropertyModifications(m_MaterialProperty);
                foreach (var variant in m_Variants)
                    variant?.TrimPreviousOverridesAndAdd(changes);
            }
        }

        public MaterialPropertyScopeDelayedOverrideRegisterer ProduceDelayedRegisterer()
        {
            if (m_HaveDelayedRegisterer)
                throw new Exception($"A delayed registerer already exists for this MaterialPropertyScope for {m_MaterialProperty.displayName}. You should only use one at the end of all operations on this property.");

            m_HaveDelayedRegisterer = true;

            return new MaterialPropertyScopeDelayedOverrideRegisterer(m_MaterialProperty, m_Variants);
        }


        public struct MaterialPropertyScopeDelayedOverrideRegisterer
        {
            MaterialProperty m_MaterialProperty;
            MaterialVariant[] m_Variants;
            bool m_Registered;

            internal MaterialPropertyScopeDelayedOverrideRegisterer(MaterialProperty materialProperty, MaterialVariant[] variants)
            {
                m_MaterialProperty = materialProperty;
                m_Variants = variants;
                m_Registered = false;
            }

            public void RegisterNow()
            {
                if (m_Registered)
                    return;

                m_Registered = true;

                if (m_Variants != null)
                {
                    System.Collections.Generic.IEnumerable<MaterialPropertyModification> changes = MaterialPropertyModification.CreateMaterialPropertyModifications(m_MaterialProperty);
                    foreach (var variant in m_Variants)
                        variant?.TrimPreviousOverridesAndAdd(changes);
                }
            }
        }
    }
}
