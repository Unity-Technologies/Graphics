using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace UnityEngine.Rendering.UIGen
{
    public partial class UIPropertySetGenerator
    {
        class GeneratorSet
        {
            Dictionary<Type, UIPropertyGenerator> m_PropertyTypeToGenerator = new();
            Dictionary<Type, UIPropertyGenerator> m_GeneratorTypeToGenerator = new();

            [MustUseReturnValue]
            public bool GetGeneratorForPropertyType(
                [DisallowNull] Type propertyType,
                [NotNullWhen(true)] out UIPropertyGenerator uiPropertyGenerator,
                [NotNullWhen(false)] out Exception error
            )
            {
                if (!m_PropertyTypeToGenerator.TryGetValue(propertyType, out uiPropertyGenerator))
                {
                    error = new ArgumentException($"Property type {propertyType.Name} has no registered generator").WithStackTrace();
                    return false;
                }

                error = default;
                return true;
            }

            [MustUseReturnValue]
            public bool GetGeneratorForGeneratorType(
                [DisallowNull] Type generatorType,
                [NotNullWhen(true)] out UIPropertyGenerator uiPropertyGenerator,
                [NotNullWhen(false)] out Exception error
            )
            {
                if (!m_GeneratorTypeToGenerator.TryGetValue(generatorType, out uiPropertyGenerator))
                {
                    error = new ArgumentException($"Generator type {generatorType.Name} is not registered generator").WithStackTrace();
                    return false;
                }

                error = default;
                return true;
            }

            [MustUseReturnValue]
            public bool TryAddPropertyTypeGenerator(
                [DisallowNull] Type supportedType,
                [DisallowNull] UIPropertyGenerator instance,
                [NotNullWhen(false)] out Exception error
            )
            {
                if (!m_PropertyTypeToGenerator.TryAdd(supportedType, instance))
                {
                    error = new ArgumentException($"{supportedType.Name} has already a generator registered").WithStackTrace();
                    return false;
                }

                error = default;
                return true;
            }

            [MustUseReturnValue]
            public bool TryAddGeneratorTypeGenerator(
                [DisallowNull] UIPropertyGenerator instance,
                [NotNullWhen(false)] out Exception error
            )
            {
                if (!m_GeneratorTypeToGenerator.TryAdd(instance.GetType(), instance))
                {
                    error = new ArgumentException($"{instance.GetType().Name} is already generator registered").WithStackTrace();
                    return false;
                }

                error = default;
                return true;
            }
        }
    }
}
