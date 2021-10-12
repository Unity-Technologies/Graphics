using System;
using Microsoft.CodeAnalysis;

namespace UnityEngine.Rendering
{
    [Generator]
    public class MainSourceGenerator : ISourceGenerator
    {
        SupportedOnGenerator m_SupportedOn = new SupportedOnGenerator();

        public void Initialize(GeneratorInitializationContext context)
        {
            m_SupportedOn.Initialize(context);
        }

        public void Execute(GeneratorExecutionContext context)
        {
            m_SupportedOn.Execute(context);
        }
    }
}
