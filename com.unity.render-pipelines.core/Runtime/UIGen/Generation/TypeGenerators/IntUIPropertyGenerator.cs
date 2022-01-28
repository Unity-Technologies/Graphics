using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using UIGen.Generation;

namespace UnityEngine.Rendering.UIGen
{
    [UIPropertyGeneratorSupports(typeof(DebugMenuUIGenerator.DebugMenu))]
    [UIPropertyGenerator(typeof(int))]
    public class IntUIPropertyGenerator : UIPropertyGenerator
    {
        public override bool Generate(
            [DisallowNull] in UIDefinition.Property property,
            [NotNullWhen(true)] out UIImplementationIntermediateDocuments documents,
            [NotNullWhen(false)] out Exception error
        )
        {
            documents = default;
            error = default;

            var element = XElement.Parse(@$"<IntegerField value=""42""/>");
            // Add namespace
            element.Name = UxmlConstants.ui + element.Name.LocalName;

            if (!UIImplementationIntermediateDocuments.From(element, out documents, out error))
                return false;

            documents.bindContextBody = documents.bindContextBody.AddStatements(SyntaxFactory.ParseStatement("int toto = 0;"));

            return true;
        }
    }
}
