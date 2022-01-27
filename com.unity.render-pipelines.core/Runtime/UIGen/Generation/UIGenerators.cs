using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Property = UnityEngine.Rendering.UIGen.UIDefinition.Property;

namespace UnityEngine.Rendering.UIGen
{
    public static class GeneratorUtility
    {
        static readonly Dictionary<Type, UIPropertyTypeGenerator> s_Generators = new()
        {
            //default generators : hardcoded list
            { IntUIPropertyTypeGenerator.typeSupported, new IntUIPropertyTypeGenerator() },
        };

        static Dictionary<string, Dictionary<Type, UIPropertyTypeGenerator>> s_OverridingGenerators = new();

        // TODO: [Fred] Can we use a Type instead of a string
        public static Dictionary<Type, UIPropertyTypeGenerator> GetGenerators(in string UIName = null)
        {
            var generators = new Dictionary<Type, UIPropertyTypeGenerator>(s_Generators);

            if (string.IsNullOrEmpty(UIName))
                return generators;

            if (!s_OverridingGenerators.ContainsKey(UIName))
                return generators;

            foreach (var kvp in s_OverridingGenerators[UIName])
                generators[kvp.Key] = kvp.Value;

            return generators;
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void FindCustomGenerator()
        {
            var customGeneratorTypes = TypeCache.GetTypesWithAttribute<CustomGeneratorAttribute>();

            foreach (var customGeneratorType in customGeneratorTypes)
            {
                //sanity check
                if (!typeof(UIPropertyTypeGenerator).IsAssignableFrom(customGeneratorType))
                {
                    Debug.LogError($"{nameof(CustomGeneratorAttribute)} should only be used on class implementing {nameof(UIPropertyTypeGenerator)}<T>. Found on {customGeneratorType.FullName}. Skipping.");
                    continue;
                }

                var attributes = customGeneratorType.GetCustomAttributes(typeof(CustomGeneratorAttribute), false);
                foreach (CustomGeneratorAttribute attribute in attributes)
                {
                    if (!s_OverridingGenerators.ContainsKey(attribute.UIName))
                        s_OverridingGenerators[attribute.UIName] = new();

                    var generator = Activator.CreateInstance(customGeneratorType) as UIPropertyTypeGenerator;

                    //sanity check
                    if (s_OverridingGenerators[attribute.UIName].ContainsKey(generator.GetTypeSupported()))
                    {
                        Debug.LogError($"There is several {nameof(CustomGeneratorAttribute)} conflicting for type {generator.GetTypeSupported().FullName}. Discarding {customGeneratorType.FullName}.");
                        continue;
                    }

                    s_OverridingGenerators[attribute.UIName][generator.GetTypeSupported()] = generator;
                }
            }
        }

        public static bool ExtractAndNicifyName([DisallowNull] in string bindingPath, [NotNullWhen(true)] out string nicifyedName, [NotNullWhen(false)] out Exception error)
        {
            nicifyedName = null;
            error = null;
            if (string.IsNullOrEmpty(bindingPath))
            {
                error = new Exception("Providen binding path is empty. Cannot be nicified.");
                return false;
            }

            int index = bindingPath.LastIndexOf('.');
            if (index == bindingPath.Length - 1)
            {
                error = new Exception("Providen binding path is cannot end with '.'.");
                return false;
            }

            string end = index == -1 ? bindingPath : bindingPath.Substring(index + 1);
            nicifyedName = ObjectNames.NicifyVariableName(end);
            return true;
        }
#endif
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CustomGeneratorAttribute : Attribute
    {
        public readonly string UIName;
        public CustomGeneratorAttribute(string UIName)
        {
            this.UIName = UIName;
        }
    }

    public interface IUIPropertyTypeGenerator
    {
        [MustUseReturnValue]
        bool Generate(
            [DisallowNull] in Property property,
            [NotNullWhen(true)] out UIImplementationIntermediateDocuments documents,
            [NotNullWhen(false)] out Exception error);
    }

    public abstract class UIPropertyTypeGenerator : IUIPropertyTypeGenerator
    {
        [MustUseReturnValue]
        public abstract bool Generate(
            [DisallowNull] in Property property,
            [NotNullWhen(true)] out UIImplementationIntermediateDocuments documents,
            [NotNullWhen(false)] out Exception error
        );

        // TODO: [Fred] A generator may support multiple type, we should find another way to register
        // TODO: [Fred] this can be a property getter instead
        internal abstract Type GetTypeSupported();

        internal UIPropertyTypeGenerator()
            => throw new Exception($"You should not inerhit directly from {nameof(UIPropertyTypeGenerator)}. Use {nameof(UIPropertyTypeGenerator)}<T> instead.");

        internal UIPropertyTypeGenerator(bool unused) { }
    }

    public abstract class UIPropertyTypeGenerator<T> : UIPropertyTypeGenerator
    {
        internal override Type GetTypeSupported() => typeSupported;
        public static Type typeSupported => typeof(T);

        public UIPropertyTypeGenerator() : base(true) { }
    }


    public class IntUIPropertyTypeGenerator : UIPropertyTypeGenerator<int>
    {
        public override bool Generate(
            [DisallowNull] in Property property,
            [NotNullWhen(true)] out UIImplementationIntermediateDocuments documents,
            [NotNullWhen(false)] out Exception error
        )
        {
            documents = default;
            error = default;

            if (!GeneratorUtility.ExtractAndNicifyName(property.propertyPath, out var niceName, out error))
                return false;

            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root></root>");

            XmlElement element = doc.CreateElement("ui:IntegerField");
            element.SetAttribute("label", niceName);
            element.SetAttribute("value", "42");
            element.SetAttribute("binding-path", property.propertyPath);
            doc.DocumentElement.AppendChild(element);

            if (!UIImplementationIntermediateDocuments.From(element, out var document, out error))
                return false;

            document.bindContextBody = document.bindContextBody.AddStatements(SyntaxFactory.ParseStatement("int toto = 0;"));

            //debug purpose only
            Debug.Log(document.bindContextBody.ToString());
            Debug.Log(doc.OuterXml);
            //end debug

            return true;
        }
    }
}
