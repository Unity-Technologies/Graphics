using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    [Serializable]
    public class SerializableType : ISerializationCallbackReceiver
    {
        public static implicit operator SerializableType(Type value)
        {
            return new SerializableType(value);
        }

        public static implicit operator Type(SerializableType value)
        {
            return value != null ? value.m_Type : null;
        }

        private SerializableType() { }
        public SerializableType(Type type)
        {
            m_Type = type;
        }

        public virtual void OnBeforeSerialize()
        {
            m_SerializableType = m_Type != null ? m_Type.AssemblyQualifiedName : string.Empty;
        }

        public virtual void OnAfterDeserialize()
        {
            m_Type = Type.GetType(m_SerializableType);
        }

        [NonSerialized]
        private Type m_Type;
        [SerializeField]
        private string m_SerializableType;
    }

    [Serializable]
    public class VFXSerializableObject
    {
        private VFXSerializableObject() { }

        public VFXSerializableObject(Type type, object obj) : this(type)
        {
            Set(obj);
        }

        public VFXSerializableObject(Type type)
        {
            m_Type = type;
        }

        public object Get()
        {
            return VFXSerializer.Load(m_Type, m_SerializableObject);
        }

        public T Get<T>()
        {
            return (T)Get();
        }

        public void Set(object obj)
        {
            if (obj == null)
                m_SerializableObject = string.Empty;
            else
            {
                if (!((Type)m_Type).IsAssignableFrom(obj.GetType()))
                    throw new ArgumentException(string.Format("Cannot assing an object of type {0} to VFXSerializedObject of type {1}", obj.GetType(), (Type)m_Type));
                m_SerializableObject = VFXSerializer.Save(obj);
            }
        }

        [SerializeField]
        private SerializableType m_Type;

        // [NonSerialized]
        //private object m_Object;
        [SerializeField]
        private string m_SerializableObject;
    }


    public static class VFXSerializer
    {
        [System.Serializable]
        public struct TypedSerializedData
        {
            public string data;
            public string type;

            public static TypedSerializedData Null = new TypedSerializedData();
        }

        [Serializable]
        private struct ObjectWrapper
        {
            public UnityEngine.Object obj;
        }


        [System.Serializable]
        class AnimCurveWrapper
        {
            [System.Serializable]
            public struct Keyframe
            {
                public float time;
                public float value;
                public float inTangent;
                public float outTangent;
                public int tangentMode;
            }
            public Keyframe[] frames;
            public WrapMode preWrapMode;
            public WrapMode postWrapMode;

        }

        [System.Serializable]
        class GradientWrapper
        {
            [System.Serializable]
            public struct ColorKey
            {
                public Color color;
                public float time;
            }
            [System.Serializable]
            public struct AlphaKey
            {
                public float alpha;
                public float time;
            }
            public ColorKey[] colorKeys;
            public AlphaKey[] alphaKeys;

            public GradientMode gradientMode;
        }

        public static TypedSerializedData SaveWithType(object obj)
        {
            TypedSerializedData data = new TypedSerializedData();
            data.data = VFXSerializer.Save(obj);
            data.type = obj.GetType().AssemblyQualifiedName;

            return data;
        }

        public static object LoadWithType(TypedSerializedData data)
        {
            if (!string.IsNullOrEmpty(data.data))
            {
                System.Type type = Type.GetType(data.type);
                if (type == null)
                {
                    Debug.LogError("Can't find type " + data.type);
                    return null;
                }

                return VFXSerializer.Load(type, data.data);
            }

            return null;
        }


        public static string Save(object obj)
        {
            if (obj == null)
                return string.Empty;

            if (obj.GetType().IsPrimitive)
            {
                return obj.ToString();
            }
            else if (obj is UnityEngine.Object) //type is a unity object
            {
                ObjectWrapper wrapper = new ObjectWrapper { obj = obj as UnityEngine.Object };
                return EditorJsonUtility.ToJson(wrapper);
            }
            else if (obj is AnimationCurve)
            {
                AnimCurveWrapper sac = new AnimCurveWrapper();
                AnimationCurve curve = obj as AnimationCurve;


                sac.frames = new AnimCurveWrapper.Keyframe[curve.keys.Length];
                for (int i = 0; i < curve.keys.Length; ++i)
                {
                    sac.frames[i].time = curve.keys[i].time;
                    sac.frames[i].value = curve.keys[i].value;
                    sac.frames[i].inTangent = curve.keys[i].inTangent;
                    sac.frames[i].outTangent = curve.keys[i].outTangent;
                    sac.frames[i].tangentMode = curve.keys[i].tangentMode;
                }
                sac.preWrapMode = curve.preWrapMode;
                sac.postWrapMode = curve.postWrapMode;

                return JsonUtility.ToJson(sac);
            }
            else if( obj is Gradient)
            {
                GradientWrapper gw = new GradientWrapper();
                Gradient gradient = obj as Gradient;

                gw.gradientMode = gradient.mode;
                gw.colorKeys = new GradientWrapper.ColorKey[gradient.colorKeys.Length];
                for(int i = 0; i < gradient.colorKeys.Length; ++i)
                {
                    gw.colorKeys[i].color = gradient.colorKeys[i].color;
                    gw.colorKeys[i].time = gradient.colorKeys[i].time;
                }
                gw.alphaKeys = new GradientWrapper.AlphaKey[gradient.alphaKeys.Length];
                for(int i = 0; i < gradient.alphaKeys.Length; ++i)
                {
                    gw.alphaKeys[i].alpha = gradient.alphaKeys[i].alpha;
                    gw.alphaKeys[i].time = gradient.alphaKeys[i].time;
                }
                return JsonUtility.ToJson(gw);
            }
            else
            {
                return EditorJsonUtility.ToJson(obj);
            }
        }

        public static object Load(System.Type type, string text)
        {
            if (type == null)
                return null;

            if (type.IsPrimitive)
            {
                return Convert.ChangeType(text, type);
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                object obj = new ObjectWrapper();
                EditorJsonUtility.FromJsonOverwrite(text, obj);

                return ((ObjectWrapper)obj).obj;
            }
            else if (type.IsAssignableFrom(typeof(AnimationCurve)))
            {
                AnimCurveWrapper sac = new AnimCurveWrapper();

                JsonUtility.FromJsonOverwrite(text,sac);

                AnimationCurve curve = new AnimationCurve();

                if (sac.frames != null)
                {
                    Keyframe[] keys = new UnityEngine.Keyframe[sac.frames.Length];
                    for (int i = 0; i < sac.frames.Length; ++i)
                    {
                        keys[i].time = sac.frames[i].time;
                        keys[i].value = sac.frames[i].value;
                        keys[i].inTangent = sac.frames[i].inTangent;
                        keys[i].outTangent = sac.frames[i].outTangent;
                        keys[i].tangentMode = sac.frames[i].tangentMode;
                    }
                    curve.keys = keys;
                    curve.preWrapMode = sac.preWrapMode;
                    curve.postWrapMode = sac.postWrapMode;
                }

                return curve;
            }
            else if( type.IsAssignableFrom(typeof(Gradient)))
            {
                GradientWrapper gw = new GradientWrapper();
                Gradient gradient = new Gradient();

                JsonUtility.FromJsonOverwrite(text, gw);

                gradient.mode = gw.gradientMode;
                GradientColorKey[] colorKeys = new GradientColorKey[gw.colorKeys.Length];

                for (int i = 0; i < gw.colorKeys.Length; ++i)
                {
                    colorKeys[i].color = gw.colorKeys[i].color;
                    colorKeys[i].time = gw.colorKeys[i].time;
                }
                

                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[gw.alphaKeys.Length];
                for (int i = 0; i < gw.alphaKeys.Length; ++i)
                {
                    alphaKeys[i].alpha = gw.alphaKeys[i].alpha;
                    alphaKeys[i].time = gw.alphaKeys[i].time;
                }
                gradient.SetKeys(colorKeys, alphaKeys);
                return gradient;
            }
            else
            {
                object obj = Activator.CreateInstance(type);
                EditorJsonUtility.FromJsonOverwrite(text, obj);
                return obj;
            }
        }
    }
}
