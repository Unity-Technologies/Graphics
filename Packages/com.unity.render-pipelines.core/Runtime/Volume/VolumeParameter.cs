using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace UnityEngine.Rendering
{
    // We need this base class to be able to store a list of VolumeParameter in collections as we
    // can't store VolumeParameter<T> with variable T types in the same collection. As a result some
    // of the following is a bit hacky...

    /// <summary>
    /// The base class for all parameters types stored in a <see cref="VolumeComponent"/>.
    /// </summary>
    /// <seealso cref="VolumeParameter{T}"/>
    public abstract class VolumeParameter : ICloneable
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal int fieldHash { get; set; }
#endif

        /// <summary>
        /// A beautified string for debugger output. This is set on a <c>DebuggerDisplay</c> on every
        /// parameter types.
        /// </summary>
        public const string k_DebuggerDisplay = "{m_Value} ({m_OverrideState})";

        /// <summary>
        /// The current override state for this parameter. The Volume system considers overriden parameters
        /// for blending, and ignores non-overriden ones.
        /// </summary>
        /// <seealso cref="overrideState"/>
        [SerializeField]
        protected bool m_OverrideState;

        /// <summary>
        /// The current override state for this parameter. The Volume system considers overriden parameters
        /// for blending, and ignores non-overriden ones.
        /// </summary>
        /// <remarks>
        /// You can override this property to define custom behaviors when the override state
        /// changes.
        /// </remarks>
        /// <seealso cref="m_OverrideState"/>
        public virtual bool overrideState
        {
            get => m_OverrideState;
            set => m_OverrideState = value;
        }

        internal abstract void Interp(VolumeParameter from, VolumeParameter to, float t);

        /// <summary>
        /// Casts and gets the typed value of this parameter.
        /// </summary>
        /// <typeparam name="T">The type of the value stored in this parameter</typeparam>
        /// <returns>A value of type <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// This method is unsafe and does not do any type checking.
        /// </remarks>
        public T GetValue<T>()
        {
            return ((VolumeParameter<T>)this).value;
        }

        /// <summary>
        /// Sets the value of this parameter to the value in <paramref name="parameter"/>.
        /// </summary>
        /// <param name="parameter">The <see cref="VolumeParameter"/> to copy the value from.</param>
        public abstract void SetValue(VolumeParameter parameter);

        /// <summary>
        /// Unity calls this method when the parent <see cref="VolumeComponent"/> loads.
        /// </summary>
        /// <remarks>
        /// Use this if you need to access fields and properties that you can not access in
        /// the constructor of a <c>ScriptableObject</c>. (<see cref="VolumeParameter"/> are
        /// generally declared and initialized in a <see cref="VolumeComponent"/>, which is a
        /// <c>ScriptableObject</c>). Unity calls this right after it constructs the parent
        /// <see cref="VolumeComponent"/>, thus allowing access to previously
        /// inaccessible fields and properties.
        /// </remarks>
        protected internal virtual void OnEnable()
        {
        }

        /// <summary>
        /// Unity calls this method when the parent <see cref="VolumeComponent"/> goes out of scope.
        /// </summary>
        protected internal virtual void OnDisable()
        {
        }

        /// <summary>
        /// Checks if a given type is an <see cref="ObjectParameter{T}"/>.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><c>true</c> if <paramref name="type"/> is an <see cref="ObjectParameter{T}"/>,
        /// <c>false</c> otherwise.</returns>
        public static bool IsObjectParameter(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ObjectParameter<>))
                return true;

            return type.BaseType != null
                && IsObjectParameter(type.BaseType);
        }

        /// <summary>
        /// Override this method to free all allocated resources
        /// </summary>
        public virtual void Release() { }

        /// <summary>
        /// Clones the current instance of the <see cref="VolumeParameter"/>
        /// </summary>
        /// <returns>A new created instance with the same values as the current instance of <see cref="VolumeParameter"/></returns>

        public abstract object Clone();
    }

    /// <summary>
    /// A generic implementation of <see cref="VolumeParameter"/>. Custom parameters should derive
    /// from this class and implement their own behavior.
    /// </summary>
    /// <typeparam name="T">The type of value to hold in this parameter.</typeparam>
    /// <remarks>
    /// <typeparamref name="T"/> should a serializable type.
    /// Due to limitations with the serialization system in Unity, you should not use this class
    /// directly to declare parameters in a <see cref="VolumeComponent"/>. Instead, use one of the
    /// pre-flatten types (like <see cref="FloatParameter"/>, or make your own by extending this
    /// class.
    /// </remarks>
    /// <example>
    /// This sample code shows how to make a custom parameter holding a <c>float</c>:
    /// <code>
    /// using UnityEngine.Rendering;
    ///
    /// [Serializable]
    /// public sealed class MyFloatParameter : VolumeParameter&lt;float&gt;
    /// {
    ///     public MyFloatParameter(float value, bool overrideState = false)
    ///         : base(value, overrideState) { }
    ///
    ///     public sealed override void Interp(float from, float to, float t)
    ///     {
    ///         m_Value = from + (to - from) * t;
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="VolumeParameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class VolumeParameter<T> : VolumeParameter, IEquatable<VolumeParameter<T>>
    {
        /// <summary>
        /// The value stored and serialized by this parameter.
        /// </summary>
        [SerializeField]
        protected T m_Value;

        /// <summary>
        /// The value that this parameter stores.
        /// </summary>
        /// <remarks>
        /// You can override this property to define custom behaviors when the value is changed.
        /// </remarks>
        public virtual T value
        {
            get => m_Value;
            set => m_Value = value;
        }

        /// <summary>
        /// Creates a new <see cref="VolumeParameter{T}"/> instance.
        /// </summary>
        public VolumeParameter()
            : this(default, false)
        {
        }

        /// <summary>
        /// Creates a new <see cref="VolumeParameter{T}"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        protected VolumeParameter(T value, bool overrideState)
        {
            m_Value = value;
            this.overrideState = overrideState;
        }

        internal override void Interp(VolumeParameter from, VolumeParameter to, float t)
        {
            // Note: this is relatively unsafe (assumes that from and to are both holding type T)
            Interp((from as VolumeParameter<T>).value, (to as VolumeParameter<T>).value, t);
        }

        /// <summary>
        /// Interpolates two values using a factor <paramref name="t"/>.
        /// </summary>
        /// <remarks>
        /// By default, this method does a "snap" interpolation, meaning it returns the value
        /// <paramref name="to"/> if <paramref name="t"/> is higher than 0, and <paramref name="from"/>
        /// otherwise.
        /// </remarks>
        /// <param name="from">The start value.</param>
        /// <param name="to">The end value.</param>
        /// <param name="t">The interpolation factor in range [0,1].</param>
        public virtual void Interp(T from, T to, float t)
        {
            // Default interpolation is naive
            m_Value = t > 0f ? to : from;
        }

        /// <summary>
        /// Sets the value for this parameter and sets its override state to <c>true</c>.
        /// </summary>
        /// <param name="x">The value to assign to this parameter.</param>
        public void Override(T x)
        {
            overrideState = true;
            m_Value = x;
        }

        /// <summary>
        /// Sets the value of this parameter to the value in <paramref name="parameter"/>.
        /// </summary>
        /// <param name="parameter">The <see cref="VolumeParameter"/> to copy the value from.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void SetValue(VolumeParameter parameter)
        {
            m_Value = ((VolumeParameter<T>)parameter).m_Value;
        }

        /// <summary>
        /// Returns a hash code for the current object.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + overrideState.GetHashCode();

                if (!EqualityComparer<T>.Default.Equals(value, default)) // Catches null for references with boxing of value types
                    hash = hash * 23 + value.GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() => $"{value} ({overrideState})";

        /// <summary>
        /// Compares the value in a parameter with another value of the same type.
        /// </summary>
        /// <param name="lhs">The first value in a <see cref="VolumeParameter"/>.</param>
        /// <param name="rhs">The second value.</param>
        /// <returns><c>true</c> if both values are equal, <c>false</c> otherwise.</returns>
        public static bool operator ==(VolumeParameter<T> lhs, T rhs) => lhs != null && !ReferenceEquals(lhs.value, null) && lhs.value.Equals(rhs);

        /// <summary>
        /// Compares the value store in a parameter with another value of the same type.
        /// </summary>
        /// <param name="lhs">The first value in a <see cref="VolumeParameter"/>.</param>
        /// <param name="rhs">The second value.</param>
        /// <returns><c>false</c> if both values are equal, <c>true</c> otherwise</returns>
        public static bool operator !=(VolumeParameter<T> lhs, T rhs) => !(lhs == rhs);

        /// <summary>
        /// Checks if this parameter is equal to another.
        /// </summary>
        /// <param name="other">The other parameter to check against.</param>
        /// <returns><c>true</c> if both parameters are equal, <c>false</c> otherwise</returns>
        public bool Equals(VolumeParameter<T> other)
        {
            if (ReferenceEquals(null, other))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return EqualityComparer<T>.Default.Equals(m_Value, other.m_Value);
        }

        /// <summary>
        /// Determines whether two object instances are equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object, <c>false</c> otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((VolumeParameter<T>)obj);
        }

        /// <inheritdoc/>
        public override object Clone()
        {
            return new VolumeParameter<T>(GetValue<T>(), overrideState);
        }

        /// <summary>
        /// Explicitly downcast a <see cref="VolumeParameter{T}"/> to a value of type
        /// <typeparamref name="T"/>.
        /// </summary>
        /// <param name="prop">The parameter to downcast.</param>
        /// <returns>A value of type <typeparamref name="T"/>.</returns>
        public static explicit operator T(VolumeParameter<T> prop) => prop.m_Value;
    }

    //
    // The serialization system in Unity can't serialize generic types, the workaround is to extend
    // and flatten pre-defined generic types.
    // For enums it's recommended to make your own types on the spot, like so:
    //
    //  [Serializable]
    //  public sealed class MyEnumParameter : VolumeParameter<MyEnum> { }
    //  public enum MyEnum { One, Two }
    //

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <c>bool</c> value.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class BoolParameter : VolumeParameter<bool>
    {
        /// <summary>
        /// Creates a new <see cref="BoolParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter</param>
        /// <param name="overrideState">The initial override state for the parameter</param>
        public BoolParameter(bool value, bool overrideState = false)
            : base(value, overrideState)
        {
        }

        /// <summary>
        /// Creates a new <see cref="BoolParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter</param>
        /// <param name="displayType">The display type to use for the parameter</param>
        /// <param name="overrideState">The initial override state for the parameter</param>
        public BoolParameter(bool value, DisplayType displayType, bool overrideState = false)
            : base(value, overrideState)
        {
            this.displayType = displayType;
        }

        /// <summary>
        /// Boolean widget type.
        /// </summary>
        public enum DisplayType
        {
            /// <summary> Display boolean parameter as checkbox. </summary>
            Checkbox,
            /// <summary> Display boolean parameter as enum popup with Disabled/Enabled options. </summary>
            EnumPopup
        }

        /// <summary>
        /// Type of widget used to display the <see cref="BoolParameter"/> in the UI.
        /// </summary>
        [NonSerialized]
        public DisplayType displayType = DisplayType.Checkbox;
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <c>LayerMask</c> value.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class LayerMaskParameter : VolumeParameter<LayerMask>
    {
        /// <summary>
        /// Creates a new <see cref="LayerMaskParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public LayerMaskParameter(LayerMask value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds an <c>int</c> value.
    /// </summary>
    /// <seealso cref="MinIntParameter"/>
    /// <seealso cref="MaxIntParameter"/>
    /// <seealso cref="ClampedIntParameter"/>
    /// <seealso cref="NoInterpIntParameter"/>
    /// <seealso cref="NoInterpMinIntParameter"/>
    /// <seealso cref="NoInterpMaxIntParameter"/>
    /// <seealso cref="NoInterpClampedIntParameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class IntParameter : VolumeParameter<int>
    {
        /// <summary>
        /// Creates a new <see cref="IntParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public IntParameter(int value, bool overrideState = false)
            : base(value, overrideState) { }

        /// <summary>
        /// Interpolates between two <c>int</c> values.
        /// </summary>
        /// <param name="from">The start value</param>
        /// <param name="to">The end value</param>
        /// <param name="t">The interpolation factor in range [0,1]</param>
        public sealed override void Interp(int from, int to, float t)
        {
            // Int snapping interpolation. Don't use this for enums as they don't necessarily have
            // contiguous values. Use the default interpolator instead (same as bool).
            m_Value = (int)(from + (to - from) * t);
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a non-interpolating <c>int</c> value.
    /// </summary>
    /// <seealso cref="IntParameter"/>
    /// <seealso cref="MinIntParameter"/>
    /// <seealso cref="MaxIntParameter"/>
    /// <seealso cref="ClampedIntParameter"/>
    /// <seealso cref="NoInterpMinIntParameter"/>
    /// <seealso cref="NoInterpMaxIntParameter"/>
    /// <seealso cref="NoInterpClampedIntParameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class NoInterpIntParameter : VolumeParameter<int>
    {
        /// <summary>
        /// Creates a new <see cref="NoInterpIntParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public NoInterpIntParameter(int value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds an <c>int</c> value clamped to a
    /// minimum value.
    /// </summary>
    /// <seealso cref="IntParameter"/>
    /// <seealso cref="MaxIntParameter"/>
    /// <seealso cref="ClampedIntParameter"/>
    /// <seealso cref="NoInterpIntParameter"/>
    /// <seealso cref="NoInterpMinIntParameter"/>
    /// <seealso cref="NoInterpMaxIntParameter"/>
    /// <seealso cref="NoInterpClampedIntParameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class MinIntParameter : IntParameter
    {
        /// <summary>
        /// The minimum value to clamp this parameter to.
        /// </summary>
        [NonSerialized]
        public int min;

        /// <summary>
        /// The value that this parameter stores.
        /// </summary>
        /// <remarks>
        /// You can override this property to define custom behaviors when the value is changed.
        /// </remarks>
        public override int value
        {
            get => m_Value;
            set => m_Value = Mathf.Max(value, min);
        }

        /// <summary>
        /// Creates a new <see cref="MinIntParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="min">The minimum value to clamp the parameter to.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public MinIntParameter(int value, int min, bool overrideState = false)
            : base(value, overrideState)
        {
            this.min = min;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a non-interpolating <c>int</c> value that
    /// clamped to a minimum value.
    /// </summary>
    /// <seealso cref="IntParameter"/>
    /// <seealso cref="MinIntParameter"/>
    /// <seealso cref="MaxIntParameter"/>
    /// <seealso cref="ClampedIntParameter"/>
    /// <seealso cref="NoInterpIntParameter"/>
    /// <seealso cref="NoInterpMaxIntParameter"/>
    /// <seealso cref="NoInterpClampedIntParameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class NoInterpMinIntParameter : VolumeParameter<int>
    {
        /// <summary>
        /// The minimum value to clamp this parameter to.
        /// </summary>
        [NonSerialized]
        public int min;

        /// <summary>
        /// The value that this parameter stores.
        /// </summary>
        /// <remarks>
        /// You can override this property to define custom behaviors when the value is changed.
        /// </remarks>
        public override int value
        {
            get => m_Value;
            set => m_Value = Mathf.Max(value, min);
        }

        /// <summary>
        /// Creates a new <see cref="NoInterpMinIntParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="min">The minimum value to clamp the parameter to.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public NoInterpMinIntParameter(int value, int min, bool overrideState = false)
            : base(value, overrideState)
        {
            this.min = min;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds an <c>int</c> value clamped to a
    /// maximum value.
    /// </summary>
    /// <seealso cref="IntParameter"/>
    /// <seealso cref="MinIntParameter"/>
    /// <seealso cref="ClampedIntParameter"/>
    /// <seealso cref="NoInterpIntParameter"/>
    /// <seealso cref="NoInterpMinIntParameter"/>
    /// <seealso cref="NoInterpMaxIntParameter"/>
    /// <seealso cref="NoInterpClampedIntParameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class MaxIntParameter : IntParameter
    {
        /// <summary>
        /// The maximum value to clamp this parameter to.
        /// </summary>
        [NonSerialized]
        public int max;

        /// <summary>
        /// The value that this parameter stores.
        /// </summary>
        /// <remarks>
        /// You can override this property to define custom behaviors when the value is changed.
        /// </remarks>
        public override int value
        {
            get => m_Value;
            set => m_Value = Mathf.Min(value, max);
        }

        /// <summary>
        /// Creates a new <see cref="MaxIntParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="max">The maximum value to clamp the parameter to.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public MaxIntParameter(int value, int max, bool overrideState = false)
            : base(value, overrideState)
        {
            this.max = max;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a non-interpolating <c>int</c> value that
    /// clamped to a maximum value.
    /// </summary>
    /// <seealso cref="IntParameter"/>
    /// <seealso cref="MinIntParameter"/>
    /// <seealso cref="MaxIntParameter"/>
    /// <seealso cref="ClampedIntParameter"/>
    /// <seealso cref="NoInterpIntParameter"/>
    /// <seealso cref="NoInterpMinIntParameter"/>
    /// <seealso cref="NoInterpClampedIntParameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class NoInterpMaxIntParameter : VolumeParameter<int>
    {
        /// <summary>
        /// The maximum value to clamp this parameter to.
        /// </summary>
        [NonSerialized]
        public int max;

        /// <summary>
        /// The value that this parameter stores.
        /// </summary>
        /// <remarks>
        /// You can override this property to define custom behaviors when the value is changed.
        /// </remarks>
        public override int value
        {
            get => m_Value;
            set => m_Value = Mathf.Min(value, max);
        }

        /// <summary>
        /// Creates a new <see cref="NoInterpMaxIntParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="max">The maximum value to clamp the parameter to.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public NoInterpMaxIntParameter(int value, int max, bool overrideState = false)
            : base(value, overrideState)
        {
            this.max = max;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds an <c>int</c> value clamped between a
    /// minimum and a maximum value.
    /// </summary>
    /// <seealso cref="IntParameter"/>
    /// <seealso cref="MinIntParameter"/>
    /// <seealso cref="MaxIntParameter"/>
    /// <seealso cref="NoInterpIntParameter"/>
    /// <seealso cref="NoInterpMinIntParameter"/>
    /// <seealso cref="NoInterpMaxIntParameter"/>
    /// <seealso cref="NoInterpClampedIntParameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class ClampedIntParameter : IntParameter
    {
        /// <summary>
        /// The minimum value to clamp this parameter to.
        /// </summary>
        [NonSerialized]
        public int min;

        /// <summary>
        /// The maximum value to clamp this parameter to.
        /// </summary>
        [NonSerialized]
        public int max;

        /// <summary>
        /// The value that this parameter stores.
        /// </summary>
        /// <remarks>
        /// You can override this property to define custom behaviors when the value is changed.
        /// </remarks>
        public override int value
        {
            get => m_Value;
            set => m_Value = Mathf.Clamp(value, min, max);
        }

        /// <summary>
        /// Creates a new <see cref="ClampedIntParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="min">The minimum value to clamp the parameter to</param>
        /// <param name="max">The maximum value to clamp the parameter to.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public ClampedIntParameter(int value, int min, int max, bool overrideState = false)
            : base(value, overrideState)
        {
            this.min = min;
            this.max = max;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a non-interpolating <c>int</c> value
    /// clamped between a minimum and a maximum value.
    /// </summary>
    /// <seealso cref="IntParameter"/>
    /// <seealso cref="MinIntParameter"/>
    /// <seealso cref="MaxIntParameter"/>
    /// <seealso cref="ClampedIntParameter"/>
    /// <seealso cref="NoInterpIntParameter"/>
    /// <seealso cref="NoInterpMinIntParameter"/>
    /// <seealso cref="NoInterpMaxIntParameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class NoInterpClampedIntParameter : VolumeParameter<int>
    {
        /// <summary>
        /// The minimum value to clamp this parameter to.
        /// </summary>
        [NonSerialized]
        public int min;

        /// <summary>
        /// The maximum value to clamp this parameter to.
        /// </summary>
        [NonSerialized]
        public int max;

        /// <summary>
        /// The value that this parameter stores.
        /// </summary>
        /// <remarks>
        /// You can override this property to define custom behaviors when the value is changed.
        /// </remarks>
        public override int value
        {
            get => m_Value;
            set => m_Value = Mathf.Clamp(value, min, max);
        }

        /// <summary>
        /// Creates a new <see cref="NoInterpClampedIntParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="min">The minimum value to clamp the parameter to</param>
        /// <param name="max">The maximum value to clamp the parameter to.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public NoInterpClampedIntParameter(int value, int min, int max, bool overrideState = false)
            : base(value, overrideState)
        {
            this.min = min;
            this.max = max;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <c>float</c> value.
    /// </summary>
    /// <seealso cref="MinFloatParameter"/>
    /// <seealso cref="MaxFloatParameter"/>
    /// <seealso cref="ClampedFloatParameter"/>
    /// <seealso cref="FloatRangeParameter"/>
    /// <seealso cref="NoInterpFloatParameter"/>
    /// <seealso cref="NoInterpMinFloatParameter"/>
    /// <seealso cref="NoInterpMaxFloatParameter"/>
    /// <seealso cref="NoInterpClampedFloatParameter"/>
    /// <seealso cref="NoInterpFloatRangeParameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class FloatParameter : VolumeParameter<float>
    {
        /// <summary>
        /// Creates a new <seealso cref="FloatParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter</param>
        /// <param name="overrideState">The initial override state for the parameter</param>
        public FloatParameter(float value, bool overrideState = false)
            : base(value, overrideState) { }

        /// <summary>
        /// Interpolates between two <c>float</c> values.
        /// </summary>
        /// <param name="from">The start value</param>
        /// <param name="to">The end value</param>
        /// <param name="t">The interpolation factor in range [0,1]</param>
        public sealed override void Interp(float from, float to, float t)
        {
            m_Value = from + (to - from) * t;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a non-interpolating <c>float</c> value.
    /// </summary>
    /// <seealso cref="FloatParameter"/>
    /// <seealso cref="MinFloatParameter"/>
    /// <seealso cref="MaxFloatParameter"/>
    /// <seealso cref="ClampedFloatParameter"/>
    /// <seealso cref="FloatRangeParameter"/>
    /// <seealso cref="NoInterpMinFloatParameter"/>
    /// <seealso cref="NoInterpMaxFloatParameter"/>
    /// <seealso cref="NoInterpClampedFloatParameter"/>
    /// <seealso cref="NoInterpFloatRangeParameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class NoInterpFloatParameter : VolumeParameter<float>
    {
        /// <summary>
        /// Creates a new <seealso cref="NoInterpFloatParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public NoInterpFloatParameter(float value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <c>float</c> value clamped to a minimum value.
    /// </summary>
    /// <seealso cref="FloatParameter"/>
    /// <seealso cref="MaxFloatParameter"/>
    /// <seealso cref="ClampedFloatParameter"/>
    /// <seealso cref="FloatRangeParameter"/>
    /// <seealso cref="NoInterpFloatParameter"/>
    /// <seealso cref="NoInterpMinFloatParameter"/>
    /// <seealso cref="NoInterpMaxFloatParameter"/>
    /// <seealso cref="NoInterpClampedFloatParameter"/>
    /// <seealso cref="NoInterpFloatRangeParameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class MinFloatParameter : FloatParameter
    {
        /// <summary>
        /// The minimum value to clamp this parameter to.
        /// </summary>
        [NonSerialized]
        public float min;

        /// <summary>
        /// The value that this parameter stores.
        /// </summary>
        /// <remarks>
        /// You can override this property to define custom behaviors when the value is changed.
        /// </remarks>
        public override float value
        {
            get => m_Value;
            set => m_Value = Mathf.Max(value, min);
        }

        /// <summary>
        /// Creates a new <seealso cref="MinFloatParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="min">The minimum value to clamp the parameter to.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public MinFloatParameter(float value, float min, bool overrideState = false)
            : base(value, overrideState)
        {
            this.min = min;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a non-interpolating <c>float</c> value clamped to
    /// a minimum value.
    /// </summary>
    /// <seealso cref="FloatParameter"/>
    /// <seealso cref="MinFloatParameter"/>
    /// <seealso cref="MaxFloatParameter"/>
    /// <seealso cref="ClampedFloatParameter"/>
    /// <seealso cref="FloatRangeParameter"/>
    /// <seealso cref="NoInterpFloatParameter"/>
    /// <seealso cref="NoInterpMaxFloatParameter"/>
    /// <seealso cref="NoInterpClampedFloatParameter"/>
    /// <seealso cref="NoInterpFloatRangeParameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class NoInterpMinFloatParameter : VolumeParameter<float>
    {
        /// <summary>
        /// The minimum value to clamp this parameter to.
        /// </summary>
        [NonSerialized]
        public float min;

        /// <summary>
        /// The value that this parameter stores.
        /// </summary>
        /// <remarks>
        /// You can override this property to define custom behaviors when the value is changed.
        /// </remarks>
        public override float value
        {
            get => m_Value;
            set => m_Value = Mathf.Max(value, min);
        }

        /// <summary>
        /// Creates a new <seealso cref="NoInterpMinFloatParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to storedin the parameter.</param>
        /// <param name="min">The minimum value to clamp the parameter to.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public NoInterpMinFloatParameter(float value, float min, bool overrideState = false)
            : base(value, overrideState)
        {
            this.min = min;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <c>float</c> value clamped to a max value.
    /// </summary>
    /// <seealso cref="FloatParameter"/>
    /// <seealso cref="MinFloatParameter"/>
    /// <seealso cref="ClampedFloatParameter"/>
    /// <seealso cref="FloatRangeParameter"/>
    /// <seealso cref="NoInterpFloatParameter"/>
    /// <seealso cref="NoInterpMinFloatParameter"/>
    /// <seealso cref="NoInterpMaxFloatParameter"/>
    /// <seealso cref="NoInterpClampedFloatParameter"/>
    /// <seealso cref="NoInterpFloatRangeParameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class MaxFloatParameter : FloatParameter
    {
        /// <summary>
        /// The maximum value to clamp this parameter to.
        /// </summary>
        [NonSerialized]
        public float max;

        /// <summary>
        /// The value that this parameter stores.
        /// </summary>
        /// <remarks>
        /// You can override this property to define custom behaviors when the value is changed.
        /// </remarks>
        public override float value
        {
            get => m_Value;
            set => m_Value = Mathf.Min(value, max);
        }

        /// <summary>
        /// Creates a new <seealso cref="MaxFloatParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="max">The maximum value to clamp the parameter to.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public MaxFloatParameter(float value, float max, bool overrideState = false)
            : base(value, overrideState)
        {
            this.max = max;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a non-interpolating <c>float</c> value clamped to
    /// a maximum value.
    /// </summary>
    /// <seealso cref="FloatParameter"/>
    /// <seealso cref="MinFloatParameter"/>
    /// <seealso cref="MaxFloatParameter"/>
    /// <seealso cref="ClampedFloatParameter"/>
    /// <seealso cref="FloatRangeParameter"/>
    /// <seealso cref="NoInterpFloatParameter"/>
    /// <seealso cref="NoInterpMinFloatParameter"/>
    /// <seealso cref="NoInterpClampedFloatParameter"/>
    /// <seealso cref="NoInterpFloatRangeParameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class NoInterpMaxFloatParameter : VolumeParameter<float>
    {
        /// <summary>
        /// The maximum value to clamp this parameter to.
        /// </summary>
        [NonSerialized]
        public float max;

        /// <summary>
        /// The value that this parameter stores.
        /// </summary>
        /// <remarks>
        /// You can override this property to define custom behaviors when the value is changed.
        /// </remarks>
        public override float value
        {
            get => m_Value;
            set => m_Value = Mathf.Min(value, max);
        }

        /// <summary>
        /// Creates a new <seealso cref="NoInterpMaxFloatParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="max">The maximum value to clamp the parameter to.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public NoInterpMaxFloatParameter(float value, float max, bool overrideState = false)
            : base(value, overrideState)
        {
            this.max = max;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <c>float</c> value clamped between a minimum and a
    /// maximum value.
    /// </summary>
    /// <seealso cref="FloatParameter"/>
    /// <seealso cref="MinFloatParameter"/>
    /// <seealso cref="MaxFloatParameter"/>
    /// <seealso cref="FloatRangeParameter"/>
    /// <seealso cref="NoInterpFloatParameter"/>
    /// <seealso cref="NoInterpMinFloatParameter"/>
    /// <seealso cref="NoInterpMaxFloatParameter"/>
    /// <seealso cref="NoInterpClampedFloatParameter"/>
    /// <seealso cref="NoInterpFloatRangeParameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class ClampedFloatParameter : FloatParameter
    {
        /// <summary>
        /// The minimum value to clamp this parameter to.
        /// </summary>
        [NonSerialized]
        public float min;

        /// <summary>
        /// The maximum value to clamp this parameter to.
        /// </summary>
        [NonSerialized]
        public float max;

        /// <summary>
        /// The value that this parameter stores.
        /// </summary>
        /// <remarks>
        /// You can override this property to define custom behaviors when the value is changed.
        /// </remarks>
        public override float value
        {
            get => m_Value;
            set => m_Value = Mathf.Clamp(value, min, max);
        }

        /// <summary>
        /// Creates a new <seealso cref="ClampedFloatParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="min">The minimum value to clamp the parameter to</param>
        /// <param name="max">The maximum value to clamp the parameter to.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public ClampedFloatParameter(float value, float min, float max, bool overrideState = false)
            : base(value, overrideState)
        {
            this.min = min;
            this.max = max;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a non-interpolating <c>float</c> value clamped between
    /// a minimum and a maximum value.
    /// </summary>
    /// <seealso cref="FloatParameter"/>
    /// <seealso cref="MinFloatParameter"/>
    /// <seealso cref="MaxFloatParameter"/>
    /// <seealso cref="ClampedFloatParameter"/>
    /// <seealso cref="FloatRangeParameter"/>
    /// <seealso cref="NoInterpFloatParameter"/>
    /// <seealso cref="NoInterpMinFloatParameter"/>
    /// <seealso cref="NoInterpMaxFloatParameter"/>
    /// <seealso cref="NoInterpFloatRangeParameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class NoInterpClampedFloatParameter : VolumeParameter<float>
    {
        /// <summary>
        /// The minimum value to clamp this parameter to.
        /// </summary>
        [NonSerialized]
        public float min;

        /// <summary>
        /// The maximum value to clamp this parameter to.
        /// </summary>
        [NonSerialized]
        public float max;

        /// <summary>
        /// The value that this parameter stores.
        /// </summary>
        /// <remarks>
        /// You can override this property to define custom behaviors when the value is changed.
        /// </remarks>
        public override float value
        {
            get => m_Value;
            set => m_Value = Mathf.Clamp(value, min, max);
        }

        /// <summary>
        /// Creates a new <seealso cref="NoInterpClampedFloatParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="min">The minimum value to clamp the parameter to</param>
        /// <param name="max">The maximum value to clamp the parameter to.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public NoInterpClampedFloatParameter(float value, float min, float max, bool overrideState = false)
            : base(value, overrideState)
        {
            this.min = min;
            this.max = max;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <c>Vector2</c> value holding a range of two
    /// <c>float</c> values clamped between a minimum and a maximum value.
    /// </summary>
    /// <seealso cref="FloatParameter"/>
    /// <seealso cref="MinFloatParameter"/>
    /// <seealso cref="MaxFloatParameter"/>
    /// <seealso cref="ClampedFloatParameter"/>
    /// <seealso cref="NoInterpFloatParameter"/>
    /// <seealso cref="NoInterpMinFloatParameter"/>
    /// <seealso cref="NoInterpMaxFloatParameter"/>
    /// <seealso cref="NoInterpClampedFloatParameter"/>
    /// <seealso cref="NoInterpFloatRangeParameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class FloatRangeParameter : VolumeParameter<Vector2>
    {
        /// <summary>
        /// The minimum value to clamp this parameter to.
        /// </summary>
        [NonSerialized]
        public float min;

        /// <summary>
        /// The maximum value to clamp this parameter to.
        /// </summary>
        [NonSerialized]
        public float max;

        /// <summary>
        /// The value that this parameter stores.
        /// </summary>
        /// <remarks>
        /// You can override this property to define custom behaviors when the value is changed.
        /// </remarks>
        public override Vector2 value
        {
            get => m_Value;
            set
            {
                m_Value.x = Mathf.Max(value.x, min);
                m_Value.y = Mathf.Min(value.y, max);
            }
        }

        /// <summary>
        /// Creates a new <seealso cref="FloatRangeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="min">The minimum value to clamp the parameter to</param>
        /// <param name="max">The maximum value to clamp the parameter to.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public FloatRangeParameter(Vector2 value, float min, float max, bool overrideState = false)
            : base(value, overrideState)
        {
            this.min = min;
            this.max = max;
        }

        /// <summary>
        /// Interpolates between two <c>Vector2</c> values.
        /// </summary>
        /// <param name="from">The start value</param>
        /// <param name="to">The end value</param>
        /// <param name="t">The interpolation factor in range [0,1]</param>
        public override void Interp(Vector2 from, Vector2 to, float t)
        {
            m_Value.x = from.x + (to.x - from.x) * t;
            m_Value.y = from.y + (to.y - from.y) * t;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a non-interpolating <c>Vector2</c> value holding
    /// a range of two <c>float</c> values clamped between a minimum and a maximum value.
    /// </summary>
    /// <seealso cref="FloatParameter"/>
    /// <seealso cref="MinFloatParameter"/>
    /// <seealso cref="MaxFloatParameter"/>
    /// <seealso cref="ClampedFloatParameter"/>
    /// <seealso cref="FloatRangeParameter"/>
    /// <seealso cref="NoInterpFloatParameter"/>
    /// <seealso cref="NoInterpMinFloatParameter"/>
    /// <seealso cref="NoInterpMaxFloatParameter"/>
    /// <seealso cref="NoInterpClampedFloatParameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class NoInterpFloatRangeParameter : VolumeParameter<Vector2>
    {
        /// <summary>
        /// The minimum value to clamp this parameter to.
        /// </summary>
        [NonSerialized]
        public float min;

        /// <summary>
        /// The maximum value to clamp this parameter to.
        /// </summary>
        [NonSerialized]
        public float max;

        /// <summary>
        /// The value that this parameter stores.
        /// </summary>
        /// <remarks>
        /// You can override this property to define custom behaviors when the value is changed.
        /// </remarks>
        public override Vector2 value
        {
            get => m_Value;
            set
            {
                m_Value.x = Mathf.Max(value.x, min);
                m_Value.y = Mathf.Min(value.y, max);
            }
        }

        /// <summary>
        /// Creates a new <seealso cref="NoInterpFloatRangeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="min">The minimum value to clamp the parameter to</param>
        /// <param name="max">The maximum value to clamp the parameter to.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public NoInterpFloatRangeParameter(Vector2 value, float min, float max, bool overrideState = false)
            : base(value, overrideState)
        {
            this.min = min;
            this.max = max;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <c>Color</c> value.
    /// </summary>
    /// <seealso cref="NoInterpColorParameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class ColorParameter : VolumeParameter<Color>
    {
        /// <summary>
        /// Is this color HDR?
        /// </summary>
        [NonSerialized]
        public bool hdr = false;

        /// <summary>
        /// Should the alpha channel be editable in the editor?
        /// </summary>
        [NonSerialized]
        public bool showAlpha = true;

        /// <summary>
        /// Should the eye dropper be visible in the editor?
        /// </summary>
        [NonSerialized]
        public bool showEyeDropper = true;

        /// <summary>
        /// Creates a new <seealso cref="ColorParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public ColorParameter(Color value, bool overrideState = false)
            : base(value, overrideState) { }

        /// <summary>
        /// Creates a new <seealso cref="ColorParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="hdr">Specifies whether the color is HDR or not.</param>
        /// <param name="showAlpha">Specifies whether you can edit the alpha channel in the Inspector or not.</param>
        /// <param name="showEyeDropper">Specifies whether the eye dropper is visible in the editor or not.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public ColorParameter(Color value, bool hdr, bool showAlpha, bool showEyeDropper, bool overrideState = false)
            : base(value, overrideState)
        {
            this.hdr = hdr;
            this.showAlpha = showAlpha;
            this.showEyeDropper = showEyeDropper;
            this.overrideState = overrideState;
        }

        /// <summary>
        /// Interpolates between two <c>Color</c> values.
        /// </summary>
        /// <remarks>
        /// For performance reasons, this function interpolates the RGBA channels directly.
        /// </remarks>
        /// <param name="from">The start value.</param>
        /// <param name="to">The end value.</param>
        /// <param name="t">The interpolation factor in range [0,1].</param>
        public override void Interp(Color from, Color to, float t)
        {
            // Lerping color values is a sensitive subject... We looked into lerping colors using
            // HSV and LCH but they have some downsides that make them not work correctly in all
            // situations, so we stick with RGB lerping for now, at least its behavior is
            // predictable despite looking desaturated when `t ~= 0.5` and it's faster anyway.
            m_Value.r = from.r + (to.r - from.r) * t;
            m_Value.g = from.g + (to.g - from.g) * t;
            m_Value.b = from.b + (to.b - from.b) * t;
            m_Value.a = from.a + (to.a - from.a) * t;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a non-interpolating <c>Color</c> value.
    /// </summary>
    /// <seealso cref="ColorParameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class NoInterpColorParameter : VolumeParameter<Color>
    {
        /// <summary>
        /// Specifies whether the color is HDR or not.
        /// </summary>
        public bool hdr = false;

        /// <summary>
        /// Specifies whether you can edit the alpha channel in the Inspector or not.
        /// </summary>
        [NonSerialized]
        public bool showAlpha = true;

        /// <summary>
        /// Specifies whether the eye dropper is visible in the editor or not.
        /// </summary>
        [NonSerialized]
        public bool showEyeDropper = true;

        /// <summary>
        /// Creates a new <seealso cref="NoInterpColorParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public NoInterpColorParameter(Color value, bool overrideState = false)
            : base(value, overrideState) { }

        /// <summary>
        /// Creates a new <seealso cref="NoInterpColorParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="hdr">Specifies whether the color is HDR or not.</param>
        /// <param name="showAlpha">Specifies whether you can edit the alpha channel in the Inspector or not.</param>
        /// <param name="showEyeDropper">Specifies whether the eye dropper is visible in the editor or not.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public NoInterpColorParameter(Color value, bool hdr, bool showAlpha, bool showEyeDropper, bool overrideState = false)
            : base(value, overrideState)
        {
            this.hdr = hdr;
            this.showAlpha = showAlpha;
            this.showEyeDropper = showEyeDropper;
            this.overrideState = overrideState;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <c>Vector2</c> value.
    /// </summary>
    /// <seealso cref="NoInterpVector2Parameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class Vector2Parameter : VolumeParameter<Vector2>
    {
        /// <summary>
        /// Creates a new <seealso cref="Vector2Parameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public Vector2Parameter(Vector2 value, bool overrideState = false)
            : base(value, overrideState) { }

        /// <summary>
        /// Interpolates between two <c>Vector2</c> values.
        /// </summary>
        /// <param name="from">The start value.</param>
        /// <param name="to">The end value.</param>
        /// <param name="t">The interpolation factor in range [0,1].</param>
        public override void Interp(Vector2 from, Vector2 to, float t)
        {
            m_Value.x = from.x + (to.x - from.x) * t;
            m_Value.y = from.y + (to.y - from.y) * t;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a non-interpolating <c>Vector2</c> value.
    /// </summary>
    /// <seealso cref="Vector2Parameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class NoInterpVector2Parameter : VolumeParameter<Vector2>
    {
        /// <summary>
        /// Creates a new <seealso cref="NoInterpVector2Parameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public NoInterpVector2Parameter(Vector2 value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <c>Vector3</c> value.
    /// </summary>
    /// <seealso cref="NoInterpVector3Parameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class Vector3Parameter : VolumeParameter<Vector3>
    {
        /// <summary>
        /// Creates a new <seealso cref="Vector3Parameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public Vector3Parameter(Vector3 value, bool overrideState = false)
            : base(value, overrideState) { }

        /// <summary>
        /// Interpolates between two <c>Vector3</c> values.
        /// </summary>
        /// <param name="from">The start value.</param>
        /// <param name="to">The end value.</param>
        /// <param name="t">The interpolation factor in range [0,1].</param>
        public override void Interp(Vector3 from, Vector3 to, float t)
        {
            m_Value.x = from.x + (to.x - from.x) * t;
            m_Value.y = from.y + (to.y - from.y) * t;
            m_Value.z = from.z + (to.z - from.z) * t;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a non-interpolating <c>Vector3</c> value.
    /// </summary>
    /// <seealso cref="Vector3Parameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class NoInterpVector3Parameter : VolumeParameter<Vector3>
    {
        /// <summary>
        /// Creates a new <seealso cref="Vector3Parameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public NoInterpVector3Parameter(Vector3 value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <c>Vector4</c> value.
    /// </summary>
    /// <seealso cref="NoInterpVector4Parameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class Vector4Parameter : VolumeParameter<Vector4>
    {
        /// <summary>
        /// Creates a new <seealso cref="Vector4Parameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public Vector4Parameter(Vector4 value, bool overrideState = false)
            : base(value, overrideState) { }

        /// <summary>
        /// Interpolates between two <c>Vector4</c> values.
        /// </summary>
        /// <param name="from">The start value.</param>
        /// <param name="to">The end value.</param>
        /// <param name="t">The interpolation factor in range [0,1].</param>
        public override void Interp(Vector4 from, Vector4 to, float t)
        {
            m_Value.x = from.x + (to.x - from.x) * t;
            m_Value.y = from.y + (to.y - from.y) * t;
            m_Value.z = from.z + (to.z - from.z) * t;
            m_Value.w = from.w + (to.w - from.w) * t;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a non-interpolating <c>Vector4</c> value.
    /// </summary>
    /// <seealso cref="Vector4Parameter"/>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class NoInterpVector4Parameter : VolumeParameter<Vector4>
    {
        /// <summary>
        /// Creates a new <seealso cref="Vector4Parameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public NoInterpVector4Parameter(Vector4 value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <c>Texture</c> value.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class TextureParameter : VolumeParameter<Texture>
    {
        /// <summary>
        /// The accepted dimension of textures.
        /// </summary>
        public TextureDimension dimension;

        /// <summary>
        /// Creates a new <seealso cref="TextureParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public TextureParameter(Texture value, bool overrideState = false)
            : this(value, TextureDimension.Any, overrideState) { }

        /// <summary>
        /// Creates a new <seealso cref="TextureParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="dimension">The accepted dimension of textures.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public TextureParameter(Texture value, TextureDimension dimension, bool overrideState = false)
            : base(value, overrideState)
        {
            this.dimension = dimension;
        }

        // TODO: Texture interpolation

        /// <summary>
        /// Returns a hash code for the current object.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
                if (value != null)
                    hash = 23 * CoreUtils.GetTextureHash(value);
            }

            return hash;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a non-interpolating <c>Texture</c> value.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class NoInterpTextureParameter : VolumeParameter<Texture>
    {
        /// <summary>
        /// Creates a new <seealso cref="NoInterpTextureParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public NoInterpTextureParameter(Texture value, bool overrideState = false)
            : base(value, overrideState) { }

        /// <summary>
        /// Returns a hash code for the current object.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
                if (value != null)
                    hash = 23 * CoreUtils.GetTextureHash(value);
            }

            return hash;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a 2D <c>Texture</c> value.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class Texture2DParameter : VolumeParameter<Texture>
    {
        /// <summary>
        /// Creates a new <seealso cref="Texture2DParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public Texture2DParameter(Texture value, bool overrideState = false)
            : base(value, overrideState) { }

        /// <summary>
        /// Returns a hash code for the current object.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
                if (value != null)
                    hash = 23 * CoreUtils.GetTextureHash(value);
            }

            return hash;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a 3D <c>Texture</c> value.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class Texture3DParameter : VolumeParameter<Texture>
    {
        /// <summary>
        /// Creates a new <seealso cref="Texture3DParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public Texture3DParameter(Texture value, bool overrideState = false)
            : base(value, overrideState) { }

        /// <summary>
        /// Returns a hash code for the current object.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
                if (value != null)
                    hash = 23 * CoreUtils.GetTextureHash(value);
            }

            return hash;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <c>RenderTexture</c> value.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class RenderTextureParameter : VolumeParameter<RenderTexture>
    {
        /// <summary>
        /// Creates a new <seealso cref="RenderTextureParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public RenderTextureParameter(RenderTexture value, bool overrideState = false)
            : base(value, overrideState) { }

        // TODO: RenderTexture interpolation

        /// <summary>
        /// Returns a hash code for the current object.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
                if (value != null)
                    hash = 23 * CoreUtils.GetTextureHash(value);
            }

            return hash;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a non-interpolating <c>RenderTexture</c> value.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class NoInterpRenderTextureParameter : VolumeParameter<RenderTexture>
    {
        /// <summary>
        /// Creates a new <seealso cref="NoInterpRenderTextureParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public NoInterpRenderTextureParameter(RenderTexture value, bool overrideState = false)
            : base(value, overrideState) { }

        /// <summary>
        /// Returns a hash code for the current object.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
                if (value != null)
                    hash = 23 * CoreUtils.GetTextureHash(value);
            }

            return hash;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <c>Cubemap</c> value.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class CubemapParameter : VolumeParameter<Texture>
    {
        /// <summary>
        /// Creates a new <seealso cref="CubemapParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public CubemapParameter(Texture value, bool overrideState = false)
            : base(value, overrideState) { }
        // TODO: Cubemap interpolation

        /// <summary>
        /// Returns a hash code for the current object.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
                if (value != null)
                    hash = 23 * CoreUtils.GetTextureHash(value);
            }

            return hash;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a non-interpolating <c>Cubemap</c> value.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class NoInterpCubemapParameter : VolumeParameter<Cubemap>
    {
        /// <summary>
        /// Creates a new <seealso cref="NoInterpCubemapParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public NoInterpCubemapParameter(Cubemap value, bool overrideState = false)
            : base(value, overrideState) { }

        /// <summary>
        /// Returns a hash code for the current object.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
                if (value != null)
                    hash = 23 * CoreUtils.GetTextureHash(value);
            }

            return hash;
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a serializable class or struct.
    /// </summary>
    /// <typeparam name="T">The type of serializable object or struct to hold in this parameter.
    /// </typeparam>
    // TODO: ObjectParameter<T> doesn't seem to be working as expect, debug me
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class ObjectParameter<T> : VolumeParameter<T>
    {
        internal ReadOnlyCollection<VolumeParameter> parameters { get; private set; }

        /// <summary>
        /// The current override state for this parameter. Note that this is always forced enabled
        /// on <see cref="ObjectParameter{T}"/>.
        /// </summary>
        public sealed override bool overrideState
        {
            get => true;
            set => m_OverrideState = true;
        }

        /// <summary>
        /// The value stored by this parameter.
        /// </summary>
        public sealed override T value
        {
            get => m_Value;
            set
            {
                m_Value = value;

                if (m_Value == null)
                {
                    parameters = null;
                    return;
                }

                // Automatically grab all fields of type VolumeParameter contained in this instance
                parameters = m_Value.GetType()
                    .GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(t => t.FieldType.IsSubclassOf(typeof(VolumeParameter)))
                    .OrderBy(t => t.MetadataToken) // Guaranteed order
                    .Select(t => (VolumeParameter)t.GetValue(m_Value))
                    .ToList()
                    .AsReadOnly();
            }
        }

        /// <summary>
        /// Creates a new <seealso cref="ObjectParameter{T}"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        public ObjectParameter(T value)
        {
            m_OverrideState = true;
            this.value = value;
        }

        internal override void Interp(VolumeParameter from, VolumeParameter to, float t)
        {
            if (m_Value == null)
                return;

            var paramOrigin = parameters;
            var paramFrom = ((ObjectParameter<T>)from).parameters;
            var paramTo = ((ObjectParameter<T>)to).parameters;

            for (int i = 0; i < paramFrom.Count; i++)
            {
                // Keep track of the override state for debugging purpose
                paramOrigin[i].overrideState = paramTo[i].overrideState;

                if (paramTo[i].overrideState)
                    paramOrigin[i].Interp(paramFrom[i], paramTo[i], t);
            }
        }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds an <c>AnimationCurve</c> value.
    /// </summary>
    [Serializable]
    public class AnimationCurveParameter : VolumeParameter<AnimationCurve>
    {
        /// <summary>
        /// Creates a new <seealso cref="AnimationCurveParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to be stored in the parameter</param>
        /// <param name="overrideState">The initial override state for the parameter</param>
        public AnimationCurveParameter(AnimationCurve value, bool overrideState = false)
            : base(value, overrideState) { }

        /// <summary>
        /// Interpolates between two AnimationCurve values. Note that it will overwrite the values in lhsCurve,
        /// whereas rhsCurve data will be unchanged. Thus, it is legal to call it as:
        ///     stateParam.Interp(stateParam, toParam, interpFactor);
        /// However, It should NOT be called when the lhsCurve parameter needs to be preserved. But the current
        /// framework modifies it anyway in VolumeComponent.Override for all types of VolumeParameters
        /// </summary>
        /// <param name="lhsCurve">The start value.</param>
        /// <param name="rhsCurve">The end value.</param>
        /// <param name="t">The interpolation factor in range [0,1].</param>
        public override void Interp(AnimationCurve lhsCurve, AnimationCurve rhsCurve, float t)
        {
            m_Value = lhsCurve;
            KeyframeUtility.InterpAnimationCurve(ref m_Value, rhsCurve, t);
        }

        /// <inheritdoc/>
        public override void SetValue(VolumeParameter parameter)
        {
            m_Value.CopyFrom(((AnimationCurveParameter)parameter).m_Value);
        }

        /// <inheritdoc/>
        public override object Clone()
        {
            return new AnimationCurveParameter(new AnimationCurve(GetValue<AnimationCurve>().keys), overrideState);
        }

        /// <summary>
        /// Returns a hash code for the animationCurve.
        /// </summary>
        /// <returns>A hash code for the animationCurve.</returns>
        public override int GetHashCode()
         {
             unchecked
             {
                var hash = overrideState.GetHashCode();

                return hash * 23 + value.GetHashCode();
             }
         }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <c>bool</c> value.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public class MaterialParameter : VolumeParameter<Material>
    {
        /// <summary>
        /// Creates a new <see cref="MaterialParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter</param>
        /// <param name="overrideState">The initial override state for the parameter</param>
        public MaterialParameter(Material value, bool overrideState = false)
            : base(value, overrideState) { }
    }
}
