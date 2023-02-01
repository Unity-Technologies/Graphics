using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.ContextLayeredDataStorage;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [Serializable]
    abstract class BakedElement
    {
        public static void BakePort(PortHandler portHandler, List<BakedElement> bakedPort)
        {
            Profiler.BeginSample($"{nameof(BakedElement)}.{nameof(BakePort)}");

            var basePath = portHandler.ID.FullPath;
            GetFieldsRecursive(portHandler.GetFields().Select(fh => fh.Reader).ToList());

            void GetFieldsRecursive(List<DataReader> elements)
            {
                foreach (var reader in elements)
                {
                    bakedPort.Add(BakedElement.Create(reader.Element, basePath));

                    var children = reader.GetChildren().ToList();
                    GetFieldsRecursive(children);
                }
            }

            Profiler.EndSample();
        }

        public static void UnbakePort(List<BakedElement> bakedPort, PortHandler portHandler)
        {
            Profiler.BeginSample($"{nameof(BakedElement)}.{nameof(UnbakePort)}");

            if (bakedPort != null)
            {
                foreach (var element in bakedPort)
                {
                    element?.ToPort(portHandler);
                }
            }

            Profiler.EndSample();
        }

        static BakedElement Create(Element source, string basePath)
        {
            switch (source)
            {
                case Element<string> e:
                    return new BakedElement<string>(e, basePath);

                case Element<bool> e:
                    return new BakedElement<bool>(e, basePath);

                case Element<int> e:
                    return new BakedElement<int>(e, basePath);

                case Element<float> e:
                    return new BakedElement<float>(e, basePath);

                case Element<ContextEntryEnumTags.DataSource> e:
                    return new BakedElement<ContextEntryEnumTags.DataSource>(e, basePath);

                case Element<ContextEntryEnumTags.PropertyBlockUsage> e:
                    return new BakedElement<ContextEntryEnumTags.PropertyBlockUsage>(e, basePath);

                case Element<GraphType.Height> e:
                    return new BakedElement<GraphType.Height>(e, basePath);

                case Element<GraphType.Length> e:
                    return new BakedElement<GraphType.Length>(e, basePath);

                case Element<GraphType.Precision> e:
                    return new BakedElement<GraphType.Precision>(e, basePath);

                case Element<GraphType.Primitive> e:
                    return new BakedElement<GraphType.Primitive>(e, basePath);

                case Element<SerializableTexture> e:
                    return new BakedElement<SerializableTexture>(e, basePath);

                case Element<BaseTextureType.TextureType> e:
                    return new BakedElement<BaseTextureType.TextureType>(e, basePath);

                case Element<ContextEntryEnumTags.TextureDefaultType> e:
                    return new BakedElement<ContextEntryEnumTags.TextureDefaultType>(e, basePath);

                case Element<SamplerStateType.Filter> e:
                    return new BakedElement<SamplerStateType.Filter>(e, basePath);

                case Element<SamplerStateType.Wrap> e:
                    return new BakedElement<SamplerStateType.Wrap>(e, basePath);

                case Element<SamplerStateType.Aniso> e:
                    return new BakedElement<SamplerStateType.Aniso>(e, basePath);

                default:
                    var sourceType = source.GetType();
                    if (sourceType.IsGenericType && sourceType.GetGenericTypeDefinition() == typeof(Element<>))
                    {
                        if (Unsupported.IsDeveloperBuild())
                            Debug.LogWarning($"Unexpected type {source.GetType()}. Add your type to {nameof(BakedElement)}.{nameof(Create)} to avoid using reflection.");

                        var typeParam = sourceType.GetGenericArguments().First();
                        var genericBakedElementType = typeof(BakedElement<>);
                        var bakedElementType = genericBakedElementType.MakeGenericType(typeParam);
                        var bakedElement = (BakedElement)Activator.CreateInstance(bakedElementType, source, basePath);
                        return bakedElement;
                    }

                    return null;
            }
        }

        [SerializeField]
        string m_Path;

        protected string Path => m_Path;

        protected BakedElement(Element e, string basePath)
        {
            m_Path = e.ID.FullPath;
            if (m_Path.StartsWith(basePath))
            {
                m_Path = m_Path[(basePath.Length + 1)..];
            }
            else
            {
                Debug.LogError($"Element path {m_Path} does not start with {basePath}.");
                m_Path = null;
            }
        }

        protected abstract void ToPort(PortHandler portHandler);
    }

    [Serializable]
    class BakedElement<T> : BakedElement
    {
        [SerializeField]
        T m_Data;

        public BakedElement(Element<T> e, string basePath)
            : base(e, basePath)
        {
            m_Data = e.GetData<T>();
        }

        protected override void ToPort(PortHandler portHandler)
        {
            if (string.IsNullOrEmpty(Path))
                return;

            var field = portHandler.GetField(Path);
            if (field != null)
            {
                field.SetData(m_Data);
            }
            else
            {
                portHandler.AddField(Path, m_Data);
            }
        }
    }
}
