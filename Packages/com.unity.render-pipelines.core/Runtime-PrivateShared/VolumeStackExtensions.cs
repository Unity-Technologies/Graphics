using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.RenderPipelines.Core.Runtime.Shared
{
    internal static class VolumeStackExtensions
    {
        public static GameObject GetSceneObjectReference<T>(this VolumeStack stack)
            where T : VolumeComponent
            => stack.GetSceneObjectReference<T>();
    }
}
