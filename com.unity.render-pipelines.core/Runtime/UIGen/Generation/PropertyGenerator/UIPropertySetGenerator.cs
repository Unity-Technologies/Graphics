using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using JetBrains.Annotations;

namespace UnityEngine.Rendering.UIGen
{
    public partial class UIPropertySetGenerator
    {
        public static UIPropertySetGenerator Empty() => new UIPropertySetGenerator();

        public static bool TryFromGeneratorTypes(
            [DisallowNull] IEnumerable<Type> types,
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
                    error = new ArgumentNullException(nameof(types), "A type provided is null").WithStackTrace();
                    return false;
                }

                if (type.GetCustomAttribute(typeof(UIPropertyGeneratorAttribute)) is not UIPropertyGeneratorAttribute attr)
                {
                    error = new ArgumentException($"Type {type.Name} do not have a {nameof(UIPropertyGeneratorAttribute)}").WithStackTrace();
                    return false;
                }

                if (type.IsAssignableFrom(typeof(UIPropertyGenerator)))
                {
                    error = new ArgumentException($"Type {type.Name} is not a {nameof(UIPropertyGenerator)}").WithStackTrace();
                    return false;
                }

                if (attr.supportedTypes.Length == 0)
                {
                    continue;
                }

                var instance = (UIPropertyGenerator)Activator.CreateInstance(type);
                if (!setGenerator.m_GeneratorSet.TryAddGeneratorTypeGenerator(instance, out error))
                    return false;

                foreach (var supportedType in attr.supportedTypes)
                {
                    if (!setGenerator.m_GeneratorSet.TryAddPropertyTypeGenerator(supportedType, instance, out error))
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
            foreach (var categorizedProperty in definition.categorizedProperties.list)
            {
                if (!GenerateIntermediateDocumentsFor(categorizedProperty, out var document, out error))
                    return false;

                result.Add(categorizedProperty.property, document);
            }

            return true;
        }

        [MustUseReturnValue]
        public bool GenerateIntermediateDocumentsFor(
            [DisallowNull] UIDefinition.CategorizedProperty categorizedProperty,
            [NotNullWhen(true)] out UIImplementationIntermediateDocuments result,
            [NotNullWhen(false)] out Exception error
        )
        {
            result = default;

            UIPropertyGenerator generator = default;
            if (categorizedProperty.property.generatorOverride != null)
            {
                if (!m_GeneratorSet.GetGeneratorForGeneratorType(
                        categorizedProperty.property.generatorOverride,
                        out generator,
                        out error
                    ))
                    return false;
            }
            else
            {
                if (!m_GeneratorSet.GetGeneratorForPropertyType(categorizedProperty.property.type,
                        out generator,
                        out error))
                    return false;
            }

            if (!generator.Generate(categorizedProperty.property, out result, out error))
                return false;

            // TODO: [Fred] Add feature mutators here

            using (ListPool<UIDefinition.IFeatureParameter>.Get(out var features))
            {
                features.AddRange(categorizedProperty.property.features);
                // Sort for deterministic process
                features.Sort((l,r) => String.CompareOrdinal(l.GetType().Name, r.GetType().Name));

                foreach (var featureParameter in features)
                {
                    if (!featureParameter.Mutate(ref result, out error))
                        return false;
                }
            }

            return true;
        }
    }
}
