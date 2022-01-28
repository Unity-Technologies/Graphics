using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Rendering.UIGen.UXML;
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
                error = new Exception("Providen binding path is empty. Cannot be nicified.").WithStackTrace();
                return false;
            }

            int index = bindingPath.LastIndexOf('.');
            if (index == bindingPath.Length - 1)
            {
                error = new Exception("Providen binding path is cannot end with '.'.").WithStackTrace();
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

        public UIPropertyGeneratorAttribute(params Type[] supportedTypes)
        {
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

            if (!GeneratorUtility.ExtractAndNicifyName((string)property.propertyPath, out var niceName, out error))
                return false;

            var element = XElement.Parse(@$"<IntegerField label=""{niceName}"" value=""42"" binding-path=""{property.propertyPath}"" xmlns=""ui""/>");

            if (!UIImplementationIntermediateDocuments.From(element, out var document, out error))
                return false;

            document.bindContextBody = document.bindContextBody.AddStatements(SyntaxFactory.ParseStatement("int toto = 0;"));

            //debug purpose only
            Debug.Log(document.bindContextBody.ToString());
            Debug.Log(element);
            //end debug

            return true;
        }
    }
}
