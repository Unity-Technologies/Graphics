namespace UnityEngine.Rendering.HighDefinition
{
    internal unsafe struct ShadowIndicesAndVisibleLightData
    {
        public HDAdditionalLightDataUpdateInfo additionalLightUpdateInfo;
        public VisibleLight visibleLight;
        public int dataIndex;
        public int lightIndex;
        public HDShadowRequestSetHandle shadowRequestSetHandle;
        public HDLightType lightType;
        public int shadowRequestCount;
        public fixed int shadowRequestIndices[HDShadowRequest.maxLightShadowRequestsCount];
        public fixed int shadowResolutionRequestIndices[HDShadowRequest.maxLightShadowRequestsCount];
    }
}
