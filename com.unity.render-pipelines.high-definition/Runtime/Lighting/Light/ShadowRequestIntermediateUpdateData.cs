namespace UnityEngine.Rendering.HighDefinition
{
    internal struct ShadowRequestIntermediateUpdateData
    {
        public VisibleLight visibleLight;
        public HDShadowRequestHandle shadowRequestHandle;
        public int additionalLightDataIndex;
        public Vector2 viewportSize;

        public int lightIndex;

        public ShadowMapUpdateType updateType;
        public BitArray8 states;

        public const int k_HasCachedComponent = 0;
        public const int k_IsSampledFromCache = 1;
        public const int k_NeedsRenderingDueToTransformChange = 2;
        public const int k_ShadowHasAtlasPlacement = 3;
        public const int k_NeedToUpdateCachedContent = 4;
    }
}
