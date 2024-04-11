using System;

namespace UnityEditor.Rendering
{
    /// <summary>Used in editor drawer part to store the state of expandable areas.</summary>
    /// <typeparam name="TState">An enum to use to describe the state.</typeparam>
    public abstract class ExpandedStateBase<TState>
        where TState : struct, IConvertible
    {
        /// <summary>Accessor to the expended state of this specific mask.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <returns>True: All flagged area are expended</returns>
        public abstract bool GetExpandedAreas(TState mask);
        /// <summary>Setter to the expended state.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <param name="value">The expended state to set</param>
        public abstract void SetExpandedAreas(TState mask, bool value);
        /// <summary> Utility to set all states to true </summary>
        public abstract void ExpandAll();
        /// <summary> Utility to set all states to false </summary>
        public abstract void CollapseAll();

        /// <summary>Get or set the state given the mask.</summary>
        /// <param name="mask">The filtering mask</param>
        /// <value>True: All flagged area are expended</value>
        public bool this[TState mask]
        {
            get => GetExpandedAreas(mask);
            set => SetExpandedAreas(mask, value);
        }
    }


    /// <summary>Used in editor drawer part to store the state of expandable areas using EditorPrefBoolFlags.</summary>
    /// <typeparam name="TState">An enum to use to describe the state.</typeparam>
    /// <typeparam name="TTarget">A type given to automatically compute the key.</typeparam>
    public class ExpandedState<TState, TTarget> : ExpandedStateBase<TState>
        where TState : struct, IConvertible
    {
        /// <summary>
        /// The variable which stores the state of expandable areas.
        /// </summary>
        protected internal EditorPrefBoolFlags<TState> m_State;

        /// <summary>
        ///     Constructor will create the key to store in the EditorPref the state given generic type passed.
        ///     The key will be formated as such prefix:TTarget:TState:UI_State.
        /// </summary>
        /// <param name="defaultValue">If key did not exist, it will be created with this value for initialization.</param>
        /// <param name="prefix">[Optional] Prefix scope of the key (Default is CoreRP)</param>
        /// <param name="stateId">[Optional] Postfix used to differentiate between different keys (Default is UI_State)</param>
        public ExpandedState(TState defaultValue, string prefix = "CoreRP", string stateId = "UI_State")
        {
            string key = $"{prefix}:{typeof(TTarget).Name}:{typeof(TState).Name}:{stateId}";
            m_State = new EditorPrefBoolFlags<TState>(key);

            //register key if not already there
            if (!EditorPrefs.HasKey(key))
            {
                EditorPrefs.SetInt(key, (int)(object)defaultValue);
            }
        }

        /// <inheritdoc/>
        public override bool GetExpandedAreas(TState mask)
        {
            return m_State.HasFlag(mask);
        }

        /// <inheritdoc/>
        public override void SetExpandedAreas(TState mask, bool value)
        {
            m_State.SetFlag(mask, value);
        }

        /// <inheritdoc/>
        public override void ExpandAll()
        {
            m_State.rawValue = uint.MaxValue;
        }

        /// <inheritdoc/>
        public override void CollapseAll()
        {
            m_State.rawValue = 0u;
        }
    }

    /// <summary>Used in editor drawer part to store the state of expandable areas using EditorPrefBoolFlags for a list of elements.</summary>
    /// <typeparam name="TTarget">A type given to automatically compute the key.</typeparam>
    public class ExpandedStateList<TTarget> : ExpandedState<int, TTarget>
    {
        /// <summary>
        ///     Constructor will create the key to store in the EditorPref the state given generic type passed.
        ///     The key will be formated as such prefix:TTarget:TState:UI_State.
        /// </summary>
        /// <param name="prefix">[Optional] Prefix scope of the key (Default is CoreRP)</param>
        public ExpandedStateList(string prefix = "CoreRP")
            : base(default(int), prefix, "UI_State_List") { }

        /// <summary>
        /// Swap flag between src index and dst index.
        /// </summary>
        /// <param name="srcIndex">src index to swap.</param>
        /// <param name="dstIndex">dst index to swap.</param>
        public void SwapFlags(int srcIndex, int dstIndex)
        {
            int srcFlag = 1 << srcIndex;
            int dstFlag = 1 << dstIndex;

            bool srcVal = GetExpandedAreas(srcFlag);
            SetExpandedAreas(srcFlag, GetExpandedAreas(dstFlag));
            SetExpandedAreas(dstFlag, srcVal);
        }

        /// <summary> Removes a flag at a given index which causes the following flags' index to decrease by one.</summary>
        /// <param name="index">The index of the flag to be removed.</param>
        public void RemoveFlagAtIndex(int index)
        {
            m_State.rawValue = RightShiftOnceFromIndexToMSB(index, m_State.rawValue);
        }

        /// <summary> Utility to logical right shift every bit from the index flag to MSB resulting in the index flag being shifted out.<\summary>
        /// <param name="index">Right shift every bit greater or equal to the index.</param>
        /// <param name="value">Value to make the operations on.</param>
        /// <returns>The right shift value after the given index.</returns>
        internal static uint RightShiftOnceFromIndexToMSB(int index, uint value)
        {                                                               // Example of each operation:
                                                                        // 1011 1001 - Value
            uint indexBit = 1u << index;                                // 0000 1000 - Index bit
            uint remainArea = indexBit - 1u;                            // 0000 0111
            uint remainBits = remainArea & value;                       // 0000 0001
            uint movedBits = (~remainArea - indexBit & value) >> 1;     // 1111 1000
                                                                        // 1111 0000
                                                                        // 1011 0000
                                                                        // 0101 1000
            return movedBits | remainBits;                              // 0101 1001 - Result
        }
    }
}
