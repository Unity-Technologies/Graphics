using UnityEngine;
using UnityEditor;
using Type = System.Type;


namespace UnityEditor.VFX.UI
{
    class FloatNAffector : IFloatNAffector<float>, IFloatNAffector<Vector2>, IFloatNAffector<Vector3>, IFloatNAffector<Vector4>
    {
        float IFloatNAffector<float>.GetValue(object floatN)
        {
            return (FloatN)floatN;
        }
        Vector2 IFloatNAffector<Vector2>.GetValue(object floatN)
        {
            return (FloatN)floatN;
        }
        Vector3 IFloatNAffector<Vector3>.GetValue(object floatN)
        {
            return (FloatN)floatN;
        }
        Vector4 IFloatNAffector<Vector4>.GetValue(object floatN)
        {
            return (FloatN)floatN;
        }

        public static FloatNAffector Default = new FloatNAffector();
    }

    static class VFXConverter
    {
        public static bool CanConvert(Type type)
        {
            return type == typeof(Color) ||
                type == typeof(Vector4) ||
                type == typeof(Vector3) ||
                type == typeof(Position) ||
                type == typeof(Vector) ||
                type == typeof(Vector2) ||
                type == typeof(float);
        }

        public static T ConvertTo<T>(object value)
        {
            return (T)ConvertTo(value, typeof(T));
        }

        class ProfilerScope : System.IDisposable
        {
            public ProfilerScope(string name)
            {
                UnityEngine.Profiling.Profiler.BeginSample(name);
            }

            void System.IDisposable.Dispose()
            {
                UnityEngine.Profiling.Profiler.EndSample();
            }
        }

        public static object ConvertTo(object value, Type type)
        {
            using (var scope = new ProfilerScope("VFXConverter.ConvertTo"))
            {
                if (value == null || value.GetType() == type)
                {
                    return value;
                }
                if (type == typeof(Color))
                {
                    return ConvertToColor(value);
                }
                else if (type == typeof(Vector4))
                {
                    return ConvertToVector4(value);
                }
                else if (type == typeof(Vector3))
                {
                    return ConvertToVector3(value);
                }
                else if (type == typeof(Position))
                {
                    return ConvertToPosition(value);
                }
                else if (type == typeof(Vector))
                {
                    return ConvertToVector(value);
                }
                else if (type == typeof(Vector2))
                {
                    return ConvertToVector2(value);
                }
                else if (type == typeof(float))
                {
                    return ConvertToFloat(value);
                }
                else
                {
                    return value;
                }
            }
        }

        public static Color ConvertToColor(object value)
        {
            if (value is Vector4)
            {
                Vector4 val = (Vector4)value;
                return new Color(val.x, val.y, val.z, val.w);
            }
            else if (value is Vector3)
            {
                Vector3 val = (Vector3)value;
                return new Color(val.x, val.y, val.z);
            }
            else if (value is FloatN)
            {
                FloatN val = (FloatN)value;
                if (val.realSize == 4)
                {
                    return new Color(val[0], val[1], val[2], val[3]);
                }
                else if (val.realSize == 3)
                {
                    return new Color(val[0], val[1], val[2]);
                }
            }
            return (Color)value;
        }

        public static Vector4 ConvertToVector4(object value)
        {
            if (value is Color)
            {
                Color val = (Color)value;
                return new Vector4(val.r, val.g, val.b, val.a);
            }
            else if (value is float)
            {
                float val = (float)value;
                return new Vector4(val, val, val, val);
            }
            else if (value is int)
            {
                float val = (float)(int)value;
                return new Vector4(val, val, val, val);
            }
            else if (value is Position)
            {
                Position val = (Position)value;
                return new Vector4(val.position.x, val.position.y, val.position.z, 1);
            }
            else if (value is Vector)
            {
                Vector val = (Vector)value;
                return new Vector4(val.vector.x, val.vector.y, val.vector.z, 0);
            }
            else if (value is FloatN)
            {
                FloatN val = (FloatN)value;

                return val;
            }
            return (Vector4)value;
        }

