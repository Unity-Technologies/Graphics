using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace Unity.RenderPipelines.Core.Runtime.Shared
{
    /*  
     *  A Validation Layer to fail early when calling RenderGraphBuilder APIs that would cause specific resources to load from GPU memory.
     *  The Render Pipeline adds or removes resources that it wants validation for per frame.
     *  
     *  The Validation:
     *  Prevent Sampling as texture (CRITICAL): this layer will prevent any of these resources from being sampled as textures. This ensures that the resources can 
     *  safely be assigned the backbuffer TextureUVOrientation when this would be different from the regular texture orientation (always the case except on OpenGL/WebGL/GLES).
     *  This validation is critical to work around the y-flip issue. This validation provides the necessary guarantee and safety for higher level code.
     *  Also, sampling as texture requires a store to GPU memory. Preventing this already prevents a lot of scenarios that would break the native render pass.
     *  
     *  Prevent using in Unsafe or Compute pass: by design, only a Raster pass can be merged. This layer will prevent any on-tile resource to be used in any other pass. 
     *  This guards against sampling as texture, but also using it as render target. Because the pass can't be merged, this would trigger a store/load operation.
     *  Technically, we could allow an Unsafe pass if it is the first and last pass that uses the resource, so it only stores but never loads. However this is 
     *  not a realistic scenario so we simplify to not allowing Unsafe pass at all.
     *  
     *  Prevent Non-Raster (Unsafe, Compute) pass inbetween Raster passes: when an On-Tile resource is used in two raster passes, and there is an Unsafe pass in between, we know for sure 
     *  that a store (raster pass 1) and load (raster pass 2) will happen and the NRP will be broken. This is a common scenario because our BlitPass uses 
     *  Unsafe pass for example. It's fast and easy to check, so this layer will prevent it.      
     *  
     *  IMPORTANT NOTE this layer does not catch every scenario that would trigger store/load action, nor should it. Any validation we do here is a win for the user.
     *  As it provides an early exception in the Record phase such that the user is aware and also has full call stack to show what builder API call specifically would break the NRP.
     */
    internal sealed class OnTileValidationLayer : RenderGraphValidationLayer
    {
        // The sizes are chosen as a realistic lower bound for an On-Tile renderer.
        // All these will automatically expand if the size would not be adequate.
        const int k_InitialTextureHandleSize = 20;
        const int k_InitialGlobalTexturesAfterPass = 6;

        const int k_NotTracked = 0;
        const int k_TrackedNotYetUsed = -1;

        struct Pass {
            public RenderPassInfo info;
            // We don't store the inputAttachments because we don't need them for now.
            public TextureHandle[] renderAttachments;
            public NativeList<TextureHandle> globalTexturesAfterPass;
            public TextureHandle renderAttachmentDepth;
            public int renderCount;

            public void Init()
            {
                renderAttachments = new TextureHandle[RenderGraph.kMaxMRTCount];
                globalTexturesAfterPass = new NativeList<TextureHandle>(k_InitialGlobalTexturesAfterPass, AllocatorManager.Persistent);
                renderCount = 0;
            }

            public void Clear()
            {
                globalTexturesAfterPass.Clear();
                renderCount = 0;
                renderAttachmentDepth = TextureHandle.nullHandle;
            }
        }
        
        Pass m_CurrentPass;        
        RenderPassInfo m_LastNonRasterPassInfo;
        bool m_InputAttachmentsHaveOnTileResource;
        int m_NumberOfPasses;
        int m_LastNonRasterPassIndex;

        internal RenderGraph renderGraph { get; set; }      

        // Sparsely filled, indexed with the handle.index, for fast lookup.
        // 0  = not tracked, not on-tile
        // -1 = tracked, but not used in a raster pass yet
        // >0 = tracked, and used in a raster pass, with the the state being the number of the pass
        DynamicArray<int> m_HandleStates;

        const string m_ErrorMessageValidationIssue = "The On Tile Validation layer has detected an issue: ";
        const string m_ErrorMessageHowToResolve = "The On Tile Validation layer is activated with the setting 'On Tile Validation' on the URP Renderer. When activated, it is not allowed to sample (RenderGraph.UseTexture) the cameraColor or cameraDepth (intermediate) textures or the GBuffers or any copies of those." +
                "You need to disable any of the following that could cause the issue: a URP setting that would break the native render pass, a ScriptableRenderPass that is enqueued from script, or a ScriptableRenderFeature that is installed on your URP Renderer.\n";

        public OnTileValidationLayer()
        {
            // Number of TextureHandles in a frame
            m_HandleStates = new DynamicArray<int>(k_InitialTextureHandleSize);

            m_CurrentPass.Init();

            Clear();
        }

        //Assumes input is valid and tracked as on tile.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ValidateNoNonRasterPassInbetween(in TextureHandle input)
        {
            var lastRasterPass = GetLastRasterPass(in input);

            if (lastRasterPass != k_TrackedNotYetUsed && m_LastNonRasterPassIndex > lastRasterPass && m_LastNonRasterPassIndex < m_NumberOfPasses)
            {
                ThrowNoNonRasterPassInBetween(in input);
            }

            // Set the last pass to this one
            SetLastRasterPass(in input, m_NumberOfPasses);
        }

        void ThrowNoNonRasterPassInBetween(in TextureHandle input)
        {
            var resourceName = renderGraph.GetTextureName(in input);
            throw new InvalidOperationException($"{m_ErrorMessageValidationIssue} render pass '{m_CurrentPass.info.name}'" +
                $" results in a load action for resource '{resourceName}' due to a previous Unsafe or Compute render pass '{m_LastNonRasterPassInfo.name}'. " +
                $"These can't be merged.\n{m_ErrorMessageHowToResolve}");
        }
        
        void ThrowTextureSamplingException(in TextureHandle input, string methodName)
        {
            var resourceName = renderGraph.GetTextureName(in input);
            throw new InvalidOperationException($"{m_ErrorMessageValidationIssue} render pass '{m_CurrentPass.info.name}' calls '{methodName}' with resource '{resourceName}'.\n{m_ErrorMessageHowToResolve}");            
        }

        void ThrowNotRasterPassException(in TextureHandle input, string methodName)
        {
            var resourceName = renderGraph.GetTextureName(in input);
            throw new InvalidOperationException($"{m_ErrorMessageValidationIssue} render pass '{m_CurrentPass.info.name}' calls '{methodName}' with resource '{resourceName}'. " +
                $"Unsafe and Compute render passes can't be merged. Use a Raster render pass and ensure that no load/store action will be performed." +
                $"\n{m_ErrorMessageHowToResolve}");
        }

        const string k_UseTexture = "UseTexture";
        const string k_SetRenderAttachment = "SetRenderAttachment";
        const string k_SetRenderAttachmentDepth = "SetRenderAttachmentDepth";
        const string k_SetGlobalTextureAfterPass = "SetGlobalTextureAfterPass";

        override public void UseTexture(in TextureHandle input, AccessFlags flags)
        {
            if (!IsTrackedOnTile(in input))
                return;

            // For RasterPass, this is a check if the resource is sampled as texture.
            // For other types, we assume that the resource will either be sampled as texture, or set as render target, which will load and/or store it.
            if (m_CurrentPass.info.type == RenderGraphPassType.Raster)
            {
                ThrowTextureSamplingException(in input, k_UseTexture);
            }
            else
            {
                ThrowNotRasterPassException(in input, k_UseTexture);
            }
        }

        override public void SetGlobalTextureAfterPass(in TextureHandle input, int propertyId)
        {
            m_CurrentPass.globalTexturesAfterPass.Add(in input);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ValidateRenderAttachment(in TextureHandle tex, string methodName)
        {
            if (IsTrackedOnTile(in tex))
            {
                if (m_CurrentPass.info.type == RenderGraphPassType.Raster)
                {
                    ValidateNoNonRasterPassInbetween(in tex);
                }
                else
                {
                    ThrowNotRasterPassException(in tex, methodName);
                }
            }
        }

        override public void SetRenderAttachment(TextureHandle tex, int index, AccessFlags flags, int mipLevel, int depthSlice)
        {
            // Lower level validation will catch this, not the responsibility of this layer.
            if (m_CurrentPass.renderCount == RenderGraph.kMaxMRTCount)
                return;

            if (!tex.IsValid())
                return;

            ValidateRenderAttachment(in tex, k_SetRenderAttachment);

            m_CurrentPass.renderAttachments[m_CurrentPass.renderCount++] = tex;
        }

        override public void SetRenderAttachmentDepth(TextureHandle tex, AccessFlags flags, int mipLevel, int depthSlice)
        {
            if (!tex.IsValid())
                return;

            ValidateRenderAttachment(in tex, k_SetRenderAttachmentDepth);

            m_CurrentPass.renderAttachmentDepth = tex;
        }

        override public void SetInputAttachment(TextureHandle tex, int index, AccessFlags flags, int mipLevel, int depthSlice)
        {
            if (!tex.IsValid())
                return;

            if (!IsTrackedOnTile(in tex))
                return;
            
            m_InputAttachmentsHaveOnTileResource = true;

            ValidateNoNonRasterPassInbetween(in tex);            
        }

        override public void OnPassAddedBegin(in RenderPassInfo renderPassInfo)
        {
            m_CurrentPass.Clear();
            m_CurrentPass.info = renderPassInfo;

            m_InputAttachmentsHaveOnTileResource = false;
        }

        override public void OnPassAddedDispose()
        {
            var pass = m_CurrentPass; 
            
            if (pass.info.type == RenderGraphPassType.Raster)
            { 
                for (int i = 0; i < pass.renderCount; i++)
                {
                    var renderAttachment = pass.renderAttachments[i];

                    // Propagate the constraints from the inputs to the outputs
                    if (m_InputAttachmentsHaveOnTileResource)
                        AddFast(in renderAttachment);                    
                }

                if (pass.renderAttachmentDepth.IsValid())
                {
                    // Propagate the constraints from the inputs to the outputs
                    if (m_InputAttachmentsHaveOnTileResource)
                        AddFast(in pass.renderAttachmentDepth);
                }
            }
            else
            {
                m_LastNonRasterPassIndex = m_NumberOfPasses;
                m_LastNonRasterPassInfo = m_CurrentPass.info;
            }         

            // Check the global textures that are set after this pass. This needs to happen after the constraints are propagated
            for (int i=0; i< pass.globalTexturesAfterPass.Length; i++)
            {
                ref var globalTexture = ref pass.globalTexturesAfterPass.ElementAt(i);

                if (IsTrackedOnTile(in globalTexture))
                {
                    ThrowTextureSamplingException(in globalTexture, k_SetGlobalTextureAfterPass);
                }               
            }

            m_NumberOfPasses++;           
        }

        public void Add(in TextureHandle handle)
        {
            if (!handle.IsValid())
                return;

            AddFast(in handle);
        }

        // Precondition: handle.IsValid() has been checked by caller; membership check only.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AddFast(in TextureHandle handle)
        {
            if (IsTrackedOnTile(in handle))
                return; // already tracked; no-op

            EnsureHandleStateCapacity(in handle);

            m_HandleStates[handle.handle.index] = k_TrackedNotYetUsed;
        }

        public void Remove(in TextureHandle handle)
        {
            // IsTrackedOnTile also ensures we don't check out of bounds
            if (IsTrackedOnTile(in handle))
                m_HandleStates[handle.handle.index] = k_NotTracked;
        }

        public override void Clear()
        {
            // Clear all values to zero.
            m_HandleStates.ResizeAndClear(m_HandleStates.size);
            // We skip zero because an entry 0 in m_HandleStates means not tracked. 
            m_NumberOfPasses = 1;
            m_LastNonRasterPassIndex = -1;
            m_InputAttachmentsHaveOnTileResource = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EnsureHandleStateCapacity(in TextureHandle handle)
        {
            var handleIndex = handle.handle.index;

            if (handleIndex >= m_HandleStates.size)
            {
                m_HandleStates.Resize(Math.Max(m_HandleStates.size * 2, handleIndex+1), true);
            }
        }

        // 0  = not tracked, not on-tile
        // -1 = tracked, but not used in a raster pass yet
        // >0 = tracked, and used in a raster pass, with the state being the number of the pass
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetHandleState(in TextureHandle handle)
        {
            if (handle.handle.index >= m_HandleStates.size)
            {
                return k_NotTracked;
            }
            return m_HandleStates[handle.handle.index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsTrackedOnTile(in TextureHandle handle)
        {
            return GetHandleState(in handle) != k_NotTracked;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetLastRasterPass(in TextureHandle handle)
        {
            return m_HandleStates[handle.handle.index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SetLastRasterPass(in TextureHandle handle, int lastRasterPass)
        {
            m_HandleStates[handle.handle.index] = lastRasterPass;
        }

        override public void Dispose()
        {
            m_CurrentPass.globalTexturesAfterPass.Dispose();
        }
    }
}
