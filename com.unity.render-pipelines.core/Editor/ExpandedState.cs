using System;

namespace UnityEditor.Rendering
{
    /// <summary>Used in editor drawer part to store the state of expendable areas.</summary>
    /// <typeparam name="TState">An enum to use to describe the state.</typeparam>
    /// <typeparam name="TTarget">A type given to automatically compute the key.</typeparam>
    public struct ExpandedState<TState, TTarget>
        where TState : struct, IConvertible
    {
        EditorPrefBoolFlags<TState> m_State;

        /// <summary>Constructor will create the key to store in the EditorPref the state given generic type passed.</summary>
        /// <param name="defaultValue">If key did not exist, it will be created with this value for initialization.</param>
        /// <param name="prefix">[Optional] Prefix scope of the key (Default is CoreRP)</param>
        public ExpandedState(TState defaultValue, string prefix = "CoreRP")
        {
            String Key = string.Format("{0}:{1}:UI_State", prefix, typeof(TTarget).Name);
            m_State = new EditorPrefBoolFlags<TState>(Key);

            //register key if not already there
            if (!EditorPrefs.HasKey(Key))
            {
                EditorPrefs.SetInt(Key, (int)(object)defaultValue);
            }
        }

        /// <summary>Get or set the state given the mask.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <returns>True: All flagged area are expended</returns>
        public bool this[TState mask]
        {
            get { return m_State.HasFlag(mask); }
            set { m_State.SetFlag(mask, value); }
        }

        /// <summary>Accessor to the expended state of this specific mask.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <returns>True: All flagged area are expended</returns>
        public bool GetExpandedAreas(TState mask)
        {
            return m_State.HasFlag(mask);
        }

        /// <summary>Setter to the expended state.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <param name="value">The expended state to set</param>
        public void SetExpandedAreas(TState mask, bool value)
        {
            m_State.SetFlag(mask, value);
        }

        /// <summary> Utility to set all states to true </summary>
        public void ExpandAll()
        {
            m_State.rawValue = ~(-1);
        }

        /// <summary> Utility to set all states to false </summary>
        public void CollapseAll()
        {
            m_State.rawValue = 0;
        }
    }
}
