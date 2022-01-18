using System;
using UnityEditor.AnimatedValues;
using System.Collections.Generic;

namespace UnityEditor.Rendering
{
    public abstract class AdditionalPropertiesStateBase<TState>
        where TState : struct, IConvertible
    {
        HashSet<Editor> m_Editors = new HashSet<Editor>();
        Dictionary<TState, AnimFloat> m_AnimFloats = new Dictionary<TState, AnimFloat>();

        void RepaintAll()
        {
            foreach (var editor in m_Editors)
            {
                editor.Repaint();
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
        public abstract bool GetAdditionalPropertiesState(TState mask);

        /// <summary>Setter to the expended state.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <param name="value">True to show the additional properties.</param>
        public void SetAdditionalPropertiesState(TState mask, bool value)
        {
            SetAdditionalPropertiesStateValue(mask, value);

            if (value)
                ResetAnimation(mask);
        }
        public abstract void SetAdditionalPropertiesStateValue(TState mask, bool value);

        /// <summary> Utility to set all states to true </summary>
        public abstract void ShowAll();

        /// <summary> Utility to set all states to false </summary>
        public abstract void HideAll();

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

        protected void ResetAnimation(TState mask)
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

    /// <summary>Used in editor drawer part to store the state of additional properties areas.</summary>
    /// <typeparam name="TState">An enum to use to describe the state.</typeparam>
    public class AdditionalPropertiesStateList<TTarget> : AdditionalPropertiesStateBase<int>
    {
        EditorPrefBoolFlags<int> m_State;

        /// <summary>Constructor will create the key to store in the EditorPref the state given generic type passed.</summary>
        public AdditionalPropertiesStateList(string prefix = "CoreRP")
        {
            string key = $"{prefix}:{typeof(TTarget).Name}:UI_AP_State";
            m_State = new EditorPrefBoolFlags<int>(key);

            //register key if not already there
            if (!EditorPrefs.HasKey(key))
            {
                EditorPrefs.SetInt(key, 0);
            }
        }

        /// <summary>Setter to the expended state value.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <param name="value">True to show the additional properties.</param>
        public override void SetAdditionalPropertiesStateValue(int mask, bool value)
        {
            m_State.SetFlag(mask, value);
        }

        /// <summary>Accessor to the expended state of this specific mask.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <returns>True: All flagged area are expended</returns>
        public override bool GetAdditionalPropertiesState(int mask)
        {
            return m_State.HasFlag(mask);
        }

        /// <summary> Utility to set all states to true </summary>
        public override void ShowAll()
        {
            m_State.rawValue = 0xFFFFFFFF;
        }

        /// <summary> Utility to set all states to false </summary>
        public override void HideAll()
        {
            m_State.rawValue = 0;
        }

        // <summary> Utility to left shift every bit after the index flag removing the index flag.
        public void removeFlagAtIndex(int index)
        {
            uint value = m_State.rawValue;                              // 1011 1001
            uint indexBit = 1u << index;                                // 0000 1000
            uint remainArea = indexBit - 1u;                            // 0000 0111
            uint remainBits = remainArea & value;                       // 0000 0001
            uint movedBits = (~remainArea - indexBit & value) >> 1;     // 1111 1000
                                                                        // 1111 0000
                                                                        // 1011 0000
                                                                        // 0101 1000
            m_State.rawValue = movedBits | remainBits;                  // 0101 1001
        }
    }

    /// <summary>Used in editor drawer part to store the state of additional properties areas.</summary>
    /// <typeparam name="TState">An enum to use to describe the state.</typeparam>
    /// <typeparam name="TTarget">A type given to automatically compute the key.</typeparam>
    public class AdditionalPropertiesState<TState, TTarget> : AdditionalPropertiesStateBase<TState>
        where TState : struct, IConvertible
    {
        EditorPrefBoolFlags<TState> m_State;

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

        /// <summary>Accessor to the expended state of this specific mask.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <returns>True: All flagged area are expended</returns>
        public override bool GetAdditionalPropertiesState(TState mask)
        {
            return m_State.HasFlag(mask);
        }

        /// <summary>Setter to the expended state value.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <param name="value">True to show the additional properties.</param>
        public override void SetAdditionalPropertiesStateValue(TState mask, bool value)
        {
            m_State.SetFlag(mask, value);
        }

        /// <summary> Utility to set all states to true </summary>
        public override void ShowAll()
        {
            m_State.rawValue = 0xFFFFFFFF;
        }

        /// <summary> Utility to set all states to false </summary>
        public override void HideAll()
        {
            m_State.rawValue = 0;
        }
    }
}
