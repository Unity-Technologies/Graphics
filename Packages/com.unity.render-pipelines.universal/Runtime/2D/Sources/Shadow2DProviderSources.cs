#if UNITY_EDITOR
using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal class Shadow2DProviderSources : Provider2DSources<ShadowCaster2DProvider, Shadow2DProviderSource>
    {

        void SetSelectedHashCode(Provider2D provider, Component component)
        {
            
        }
    }
}
#endif
