using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Assertions;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Context struct for passing into GPUResidentDrawer.PostCullBeginCameraRendering
    /// </summary>
    public struct RenderRequestBatcherContext
    {
        /// <summary>
        /// CommandBuffer that will be used for resulting commands
        /// </summary>
        public CommandBuffer commandBuffer;

        /// <summary>
        /// Ambient probe to be set
        /// </summary>
        public SphericalHarmonicsL2 ambientProbe;
    }

    /// <summary>
    /// The type of occlusion test
    /// </summary>
    public enum OcclusionTest
    {
        /// <summary>No occlusion test, all instances are visible.</summary>
        None,
        /// <summary>Test all instances against the latest occluders.</summary>
        TestAll,
        /// <summary>Only test the culled objects from the previous pass.</summary>
        TestCulled,
    }

    /// <summary>Extension methods for OcclusionTest.</summary>
    public static class OcclusionTestMethods
    {
        /// <summary>
        /// Converts this occlusion test into a batch layer mask for rendering.
        /// This helper function is used to limit the second rendering pass when building
        /// occluders to only indirect draw calls, so that only false positives from
        /// the first rendering pass are rendered.
        /// </summary>
        /// <param name="occlusionTest">The occlusion test.</param>
        /// <returns>The batch layer mask that should be used to render the results of this occlusion test.</returns>
        public static uint GetBatchLayerMask(this OcclusionTest occlusionTest)
        {
            // limit to indirect batches only when rendering false positives, otherwise render everything
            return (occlusionTest == OcclusionTest.TestCulled) ? BatchLayer.InstanceCullingIndirectMask : uint.MaxValue;
        }
    }

    /// <summary>Parameter structure for passing to GPUResidentDrawer.InstanceOcclusionTest.</summary>
    public struct OcclusionCullingSettings
    {
        /// <summary>The instance ID of the camera, to identify the culling output and occluders to use.</summary>
        public int viewInstanceID;
        /// <summary>The occlusion test to use.</summary>
        public OcclusionTest occlusionTest;

        /// <summary>Creates a new structure using the given parameters.</summary>
        /// <param name="viewInstanceID">The instance ID of the camera to find culling output and occluders for.</param>
        /// <param name="occlusionTest">The occlusion test to use.</param>
        public OcclusionCullingSettings(int viewInstanceID, OcclusionTest occlusionTest)
        {
            this.viewInstanceID = viewInstanceID;
            this.occlusionTest = occlusionTest;
        }
    }

    /// <summary>Parameters structure for passing to GPUResidentDrawer.UpdateInstanceOccluders.</summary>
    public struct OccluderParameters
    {
        /// <summary>The instance ID of the camera, used to identify these occluders for the occlusion test.</summary>
        public int viewInstanceID;

        /// <summary>The transform from world space to view space when rendering the depth buffer.</summary>
        public Matrix4x4 viewMatrix;
        /// <summary>The transform from view space to world space when rendering the depth buffer.</summary>
        public Matrix4x4 invViewMatrix;
        /// <summary>The GPU projection matrix when rendering the depth buffer.</summary>
        public Matrix4x4 gpuProjMatrix;
        /// <summary>An additional world space offset to apply when moving between world space and view space.</summary>
        public Vector3 viewOffsetWorldSpace;

        /// <summary>The depth texture to read.</summary>
        public TextureHandle depthTexture;
        /// <summary>The offset in pixels to the start of the depth data to read.</summary>
        public Vector2Int depthOffset;
        /// <summary>The size in pixels of the area of the depth data to read.</summary>
        public Vector2Int depthSize;
        /// <summary>The number of slices, expected to be 0 or 1 for 2D and 2DArray textures respectively.</summary>
        public int depthSliceCount;

        /// <summary>Creates a new structure using the given parameters.</summary>
        /// <param name="viewInstanceID">The instance ID of the camera to associate with these occluders.</param>
        public OccluderParameters(int viewInstanceID)
        {
            this.viewInstanceID = viewInstanceID;
            this.viewMatrix = Matrix4x4.identity;
            this.invViewMatrix = Matrix4x4.identity;
            this.gpuProjMatrix = Matrix4x4.identity;
            this.viewOffsetWorldSpace = Vector3.zero;
            this.depthTexture = TextureHandle.nullHandle;
            this.depthOffset = Vector2Int.zero;
            this.depthSize = Vector2Int.zero;
            this.depthSliceCount = 0;
        }
    }
}
