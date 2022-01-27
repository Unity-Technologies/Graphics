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

    public static class BindableViewExtensions
    {
        public struct DiskLocation
        {
            string assetLocation;
            string runtimeCodeLocation;
            string editorCodeLocation;

            public bool GetEditorPath(
                [DisallowNull] string relativePath,
                [NotNullWhen(true)] out string path,
                [NotNullWhen(false)] out Exception error
            )
            {
                throw new NotImplementedException();
            }
        }

        // Consider async API?
        [MustUseReturnValue]
        public static bool WriteToDisk(
            [DisallowNull] this UIImplementationDocuments view,
            DiskLocation location,
            [NotNullWhen(false)] out Exception error
        )
        {
            throw new NotImplementedException();
        }
    }
}
