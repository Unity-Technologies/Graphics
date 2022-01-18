using System;
using UnityEngine;

namespace UnityEditor.Rendering
{
    public interface IExpandedState<TState>
        where TState : struct, IConvertible
    {
        bool GetExpandedAreas(TState mask);
        void SetExpandedAreas(TState mask, bool value);
        void ExpandAll();
        void CollapseAll();

        /// <summary>Get or set the state given the mask.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <returns>True: All flagged area are expended</returns>
        public bool this[TState mask]
        {
            get { return GetExpandedAreas(mask); }
            set { SetExpandedAreas(mask, value); }
        }
    }

    public struct ExpandedStateList<TTarget> : IExpandedState<int>
    {
        EditorPrefBoolFlags<int> m_State;

        /// <summary>
        ///     Constructor will create the key to store in the EditorPref the state given generic type passed.
        ///     The key will be formated as such prefix:TTarget:TState:UI_State.
        /// </summary>
        /// <param name="defaultValue">If key did not exist, it will be created with this value for initialization.</param>
        /// <param name="prefix">[Optional] Prefix scope of the key (Default is CoreRP)</param>
        public ExpandedStateList(string prefix = "CoreRP")
        {
            string key = $"{prefix}:{typeof(TTarget).Name}:UI_State";
            m_State = new EditorPrefBoolFlags<int>(key);

            //register key if not already there
            if (!EditorPrefs.HasKey(key))
            {
                EditorPrefs.SetInt(key, 0);
            }
        }

        /// <summary>Accessor to the expended state of this specific mask.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <returns>True: All flagged area are expended</returns>
        public bool GetExpandedAreas(int index)
        {
            return m_State.HasFlag(index);
        }

        /// <summary>Setter to the expended state.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <param name="value">The expended state to set</param>
        public void SetExpandedAreas(int index, bool value)
        {
            m_State.SetFlag(index, value);
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

    /// <summary>Used in editor drawer part to store the state of expendable areas.</summary>
    /// <typeparam name="TState">An enum to use to describe the state.</typeparam>
    /// <typeparam name="TTarget">A type given to automatically compute the key.</typeparam>
    public struct ExpandedState<TState, TTarget> : IExpandedState<TState>
        where TState : struct, IConvertible
    {
        EditorPrefBoolFlags<TState> m_State;

        /// <summary>
        ///     Constructor will create the key to store in the EditorPref the state given generic type passed.
        ///     The key will be formated as such prefix:TTarget:TState:UI_State.
        /// </summary>
        /// <param name="defaultValue">If key did not exist, it will be created with this value for initialization.</param>
        /// <param name="prefix">[Optional] Prefix scope of the key (Default is CoreRP)</param>
        public ExpandedState(TState defaultValue, string prefix = "CoreRP")
        {
            string key = $"{prefix}:{typeof(TTarget).Name}:{typeof(TState).Name}:UI_State";
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
