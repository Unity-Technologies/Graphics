namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Custom Post Processing injection points.
    /// </summary>
    public enum CustomPostProcessInjectionPoint
    {
        /// <summary>After Opaque and Sky.</summary>
        [InspectorName("After Opaque And Sky")]
        AfterOpaqueAndSky = 0,
        /// <summary>Before TAA and Post Processing.</summary>
        [InspectorName("Before Temporal Anti-Aliasing")]
        BeforeTAA = 3,
        /// <summary>Before Post Processing.</summary>
        [InspectorName("Before Post Process")]
        BeforePostProcess = 1,
        /// <summary>After Post Process Blurs.</summary>
        [InspectorName("After Post Process Blurs")]
        AfterPostProcessBlurs = 4,
        /// <summary>After Post Processing.</summary>
        [InspectorName("After Post Process")]
        AfterPostProcess = 2,
    }
}
