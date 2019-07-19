using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public interface IShadowCasterGroup2D
    {
        int GetShadowGroup();

        List<ShadowCaster2D> GetShadowCasters();

        void RegisterShadowCaster2D(ShadowCaster2D shadowCaster2D);
        void UnregisterShadowCaster2D(ShadowCaster2D shadowCaster2D);
    }
}
