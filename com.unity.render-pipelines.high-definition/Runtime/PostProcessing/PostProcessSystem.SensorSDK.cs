using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class PostProcessSystem
    {
        public void GenerateColorGrading(CommandBuffer cmd, ComputeShader cs, int kernel, HDCamera camera)
        {
            DoColorGrading(cmd, cs, kernel, camera);
        }
    }
}
