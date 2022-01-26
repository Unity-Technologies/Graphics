using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Xml;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnityEngine.Rendering.UIGen
{
    public static class DebugMenuUIGenerator
    {
        public struct Parameters
        {
            public string uiViewTypeName;
            public string uiViewContextTypeName;

            public static Parameters Default() => new ()
            {
                uiViewTypeName = "DebugMenu",
                uiViewContextTypeName = "DebugMenuContext",
            };
        }

        [MustUseReturnValue]
        public static bool GenerateDebugMenuBindableView(
            [DisallowNull] this UIDefinition definition,
            Parameters parameters,
            [NotNullWhen(true)] out UIImplementationDocuments result,
            [NotNullWhen(false)] out Exception error
        )
        {
            result = default;

            if (!parameters.uiViewTypeName.FailIfNullOrEmpty(nameof(parameters.uiViewTypeName), out error)
                || !parameters.uiViewContextTypeName.FailIfNullOrEmpty(nameof(parameters.uiViewContextTypeName), out error))
                return false;

            // TODO multithreading:
            //   - Map
            //   - Property generation C# + UXML (multithreading inside)
            if (!UIDefinitionPropertyCategoryIndex.FromDefinition(definition, out var index, out error))
                return false;

            if (!GenerateBindableViewIntermediateDocumentFromProperties(
                    definition,
                    out var intermediateDocuments,
                    out error))
                return false;

            using (index)
                return GenerateDocumentFromIntermediate(parameters, index, intermediateDocuments, out result, out error);
        }

        [MustUseReturnValue]
        static bool GenerateBindableViewIntermediateDocumentFromProperties(
            [DisallowNull] UIDefinition definition,
            out Dictionary<UIDefinition.Property, UIImplementationIntermediateDocuments> result,
            [NotNullWhen(false)] out Exception error
        )
        {
            throw new NotImplementedException();
        }


        [MustUseReturnValue]
        static bool GenerateDocumentFromIntermediate(
            Parameters parameters,
            [DisallowNull] UIDefinitionPropertyCategoryIndex index,
            [DisallowNull] Dictionary<UIDefinition.Property, UIImplementationIntermediateDocuments> intermediateDocuments,
            [NotNullWhen(true)] out UIImplementationDocuments result,
            [NotNullWhen(false)] out Exception error
        )
        {
            result = default;

            if (!GenerateRuntimeCode(parameters, intermediateDocuments, out var runtimeCode, out error))
                return false;

            if (!GenerateVisualTreeAsset(parameters, index, intermediateDocuments, out var visualTreeAsset, out error))
                return false;

            if (!UIImplementationDocuments.From(visualTreeAsset, runtimeCode, out result, out error))
                return false;

            return true;
        }

        [MustUseReturnValue]
        static bool GenerateVisualTreeAsset(
            Parameters parameters,
            [DisallowNull] UIDefinitionPropertyCategoryIndex index,
            [DisallowNull] Dictionary<UIDefinition.Property, UIImplementationIntermediateDocuments> intermediateDocuments,
            [NotNullWhen(true)] out XmlDocument visualTreeAsset,
            [NotNullWhen(false)] out Exception error
        )
        {
            throw new NotImplementedException();
        }

        [MustUseReturnValue]
        static bool GenerateRuntimeCode(
            Parameters parameters,
            [DisallowNull] Dictionary<UIDefinition.Property, UIImplementationIntermediateDocuments> intermediateDocuments,
            [NotNullWhen(true)] out CSharpSyntaxTree runtimeCode,
            [NotNullWhen(false)] out Exception error
        )
        {
            error = default;
            runtimeCode = default;

            using (HashSetPool<string>.Get(out var usings))
            using (ListPool<MemberDeclarationSyntax>.Get(out var types))
            {
                if (!GenerateUIViewDeclaration(parameters, intermediateDocuments, usings, out var uiViewDeclaration, out error))
                    return false;

                if (!GenerateContextDeclarations(parameters, intermediateDocuments, usings, out var interfaceDeclaration, out var implementationDeclaration, out error))
                    return false;

                types.Add(uiViewDeclaration);
                types.Add(interfaceDeclaration);
                types.Add(implementationDeclaration);

                var usingDirectives = SyntaxFactory.List<UsingDirectiveSyntax>(
                    usings.Select(u => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(u))));

                runtimeCode = (CSharpSyntaxTree) SyntaxFactory.SyntaxTree(
                    SyntaxFactory.CompilationUnit(
                        SyntaxFactory.List<ExternAliasDirectiveSyntax>(),
                        usingDirectives,
                        SyntaxFactory.List<AttributeListSyntax>(),
                        SyntaxFactory.List(types)
                    )
                );
            }

            return true;
        }

        [MustUseReturnValue]
        static bool GenerateUIViewDeclaration(
            Parameters parameters,
            [DisallowNull] Dictionary<UIDefinition.Property, UIImplementationIntermediateDocuments> intermediateDocuments,
            [DisallowNull] HashSet<string> usings,
            [NotNullWhen(true)] out TypeDeclarationSyntax declaration,
            [NotNullWhen(false)] out Exception error
        )
        {
            error = default;

            var bindContextBodies = intermediateDocuments.Select(p => p.Value.bindContextBody)
                .Aggregate(new StringBuilder(), (acc, s) =>
                {
                    acc.Append(s);
                    return acc;
                }).ToString();
            var unbindContextBodies = intermediateDocuments.Select(p => p.Value.unbindContextBody)
                .Aggregate(new StringBuilder(), (acc, s) =>
                {
                    acc.Append(s);
                    return acc;
                }).ToString();

            usings.Add("System");
            usings.Add("System.Diagnostics.CodeAnalysis");
            usings.Add("UnityEngine.UIElements");
            usings.Add("UnityEngine.Rendering.UIGen");

            var code = $@"public sealed class {parameters.uiViewTypeName} : UIView<{parameters.uiViewTypeName}, I{parameters.uiViewContextTypeName}>
{{
    static DebugMenu()
    {{
        UIViewDefaults<{parameters.uiViewTypeName}>.DefaultTemplateAssetPath = ""Assets/DebugMenu.uxml"";
    }}

    protected override bool BindContext(
        [DisallowNull] TContext context,
        [DisallowNull] TemplateContainer container,
        [NotNullWhen(false)] out Exception error
    )
    {{
        {bindContextBodies}
    }}

    protected override bool UnbindContext(
        [DisallowNull] TContext context,
        [DisallowNull] TemplateContainer container,
        [NotNullWhen(false)] out Exception error
    )
    {{
        {unbindContextBodies}
    }}
}}";
            declaration = (TypeDeclarationSyntax) SyntaxFactory.ParseSyntaxTree(code).GetRoot();
            return true;
        }

        [MustUseReturnValue]
        static bool GenerateContextDeclarations(
            Parameters parameters,
            [DisallowNull] Dictionary<UIDefinition.Property, UIImplementationIntermediateDocuments> intermediateDocuments,
            [DisallowNull] HashSet<string> usings,
            [NotNullWhen(true)] out TypeDeclarationSyntax interfaceDeclaration,
            [NotNullWhen(true)] out TypeDeclarationSyntax implementationDeclaration,
            [NotNullWhen(false)] out Exception error
        )
        {
            error = default;

            // TODO: Find a way to acquire the data needed to generate context types
            //  1. Type and name of root properties
            //  2. Static access for the implementation of the root properties
            //    UIDefinition may not need to know this, maybe it is an additional input to the generation

            throw new NotImplementedException();
            return true;
        }
    }
}
