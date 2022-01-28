using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using UIGen.Generation;

namespace UnityEngine.Rendering.UIGen
{
    [UIPropertyGeneratorSupports(typeof(DebugMenuUIGenerator.DebugMenu))]
    [UIPropertyGenerator(typeof(float))]
    public class FloatUIPropertyGenerator : UIPropertyGenerator
    {
        public override bool Generate(
            [DisallowNull] in UIDefinition.Property property,
            [NotNullWhen(true)] out UIImplementationIntermediateDocuments documents,
            [NotNullWhen(false)] out Exception error
        )
        {
            documents = default;
            error = default;

            if (!GeneratorUtility.ExtractAndNicifyName((string)property.propertyPath, out var niceName, out error))
                return false;

            var element = XElement.Parse(@$"<FloatField label=""{niceName}"" value=""42"" binding-path=""{property.propertyPath}""/>");
            // Add namespace
            element.Name = UxmlConstants.ui + element.Name.LocalName;

            if (!UIImplementationIntermediateDocuments.From(element, out documents, out error))
                return false;

            documents.bindContextBody = documents.bindContextBody.AddStatements(SyntaxFactory.ParseStatement("int toto = 0;"));

            return true;
        }
    }
}
