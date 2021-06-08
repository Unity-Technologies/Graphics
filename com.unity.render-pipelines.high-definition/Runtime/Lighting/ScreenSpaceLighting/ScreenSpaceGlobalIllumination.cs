using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL]
    // Define if we use SSGI, RTGI or none
    enum IndirectDiffuseMode
    {
        Off,
        ScreenSpace,
        Raytrace
    }
}
