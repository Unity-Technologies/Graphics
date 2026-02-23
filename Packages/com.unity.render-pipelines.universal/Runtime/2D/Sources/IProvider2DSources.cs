#if UNITY_EDITOR
using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    internal interface IProvider2DSources
    {
        List<SelectionSource> GetAdditionalSources();
        GUIContent[] GetSourceNames();
    }
}
#endif
