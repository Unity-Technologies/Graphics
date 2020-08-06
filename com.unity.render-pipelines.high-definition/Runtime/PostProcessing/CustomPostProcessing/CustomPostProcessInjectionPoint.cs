namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Custom Post Processing injection points.
    /// </summary>
    public enum CustomPostProcessInjectionPoint
    {
        /// <summary>After Opaque and Sky.</summary>
        AfterOpaqueAndSky   = 0,
        /// <summary>Before TAA and Post Processing.</summary>
        BeforeTAA           = 3,
        /// <summary>Before Post Processing.</summary>
        BeforePostProcess   = 1,
        /// <summary>After Post Processing.</summary>
        AfterPostProcess    = 2,
    }
}