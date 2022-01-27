using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using JetBrains.Annotations;

namespace UnityEngine.Rendering.UIGen
{
    public class UIPropertySetGenerator
    {
        class GeneratorSet
        {
            Dictionary<Type, UIPropertyGenerator> m_Generators = new();

            [MustUseReturnValue]
            public bool Get(
                [DisallowNull] Type propertyType,
                [NotNullWhen(true)] out UIPropertyGenerator uiPropertyGenerator,
                [NotNullWhen(false)] out Exception error
            )
            {
                if (!m_Generators.TryGetValue(propertyType, out uiPropertyGenerator))
                {
                    error = new ArgumentException($"Property type {propertyType.Name} has no registered generator");
                    return false;
                }

                error = default;
                return true;
            }

            [MustUseReturnValue]
            public bool TryAdd(
                [DisallowNull] Type supportedType,
                [DisallowNull] UIPropertyGenerator instance,
                [NotNullWhen(false)] out Exception error
            )
            {
                if (!m_Generators.TryAdd(supportedType, instance))
                {
                    error = new ArgumentException($"{supportedType.Name} has already a generator registered");
                    return false;
                }

                error = default;
                return true;
            }
        }

        public static UIPropertySetGenerator Empty() => new UIPropertySetGenerator();

        public static bool TryFromGeneratorTypes(
            [DisallowNull] Type[] types,
            [NotNullWhen(true)] out UIPropertySetGenerator setGenerator,
            [NotNullWhen(false)] out Exception error
        )
        {
            setGenerator = default;

            setGenerator = new();
            foreach (var type in types)
            {
                if (type == null)
                {
                    error = new ArgumentNullException(nameof(types), "A type provided is null");
                    return false;
                }

                if (type.GetCustomAttribute(typeof(UIPropertyGeneratorAttribute)) is not UIPropertyGeneratorAttribute attr)
                {
                    error = new ArgumentException($"Type {type.Name} do not have a {nameof(UIPropertyGeneratorAttribute)}");
                    return false;
                }

                if (type.IsAssignableFrom(typeof(UIPropertyGenerator)))
                {
                    error = new ArgumentException($"Type {type.Name} is not a {nameof(UIPropertyGenerator)}");
                    return false;
                }

                if (attr.supportedTypes.Length == 0)
                {
                    continue;
                }

                var instance = (UIPropertyGenerator)Activator.CreateInstance(type);
                foreach (var supportedType in attr.supportedTypes)
                {
                    if (!setGenerator.m_GeneratorSet.TryAdd(supportedType, instance, out error))
                        return false;
                }
            }

            error = default;
            return true;
        }

        public static UIPropertySetGenerator FromGeneratorTypesOrEmpty([DisallowNull] params Type[] types)
        {
            if (!TryFromGeneratorTypes(types, out var setGenerator, out var error))
            {
                Debug.LogException(error);
                return Empty();
            }

            return setGenerator;
        }

        UIPropertySetGenerator() { }

        GeneratorSet m_GeneratorSet = new();


        [MustUseReturnValue]
        public bool GenerateIntermediateDocumentsFor(
            [DisallowNull] UIDefinition definition,
            [NotNullWhen(true)] out Dictionary<UIDefinition.Property, UIImplementationIntermediateDocuments> result,
            [NotNullWhen(false)] out Exception error
        )
        {
            error = default;
            // TODO: [Fred] Must be pooled
            result = new();
            foreach (var property in definition.properties.list)
            {
                if (GenerateIntermediateDocumentsFor(property, out var document, out error))
                    return false;

                result.Add(property, document);
            }

            return true;
        }

        [MustUseReturnValue]
        public bool GenerateIntermediateDocumentsFor(
            [DisallowNull] UIDefinition.Property property,
            [NotNullWhen(true)] out UIImplementationIntermediateDocuments result,
            [NotNullWhen(false)] out Exception error
        )
        {
            result = default;

            if (!m_GeneratorSet.Get(property.type, out UIPropertyGenerator generator, out error))
                return false;

            if (!generator.Generate(property, out result, out error))
                return false;

            // TODO: [Fred] Add feature mutators here

            return true;
        }
    }
}
