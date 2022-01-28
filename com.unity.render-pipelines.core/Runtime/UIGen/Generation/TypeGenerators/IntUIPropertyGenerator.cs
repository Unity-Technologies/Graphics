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

            if (!GeneratorUtility.ExtractAndNicifyName((string)property.propertyPath, out var niceName, out error))
                return false;

            var element = XElement.Parse(@$"<IntegerField label=""{niceName}"" value=""42"" name=""{property.propertyPath}"" binding-path=""{property.propertyPath}""/>");
            // Add namespace
            element.Name = UxmlConstants.ui + element.Name.LocalName;

            if (!UIImplementationIntermediateDocuments.From(element, out documents, out error))
                return false;

            documents.bindContextBody = documents.bindContextBody.AddStatements(SyntaxFactory.ParseStatement($@"
                var fieldContainer = container.Q<IntegerField>(""{property.propertyPath}"");
                fieldContainer.SetValueWithoutNotify(context.{property.propertyPath});
                EventCallback<ChangeEvent<int>> callback = (ChangeEvent<int> evt) =>
                {{
                    context.{property.propertyPath} = evt.newValue;
                }};
                m_Bindings[""{property.propertyPath}""] = new Binding<int>() {{ callback = callback }};
                fieldContainer.RegisterValueChangedCallback(callback);
            "));

            documents.unbindContextBody = documents.unbindContextBody.AddStatements(SyntaxFactory.ParseStatement($@"
                var fieldContainer = container.Q<IntegerField>(""{property.propertyPath}"");
                Binding<int> binding = (Binding<int>)m_Bindings[""{property.propertyPath}""];
                fieldContainer.UnregisterValueChangedCallback(binding.callback);
            "));

            return true;
        }
    }
}
