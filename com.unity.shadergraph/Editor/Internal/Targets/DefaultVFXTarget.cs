using System;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph.Internal
{
    class DefaultVFXTarget : ITargetImplementation
    {
        public Type targetType => typeof(VFXTarget);
        public string displayName => "Default";
        public string passTemplatePath => null;
        public string sharedTemplateDirectory => null;

        public bool IsValid(IMasterNode masterNode)
        {
            return masterNode is VfxMasterNode;
        }

        public void SetupTarget(ref TargetSetupContext context)
        {
        }
    }
}
