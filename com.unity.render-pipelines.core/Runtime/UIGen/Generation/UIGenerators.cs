using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Property = UnityEngine.Rendering.UIGen.UIDefinition.Property;

namespace UnityEngine.Rendering.UIGen
{
    public static class GeneratorUtility
    {
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
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class UIPropertyGeneratorSupportsAttribute : Attribute
    {
        public readonly Type uiType;
        public UIPropertyGeneratorSupportsAttribute(Type uiType)
        {
            this.uiType = uiType;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class UIPropertyGeneratorAttribute : Attribute
    {
        public Type[] supportedTypes { get; }

        public UIPropertyGeneratorAttribute(params Type[] supportedTypes) {
            this.supportedTypes = supportedTypes;
        }
    }

    public interface IUIPropertyGenerator
    {
        [MustUseReturnValue]
        bool Generate(
            [DisallowNull] in Property property,
            [NotNullWhen(true)] out UIImplementationIntermediateDocuments documents,
            [NotNullWhen(false)] out Exception error);
    }

    public abstract class UIPropertyGenerator : IUIPropertyGenerator
    {
        [MustUseReturnValue]
        public abstract bool Generate(
            [DisallowNull] in Property property,
            [NotNullWhen(true)] out UIImplementationIntermediateDocuments documents,
            [NotNullWhen(false)] out Exception error
        );
    }

    [UIPropertyGeneratorSupports(typeof(DebugMenuUIGenerator.DebugMenu))]
    [UIPropertyGenerator(typeof(int))]
    public class IntUIPropertyGenerator : UIPropertyGenerator
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
