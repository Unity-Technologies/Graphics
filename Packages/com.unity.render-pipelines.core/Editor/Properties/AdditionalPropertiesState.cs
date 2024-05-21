using System;
using UnityEditor.AnimatedValues;
using System.Collections.Generic;

namespace UnityEditor.Rendering
{
    /// <summary>Used in editor drawer part to store the state of additional properties areas.</summary>
    /// <typeparam name="TState">An enum to use to describe the state.</typeparam>
    public abstract class AdditionalPropertiesStateBase<TState>
        where TState : struct, IConvertible
    {
        HashSet<Editor> m_Editors = new ();

        /// <summary>Get or set the state given the mask.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <value>True: All flagged area are expended</value>
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
        /// <param name="mask">The filtering mask.</param>
        /// <param name="value">True to show the additional properties.</param>
        public void SetAdditionalPropertiesState(TState mask, bool value)
        {
            SetAdditionalPropertiesStateValue(mask, value);
        }

        /// <summary>Setter to the expended state without resetting animation.</summary>
        /// <param name="mask">The filtering mask.</param>
        /// <param name="value">True to show the additional properties.</param>
        protected abstract void SetAdditionalPropertiesStateValue(TState mask, bool value);

        /// <summary> Utility to set all states to true </summary>
        public abstract void ShowAll();

        /// <summary> Utility to set all states to false </summary>
        public abstract void HideAll();

        /// <summary>
        /// Resets the animation associated with the given mask to a default state with the animated value set to 1.0 and the target value set to 0.0.
        /// </summary>
        /// <param name="mask">The state mask used to retrieve the associated animation.</param>
        protected internal void ResetAnimation(TState mask)
        {
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
    /// <typeparam name="TTarget">A type given to automatically compute the key.</typeparam>
    public class AdditionalPropertiesState<TState, TTarget> : AdditionalPropertiesStateBase<TState>
        where TState : struct, IConvertible
    {
        /// <summary>
        /// Stores the expanded or collapsed state of each section defined by <typeparamref name="TState"/>.
        /// </summary>
        protected internal EditorPrefBoolFlags<TState> m_State;


        /// <summary>Constructor will create the key to store in the EditorPref the state given generic type passed.</summary>
        /// <param name="defaultValue">If key did not exist, it will be created with this value for initialization.</param>
        /// <param name="prefix">[Optional] Prefix scope of the key (Default is CoreRP)</param>
        /// <param name="stateId">[Optional] Postfix used to differentiate between different keys (Default is UI_AP_State)</param>
        public AdditionalPropertiesState(TState defaultValue, string prefix = "CoreRP", string stateId = "UI_AP_State")
        {
            string key = $"{prefix}:{typeof(TTarget).Name}:{typeof(TState).Name}:{stateId}";
            m_State = new EditorPrefBoolFlags<TState>(key);
            AdvancedProperties.UpdateShowAdvancedProperties(key, m_State.rawValue != 0u);
        }

        /// <inheritdoc/>
        public override bool GetAdditionalPropertiesState(TState _)
        {
            return AdvancedProperties.enabled;
        }

        /// <inheritdoc/>
        protected override void SetAdditionalPropertiesStateValue(TState _, bool value)
        {
            AdvancedProperties.enabled = value;
        }

        /// <inheritdoc/>
        public override void ShowAll()
        {
            AdvancedProperties.enabled = true;
        }

        /// <inheritdoc/>
        public override void HideAll()
        {
            AdvancedProperties.enabled = false;
        }
    }

    /// <summary>Used in editor drawer part to store the state of additional properties for a list of elements.</summary>
    /// <typeparam name="TTarget">A type given to automatically compute the key.</typeparam>
    public class AdditionalPropertiesStateList<TTarget> : AdditionalPropertiesState<int, TTarget>
    {
        /// <summary>Constructor will create the key to store in the EditorPref the state given generic type passed.</summary>
        /// <param name="prefix">[Optional] Prefix scope of the key (Default is CoreRP)</param>
        public AdditionalPropertiesStateList(string prefix = "CoreRP")
            : base(default(int), prefix, "UI_AP_State_List") { }

        /// <summary>
        /// Swap flag between src index and dst index.
        /// </summary>
        /// <param name="srcIndex">src index to swap.</param>
        /// <param name="dstIndex">dst index to swap.</param>
        public void SwapFlags(int srcIndex, int dstIndex)
        {
            int srcFlag = 1 << srcIndex;
            int dstFlag = 1 << dstIndex;

            bool srcVal = GetAdditionalPropertiesState(srcFlag);
            SetAdditionalPropertiesState(srcFlag, GetAdditionalPropertiesState(dstFlag));
            SetAdditionalPropertiesState(dstFlag, srcVal);
        }

        /// <summary> Removes a flag at a given index which causes the following flags' index to decrease by one.</summary>
        /// <param name="index">The index of the flag to be removed.</param>
        public void RemoveFlagAtIndex(int index)
        {
            m_State.rawValue = ExpandedStateList<TTarget>.RightShiftOnceFromIndexToMSB(index, m_State.rawValue);
        }
    }
}
