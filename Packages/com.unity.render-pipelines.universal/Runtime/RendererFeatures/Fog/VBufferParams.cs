#if VOLUMETRIC_FOG

namespace UnityEngine.Rendering.Universal
{
    // Shared VBuffer parameters. Computed once per camera so VolumeVoxelizationPass and
    // VolumetricLightingPass agree on dimensions and log-depth encoding.
    internal struct VBufferParams
    {
        public int vbufferW;
        public int vbufferH;
        public int sliceCount;
        public Vector4 viewportSize;        // (w, h, 1/w, 1/h)
        public float voxelSize;
        public float unitDepthTexelSpacing; // tan(vFoV/2) * 2 / vbufferH
        public float halfVoxelArcLength;    // half angular size of one voxel at 1m
        public Vector4 encodingParams;      // logarithmic-depth encoding
        public Vector4 decodingParams;      // logarithmic-depth decoding
        public Matrix4x4 coordToViewDirWS;  // pixel coord → world-space view direction

        internal VBufferParams(Camera camera, RenderTextureDescriptor descriptor, float screenFraction, int sliceCount, float depthExtent, float sliceDistributionUniformity)
        {
            int screenWidth = descriptor.width;
            int screenHeight = descriptor.height;

            // At the default 12.5% this gives the same results as a fixed 8-pixel
            // voxel for screen sizes that are multiples of 8.
            vbufferW = Mathf.Max(1, Mathf.RoundToInt(screenWidth * screenFraction));
            vbufferH = Mathf.Max(1, Mathf.RoundToInt(screenHeight * screenFraction));
            voxelSize = 1.0f / screenFraction;
            this.sliceCount = sliceCount;

            float nearPlane = camera.nearClipPlane;
            float vFoV = camera.fieldOfView * Mathf.Deg2Rad;
            float tanHalfVertFoV = Mathf.Tan(0.5f * vFoV);
            viewportSize = new Vector4(vbufferW, vbufferH, 1.0f / vbufferW, 1.0f / vbufferH);
            unitDepthTexelSpacing = tanHalfVertFoV * (2.0f / vbufferH);
            halfVoxelArcLength = vFoV * 0.5f / vbufferH;

            // Logarithmic depth encoding/decoding parameters
            {
                float farPlane = nearPlane + depthExtent;
                float c = Mathf.Max(2.0f - 2.0f * sliceDistributionUniformity, 0.001f);

                encodingParams.y = 1.0f / Mathf.Log(c * (farPlane - nearPlane) + 1, 2);
                encodingParams.x = Mathf.Log(c, 2) * encodingParams.y;
                encodingParams.z = nearPlane - 1.0f / c;
                encodingParams.w = 0.0f;

                decodingParams.x = 1.0f / c;
                decodingParams.y = Mathf.Log(c * (farPlane - nearPlane) + 1, 2);
                decodingParams.z = nearPlane - 1.0f / c;
                decodingParams.w = 0.0f;
            }

            // Pixel-coord to world-space view-direction matrix
            {
                float aspectRatio = viewportSize.x * viewportSize.w;
                float m00 = -2.0f * viewportSize.z * tanHalfVertFoV * aspectRatio;
                float m11 = -2.0f * viewportSize.w * tanHalfVertFoV;
                float m20 = tanHalfVertFoV * aspectRatio;
                float m21 = tanHalfVertFoV;

                var viewSpaceRasterTransform = new Matrix4x4(
                    new Vector4(m00, 0.0f, 0.0f, 0.0f),
                    new Vector4(0.0f, m11, 0.0f, 0.0f),
                    new Vector4(m20, m21, -1.0f, 0.0f),
                    new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

                Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
                viewMatrix.SetColumn(3, new Vector4(0, 0, 0, 1));
                viewMatrix.SetRow(2, -viewMatrix.GetRow(2));
                coordToViewDirWS = Matrix4x4.Transpose(viewMatrix.transpose * viewSpaceRasterTransform);
            }
        }
    }
}

#endif
