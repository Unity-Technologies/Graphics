using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Serialized state of a Debug Item.
    /// </summary>
    [Serializable]
    public abstract class DebugState : ScriptableObject
    {
        /// <summary>
        /// Path of the Debug Item.
        /// </summary>
        [SerializeField]
        protected string m_QueryPath;

        // We need this to keep track of the state modified in the current frame.
        // This helps reduces the cost of re-applying states to original widgets and is also needed
        // when two states point to the same value (e.g. when using split enums like HDRP does for
        // the `fullscreenDebugMode`.
        internal static DebugState m_CurrentDirtyState;

        /// <summary>
        /// Path of the Debug Item.
        /// </summary>
        public string queryPath
        {
            get { return m_QueryPath; }
            internal set { m_QueryPath = value; }
        }

        /// <summary>
        /// Returns the value of the Debug Item.
        /// </summary>
        /// <returns>Value of the Debug Item.</returns>
        public abstract object GetValue();

        /// <summary>
        /// Set the value of the Debug Item.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="field">Debug Item field.</param>
        public abstract void SetValue(object value, DebugUI.IValueField field);

        /// <summary>
        /// OnEnable implementation.
        /// </summary>
        public virtual void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
        }
    }

    /// <summary>
    /// Generic serialized state of a Debug Item.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class DebugState<T> : DebugState
    {
        /// <summary>
        /// Value of the Debug Item.
        /// </summary>
        [SerializeField]
        protected T m_Value;

        /// <summary>
        /// Value of the Debug Item
        /// </summary>
        public virtual T value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        /// <summary>
        /// Returns the value of the Debug Item.
        /// </summary>
        /// <returns>Value of the Debug Item.</returns>
        public override object GetValue()
        {
            return value;
        }

        /// <summary>
        /// Set the value of the Debug Item.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="field">Debug Item field.</param>
        public override void SetValue(object value, DebugUI.IValueField field)
        {
            this.value = (T)field.ValidateValue(value);
        }

        /// <summary>
        /// Returns the hash code of the Debug Item.
        /// </summary>
        /// <returns>Hash code of the Debug Item</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 13;
                hash = hash * 23 + m_QueryPath.GetHashCode();
                if (value != null)
                    hash = hash * 23 + value.GetHashCode();
                
                return hash;
            }
        }
    }

    /// <summary>
    /// Attribute specifying which types should be save as this Debug State.
    /// </summary>
    public sealed class DebugStateAttribute : Attribute
    {
        internal readonly Type[] types;

        /// <summary>
        /// Debug State Attribute constructor
        /// </summary>
        /// <param name="types">List of types of the Debug State.</param>
        public DebugStateAttribute(params Type[] types)
        {
            this.types = types;
        }
    }

    // Builtins
    /// <summary>
    /// Boolean Debug State.
    /// </summary>
    [Serializable, DebugState(typeof(DebugUI.BoolField), typeof(DebugUI.Foldout), typeof(DebugUI.HistoryBoolField))]
    public sealed class DebugStateBool : DebugState<bool> { }

    /// <summary>
    /// Enums Debug State.
    /// </summary>
    [Serializable, DebugState(typeof(DebugUI.EnumField), typeof(DebugUI.HistoryEnumField))]
    public sealed class DebugStateEnum : DebugState<int>
    {
        DebugUI.EnumField m_EnumField;

        /// <summary>
        /// Set the value of the Debug Item.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="field">Debug Item field.</param>
        public override void SetValue(object value, DebugUI.IValueField field)
        {
            m_EnumField = field as DebugUI.EnumField;
            base.SetValue(value, field);
        }

        /// <summary>
        /// On Enable method from <see cref="ScriptableObject"/>
        /// </summary>
        public override void OnEnable()
        {
            base.OnEnable();

            if (m_EnumField == null)
                return;

            m_EnumField.SetValue(value);
            base.SetValue(value, m_EnumField);
        }
    }

    /// <summary>
    /// Integer Debug State.
    /// </summary>
    [Serializable, DebugState(typeof(DebugUI.IntField))]
    public sealed class DebugStateInt : DebugState<int> { }

    /// <summary>
    /// Object Debug State.
    /// </summary>
    [Serializable, DebugState(typeof(DebugUI.ObjectPopupField))]
    public sealed class DebugStateObject : DebugState<UnityEngine.Object> { }

    /// <summary>
    /// Flags Debug State.
    /// </summary>
    [Serializable, DebugState(typeof(DebugUI.BitField))]
    public sealed class DebugStateFlags : DebugState<Enum>
    {
        [SerializeField]
        private SerializableEnum m_SerializableEnum;

        /// <summary>
        /// Value of the Debug Item
        /// </summary>
        public override Enum value
        {
            get => m_SerializableEnum?.value ?? default;
            set => m_SerializableEnum.value = value;
        }

        /// <summary>
        /// Set the value of the Debug Item.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="field">Debug Item field.</param>
        public override void SetValue(object value, DebugUI.IValueField field)
        {
            if (m_SerializableEnum == null)
                m_SerializableEnum = new SerializableEnum((field as DebugUI.BitField).enumType);
            base.SetValue(value, field);
        }
    }

    /// <summary>
    /// Unsigned Integer Debug State.
    /// </summary>
    [Serializable, DebugState(typeof(DebugUI.UIntField))]
    public sealed class DebugStateUInt : DebugState<uint> { }

    /// <summary>
    /// Float Debug State.
    /// </summary>
    [Serializable, DebugState(typeof(DebugUI.FloatField))]
    public sealed class DebugStateFloat : DebugState<float> { }

    /// <summary>
    /// Color Debug State.
    /// </summary>
    [Serializable, DebugState(typeof(DebugUI.ColorField))]
    public sealed class DebugStateColor : DebugState<Color> { }

    /// <summary>
    /// Vector2 Debug State.
    /// </summary>
    [Serializable, DebugState(typeof(DebugUI.Vector2Field))]
    public sealed class DebugStateVector2 : DebugState<Vector2> { }

    /// <summary>
    /// Vector3 Debug State.
    /// </summary>
    [Serializable, DebugState(typeof(DebugUI.Vector3Field))]
    public sealed class DebugStateVector3 : DebugState<Vector3> { }

    /// <summary>
    /// Vector4 Debug State.
    /// </summary>
    [Serializable, DebugState(typeof(DebugUI.Vector4Field))]
    public sealed class DebugStateVector4 : DebugState<Vector4> { }
}
