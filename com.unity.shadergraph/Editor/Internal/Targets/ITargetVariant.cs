using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Internal
{
    interface ITargetImplementation
    {
        Type targetType { get; } 
        string displayName { get; }
        string passTemplatePath { get; }
        string sharedTemplateDirectory { get; }

        bool IsValid(IMasterNode masterNode);
        void SetupTarget(ref TargetSetupContext context);
    }
}
