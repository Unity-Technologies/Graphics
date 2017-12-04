using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace UnityEngine.Experimental.Rendering
{
    // We need this base class to be able to store a list of VolumeParameter in collections as we
    // can't store VolumeParameter<T> with variable T types in the same collection. As a result some
    // of the following is a bit hacky...
    public abstract class VolumeParameter
    {
        [SerializeField]
        protected bool m_OverrideState;

        public virtual bool overrideState
        {
            get { return m_OverrideState; }
            set { m_OverrideState = value; }
        }

        internal abstract void Interp(VolumeParameter from, VolumeParameter to, float t);

        public T GetValue<T>()
        {
            return ((VolumeParameter<T>)this).value;
        }

        internal abstract void SetValue(VolumeParameter parameter);

        public static bool IsObjectParameter(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ObjectParameter<>))
                return true;

            return type.BaseType != null
                && IsObjectParameter(type.BaseType);
        }
    }

    [Serializable]
    public class VolumeParameter<T> : VolumeParameter
    {
        [SerializeField]
        protected T m_Value;

        public virtual T value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public VolumeParameter()
            : this(default(T), false)
        {
        }

        protected VolumeParameter(T value, bool overrideState)
        {
            m_Value = value;
            this.overrideState = overrideState;
        }

        internal override void Interp(VolumeParameter from, VolumeParameter to, float t)
        {
            // Note: this is relatively unsafe (assumes that from and to are both holding type T)
            Interp(from.GetValue<T>(), to.GetValue<T>(), t);
        }

        public virtual void Interp(T from, T to, float t)
        {
            // Returns `b` if `dt > 0` by default so we don't have to write overrides for bools and
            // enumerations.
            m_Value = t > 0f ? to : from;
        }

        public void Override(T x)
        {
            overrideState = true;
            m_Value = x;
        }

        internal override void SetValue(VolumeParameter parameter)
        {
            m_Value = parameter.GetValue<T>();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + overrideState.GetHashCode();
                hash = hash * 23 + value.GetHashCode();
                return hash;
            }
        }

        //
        // Implicit conversion; assuming the following:
        //
        //   var myFloatProperty = new ParameterOverride<float> { value = 42f; };
        //
        // It allows for implicit casts:
        //
        //   float myFloat = myFloatProperty.value; // No implicit cast
        //   float myFloat = myFloatProperty;       // Implicit cast
        //
        // For safety reason this is one-way only.
        //
        public static implicit operator T(VolumeParameter<T> prop)
        {
            return prop.m_Value;
        }
    }

    public enum ParameterClampMode
    {
        Min,
        Max,
        MinMax
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

    [Serializable]
    public sealed class BoolParameter : VolumeParameter<bool> { }

    [Serializable]
    public sealed class IntParameter : VolumeParameter<int>
    {
        public override void Interp(int from, int to, float t)
        {
            // Int snapping interpolation. Don't use this for enums as they don't necessarily have
            // contiguous values. Use the default interpolator instead (same as bool).
            m_Value = (int)(from + (to - from) * t);
        }
    }

    [Serializable]
    public sealed class InstantIntParameter : VolumeParameter<int> { }

    [Serializable]
    public sealed class ClampedIntParameter : VolumeParameter<int>
    {
        public ParameterClampMode clampMode = ParameterClampMode.MinMax;
        public int min = 0;
        public int max = 10;

        public override int value
        {
            get { return m_Value; }
            set
            {
                switch (clampMode)
                {
                    case ParameterClampMode.Min: m_Value = Mathf.Max(min, value); break;
                    case ParameterClampMode.Max: m_Value = Mathf.Min(max, value); break;
                    case ParameterClampMode.MinMax: m_Value = Mathf.Clamp(value, min, max); break;
                }
            }
        }

        public override void Interp(int from, int to, float t)
        {
            m_Value = (int)(from + (to - from) * t);
        }
    }

    [Serializable]
    public sealed class InstantClampedIntParameter : VolumeParameter<int>
    {
        public ParameterClampMode clampMode = ParameterClampMode.MinMax;
        public int min = 0;
        public int max = 10;

        public override int value
        {
            get { return m_Value; }
            set
            {
                switch (clampMode)
                {
                    case ParameterClampMode.Min: m_Value = Mathf.Max(min, value); break;
                    case ParameterClampMode.Max: m_Value = Mathf.Min(max, value); break;
                    case ParameterClampMode.MinMax: m_Value = Mathf.Clamp(value, min, max); break;
                }
            }
        }
    }

    [Serializable]
    public sealed class FloatParameter : VolumeParameter<float>
    {
        public override void Interp(float from, float to, float t)
        {
            m_Value = from + (to - from) * t;
        }
    }

    [Serializable]
    public sealed class InstantFloatParameter : VolumeParameter<float> { }

    [Serializable]
    public sealed class ClampedFloatParameter : VolumeParameter<float>
    {
        public ParameterClampMode clampMode = ParameterClampMode.MinMax;
        public float min = 0f;
        public float max = 1f;

        public override float value
        {
            get { return m_Value; }
            set
            {
                switch (clampMode)
                {
                    case ParameterClampMode.Min: m_Value = Mathf.Max(min, value); break;
                    case ParameterClampMode.Max: m_Value = Mathf.Min(max, value); break;
                    case ParameterClampMode.MinMax: m_Value = Mathf.Clamp(value, min, max); break;
                }
            }
        }

        // We could override FloatParameter here but that would require making it not-sealed which
        // will stop the compiler from doing specific optimizations on virtual methods - considering
        // how often float is used, duplicating the code in this case is a definite win
        public override void Interp(float from, float to, float t)
        {
            m_Value = from + (to - from) * t;
        }
    }

    [Serializable]
    public sealed class InstantClampedFloatParameter : VolumeParameter<float>
    {
        public ParameterClampMode clampMode = ParameterClampMode.MinMax;
        public float min = 0f;
        public float max = 1f;

        public override float value
        {
            get { return m_Value; }
            set
            {
                switch (clampMode)
                {
                    case ParameterClampMode.Min: m_Value = Mathf.Max(min, value); break;
                    case ParameterClampMode.Max: m_Value = Mathf.Min(max, value); break;
                    case ParameterClampMode.MinMax: m_Value = Mathf.Clamp(value, min, max); break;
                }
            }
        }
    }

    // Holds a min & a max values clamped in a range (MinMaxSlider in the editor)
    [Serializable]
    public sealed class RangeParameter : VolumeParameter<Vector2>
    {
        public float min = 0;
        public float max = 1;

        public override Vector2 value
        {
            get { return m_Value; }
            set
            {
                m_Value.x = Mathf.Max(value.x, min);
                m_Value.y = Mathf.Min(value.y, max);
            }
        }

        public override void Interp(Vector2 from, Vector2 to, float t)
        {
            m_Value.x = from.x + (to.x - from.x) * t;
            m_Value.y = from.y + (to.y - from.y) * t;
        }
    }

    [Serializable]
    public sealed class InstantRangeParameter : VolumeParameter<Vector2>
    {
        public float min = 0;
        public float max = 1;

        public override Vector2 value
        {
            get { return m_Value; }
            set
            {
                m_Value.x = Mathf.Max(value.x, min);
                m_Value.y = Mathf.Min(value.y, max);
            }
        }
    }

    // 32-bit RGBA
    [Serializable]
    public sealed class ColorParameter : VolumeParameter<Color>
    {
        public bool hdr = false;
        public bool showAlpha = true;
        public bool showEyeDropper = true;

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

    [Serializable]
    public sealed class InstantColorParameter : VolumeParameter<Color> { }

    [Serializable]
    public sealed class Vector2Parameter : VolumeParameter<Vector2>
    {
        public override void Interp(Vector2 from, Vector2 to, float t)
        {
            m_Value.x = from.x + (to.x - from.x) * t;
            m_Value.y = from.y + (to.y - from.y) * t;
        }
    }

    [Serializable]
    public sealed class InstantVector2Parameter : VolumeParameter<Vector2> { }

    [Serializable]
    public sealed class Vector3Parameter : VolumeParameter<Vector3>
    {
        public override void Interp(Vector3 from, Vector3 to, float t)
        {
            m_Value.x = from.x + (to.x - from.x) * t;
            m_Value.y = from.y + (to.y - from.y) * t;
            m_Value.z = from.z + (to.z - from.z) * t;
        }
    }

    [Serializable]
    public sealed class InstantVector3Parameter : VolumeParameter<Vector3> { }

    [Serializable]
    public sealed class Vector4Parameter : VolumeParameter<Vector4>
    {
        public override void Interp(Vector4 from, Vector4 to, float t)
        {
            m_Value.x = from.x + (to.x - from.x) * t;
            m_Value.y = from.y + (to.y - from.y) * t;
            m_Value.z = from.z + (to.z - from.z) * t;
            m_Value.w = from.w + (to.w - from.w) * t;
        }
    }

    [Serializable]
    public sealed class InstantVector4Parameter : VolumeParameter<Vector4> { }

    // Used as a container to store custom serialized classes/structs inside volume components
    [Serializable]
    public class ObjectParameter<T> : VolumeParameter<T>
    {
        internal ReadOnlyCollection<VolumeParameter> parameters { get; private set; }

        // Force override state to true for container objects
        public override bool overrideState
        {
            get { return true; }
            set { m_OverrideState = true; }
        }

        public override T value
        {
            get { return m_Value; }
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

        internal override void Interp(VolumeParameter from, VolumeParameter to, float t)
        {
            if (m_Value == null)
                return;

            var paramOrigin = parameters;
            var paramFrom = ((ObjectParameter<T>)from).parameters;
            var paramTo = ((ObjectParameter<T>)to).parameters;

            for (int i = 0; i < paramFrom.Count; i++)
            {
                if (paramOrigin[i].overrideState)
                    paramOrigin[i].Interp(paramFrom[i], paramTo[i], t);
            }
        }
    }
}
