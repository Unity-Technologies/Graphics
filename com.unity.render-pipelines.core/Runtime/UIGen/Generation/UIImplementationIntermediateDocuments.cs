using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnityEngine.Rendering.UIGen
{
    public class UIImplementationIntermediateDocuments
    {
        XmlElement m_PropertyUxml;
        BlockSyntax m_BindContextBody;
        BlockSyntax m_UnbindContextBody;

        public static bool From(
            [DisallowNull] XmlElement element,
            [NotNullWhen(true)] out UIImplementationIntermediateDocuments documents,
            [NotNullWhen(false)] out Exception error
        )
        {
            documents = new UIImplementationIntermediateDocuments()
            {
                m_BindContextBody = SyntaxFactory.Block(Enumerable.Empty<StatementSyntax>()),
                m_UnbindContextBody = SyntaxFactory.Block(Enumerable.Empty<StatementSyntax>()),
                m_PropertyUxml = element
            };
            error = null;
            return true;
        }

        [DisallowNull]
        public XmlElement propertyUxml
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
