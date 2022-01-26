using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnityEngine.Rendering.UIGen
{
    public static class DebugMenuUIGenerator
    {
        public struct Parameters { }

        [MustUseReturnValue]
        public static bool GenerateDebugMenuBindableView(
            [DisallowNull] this UIDefinition definition,
            Parameters parameters,
            [NotNullWhen(true)] out UIImplementationDocuments result,
            [NotNullWhen(false)] out Exception error
        )
        {
            result = default;

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
                return GenerateDocumentFromIntermediate(index, intermediateDocuments, out result, out error);
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
            [DisallowNull] UIDefinitionPropertyCategoryIndex index,
            [DisallowNull] Dictionary<UIDefinition.Property, UIImplementationIntermediateDocuments> intermediateDocuments,
            [NotNullWhen(true)] out UIImplementationDocuments result,
            [NotNullWhen(false)] out Exception error
        )
        {
            var bindContextBodies = intermediateDocuments.Select(p => p.Value.bindContextBody)
                .Aggregate(new StringBuilder(), (acc, s) =>
                {
                    acc.Append(s.ToString());
                    return acc;
                }).ToString();
            var unbindContextBodies = intermediateDocuments.Select(p => p.Value.unbindContextBody)
                .Aggregate(new StringBuilder(), (acc, s) =>
                {
                    acc.Append(s.ToString());
                    return acc;
                }).ToString();

            var code = $@"public sealed class ";

            throw new NotImplementedException();
        }
    }
}
