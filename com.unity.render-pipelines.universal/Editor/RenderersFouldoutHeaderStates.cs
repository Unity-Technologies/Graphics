

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    public class RendererExpandedState : IExpandedState<int>
    {
        EditorPrefBoolFlags<int> m_State;

        /// <summary>
        ///     Constructor will create the key to store in the EditorPref the state given generic type passed.
        ///     The key will be formated as such prefix:TTarget:TState:UI_State.
        /// </summary>
        /// <param name="defaultValue">If key did not exist, it will be created with this value for initialization.</param>
        /// <param name="prefix">[Optional] Prefix scope of the key (Default is CoreRP)</param>
        public RendererExpandedState(int index)
        {
            string key = $"URP:{typeof(ScriptableRendererData).Name}:{index}:UI_State";
            m_State = new EditorPrefBoolFlags<int>(key);

            //register key if not already there
            if (!EditorPrefs.HasKey(key))
            {
                EditorPrefs.SetInt(key, 0);
            }
        }

        /// <summary>Get or set the state given the mask.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <returns>True: All flagged area are expended</returns>
        public bool this[int mask]
        {
            get { return m_State.HasFlag(mask); }
            set { m_State.SetFlag(mask, value); }
        }

        /// <summary>Accessor to the expended state of this specific mask.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <returns>True: All flagged area are expended</returns>
        public bool GetExpandedAreas(int mask)
        {
            return m_State.HasFlag(mask);
        }

        /// <summary>Setter to the expended state.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <param name="value">The expended state to set</param>
        public void SetExpandedAreas<TState>(TState mask, bool value) where TState : struct, IConvertible
        {
            m_State.SetFlag((int)(object)mask, value);
        }

        /// <summary>Accessor to the expended state of this specific mask.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <returns>True: All flagged area are expended</returns>
        public bool GetExpandedAreas<TState>(TState mask) where TState : struct, IConvertible
        {
            return m_State.HasFlag((int)(object)mask);
        }

        /// <summary>Setter to the expended state.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <param name="value">The expended state to set</param>
        public void SetExpandedAreas(int mask, bool value)
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

        public void CopyKeys(RendererExpandedState state)
        {
            m_State.value = state.m_State.value;
        }

        public void SwapKeys(ref RendererExpandedState state)
        {
            uint keys = m_State.rawValue;
            m_State.rawValue = state.m_State.rawValue;
            state.m_State.rawValue = keys;
        }
    }

    internal static class RenderersFoldoutStates
    {
        static readonly ExpandedStateList<ScriptableRendererData> k_showRenderersUI = new("URP");
        static readonly AdditionalPropertiesStateList<ScriptableRendererData> k_showAdditionalRenderersUI = new("URP");
        static List<RendererExpandedState> s_RendererStates = new();

        public static ExpandedStateList<ScriptableRendererData> GetRenderersShowState()
        {
            return k_showRenderersUI;
        }

        public static AdditionalPropertiesStateList<ScriptableRendererData> GetAdditionalRenderersShowState()
        {
            return k_showAdditionalRenderersUI;
        }

        public static RendererExpandedState GetRendererState(int index)
        {
            int size = s_RendererStates.Count;
            for (int i = size; i <= index; i++)
            {
                s_RendererStates.Add(new RendererExpandedState(size + i));
            }
            return s_RendererStates[index];
        }

        public static void SwapRendererStates(int indexA, int indexB)
        {
            int size = s_RendererStates.Count;
            for (int i = size; i <= Math.Max(indexA, indexB); i++)
            {
                s_RendererStates.Add(new RendererExpandedState(size + i));
            }
            var tmp = s_RendererStates[indexB];
            s_RendererStates[indexA].SwapKeys(ref tmp);
            s_RendererStates[indexB] = tmp;
        }

        [SetAdditionalPropertiesVisibility]
        internal static void SetAdditionalPropertiesVisibility(bool value)
        {
            if (value)
                k_showAdditionalRenderersUI.ShowAll();
            else
                k_showAdditionalRenderersUI.HideAll();
        }

    }
}
