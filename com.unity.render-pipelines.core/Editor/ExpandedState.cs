using System;
using UnityEditor;

namespace UnityEditor.Experimental.Rendering
{
    /// <summary>Used in editor drawer part to store the state of expendable areas.</summary>
    /// <typeparam name="TState">An enum to use to describe the state.</typeparam>
    /// <typeparam name="TTarget">A type given to automatically compute the key.</typeparam>
    public struct ExpandedState<TState, TTarget>
        where TState : struct, IConvertible
    {
        /// <summary>Key is automatically computed regarding the target type given</summary>
        public readonly string stateKey;

        /// <summary>Constructor will create the key to store in the EditorPref the state given generic type passed.</summary>
        /// <param name="defaultValue">If key did not exist, it will be created with this value for initialization.</param>
        public ExpandedState(TState defaultValue, string prefix = "CoreRP")
        {
            stateKey = string.Format("{0}:{1}:UI_State", prefix, typeof(TTarget).Name);

            //register key if not already there
            if (!EditorPrefs.HasKey(stateKey))
            {
                EditorPrefs.SetInt(stateKey, (int)(object)defaultValue);
            }
        }
        
        uint expandedState { get { return (uint)EditorPrefs.GetInt(stateKey); } set { EditorPrefs.SetInt(stateKey, (int)value); } }
        
        /// <summary>Get or set the state given the mask.</summary>
        public bool this[TState mask]
        {
            get { return GetExpandedAreas(mask); }
            set { SetExpandedAreas(mask, value); }
        }

        /// <summary>Accessor to the expended state of this specific mask.</summary>
        public bool GetExpandedAreas(TState mask)
        {
            // note on cast:
            //   - to object always ok
            //   - to int ok because of IConvertible. Cannot directly go to uint
            return (expandedState & (uint)(int)(object)mask) > 0;
        }

        /// <summary>Setter to the expended state.</summary>
        public void SetExpandedAreas(TState mask, bool value)
        {
            uint state = expandedState;
            // note on cast:
            //   - to object always ok
            //   - to int ok because of IConvertible. Cannot directly go to uint
            uint workMask = (uint)(int)(object)mask;

            if (value)
            {
                state |= workMask;
            }
            else
            {
                workMask = ~workMask;
                state &= workMask;
            }

            expandedState = state;
        }
    }
}
