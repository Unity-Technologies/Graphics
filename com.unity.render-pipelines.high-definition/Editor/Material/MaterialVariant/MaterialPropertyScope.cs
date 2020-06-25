using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace Unity.Assets.MaterialVariant.Editor
{
    public struct MaterialPropertyScope : IDisposable
    {
        MaterialProperty m_MaterialProperty;
        MaterialVariant[] m_Variants;
        bool m_Force;

        bool m_HaveDelayedRegisterer;
        bool m_IsBlocked;
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
            m_Force = force;

            m_HaveDelayedRegisterer = false;
            m_IsBlocked = false;

            // Get the current Y coordinate before drawing the property
            // We define a new empty rect in order to grab the current height even if there was nothing drawn in the block (GetLastRect cause issue if it was first element of block)
            m_StartY = GUILayoutUtility.GetRect(0, 0).yMax;

            if (m_Force || m_Variants == null)
                return;

            var name = m_MaterialProperty.name;
            m_IsBlocked = m_Variants.Any(o => o.IsPropertyBlockedInAncestors(name));

            if (m_IsBlocked)
                EditorGUI.BeginDisabledGroup(true);
            else
                EditorGUI.BeginChangeCheck();
        }

        void IDisposable.Dispose()
        {
            if (m_Variants == null)
                return;

            // force registration is for MaterialProperty that are changed at inspector frame without change from the user
            if (!m_Force)
            {
                if (m_IsBlocked)
                {

                    EditorGUI.EndDisabledGroup();
                }

                {
                    bool isOverride = !m_IsBlocked && m_Variants[0].IsOverriddenProperty(m_MaterialProperty);

                    Rect r = GUILayoutUtility.GetLastRect();
                    float endY = r.yMax;
                    r.xMin = 1;
                    r.yMin = m_StartY + 2;
                    r.yMax = endY - 2;
                    r.width = EditorGUIUtility.labelWidth;

                    MaterialVariant matVariant = m_Variants[0];
                    MaterialProperty matProperty = m_MaterialProperty;
                    MaterialVariantEditor.DrawPropertyScopeContextMenuAndIcons(matVariant, m_MaterialProperty.name, isOverride, m_IsBlocked, r,
                    () => matVariant.ResetOverride(matProperty),
                    () => matVariant.TogglePropertyBlocked(matProperty.name));
                }
            }

            bool hasChanged = m_Force;
            if (!hasChanged && !m_IsBlocked)
                hasChanged = EditorGUI.EndChangeCheck(); //Stop registering change

            if (hasChanged && !m_HaveDelayedRegisterer)
            {
                ApplyChangesToMaterialVariants();
            }
        }

        void ApplyChangesToMaterialVariants()
        {
            System.Collections.Generic.IEnumerable<MaterialPropertyModification> changes = MaterialPropertyModification.CreateMaterialPropertyModifications(m_MaterialProperty);
            foreach (var variant in m_Variants)
                variant?.TrimPreviousOverridesAndAdd(changes);
        }

        public DelayedOverrideRegisterer ProduceDelayedRegisterer()
        {
            if (m_HaveDelayedRegisterer)
                throw new Exception($"A delayed registerer already exists for this MaterialPropertyScope for {m_MaterialProperty.displayName}. You should only use one at the end of all operations on this property.");

            m_HaveDelayedRegisterer = true;

            return new DelayedOverrideRegisterer(ApplyChangesToMaterialVariants);
        }

        public struct DelayedOverrideRegisterer
        {
            internal delegate void RegisterFunction();

            RegisterFunction m_RegisterFunction;
            bool m_AlreadyRegistered;

            internal DelayedOverrideRegisterer(RegisterFunction registerFunction)
            {
                m_RegisterFunction = registerFunction;
                m_AlreadyRegistered = false;
            }

            public void RegisterNow()
            {
                if (!m_AlreadyRegistered)
                {
                    m_RegisterFunction();
                    m_AlreadyRegistered = true;
                }
            }
        }
    }

    public struct MaterialRenderQueueScope : IDisposable
    {
        MaterialVariant[] m_Variants;
        Func<int> m_ValueGetter;

        bool m_HaveDelayedRegisterer;
        bool m_IsBlocked;
        float m_StartY;

        const string k_SerializedPropertyName = "m_CustomRenderQueue";

        /// <summary>
        /// MaterialRenderQueueScope is used to handle MaterialPropertyModification in material instances around renderqueue.
        /// This will do the registration of any new override but also this will do the UI (contextual menu and left bar displayed when there is an override).
        /// </summary>
        /// <param name="variants">The list of MaterialVariant should have the same size than elements in selection.</param>
        public MaterialRenderQueueScope(MaterialVariant[] variants, Func<int> valueGetter)
        {
            m_Variants = variants;
            m_ValueGetter = valueGetter;

            m_HaveDelayedRegisterer = false;
            m_IsBlocked = false;

            // Get the current Y coordinate before drawing the property
            // We define a new empty rect in order to grab the current height even if there was nothing drawn in the block (GetLastRect cause issue if it was first element of block)
            m_StartY = GUILayoutUtility.GetRect(0, 0).yMax;

            //Starting registering change
            if (m_Variants == null)
                return;

            m_IsBlocked = m_Variants.Any(o => o.IsPropertyBlockedInAncestors(k_SerializedPropertyName));

            if (m_IsBlocked)
                EditorGUI.BeginDisabledGroup(true);
            else
                EditorGUI.BeginChangeCheck();
        }

        void IDisposable.Dispose()
        {
            if (m_Variants == null)
                return;

            if (m_IsBlocked)
                EditorGUI.EndDisabledGroup();

            {
                bool isOverride = !m_IsBlocked && m_Variants[0].IsOverriddenPropertyForNonMaterialProperty(k_SerializedPropertyName);

                Rect r = GUILayoutUtility.GetLastRect();
                float endY = r.yMax;
                r.xMin = 1;
                r.yMin = m_StartY + 2;
                r.yMax = endY - 2;
                r.width = EditorGUIUtility.labelWidth;

                MaterialVariant matVariant = m_Variants[0];
                MaterialVariantEditor.DrawPropertyScopeContextMenuAndIcons(matVariant, k_SerializedPropertyName, isOverride, m_IsBlocked, r,
                    () => matVariant.ResetOverrideForNonMaterialProperty(k_SerializedPropertyName),
                    () => matVariant.TogglePropertyBlocked(k_SerializedPropertyName));
            }

            bool hasChanged = !m_IsBlocked && EditorGUI.EndChangeCheck();

            if (hasChanged && !m_HaveDelayedRegisterer)
            {
                ApplyChangesToMaterialVariants();
            }
        }
        void ApplyChangesToMaterialVariants()
        {
            System.Collections.Generic.IEnumerable<MaterialPropertyModification> changes = MaterialPropertyModification.CreateMaterialPropertyModificationsForNonMaterialProperty(k_SerializedPropertyName, m_ValueGetter());
            foreach (var variant in m_Variants)
                variant?.TrimPreviousOverridesAndAdd(changes);
        }

        public DelayedOverrideRegisterer ProduceDelayedRegisterer()
        {
            if (m_HaveDelayedRegisterer)
                throw new Exception($"A delayed registerer already exists for this MaterialPropertyScope for {k_SerializedPropertyName}. You should only use one at the end of all operations on this property.");

            m_HaveDelayedRegisterer = true;

            return new DelayedOverrideRegisterer(ApplyChangesToMaterialVariants);
        }

        public struct DelayedOverrideRegisterer
        {
            internal delegate void RegisterFunction();

            RegisterFunction m_RegisterFunction;
            bool m_AlreadyRegistered;

            internal DelayedOverrideRegisterer(RegisterFunction registerFunction)
            {
                m_RegisterFunction = registerFunction;
                m_AlreadyRegistered = false;
            }

            public void RegisterNow()
            {
                if (!m_AlreadyRegistered)
                {
                    m_RegisterFunction();
                    m_AlreadyRegistered = true;
                }
            }
        }
    }

    public struct MaterialNonDrawnPropertyScope<T> : IDisposable
        where T : struct
    {
        MaterialVariant[] m_Variants;
        string m_PropertyName;
        T m_Value;

        /// <summary>
        /// MaterialPropertyScope are used to handle MaterialPropertyModification in material instances.
        /// This will do the registration of any new override but also this will do the UI (contextual menu and left bar displayed when there is an override).
        /// </summary>
        /// <param name="propertyName">The key to register</param>
        /// <param name="value">value to register</param>
        /// <param name="variants">The list of MaterialVariant should have the same size than elements in selection.</param>
        public MaterialNonDrawnPropertyScope(string propertyName, T value, MaterialVariant[] variants)
        {
            m_Variants = variants;
            m_PropertyName = propertyName;
            m_Value = value;
        }

        void IDisposable.Dispose()
        {
            //Registering change
            if (m_Variants != null)
            {
                System.Collections.Generic.IEnumerable<MaterialPropertyModification> changes = MaterialPropertyModification.CreateMaterialPropertyModificationsForNonMaterialProperty(m_PropertyName, m_Value);
                foreach (var variant in m_Variants)
                    variant?.TrimPreviousOverridesAndAdd(changes);
            }
        }
    }

}
