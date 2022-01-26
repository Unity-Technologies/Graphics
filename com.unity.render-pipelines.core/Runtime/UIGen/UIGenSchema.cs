using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

// namespace UnityEngine.Rendering.UIGen
// {
//     // TODO: Make immutable
//     class UIGenSchema
//     {
//         [DefaultValue(null)]
//         public HashSet<Type> types = new();
//         public List<UIGenSchemaFeature> features;
//         public List<UIGenSchemaCategorizedProperty> properties;
//     }
//
//     abstract class UIGenSchemaFeature
//     {
//         [DefaultValue(null)]
//         public HashSet<Type> validFor = new();
//     }
//
//     sealed class UIGenSchemaFeature<TParameter> : UIGenSchemaFeature
//     {
//
//     }
//
//     abstract class UIGenSchemaFeatureParameters
//     {
//         public Type feature;
//     }
//
//     sealed class UIGenSchemaFeatureParameters<TParameter> : UIGenSchemaFeatureParameters
//     {
//         public TParameter parameters;
//     }
//
//     class UIGenSchemaProperty
//     {
//         [Flags]
//         public enum Options
//         {
//             None = 0,
//             Runtime = 1 << 0,
//             Editor = 1 << 1,
//             EditorForceUpdate = 1 << 2,
//         }
//
//         public Type type;
//         public string name;
//         public string tooltip;
//         public List<UIGenSchemaFeatureParameters> features;
//         public Options options;
//         public List<UIGenSchemaProperty> children;
//     }
//
//     class UIGenSchemaCategory
//     {
//         public string primary;
//         [DefaultValue("")]
//         public string secondary;
//     }
//
//     class UIGenSchemaCategorizedProperty
//     {
//         public UIGenSchemaProperty property;
//         public UIGenSchemaCategory category;
//     }
// }

namespace UnityEngine.Rendering.UIGen
{
    public class UIDefinition
    {
        public struct CategoryId : IEquatable<CategoryId>
        {
            string m_Name;

            public bool Equals(CategoryId other)
            {
                return m_Name == other.m_Name;
            }

            public override bool Equals(object obj)
            {
                return obj is CategoryId other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (m_Name != null ? m_Name.GetHashCode() : 0);
            }
        }

        public struct QualifiedCategory : IEquatable<QualifiedCategory>
        {
            CategoryId primary;
            CategoryId secondary;

            public bool Equals(QualifiedCategory other)
            {
                return primary.Equals(other.primary) && secondary.Equals(other.secondary);
            }

            public override bool Equals(object obj)
            {
                return obj is QualifiedCategory other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(primary, secondary);
            }
        }

        public class CategorizedProperty
        {
            QualifiedCategory category;
            Property property;
        }

        public class Property
        {
            List<IFeatureParameter> featureParameters = new();
            Property parent;

            /// <summary>
            /// path from context root to this property, member access separated by `.`
            /// </summary>
            public string propertyPath { get; }
        }

        public interface IFeatureParameter { }

        public struct Min<T> : IFeatureParameter
        {
            public readonly T value;
        }

        public struct Max<T> : IFeatureParameter
        {
            public readonly T value;
        }

        //Range is Min + Max. It only add a display as a slider
        public struct DisplayAsSlider : IFeatureParameter // range is better
        { }

        public struct ColorUsage : IFeatureParameter
        {
            public readonly bool showAlpha;
            public readonly bool hdr;
        }

        public struct EnabledIf : IFeatureParameter
        {
            public readonly Func<bool> isEnabled;
        }

        public struct CallMethod : IFeatureParameter
        {
            public readonly Action call;
        }

        public struct DataBind/*<T>*/ : IFeatureParameter
        {
            string bindingPath;
            //public readonly Func<T> get;
            //public readonly Action<T> set;
        }

        public PooledList<Property> properties = new();


        // Not Weird API, it is always confusing as for the direction of the data flow
        // Consider a static API
        /// <summary>
        /// Move the data from <paramref name="toMerge"/> into self.
        /// </summary>
        /// <param name="toMerge"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [MustUseReturnValue]
        public bool TakeAndMerge(
            [DisallowNull] UIDefinition toMerge,
            [NotNullWhen(false)] out Exception error
        )
        {
            throw new NotImplementedException();
        }
    }

    public static class UIDefinitionExtensions
    {
        [MustUseReturnValue]
        public static bool Aggregate<TList>(
            [DisallowNull] this TList definitions,
            [NotNullWhen(true)] out UIDefinition merged,
            [NotNullWhen(false)] out Exception error
        ) where TList : IList<UIDefinition>
        {
            throw new NotImplementedException();
        }

        [MustUseReturnValue]
        public static bool ComputeHash(
            [DisallowNull] this UIDefinition definition,
            out Hash128 hash,
            [NotNullWhen(false)] out Exception error
        )
        {
            throw new NotImplementedException();
        }
    }
}

namespace UnityEngine.Rendering.UIGen
{
    public class UIDefinitionPropertyCategoryIndex : IDisposable
    {
        [MustUseReturnValue]
        public static bool FromDefinition(
            [DisallowNull] UIDefinition definition,
            [NotNullWhen(true)] out UIDefinitionPropertyCategoryIndex index,
            [NotNullWhen(false)] out Exception error
        )
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }
    }
}

namespace UnityEngine.Rendering.UIGen
{
    //make pooled dictionary
    public struct PooledList<TValue> : IDisposable
    {
        List<TValue> m_List;

        [MustUseReturnValue]
        public bool TryGet(
            [NotNullWhen(true)] out List<TValue> thisList,
            [NotNullWhen(false)] out Exception error
        )
        {
            if (m_List == null)
            {
                error = new ObjectDisposedException(nameof(PooledList<TValue>));
                thisList = null;
                return false;
            }

            thisList = m_List;
            error = null;
            return true;
        }

        public List<TValue> list
        {
            get
            {
                if (!TryGet(out var thisList, out var error))
                    throw error;
                return thisList;
            }
        }

        public unsafe List<TValue> listUnsafe => m_List;

        public void Dispose()
        {
            if (m_List == null)
                return;
            ListPool<TValue>.Release(m_List);
            m_List = null;
        }
    }
}

namespace UnityEngine.Rendering.UIGen
{ }