        public static Vector3 ConvertToVector3(object value)
        {
            if (value is Color)
            {
                Color val = (Color)value;
                return new Vector3(val.r, val.g, val.b);
            }
            else if (value is Vector4)
            {
                Vector4 val = (Vector4)value;
                return new Vector3(val.x, val.y, val.z);
            }
            else if (value is float)
            {
                float val = (float)value;
                return new Vector3(val, val, val);
            }
            else if (value is int)
            {
                float val = (float)(int)value;
                return new Vector3(val, val, val);
            }
            else if (value is Position)
            {
                Position val = (Position)value;
                return val.position;
            }
            else if (value is Vector)
            {
                Vector val = (Vector)value;
                return val.vector;
            }
            else if (value is DirectionType)
            {
                DirectionType val = (DirectionType)value;
                return val.direction;
            }
            else if (value is FloatN)
            {
                FloatN val = (FloatN)value;

                return val;
            }
            return (Vector3)value;
        }

        public static Vector4 ConvertToVector2(object value)
        {
            if (value is Color)
            {
                Color val = (Color)value;
                return new Vector3(val.r, val.g);
            }
            else if (value is Vector4)
            {
                Vector4 val = (Vector4)value;
                return new Vector2(val.x, val.y);
            }
            else if (value is Vector3)
            {
                Vector3 val = (Vector3)value;
                return new Vector2(val.x, val.y);
            }
            else if (value is float)
            {
                float val = (float)value;
                return new Vector2(val, val);
            }
            else if (value is int)
            {
                float val = (float)(int)value;
                return new Vector2(val, val);
            }
            else if (value is FloatN)
            {
                FloatN val = (FloatN)value;

                return val;
            }
            return (Vector2)value;
        }

        public static float ConvertToFloat(object value)
        {
            if (value is int)
            {
                return (float)(int)value;
            }
            else if (value is uint)
            {
                return (float)(uint)value;
            }
            else if (value is FloatN)
            {
                FloatN val = (FloatN)value;

                return val;
            }
            else if (value is Vector4)
            {
                return ((Vector4)value).x;
            }
            else if (value is Vector3)
            {
                return ((Vector3)value).x;
            }
            else if (value is Vector2)
            {
                return ((Vector2)value).x;
            }
            return (float)value;
        }

        public static Position ConvertToPosition(object value)
        {
            if (value is Color)
            {
                Color val = (Color)value;
                return new Position { position = new Vector3(val.r, val.g, val.b) };
            }
            else if (value is Vector4)
            {
                Vector4 val = (Vector4)value;
                return new Position { position = val };
            }
            else if (value is Vector3)
            {
                Vector3 val = (Vector3)value;
                return new Position { position = val };
            }
            else if (value is float)
            {
                float val = (float)value;
                return new Position { position = new Vector3(val, val, val) };
            }
            else if (value is int)
            {
                float val = (float)(int)value;
                return new Position { position = new Vector3(val, val, val) };
            }
            else if (value is Vector)
            {
                Vector val = (Vector)value;
                return new Position { position = val.vector };
            }
            else if (value is FloatN)
            {
                FloatN val = (FloatN)value;

                return new Position {position = val};
            }
            return (Position)value;
        }

        public static Vector ConvertToVector(object value)
        {
            if (value is Color)
            {
                Color val = (Color)value;
                return new Vector { vector = new Vector3(val.r, val.g, val.b) };
            }
            else if (value is Vector4)
            {
                Vector4 val = (Vector4)value;
                return new Vector { vector = val };
            }
            else if (value is Vector3)
            {
                Vector3 val = (Vector3)value;
                return new Vector { vector = val };
            }
            else if (value is float)
            {
                float val = (float)value;
                return new Vector { vector = new Vector3(val, val, val) };
            }
            else if (value is int)
            {
                float val = (float)(int)value;
                return new Vector { vector = new Vector3(val, val, val) };
            }
            else if (value is Vector)
            {
                Vector val = (Vector)value;
                return new Vector { vector = val.vector };
            }
            else if (value is FloatN)
            {
                FloatN val = (FloatN)value;

                return new Vector { vector = val };
            }
            return (Vector)value;
        }
    }
}
