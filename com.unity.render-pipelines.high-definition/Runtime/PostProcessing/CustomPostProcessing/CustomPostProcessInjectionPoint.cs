namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Custom Post Processing injection points.
    /// </summary>
    public enum CustomPostProcessInjectionPoint
    {
        /// <summary>After Opaque and Sky.</summary>
        AfterOpaqueAndSky,
        /// <summary>Before Post Processing.</summary>
        BeforePostProcess,
        /// <summary>After Post Processing.</summary>
        AfterPostProcess,
    }
}