using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using UnityEngine.Rendering.UIGen.UXML;

namespace UnityEngine.Rendering.UIGen
{
    public class UIImplementationIntermediateDocuments
    {
        UxmlElement m_PropertyUxml;
        BlockSyntax m_BindContextBody;
        BlockSyntax m_UnbindContextBody;

        UIImplementationIntermediateDocuments([DisallowNull] UxmlElement element)
        {
            m_BindContextBody = SyntaxFactory.Block(Enumerable.Empty<StatementSyntax>());
            m_UnbindContextBody = SyntaxFactory.Block(Enumerable.Empty<StatementSyntax>());
            m_PropertyUxml = element;
        }

        public static bool From(
            [DisallowNull] UxmlElement element,
            [NotNullWhen(true)] out UIImplementationIntermediateDocuments documents,
            [NotNullWhen(false)] out Exception error
        )
        {
            documents = new UIImplementationIntermediateDocuments(element);
            error = null;
            return true;
        }

        [DisallowNull]
        public UxmlElement propertyUxml
        {
            get => m_PropertyUxml;
            set => m_PropertyUxml = value;
        }

        [DisallowNull]
        public BlockSyntax bindContextBody
        {
            get => m_BindContextBody;
            set => m_BindContextBody = value;
        }

        [DisallowNull]
        public BlockSyntax unbindContextBody
        {
            get => m_UnbindContextBody;
            set => m_UnbindContextBody = value;
        }
    }
}
