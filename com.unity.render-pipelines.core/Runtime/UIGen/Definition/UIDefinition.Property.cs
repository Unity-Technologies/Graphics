using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace UnityEngine.Rendering.UIGen
{
    public partial class UIDefinition
    {
        public class Property
        {
            Dictionary<Type, IFeatureParameter> m_FeatureParametersPerFeatureType = new();
            Property parent;

            /// <summary>
            /// path from context root to this property, member access separated by `.`
            /// </summary>
            public PropertyPath propertyPath { get; set; }
            public Type type { get; }
            // TODO: [Fred] NewType pattern: UIPropertyGeneratorType
            public Type generatorOverride { get; }

            Property(PropertyPath propertyPath, [DisallowNull] Type type)
            {
                this.propertyPath = propertyPath;
                this.type = type;
            }

            [MustUseReturnValue]
            public bool AddFeature<TFeature>(
                [DisallowNull] in TFeature feature,
                [NotNullWhen(false)] out Exception error
            ) where TFeature: IFeatureParameter
            {
                if (!m_FeatureParametersPerFeatureType.TryAdd(typeof(TFeature), feature))
                {
                    error = new ArgumentException($"Feature {typeof(TFeature).Name} is already added");
                    return false;
                }

                error = default;
                return true;
            }

            [MustUseReturnValue]
            public static bool New(
                PropertyPath path,
                [DisallowNull] Type type,
                [NotNullWhen(true)] out Property property,
                [NotNullWhen(false)] out Exception error
            )
            {
                property = new Property(path, type);
                error = default;
                return true;
            }

            [MustUseReturnValue]
            public bool SetDisplayName(
                PropertyName name,
                [NotNullWhen(false)] out Exception error
            )
            {
                if (!AddFeature(new DisplayName(name), out error))
                    return false;

                return true;
            }

            [MustUseReturnValue]
            public bool SetTooltip(
                PropertyTooltip tooltip,
                [NotNullWhen(false)] out Exception error
            )
            {
                if (!AddFeature(new Tooltip(tooltip), out error))
                    return false;

                return true;
            }
        }
    }
}
