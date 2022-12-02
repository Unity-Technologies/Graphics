using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL]
    // Define if we use SSGI, RTGI, Mixed or none
    enum IndirectDiffuseMode
    {
        Off,
        ScreenSpace,
        RayTraced,
        Mixed
    }
}
