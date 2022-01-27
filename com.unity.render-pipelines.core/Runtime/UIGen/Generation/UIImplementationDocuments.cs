using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp;

namespace UnityEngine.Rendering.UIGen
{
    public class UIImplementationDocuments
    {
        XmlDocument m_Uxml;
        CSharpSyntaxTree m_RuntimeCode;

        public static bool From(
            [DisallowNull] XmlDocument visualTree,
            [DisallowNull] CSharpSyntaxTree runtimeCode,
            [NotNullWhen(true)] out UIImplementationDocuments documents,
            [NotNullWhen(false)] out Exception error
        )
        {
            throw new NotImplementedException();
        }
    }

    public static partial class UIImplementationDocumentsExtensions
    {
        // Consider async API?
        [MustUseReturnValue]
        public static bool WriteToDisk(
            [DisallowNull] this UIImplementationDocuments view,
            GenerationTargetLocations locations,
            [NotNullWhen(false)] out Exception error
        )
        {
            throw new NotImplementedException();
        }
    }
}
