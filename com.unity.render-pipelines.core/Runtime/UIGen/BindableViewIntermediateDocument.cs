using System;
using System.Xml;
using Microsoft.CodeAnalysis.CSharp;

namespace UnityEngine.Rendering.UIGen
{
    public class BindableViewIntermediateDocument
    {
        XmlElement m_Uxml;
        CSharpSyntaxNode m_Code;
    }
}
