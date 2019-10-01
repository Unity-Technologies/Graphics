using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph.Internal
{
    class DecalTarget : ITarget
    {
        public string displayName => "Decal";
        public string passTemplatePath => string.Empty;
        public string sharedTemplateDirectory => string.Empty;

        public bool IsValid(IMasterNode masterNode)
        {
            return false;
        }

        public void SetupTarget(ref TargetSetupContext context) {}
    }
}
