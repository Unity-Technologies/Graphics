using System;
using UnityEditor.AnimatedValues;
using System.Collections.Generic;

namespace UnityEditor.Rendering
{
    /// <summary>Used in editor drawer part to store the state of additional properties areas.</summary>
    /// <typeparam name="TState">An enum to use to describe the state.</typeparam>
    /// <typeparam name="TTarget">A type given to automatically compute the key.</typeparam>
    public class AdditionalPropertiesState<TState, TTarget>
        where TState : struct, IConvertible
    {
        EditorPrefBoolFlags<TState> m_State;
        HashSet<Editor> m_Editors = new HashSet<Editor>();
        Dictionary<TState, AnimFloat> m_AnimFloats = new Dictionary<TState, AnimFloat>();

        void RepaintAll()
        {
            foreach (var editor in m_Editors)
            {
                editor.Repaint();
            }
        }

        /// <summary>Constructor will create the key to store in the EditorPref the state given generic type passed.</summary>
        /// <param name="defaultValue">If key did not exist, it will be created with this value for initialization.</param>
        /// <param name="prefix">[Optional] Prefix scope of the key (Default is CoreRP)</param>
        public AdditionalPropertiesState(TState defaultValue, string prefix = "CoreRP")
        {
            string key = $"{prefix}:{typeof(TTarget).Name}:{typeof(TState).Name}:UI_AP_State";
            m_State = new EditorPrefBoolFlags<TState>(key);

            //register key if not already there
            if (!EditorPrefs.HasKey(key))
            {
                EditorPrefs.SetInt(key, (int)(object)defaultValue);
            }
        }

        /// <summary>Get or set the state given the mask.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <returns>True: All flagged area are expended</returns>
        public bool this[TState mask]
        {
            get => GetAdditionalPropertiesState(mask);
            set => SetAdditionalPropertiesState(mask, value);
        }

        /// <summary>Accessor to the expended state of this specific mask.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <returns>True: All flagged area are expended</returns>
        public bool GetAdditionalPropertiesState(TState mask)
        {
            return m_State.HasFlag(mask);
        }

        /// <summary>Setter to the expended state.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <param name="value">True to show the additional properties.</param>
        public void SetAdditionalPropertiesState(TState mask, bool value)
        {
            m_State.SetFlag(mask, value);

            if (value)
                ResetAnimation(mask);
        }

        /// <summary> Utility to set all states to true </summary>
        public void ShowAll()
        {
            m_State.rawValue = 0xFFFFFFFF;
        }

        /// <summary> Utility to set all states to false </summary>
        public void HideAll()
        {
            m_State.rawValue = 0;
        }

        internal AnimFloat GetAnimation(TState mask)
        {
            AnimFloat anim = null;
            if (!m_AnimFloats.TryGetValue(mask, out anim))
            {
                anim = new AnimFloat(0, RepaintAll);
                anim.speed = CoreEditorConstants.additionalPropertiesHightLightSpeed;
                m_AnimFloats.Add(mask, anim);
            }

            return anim;
        }

        void ResetAnimation(TState mask)
        {
            AnimFloat anim = GetAnimation(mask);

            anim.value = 1.0f;
            anim.target = 0.0f;
        }

        /// <summary>
        /// Register an editor for this set of additional properties.
        /// </summary>
        /// <param name="editor">Editor to register.</param>
        public void RegisterEditor(Editor editor)
        {
            m_Editors.Add(editor);
        }

        /// <summary>
        /// Unregister an editor for this set of additional properties.
        /// </summary>
        /// <param name="editor">Editor to unregister.</param>
        public void UnregisterEditor(Editor editor)
        {
            m_Editors.Remove(editor);
        }
    }
}
