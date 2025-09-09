using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.PathTracing.Core
{
    internal enum UVChannel : uint { UV0 = 0, UV1 = 1 };

    internal class MaterialPool : IDisposable
    {
        [Flags]
        private enum MaterialFlags : uint
        {
            None = 0,
            IsTransmissive = 1 << 0,
            DoubleSidedGI = 1 << 1,
            PointSampleTransmission = 1 << 2,
        }

        private struct PTMaterial
        {
            public int AlbedoTextureIndex;
            public int EmissionTextureIndex;
            public int TransmissionTextureIndex;
            public MaterialFlags Flags;
            public float2 AlbedoScale;
            public float2 AlbedoOffset;
            public float2 EmissionScale;
            public float2 EmissionOffset;
            public float2 TransmissionScale;
            public float2 TransmissionOffset;
            public float3 EmissionColor;
            public UVChannel AlbedoAndEmissionUVChannel;
        };

        private enum TextureType { Albedo, Emission, Transmission, LightCookie, LightCubemap };

        private class MaterialEntry
        {
            public MaterialEntry(BlockAllocator.Allocation indexInBufferAlloc)
            {
                IndexInBuffer = indexInBufferAlloc;
            }

            public readonly BlockAllocator.Allocation IndexInBuffer;
            public TextureSlotAllocator.TextureLocation AlbedoTextureLocation = TextureSlotAllocator.TextureLocation.Invalid;
            public TextureSlotAllocator.TextureLocation EmissionTextureLocation = TextureSlotAllocator.TextureLocation.Invalid;
            public TextureSlotAllocator.TextureLocation TransmissionTextureLocation = TextureSlotAllocator.TextureLocation.Invalid;
            public float3 EmissionColor = new(0, 0, 0);
            public UVChannel AlbedoAndEmissionUVChannel = 0;
            public bool DoubleSidedGI;
            public bool IsTransmissive;
            public bool PointSampleTransmission;
        };

        private readonly Dictionary<UInt64, MaterialEntry> _materials = new();
        private readonly Dictionary<ulong, BlockAllocator.Allocation> _lightCookies = new();  // this dictionary is used to keep track of which textures are inserted in the atlas and avoid duplication (important for LiveGI)
        private readonly Dictionary<int, ulong> _cookieHandleToID = new();                    // this dictionary is used when deleting lights with cookies attached, so we can free-up the atlas slots

        public int MaterialCount => _materials.Count;
        private BlockAllocator _materialSlotAllocator;
        public ComputeBuffer MaterialBuffer;
        private NativeArray<PTMaterial> _materialList;
        private bool _materialArrayDirty = true;

        private readonly TextureSlotAllocator _albedoTextureAllocator;
        private readonly TextureSlotAllocator _emissionTextureAllocator;
        private readonly TextureSlotAllocator _transmissionTextureAllocator;
        public RenderTexture AlbedoTextures => _albedoTextureAllocator.Texture;
        public RenderTexture EmissionTextures => _emissionTextureAllocator.Texture;
        public RenderTexture TransmissionTextures => _transmissionTextureAllocator.Texture;

        private static Mesh _planeMesh;
        public RenderTexture LightCookieTextures;
        private BlockAllocator _lightCookieTexturesSlotAllocator;

        public RenderTexture LightCubemapTextures;
        private BlockAllocator _lightCubemapTexturesSlotAllocator;

        private readonly ComputeShader _setAlphaChannelShader;
        private readonly int _setAlphaChannelKernel;
        private readonly uint3 _alphaShaderThreadGroupSizes;

        private readonly ComputeShader _blitCubemapShader;
        private readonly int _blitCubemapKernel;

        private readonly ComputeShader _blitGrayscaleCookieShader;
        private readonly int _blitGrayscaleCookieKernel;

        private const int AtlasSize = 256;

        public MaterialPool(ComputeShader setAlphaChannelShader, ComputeShader blitCubemapShader, ComputeShader blitGrayscaleCookieShader)
        {
            _setAlphaChannelShader = setAlphaChannelShader;
            _setAlphaChannelKernel = _setAlphaChannelShader.FindKernel("SetAlphaChannel");
            _setAlphaChannelShader.GetKernelThreadGroupSizes(_setAlphaChannelKernel,
                out _alphaShaderThreadGroupSizes.x, out _alphaShaderThreadGroupSizes.y, out _alphaShaderThreadGroupSizes.z);

            _blitCubemapShader = blitCubemapShader;
            _blitCubemapKernel  = _blitCubemapShader.FindKernel("BlitCubemap");

            _blitGrayscaleCookieShader = blitGrayscaleCookieShader;
            _blitGrayscaleCookieKernel = _blitGrayscaleCookieShader.FindKernel("BlitGrayScaleCookie");

            const int initialMaxLightCookieTextureCount = 30;
            const int initialMaxLightCubemapTextureCount = 3;
            const int initialMaxMaterialCount = 100;

            _albedoTextureAllocator = new TextureSlotAllocator(AtlasSize, GetTextureFormat(TextureType.Albedo), FilterMode.Bilinear);
            _emissionTextureAllocator = new TextureSlotAllocator(AtlasSize, GetTextureFormat(TextureType.Emission), FilterMode.Bilinear);
            _transmissionTextureAllocator = new TextureSlotAllocator(AtlasSize, GetTextureFormat(TextureType.Transmission), FilterMode.Bilinear);

            _materialSlotAllocator.Initialize(initialMaxMaterialCount);
            MaterialBuffer = new ComputeBuffer(initialMaxMaterialCount, Marshal.SizeOf<PTMaterial>());
            _materialList = new NativeArray<PTMaterial>(initialMaxMaterialCount, Allocator.Persistent);

            LightCookieTextures = CreateTextureArray(initialMaxLightCookieTextureCount, TextureType.LightCookie);
            _lightCookieTexturesSlotAllocator.Initialize(initialMaxLightCookieTextureCount);

            LightCubemapTextures = CreateTextureArray(initialMaxLightCubemapTextureCount, TextureType.LightCubemap);
            _lightCubemapTexturesSlotAllocator.Initialize(initialMaxLightCubemapTextureCount);
        }

        public void Dispose()
        {
            _materialSlotAllocator.Dispose();
            MaterialBuffer?.Dispose();
            if (_materialList.IsCreated)
                _materialList.Dispose();

            _albedoTextureAllocator.Dispose();
            _emissionTextureAllocator.Dispose();
            _transmissionTextureAllocator.Dispose();

            _lightCookieTexturesSlotAllocator.Dispose();
            if (LightCookieTextures != null)
            {
                LightCookieTextures.Release();
                CoreUtils.Destroy(LightCookieTextures);
            }

            _lightCubemapTexturesSlotAllocator.Dispose();
            if (LightCubemapTextures != null)
            {
                LightCubemapTextures.Release();
                CoreUtils.Destroy(LightCubemapTextures);
            }
        }

        public void AddMaterial(UInt64 materialHandle, in World.MaterialDescriptor material, UVChannel albedoAndEmissionUVChannel)
        {
            MaterialEntry materialEntry;
            Debug.Assert(!_materials.ContainsKey(materialHandle), "Material was already added to the pool.");

            var slotAllocation = _materialSlotAllocator.Allocate(1);
            if (!slotAllocation.valid)
            {
                _materialSlotAllocator.Grow(_materialSlotAllocator.capacity+1);
                RecreateMaterialList();
                slotAllocation = _materialSlotAllocator.Allocate(1);
                Assert.IsTrue(slotAllocation.valid);
            }

            materialEntry = new MaterialEntry(slotAllocation);
            UpdateMaterial(in material, albedoAndEmissionUVChannel, materialEntry);

            _materials.Add(materialHandle, materialEntry);

            UpdateMaterialList(materialEntry);
        }

        public void UpdateMaterial(UInt64 materialHandle, in World.MaterialDescriptor material, UVChannel albedoAndEmissionUVChannel)
        {
            MaterialEntry materialEntry = _materials[materialHandle];
            UpdateMaterial(in material, albedoAndEmissionUVChannel, materialEntry);
        }

        public void RemoveMaterial(UInt64 materialInstanceID)
        {
            if (!_materials.TryGetValue(materialInstanceID, out var materialEntry))
            {
                Debug.Assert(false, "Material was not added to the pool.");
                return;
            }

            _materialSlotAllocator.FreeAllocation(in materialEntry.IndexInBuffer);
            RemoveTextureIfPresent(TextureType.Albedo, ref materialEntry.AlbedoTextureLocation);
            RemoveTextureIfPresent(TextureType.Emission, ref materialEntry.EmissionTextureLocation);
            RemoveTextureIfPresent(TextureType.Transmission, ref materialEntry.TransmissionTextureLocation);

            _materials.Remove(materialInstanceID);
        }

        public void GetMaterialInfo(UInt64 materialHandle, out uint materialIndex, out bool isTransmissive)
        {
            MaterialEntry entry;
            if (_materials.TryGetValue(materialHandle, out entry))
            {
                materialIndex = (uint)entry.IndexInBuffer.block.offset;
                isTransmissive = entry.IsTransmissive;
            }
            else
                throw new InvalidOperationException("Material not in pool");
        }

        public bool IsEmissive(UInt64 materialHandle, out float3 emissionColor)
        {
            float3 zero = new(0, 0, 0);
            emissionColor = zero;

            if (_materials.TryGetValue(materialHandle, out var entry))
            {
                if (entry.EmissionTextureLocation.IsValid || math.any(entry.EmissionColor != zero))
                {
                    emissionColor = entry.EmissionColor;
                    return true;
                }
                return false;
            }
            else
                return false;
        }

        public void Build(CommandBuffer cmd)
        {
            if (cmd is null)
                throw new InvalidOperationException("Invalid CommandBuffer, did you forget to call Initialize()?");
            if (_materialArrayDirty)
            {
                cmd.SetBufferData(MaterialBuffer, _materialList);
                _materialArrayDirty = false;
            }
        }

        private void UpdateMaterial(in World.MaterialDescriptor material, UVChannel albedoAndEmissionUVChannel, MaterialEntry materialEntry)
        {
            AddOrUpdateTexture(in material, TextureType.Albedo, ref materialEntry.AlbedoTextureLocation);

            if (material.TransmissionChannels == TransmissionChannels.Alpha)
                FillAlbedoTextureAlphaWithOpacity(in material, materialEntry.AlbedoTextureLocation);

            materialEntry.AlbedoAndEmissionUVChannel = albedoAndEmissionUVChannel;
            materialEntry.DoubleSidedGI = material.DoubleSidedGI;
            materialEntry.IsTransmissive = material.TransmissionChannels != TransmissionChannels.None;
            materialEntry.PointSampleTransmission = material.PointSampleTransmission;

            var emissionType = material.EmissionType;
            if (emissionType == MaterialPropertyType.Texture)
            {
                AddOrUpdateTexture(in material, TextureType.Emission, ref materialEntry.EmissionTextureLocation);
                materialEntry.EmissionColor = 0;
            }
            else if (emissionType == MaterialPropertyType.Color)
            {
                RemoveTextureIfPresent(TextureType.Emission, ref materialEntry.EmissionTextureLocation);
                materialEntry.EmissionColor = material.EmissionColor;
            }
            else if (emissionType == MaterialPropertyType.None)
            {
                RemoveTextureIfPresent(TextureType.Emission, ref materialEntry.EmissionTextureLocation);
                materialEntry.EmissionColor = 0;
            }

            if (material.TransmissionChannels == TransmissionChannels.RGB && material.Transmission != null)
            {
                AddOrUpdateTexture(in material, TextureType.Transmission, ref materialEntry.TransmissionTextureLocation);
            }
            else
            {
                RemoveTextureIfPresent(TextureType.Transmission, ref materialEntry.TransmissionTextureLocation);
            }

            UpdateMaterialList(materialEntry);
        }

        private void UpdateMaterialList(MaterialEntry entry)
        {
            // Default values, filled in below
            var gpuMat = new PTMaterial()
            {
                // Default values, filled in below
                AlbedoTextureIndex = -1,
                EmissionTextureIndex = -1,
                TransmissionTextureIndex = -1,
                AlbedoScale = Vector2.one,
                AlbedoOffset = Vector2.zero,
                EmissionScale = Vector2.one,
                EmissionOffset = Vector2.zero,
                TransmissionScale = Vector2.one,
                TransmissionOffset = Vector2.zero,

                // Values to be filled in immediately
                EmissionColor = entry.EmissionColor,
                AlbedoAndEmissionUVChannel = entry.AlbedoAndEmissionUVChannel,
                Flags = (entry.IsTransmissive ? MaterialFlags.IsTransmissive : MaterialFlags.None) |
                        (entry.DoubleSidedGI ? MaterialFlags.DoubleSidedGI : MaterialFlags.None) |
                        (entry.PointSampleTransmission ? MaterialFlags.PointSampleTransmission : MaterialFlags.None),
            };

            if (entry.AlbedoTextureLocation.IsValid)
            {
                gpuMat.AlbedoTextureIndex = entry.AlbedoTextureLocation.AtlasIndex;
                _albedoTextureAllocator.GetScaleAndOffset(in entry.AlbedoTextureLocation, out var albedoScale, out var albedoOffset);
                gpuMat.AlbedoScale = albedoScale;
                gpuMat.AlbedoOffset = albedoOffset;
            }
            if (entry.EmissionTextureLocation.IsValid)
            {
                gpuMat.EmissionTextureIndex = entry.EmissionTextureLocation.AtlasIndex;
                _emissionTextureAllocator.GetScaleAndOffset(in entry.EmissionTextureLocation, out var emissionScale, out var emissionOffset);
                gpuMat.EmissionScale = emissionScale;
                gpuMat.EmissionOffset = emissionOffset;
            }
            if (entry.TransmissionTextureLocation.IsValid)
            {
                gpuMat.TransmissionTextureIndex = entry.TransmissionTextureLocation.AtlasIndex;
                _transmissionTextureAllocator.GetScaleAndOffset(in entry.TransmissionTextureLocation, out var transmissionScale, out var transmissionOffset);
                gpuMat.TransmissionScale = transmissionScale;
                gpuMat.TransmissionOffset = transmissionOffset;
            }

            _materialList[entry.IndexInBuffer.block.offset] = gpuMat;
            _materialArrayDirty = true;
        }

        #region Cookie support
        private static int GetCookieFaces(Texture tex)
        {
            return (tex.dimension == TextureDimension.Cube) ? 6 : 1;
        }

        private static int GetCookieHandle(int slices, int handle)
        {
            int offset = (slices > 1) ? 1 : 0;
            // use the first bit to indicate the array type
            return 2 * handle + offset;
        }

        public int AddCookieTexture(Texture cookie)
        {
            if (cookie != null)
            {
                var cookieID = Util.EntityIDToUlong(cookie.GetEntityId());
                if (_lightCookies.TryGetValue(cookieID, out var allocator))
                {
                    // This cookie was already copied in the atlas in a previous frame
                    // Baking will never hit this path, as the lights are not updated during a bake, but it's important for LiveGI
                    return allocator.handle;
                }

                int slices = GetCookieFaces(cookie);

                BlockAllocator.Allocation slotAllocation;
                if (slices > 1)
                {
                    slotAllocation = _lightCubemapTexturesSlotAllocator.Allocate(1);
                    ExpandCookieTextureArray(true, ref _lightCubemapTexturesSlotAllocator, ref LightCubemapTextures, ref slotAllocation);

                    RenderTexture prevRT = RenderTexture.active;
                    BlitCubemapCookie(cookie, LightCubemapTextures, slotAllocation.block.offset);
                    RenderTexture.active = prevRT;
                }
                else
                {
                    slotAllocation = _lightCookieTexturesSlotAllocator.Allocate(1);
                    ExpandCookieTextureArray(false, ref _lightCookieTexturesSlotAllocator, ref LightCookieTextures, ref slotAllocation);

                    RenderTexture prevRT = RenderTexture.active;
                    Blit2DCookie(cookie, LightCookieTextures, slotAllocation.block.offset);
                    RenderTexture.active = prevRT;
                }

                int handle = GetCookieHandle(slices, slotAllocation.handle);
                _lightCookies.Add(cookieID, slotAllocation);
                _cookieHandleToID.Add(handle, cookieID);
                return slotAllocation.handle;
            }

            return -1;
        }

        public void RemoveCookieTexture(int cookieFaces, int cookieIndex)
        {
            if (cookieIndex == -1)
                return;

            int cookieHandle = GetCookieHandle(cookieFaces, cookieIndex);
            if (!_cookieHandleToID.TryGetValue(cookieHandle, out var cookieID))
            {
                if (!_lightCookies.TryGetValue(cookieID, out var cookieAllocation))
                {
                    Debug.Assert(false, "Light cookie was not found in the pool.");
                    return;
                }

                if (cookieFaces > 1)
                    _lightCubemapTexturesSlotAllocator.FreeAllocation(in cookieAllocation);
                else
                    _lightCookieTexturesSlotAllocator.FreeAllocation(in cookieAllocation);

                _lightCookies.Remove(cookieID);
                _cookieHandleToID.Remove(cookieHandle);
            }
        }

        private void ExpandCookieTextureArray(bool isCubemapCookie, ref BlockAllocator allocator, ref RenderTexture texture, ref BlockAllocator.Allocation textureAlloc)
        {
            if (!textureAlloc.valid)
            {
                textureAlloc = allocator.Allocate(1);
                if (!textureAlloc.valid)
                {
                    var oldCapacity = allocator.capacity;
                    allocator.Grow(allocator.capacity + 1);

                    var newTexture = CreateTextureArray(allocator.capacity, isCubemapCookie ? TextureType.LightCubemap : TextureType.LightCookie);

                    using var cmd = new CommandBuffer();
                    for (int i = 0; i < oldCapacity; ++i)
                        cmd.CopyTexture(texture, i, newTexture, i);
                    Graphics.ExecuteCommandBuffer(cmd);

                    texture.Release();
                    texture = newTexture;

                    textureAlloc = allocator.Allocate(1);
                    Assert.IsTrue(textureAlloc.valid);
                }
            }
        }

        // Blits all faces of a cubemap to a slice of a cubemap array, performing any format conversions that are necessary
        private void BlitCubemapCookie(Texture source, RenderTexture dest, int destIndex)
        {
            using var cmd = new CommandBuffer();

            RenderTexture tempRT = new RenderTexture(AtlasSize, AtlasSize, 0, dest.format, 0);
            tempRT.enableRandomWrite = true;
            tempRT.Create();

            if (GraphicsFormatUtility.IsAlphaOnlyFormat(source.graphicsFormat))
                _blitCubemapShader.EnableKeyword("GRAYSCALE_BLIT");
            else
                _blitCubemapShader.DisableKeyword("GRAYSCALE_BLIT");

            for (int i = 0; i < 6; ++i)
            {
                cmd.SetComputeIntParam(_blitCubemapShader, Shader.PropertyToID("g_TextureSize"), dest.width);
                cmd.SetComputeIntParam(_blitCubemapShader, Shader.PropertyToID("g_Face"), i);
                cmd.SetComputeTextureParam(_blitCubemapShader, _blitCubemapKernel, Shader.PropertyToID("g_Source"), source);
                cmd.SetComputeTextureParam(_blitCubemapShader, _blitCubemapKernel, Shader.PropertyToID("g_Destination"), tempRT);
                int dispatchSize = GraphicsHelpers.DivUp(dest.width, 8);
                cmd.DispatchCompute(_blitCubemapShader, _blitCubemapKernel, dispatchSize, dispatchSize, 1);
                cmd.CopyTexture(tempRT, 0, dest, 6 * destIndex + i);
            }

            Graphics.ExecuteCommandBuffer(cmd);
            tempRT.Release();
            CoreUtils.Destroy(tempRT);
        }

        private void Blit2DCookie(Texture source, RenderTexture dest, int destIndex)
        {
            if (!GraphicsFormatUtility.IsAlphaOnlyFormat(source.graphicsFormat))
            {
                Graphics.Blit(source, dest, 0, destIndex);
            }
            else
            {
                // Handle grayscale 2d cookie to RGB conversion
                RenderTexture tempRT = new RenderTexture(AtlasSize, AtlasSize, 0, dest.format, 0);
                tempRT.enableRandomWrite = true;
                tempRT.Create();

                using var cmd = new CommandBuffer();
                cmd.SetComputeIntParam(_blitGrayscaleCookieShader, Shader.PropertyToID("g_TextureWidth"), dest.width);
                cmd.SetComputeIntParam(_blitGrayscaleCookieShader, Shader.PropertyToID("g_TextureHeight"), dest.height);
                cmd.SetComputeTextureParam(_blitGrayscaleCookieShader, _blitGrayscaleCookieKernel, Shader.PropertyToID("g_Source"), source);
                cmd.SetComputeTextureParam(_blitGrayscaleCookieShader, _blitGrayscaleCookieKernel, Shader.PropertyToID("g_Destination"), tempRT);

                cmd.DispatchCompute(_blitGrayscaleCookieShader, _blitGrayscaleCookieKernel, GraphicsHelpers.DivUp(dest.width, 8), GraphicsHelpers.DivUp(dest.height, 8), 1);
                cmd.CopyTexture(tempRT, 0, dest, destIndex);

                Graphics.ExecuteCommandBuffer(cmd);
                tempRT.Release();
                CoreUtils.Destroy(tempRT);
            }
        }
        #endregion

        private void AddOrUpdateTexture(in World.MaterialDescriptor material, TextureType textureType, ref TextureSlotAllocator.TextureLocation location)
        {
            // Find appropriate allocator, scale, offset and texture
            var allocator = _transmissionTextureAllocator;
            Vector2 scale = material.TransmissionScale;
            Vector2 offset = material.TransmissionOffset;
            offset.y = -offset.y; // TODO: During extraction this value is negated. This should be fixed. Tracked here: https://jira.unity3d.com/browse/GFXFEAT-802
            Texture texture = material.Transmission;
            if (textureType == TextureType.Albedo)
            {
                allocator = _albedoTextureAllocator;
                texture = material.Albedo;
                scale = material.AlbedoScale;
                offset = material.AlbedoOffset;
            }
            else if (textureType == TextureType.Emission)
            {
                allocator = _emissionTextureAllocator;
                texture = material.Emission;
                scale = material.EmissionScale;
                offset = material.EmissionOffset;
            }

            // Invalid texture - bail
            if (texture == null)
                return;

            // Update path
            if (location.IsValid)
            {
                allocator.UpdateTexture(in location, texture, scale, offset);
            }
            // Add path
            else
            {
                location = allocator.AddTexture(texture, scale, offset);
            }
        }

        private void RemoveTextureIfPresent(TextureType textureType, ref TextureSlotAllocator.TextureLocation location)
        {
            if (!location.IsValid)
                return;

            if (textureType == TextureType.Albedo)
                _albedoTextureAllocator.RemoveTexture(in location);
            else if (textureType == TextureType.Emission)
                _emissionTextureAllocator.RemoveTexture(in location);
            else
                _transmissionTextureAllocator.RemoveTexture(in location);

            location = TextureSlotAllocator.TextureLocation.Invalid;
        }

        private void FillAlbedoTextureAlphaWithOpacity(in World.MaterialDescriptor material, TextureSlotAllocator.TextureLocation location)
        {
            if (material.Transmission == null)
                return;

            Vector2Int targetSize = _albedoTextureAllocator.GetTextureSize(location);
            int targetOffsetX = location.TextureNode.PosX;
            int targetOffsetY = location.TextureNode.PosY;

            using var cmd = new CommandBuffer();
            cmd.SetComputeIntParam(_setAlphaChannelShader, Shader.PropertyToID("g_TextureWidth"), targetSize.x);
            cmd.SetComputeIntParam(_setAlphaChannelShader, Shader.PropertyToID("g_TextureHeight"), targetSize.y);
            cmd.SetComputeIntParam(_setAlphaChannelShader, Shader.PropertyToID("g_TargetSlice"), location.AtlasIndex);
            cmd.SetComputeIntParam(_setAlphaChannelShader, Shader.PropertyToID("g_TargetOffsetX"), targetOffsetX);
            cmd.SetComputeIntParam(_setAlphaChannelShader, Shader.PropertyToID("g_TargetOffsetY"), targetOffsetY);
            cmd.SetComputeFloatParam(_setAlphaChannelShader, Shader.PropertyToID("g_Alpha"), material.Alpha);
            cmd.SetComputeFloatParam(_setAlphaChannelShader, Shader.PropertyToID("g_AlphaCutoff"), material.AlphaCutoff);
            cmd.SetComputeIntParam(_setAlphaChannelShader, Shader.PropertyToID("g_UseAlphaCutoff"), material.UseAlphaCutoff ? 1 : 0);
            cmd.SetComputeTextureParam(_setAlphaChannelShader, _setAlphaChannelKernel, Shader.PropertyToID("g_AlbedoTextures"), AlbedoTextures);
            cmd.SetComputeTextureParam(_setAlphaChannelShader, _setAlphaChannelKernel, Shader.PropertyToID("g_OpacityTexture"), material.Transmission);
            cmd.SetComputeVectorParam(_setAlphaChannelShader, Shader.PropertyToID("g_OpacityTextureUVTransform"), new float4(material.TransmissionScale, material.TransmissionOffset));

            cmd.DispatchCompute(_setAlphaChannelShader, _setAlphaChannelKernel, GraphicsHelpers.DivUp(targetSize.x, _alphaShaderThreadGroupSizes.x), GraphicsHelpers.DivUp(targetSize.y, _alphaShaderThreadGroupSizes.y), 1);
            Graphics.ExecuteCommandBuffer(cmd);
        }

        // Render out an albedo/emission texture using the meta pass
        private static Texture RenderGITexture(Material material, TextureType textureType)
        {
            if (textureType == TextureType.Transmission)
            {
                Debug.Assert(false, "Transmission textures should be handled outside of this function.");
                return null;
            }

            var targetTexture = CreateTexture(textureType);

            int metaPassIndex = material.FindPass("Meta");

            var fragmentControl = Vector4.zero;
            fragmentControl.x = textureType == TextureType.Albedo ? 1 : 0;
            fragmentControl.y = textureType == TextureType.Emission ? 1 : 0;

            var properties = new MaterialPropertyBlock();
            properties.SetVector(Shader.PropertyToID("unity_MetaVertexControl"), new Vector4(0, 0, 0, 0));
            properties.SetVector(Shader.PropertyToID("unity_MetaFragmentControl"), fragmentControl);
            properties.SetFloat(Shader.PropertyToID("unity_OneOverOutputBoost"), 1.0f);
            properties.SetFloat(Shader.PropertyToID("unity_MaxOutputValue"), 0.97f); // only used by albedo pass
            properties.SetInt(Shader.PropertyToID("unity_UseLinearSpace"), QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 0);
            material.DisableKeyword("EDITOR_VISUALIZATION");

            using var cmd = new CommandBuffer();
            cmd.SetRenderTarget(targetTexture);

            cmd.SetViewProjectionMatrices(Matrix4x4.identity, GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(-1, 1, -1, 1, -50, 50), true));
            cmd.SetViewport(new Rect(0, 0, targetTexture.width, targetTexture.height));
            cmd.ClearRenderTarget(false, true, new Color(0, 0, 0, 1));

            if (_planeMesh == null)
                _planeMesh = CreateQuadMesh();

            if (metaPassIndex != -1)
                cmd.DrawMesh(_planeMesh, Matrix4x4.identity, material, 0, metaPassIndex, properties);

            Graphics.ExecuteCommandBuffer(cmd);
            return targetTexture;
        }

        // The textures referenced by the material descriptor are owned by the material descriptor.
        public static World.MaterialDescriptor ConvertUnityMaterialToMaterialDescriptor(Material material)
        {
            World.MaterialDescriptor descriptor = new();

            // Emission
            var emission = MaterialAspectOracle.GetEmission(material);
            descriptor.EmissionType = emission.Type;
            descriptor.EmissionColor = emission.Color;
            if (emission.Type == MaterialPropertyType.Texture)
            {
                descriptor.Emission = RenderGITexture(material, TextureType.Emission);
                descriptor.EmissionScale = Vector2.one; // Scale and offset handled by meta pass
                descriptor.EmissionOffset = Vector2.zero;
            }

            // Albedo
            descriptor.Albedo = RenderGITexture(material, TextureType.Albedo);
            descriptor.AlbedoScale = Vector2.one; // Scale and offset handled by meta pass
            descriptor.AlbedoOffset = Vector2.zero;

            // Transmission
            var transmission = MaterialAspectOracle.GetTransmission(material);
            descriptor.TransmissionChannels = transmission.Channels;
            descriptor.TransmissionScale = Vector2.one; // Scale and offset handled in the Blit below, so we don't have to apply it later
            descriptor.TransmissionOffset = Vector2.zero;
            if (transmission.Channels != TransmissionChannels.None)
            {
                RenderTexture prevRT = RenderTexture.active;
                var transmissionTexture = CreateTexture(TextureType.Transmission);
                Graphics.Blit(transmission.SourceTexture, transmissionTexture, transmission.Scale, transmission.Offset);
                descriptor.Transmission = transmissionTexture;
                RenderTexture.active = prevRT;
            }
            descriptor.Alpha = MaterialAspectOracle.GetAlpha(material);
            descriptor.UseAlphaCutoff = MaterialAspectOracle.UsesAlphaCutoff(material);
            descriptor.AlphaCutoff = MaterialAspectOracle.GetAlphaCutoff(material);

            // Other properties
            descriptor.DoubleSidedGI = material.doubleSidedGI;

            return descriptor;
        }

        static private GraphicsFormat GetTextureFormat(TextureType textureType)
        {
            switch (textureType)
            {
                case TextureType.Albedo: return GraphicsFormat.R8G8B8A8_UNorm;
                case TextureType.Emission: return GraphicsFormat.R16G16B16A16_SFloat; // TODO(Yvain) might want to use B10G11R11_UFloatPack32 for emissionthere
                case TextureType.Transmission: return GraphicsFormat.R8G8B8A8_UNorm;
                case TextureType.LightCookie: return GraphicsFormat.R16G16B16A16_SFloat;  // same as emission
                case TextureType.LightCubemap: return GraphicsFormat.R16G16B16A16_SFloat; // same as emission
            }

            return 0;
        }

        private static RenderTexture CreateTextureArray(int sliceCount, TextureType textureType)
        {
            TextureDimension dimension = (textureType == TextureType.LightCubemap) ? TextureDimension.CubeArray: TextureDimension.Tex2DArray;
            return CreateTexture(textureType, dimension, textureType == TextureType.LightCubemap ? sliceCount * 6 : sliceCount);
        }

        private static RenderTexture CreateTexture(TextureType textureType, TextureDimension dimension = TextureDimension.Tex2D, int sliceCount = 1)
        {
            var format = GetTextureFormat(textureType);

            var texture = new RenderTexture(new RenderTextureDescriptor(AtlasSize, AtlasSize)
            {
                dimension = dimension,
                depthBufferBits = 0,
                volumeDepth = sliceCount,
                msaaSamples = 1,
                vrUsage = VRTextureUsage.OneEye,
                graphicsFormat = format,
                enableRandomWrite = true,
            })
            {
                name = "CreateTexture (MaterialPool)",
                hideFlags = HideFlags.DontSaveInEditor,
                wrapMode = TextureWrapMode.Repeat,
                wrapModeU = TextureWrapMode.Repeat,
                wrapModeV = TextureWrapMode.Repeat
            };
            texture.Create();

            return texture;
        }

        private void RecreateMaterialList()
        {
            MaterialBuffer?.Dispose();
            MaterialBuffer = new ComputeBuffer(_materialSlotAllocator.capacity, Marshal.SizeOf<PTMaterial>());

            var oldMaterialList = _materialList;
            _materialList = new NativeArray<PTMaterial>(_materialSlotAllocator.capacity, Allocator.Persistent);
            NativeArray<PTMaterial>.Copy(oldMaterialList, _materialList, oldMaterialList.Length);
            oldMaterialList.Dispose();
        }
        private static Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh();

            Vector3[] vertices = {
                new(-1.0f, -1.0f, 0),
                new(1.0f, -1.0f, 0),
                new(-1.0f, 1.0f, 0),
                new(1.0f, 1.0f, 0)
            };
            mesh.vertices = vertices;

            Vector3[] normals = {
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward
            };
            mesh.normals = normals;

            Vector2[] uv = {
                new(0, 1),
                new(1, 1),
                new(0, 0),
                new(1, 0)
            };
            mesh.uv = uv;
            mesh.uv2 = uv;

            int[] tris = {
                0, 2, 1, // lower left triangle
                2, 3, 1 // upper right triangle
            };
            mesh.triangles = tris;

            mesh.hideFlags = HideFlags.DontSaveInEditor;

            return mesh;
        }
    }
}


