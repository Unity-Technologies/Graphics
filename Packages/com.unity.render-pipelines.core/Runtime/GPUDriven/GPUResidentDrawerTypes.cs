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
    public struct SubviewOcclusionTest
    {
        /// <summary>The split index to read from the CPU culling output.</summary>
        public int cullingSplitIndex;
        /// <summary>The occluder subview to occlusion test against.</summary>
        public int occluderSubviewIndex;
    }

    /// <summary>Parameter structure for passing to GPUResidentDrawer.InstanceOcclusionTest.</summary>
    public struct OcclusionCullingSettings
    {
        /// <summary>The instance ID of the camera, to identify the culling output and occluders to use.</summary>
        public int viewInstanceID;
        /// <summary>The occlusion test to use.</summary>
        public OcclusionTest occlusionTest;
        /// <summary>An instance multiplier to use for the generated indirect draw calls.</summary>
        public int instanceMultiplier;

        /// <summary>Creates a new structure using the given parameters.</summary>
        /// <param name="viewInstanceID">The instance ID of the camera to find culling output and occluders for.</param>
        /// <param name="occlusionTest">The occlusion test to use.</param>
        public OcclusionCullingSettings(int viewInstanceID, OcclusionTest occlusionTest)
        {
            this.viewInstanceID = viewInstanceID;
            this.occlusionTest = occlusionTest;
            this.instanceMultiplier = 1;
        }
    }

    /// <summary>Parameters structure for passing to GPUResidentDrawer.UpdateInstanceOccluders.</summary>
    public struct OccluderSubviewUpdate
    {
        /// <summary>
        /// The subview index within this camera or light, used to identify these occluders for the occlusion test.
        /// </summary>
        public int subviewIndex;

        /// <summary>The slice index of the depth data to read.</summary>
        public int depthSliceIndex;
        /// <summary>The offset in pixels to the start of the depth data to read.</summary>
        public Vector2Int depthOffset;

        /// <summary>The transform from world space to view space when rendering the depth buffer.</summary>
        public Matrix4x4 viewMatrix;
        /// <summary>The transform from view space to world space when rendering the depth buffer.</summary>
        public Matrix4x4 invViewMatrix;
        /// <summary>The GPU projection matrix when rendering the depth buffer.</summary>
        public Matrix4x4 gpuProjMatrix;
        /// <summary>An additional world space offset to apply when moving between world space and view space.</summary>
        public Vector3 viewOffsetWorldSpace;

        /// <summary>Creates a new structure using the given parameters.</summary>
        /// <param name="subviewIndex">The index of the subview within this occluder.</param>
        public OccluderSubviewUpdate(int subviewIndex)
        {
            this.subviewIndex = subviewIndex;

            this.depthSliceIndex = 0;
            this.depthOffset = Vector2Int.zero;

            this.viewMatrix = Matrix4x4.identity;
            this.invViewMatrix = Matrix4x4.identity;
            this.gpuProjMatrix = Matrix4x4.identity;
            this.viewOffsetWorldSpace = Vector3.zero;
        }
    }

    /// <summary>Parameters structure for passing to GPUResidentDrawer.UpdateInstanceOccluders.</summary>
    public struct OccluderParameters
    {
        /// <summary>The instance ID of the camera, used to identify these occluders for the occlusion test.</summary>
        public int viewInstanceID;
        /// <summary>The total number of subviews for this occluder.</summary>
        public int subviewCount;

        /// <summary>The depth texture to read.</summary>
        public TextureHandle depthTexture;
        /// <summary>The size in pixels of the area of the depth data to read.</summary>
        public Vector2Int depthSize;
        /// <summary>True if the depth texture is a texture array, false otherwise.</summary>
        public bool depthIsArray;

        /// <summary>Creates a new structure using the given parameters.</summary>
        /// <param name="viewInstanceID">The instance ID of the camera to associate with these occluders.</param>
        public OccluderParameters(int viewInstanceID)
        {
            this.viewInstanceID = viewInstanceID;
            this.subviewCount = 1;

            this.depthTexture = TextureHandle.nullHandle;
            this.depthSize = Vector2Int.zero;
            this.depthIsArray = false;
        }
    }
}
