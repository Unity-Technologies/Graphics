using System;
using System.Collections.Generic;
using UnityEngine.PathTracing.Core;
using UnityEngine.Rendering;
using Unity.Mathematics;
using UnityEditor.LightBaking;
using UnityEngine;
using UnityEngine.PathTracing.Lightmapping;
using UnityEngine.PathTracing.Integration;
using UnityEngine.Rendering.Sampling;
using UnityEngine.Rendering.UnifiedRayTracing;
using InputExtraction = UnityEngine.LightTransport.InputExtraction;
using Material = UnityEngine.Material;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEditor.PathTracing.LightBakerBridge
{
    using MaterialHandle = Handle<MaterialPool.MaterialDescriptor>;
    using LightHandle = Handle<World.LightDescriptor>;

    internal static class BakeInputToWorldConversion
    {
        private static bool CanBulkCopy(VertexData vertexData, (VertexAttribute attr, int attributeIndex)[] attributeMapping, NativeArray<byte> destination)
        {
            bool canBulkCopy = vertexData.data.Length == destination.Length;
            if (canBulkCopy)
            {
                // Calculate destination stride (size of one vertex with all attributes)
                int destStride = destination.Length / (int)vertexData.vertexCount;

                // Verify that all attributes use the same stride (interleaved) and offsets match Unity's layout
                int expectedOffset = 0;
                for (int a = 0; a < attributeMapping.Length; a++)
                {
                    int attributeIndex = attributeMapping[a].attributeIndex;
                    int srcStride = (int)vertexData.stride[attributeIndex];
                    int srcOffset = (int)vertexData.offsets[attributeIndex];
                    int dimension = (int)vertexData.dimensions[attributeIndex];
                    int attributeSize = dimension * sizeof(float);

                    if (srcStride != destStride || srcOffset != expectedOffset)
                    {
                        canBulkCopy = false;
                        break;
                    }
                    expectedOffset += attributeSize;
                }
            }
            return canBulkCopy;
        }

        private static void CopyVertexData(VertexData vertexData, (VertexAttribute attr, int attributeIndex)[] attributeMapping, NativeArray<byte> destination)
        {
            int destStride = destination.Length / (int)vertexData.vertexCount;
            // Copy each vertex's attributes using stride/offset information
            for (int v = 0; v < vertexData.vertexCount; v++)
            {
                int destAttributeOffset = 0;
                // Copy all attributes for the vertex - use attributeMapping to find the right source attribute and where it should go in the destination vertex layout
                for (int a = 0; a < attributeMapping.Length; a++)
                {
                    int attributeIndex = attributeMapping[a].attributeIndex;
                    int srcStride = (int)vertexData.stride[attributeIndex];
                    int srcBaseOffset = (int)vertexData.offsets[attributeIndex];
                    int dimension = (int)vertexData.dimensions[attributeIndex];
                    int attributeSize = dimension * sizeof(float);
                    int srcIndex = srcBaseOffset + v * srcStride;
                    int destIndex = v * destStride + destAttributeOffset;

                    // Copy attribute data
                    for (int b = 0; b < attributeSize; b++)
                        destination[destIndex + b] = vertexData.data[srcIndex + b];

                    destAttributeOffset += attributeSize;
                }
            }
        }

        internal static Mesh MeshDataToMesh(in MeshData meshData)
        {
            ref readonly VertexData vertexData = ref meshData.vertexData;

            var outMesh = new Mesh();
            var outRawMeshArray = Mesh.AllocateWritableMeshData(1);
            var outRawMesh = outRawMeshArray[0];

            int vertexCount = (int)vertexData.vertexCount;

            // Build attribute list with correct dimensions from vertexData
            List<VertexAttributeDescriptor> vertexLayout = new();
            List<(VertexAttribute attr, int attributeIndex)> attributeMapping = new();
            int attributeIndex = 0;

            if (vertexData.meshShaderChannelMask.HasFlag(MeshShaderChannelMask.Vertex))
            {
                int dimension = (int)vertexData.dimensions[attributeIndex];
                vertexLayout.Add(new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, dimension));
                attributeMapping.Add((VertexAttribute.Position, attributeIndex));
                attributeIndex++;
            }
            if (vertexData.meshShaderChannelMask.HasFlag(MeshShaderChannelMask.Normal))
            {
                int dimension = (int)vertexData.dimensions[attributeIndex];
                vertexLayout.Add(new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, dimension));
                attributeMapping.Add((VertexAttribute.Normal, attributeIndex));
                attributeIndex++;
            }
            if (vertexData.meshShaderChannelMask.HasFlag(MeshShaderChannelMask.TexCoord0))
            {
                int dimension = (int)vertexData.dimensions[attributeIndex];
                vertexLayout.Add(new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, dimension));
                attributeMapping.Add((VertexAttribute.TexCoord0, attributeIndex));
                attributeIndex++;
            }
            if (vertexData.meshShaderChannelMask.HasFlag(MeshShaderChannelMask.TexCoord1))
            {
                int dimension = (int)vertexData.dimensions[attributeIndex];
                vertexLayout.Add(new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, dimension));
                attributeMapping.Add((VertexAttribute.TexCoord1, attributeIndex));
                attributeIndex++;
            }
            outRawMesh.SetVertexBufferParams(vertexCount, vertexLayout.ToArray());

            // Copy vertex data respecting source stride and offset
            var destVertexData = outRawMesh.GetVertexData<byte>();

            // Check if source data layout matches Unity's expected layout for a fast bulk copy
            bool canBulkCopy = CanBulkCopy(vertexData, attributeMapping.ToArray(), destVertexData);
            if (canBulkCopy)
                destVertexData.CopyFrom(vertexData.data); // Fast path: source layout matches destination, do bulk copy
            else
                CopyVertexData(vertexData, attributeMapping.ToArray(), destVertexData);

            outRawMesh.SetIndexBufferParams(meshData.indexBuffer.Length, IndexFormat.UInt32);
            outRawMesh.GetIndexData<uint>().CopyFrom(meshData.indexBuffer);

            int subMeshCount = meshData.subMeshAABB.Length;
            outRawMesh.subMeshCount = subMeshCount;
            for (int sm = 0; sm < outRawMesh.subMeshCount; sm++)
            {
                var smd = new SubMeshDescriptor((int)meshData.subMeshIndexOffset[sm], (int)meshData.subMeshIndexCount[sm]);
                outRawMesh.SetSubMesh(sm, smd);
            }

            Mesh.ApplyAndDisposeWritableMeshData(outRawMeshArray, outMesh);
            outMesh.RecalculateBounds();

            // MeshData from LightBaker contains UVs that are scaled to perfectly fit in the [0, 1] range.
            // The scale and offset used to achieve that are stored in the uvScaleOffset field.
            // Here we undo the scaling to get the original UVs of the input mesh.
            Vector2[] uv2 = outMesh.uv2;
            float4 uvScaleOffset = meshData.uvScaleOffset;
            Vector2 uvScale = new Vector2(uvScaleOffset.x, uvScaleOffset.y);
            Vector2 uvOffset = new Vector2(uvScaleOffset.z, uvScaleOffset.w);
            for (int i = 0; i < uv2.Length; i++)
            {
                uv2[i] = (uv2[i] - uvOffset) / uvScale;
            }
            outMesh.uv2 = uv2;

            return outMesh;
        }

        private static Texture2D CreateTexture2DFromTextureData(in TextureData textureData, string name = "CreateTexture2DFromTextureData")
        {
            Texture2D texture = new Texture2D((int)textureData.width, (int)textureData.height, TextureFormat.RGBAFloat, false, linear: true) { name = name };
            texture.SetPixelData(textureData.data, 0);
            texture.Apply(false, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        private static Texture CreateTextureFromCookieData(in CookieData textureData)
        {
            if (textureData.slices == 1)
            {
                Texture2D texture = new Texture2D((int)textureData.width, (int)textureData.height, TextureFormat.RGBA32, false, linear: true);
                texture.SetPixelData(textureData.textureData, 0);
                texture.Apply(false, false);
                return texture;

            }
            else
            {
                Cubemap texture = new Cubemap((int)textureData.width, TextureFormat.RGBA32, false);
                uint faceStride = textureData.width * textureData.width * textureData.pixelStride;

                for (int faceIndex = 0; faceIndex < textureData.slices; faceIndex++)
                    texture.SetPixelData(textureData.textureData, 0, (CubemapFace)faceIndex, faceIndex * (int)faceStride);

                texture.Apply(false, false);
                return texture;
            }
        }

        private static UnityEngine.LightType LightBakerLightTypeToUnityLightType(LightBakerBridge.LightType type)
        {
            switch (type)
            {
                case LightBakerBridge.LightType.Directional: return UnityEngine.LightType.Directional;
                case LightBakerBridge.LightType.Point: return UnityEngine.LightType.Point;
                case LightBakerBridge.LightType.Spot: return UnityEngine.LightType.Spot;
                case LightBakerBridge.LightType.Rectangle: return UnityEngine.LightType.Rectangle;
                case LightBakerBridge.LightType.Disc: return UnityEngine.LightType.Disc;
                case LightBakerBridge.LightType.SpotBoxShape: return UnityEngine.LightType.Box;
                case LightBakerBridge.LightType.SpotPyramidShape: return UnityEngine.LightType.Pyramid;
                default: throw new ArgumentException("Unknown light type");
            }
        }

        private static UnityEngine.Experimental.GlobalIllumination.FalloffType LightBakerFalloffTypeToUnityFalloffType(FalloffType falloff)
        {
            switch (falloff)
            {
                case FalloffType.InverseSquared:
                    return UnityEngine.Experimental.GlobalIllumination.FalloffType.InverseSquared;
                case FalloffType.InverseSquaredNoRangeAttenuation:
                    return UnityEngine.Experimental.GlobalIllumination.FalloffType.InverseSquaredNoRangeAttenuation;
                case FalloffType.Linear:
                    return UnityEngine.Experimental.GlobalIllumination.FalloffType.Linear;
                case FalloffType.Legacy:
                    return UnityEngine.Experimental.GlobalIllumination.FalloffType.Legacy;
                case FalloffType.None:
                    return UnityEngine.Experimental.GlobalIllumination.FalloffType.Undefined;
                default:
                    Debug.Assert(false, $"Unknown falloff type: {falloff}");
                    return UnityEngine.Experimental.GlobalIllumination.FalloffType.Undefined;
            }
        }

        internal static void InjectAnalyticalLights(
            World world,
            bool autoEstimateLUTRange,
            in BakeInput bakeInput,
            out LightHandle[] lightHandles,
            List<UnityEngine.Object> allocatedObjects)
        {
            // Extract lights
            var lights = new World.LightDescriptor[bakeInput.lightData.Length];
            for (int i = 0; i < bakeInput.lightData.Length; i++)
            {
                ref readonly LightData lightData = ref bakeInput.lightData[i];

                float bounceIntensity = lightData.indirectMultiplier;

                World.LightDescriptor lightDescriptor;
                lightDescriptor.Type = LightBakerLightTypeToUnityLightType(lightData.type);
                // We multiply intensity by PI, since LightBaker produces radiance estimates that are too bright by a factor of PI,
                // for light coming from punctual light sources. This isn't correct, but we need to match LightBaker's output.
                // Instead of adding incorrect code to the baker itself, we do the multiplication on the outside.
                float3 linearColor = lightData.color;
                lightDescriptor.LinearLightColor = linearColor;
                lightDescriptor.Shadows = lightData.castsShadows ? LightShadows.Hard : LightShadows.None;
                lightDescriptor.Transform = Matrix4x4.TRS(lightData.position, lightData.orientation, Vector3.one);
                lightDescriptor.ColorTemperature = 0;
                lightDescriptor.LightmapBakeType = lightData.mode == LightMode.Mixed ? LightmapBakeType.Mixed : LightmapBakeType.Baked;
                lightDescriptor.AreaSize = Vector2.one;
                lightDescriptor.SpotAngle = 0;
                lightDescriptor.InnerSpotAngle = 0;
                lightDescriptor.CullingMask = uint.MaxValue;
                lightDescriptor.BounceIntensity = bounceIntensity;
                lightDescriptor.Range = lightData.range;
                lightDescriptor.ShadowMaskChannel = (lightData.shadowMaskChannel < 4) ? (int)lightData.shadowMaskChannel : -1;
                lightDescriptor.UseColorTemperature = false;
                lightDescriptor.FalloffType = LightBakerFalloffTypeToUnityFalloffType(lightData.falloff);
                lightDescriptor.ShadowRadius = GetLightShadowRadius(lightDescriptor.Type, lightData);
                lightDescriptor.CookieSize = lightData.cookieScale;
                lightDescriptor.CookieTexture = Util.IsCookieValid(lightData.cookieTextureIndex) ? CreateTextureFromCookieData(in bakeInput.cookieData[lightData.cookieTextureIndex]) : null;
                if (lightDescriptor.CookieTexture != null)
                    allocatedObjects.Add(lightDescriptor.CookieTexture);

                switch (lightDescriptor.Type)
                {
                    case UnityEngine.LightType.Box:
                    case UnityEngine.LightType.Rectangle:
                        lightDescriptor.AreaSize = new Vector2(lightData.shape0, lightData.shape1);
                        break;

                    case UnityEngine.LightType.Pyramid:
                        lightDescriptor.AreaSize = GetPyramidLightRect(lightData.coneAngle, lightData.shape0);
                        break;

                    case UnityEngine.LightType.Disc:
                        lightDescriptor.AreaSize = new Vector2(lightData.shape0, lightData.shape0);
                        break;

                    case UnityEngine.LightType.Spot:
                        lightDescriptor.SpotAngle = Mathf.Rad2Deg * lightData.coneAngle;
                        // TODO(pema.malling): This isn't quite correct, but very close. I couldn't figure out the math. See ExtractInnerCone(). https://jira.unity3d.com/browse/LIGHT-1727
                        lightDescriptor.InnerSpotAngle = Mathf.Rad2Deg * lightData.innerConeAngle;
                        break;

                    case UnityEngine.LightType.Directional:
                        lightDescriptor.AreaSize = new Vector2(lightData.coneAngle, lightData.innerConeAngle);
                        break;
                }

                lights[i] = lightDescriptor;
            }

            world.lightPickingMethod = bakeInput.lightingSettings.lightAccelerationStructure == LightAccelerationStructure.LightGrid ? LightPickingMethod.LightGrid : LightPickingMethod.Uniform;
            lightHandles = world.AddLights(lights, false, autoEstimateLUTRange, bakeInput.lightingSettings.mixedLightingMode);
        }

        static Vector2 GetPyramidLightRect(float coneAngleInRads, float pyramidAspectRadio)
        {
            float ar = pyramidAspectRadio;
            float h = 2.0f * Mathf.Tan(coneAngleInRads * 0.5f);

            float width = ar >= 1.0f ? (h * ar) : h;
            float height = ar >= 1.0f ? h : (h / ar);

            return new Vector2(width, height);
        }

        static float GetLightShadowRadius(UnityEngine.LightType lightType, in LightData lightData)
        {
            if (lightType == UnityEngine.LightType.Pyramid)
                return lightData.shape1;  // For pyramid lights, shadow radius is in shape1 (shape0 is aspect ratio)
            return (Util.IsPunctualLightType(lightType) && lightType != UnityEngine.LightType.Box) ? lightData.shape0 : 0.0f;
        }

        internal static void InjectEnvironmentLight(
            World world,
            in BakeInput bakeInput,
            List<UnityEngine.Object> allocatedObjects)
        {
            // Setup environment light
            int envCubemapResolution = (int)bakeInput.environmentData.cubeResolution;
            var envCubemap = new Cubemap(envCubemapResolution, TextureFormat.RGBAFloat, false);
            bool isEmptyCubemap = envCubemapResolution == 1;
            for (int i = 0; i < 6; i++)
            {
                envCubemap.SetPixelData(bakeInput.environmentData.cubeData, 0, (CubemapFace)i, envCubemapResolution * envCubemapResolution * i);
                isEmptyCubemap &= math.all(bakeInput.environmentData.cubeData[i].xyz == float3.zero);
            }

            envCubemap.Apply();
            allocatedObjects.Add(envCubemap);
            // If we have no cubemap (i.e. the 1x1x1 black texture), don't set it - we don't want to waste samples directly sampling it.
            if (!isEmptyCubemap)
            {
                var envCubemapMaterial = new Material(Shader.Find("Hidden/PassthroughSkybox"));
                envCubemapMaterial.SetTexture("_Tex", envCubemap);
                world.SetEnvironmentMaterial(envCubemapMaterial);
                allocatedObjects.Add(envCubemapMaterial);
            }
        }

        internal static void InjectMaterials(
            World world,
            in BakeInput bakeInput,
            out MaterialHandle[][] perInstanceSubMeshMaterials,
            out bool[][] perInstanceSubMeshVisibility,
            List<UnityEngine.Object> allocatedObjects)
        {
            int allocationCount = allocatedObjects.Count;

            // Create albedo and emission textures from materials
            var perTexturePairMaterials = new MaterialPool.MaterialDescriptor[bakeInput.albedoData.Length];
            Debug.Assert(bakeInput.albedoData.Length == bakeInput.emissiveData.Length);
            Debug.Assert(bakeInput.albedoData.Length == bakeInput.albedoDataProperties.Length);
            Debug.Assert(bakeInput.emissiveData.Length == bakeInput.emissiveDataProperties.Length);
            for (int i = 0; i < bakeInput.albedoData.Length; i++)
            {
                ref var material = ref perTexturePairMaterials[i];
                var baseTexture = CreateTexture2DFromTextureData(in bakeInput.albedoData[i], $"World (albedo) {i}");
                ref readonly TextureProperties albedoProperties = ref bakeInput.albedoDataProperties[i];
                baseTexture.wrapModeU = albedoProperties.wrapModeU;
                baseTexture.wrapModeV = albedoProperties.wrapModeV;
                baseTexture.filterMode = albedoProperties.filterMode;
                allocatedObjects.Add(baseTexture);
                var emissiveTexture = CreateTexture2DFromTextureData(in bakeInput.emissiveData[i], $"World (emissive) {i}");
                ref readonly TextureProperties emissiveProperties = ref bakeInput.emissiveDataProperties[i];
                emissiveTexture.wrapModeU = emissiveProperties.wrapModeU;
                emissiveTexture.wrapModeV = emissiveProperties.wrapModeV;
                emissiveTexture.filterMode = emissiveProperties.filterMode;
                allocatedObjects.Add(emissiveTexture);
                material.Albedo = baseTexture;
                material.Emission = emissiveTexture;
                material.PointSampleAlbedo = albedoProperties.filterMode == FilterMode.Point;
                material.PointSampleEmission = emissiveProperties.filterMode == FilterMode.Point;

                // Only mark emissive if it isn't the default black texture
                bool isEmissiveSinglePixel = bakeInput.emissiveData[i].data.Length == 1;
                bool isEmissiveBlack = math.all(bakeInput.emissiveData[i].data[0].xyz == float3.zero);
                if (isEmissiveSinglePixel && isEmissiveBlack)
                {
                    material.EmissionType = UnityEngine.PathTracing.Core.MaterialPropertyType.None;
                    material.EmissionColor = Vector3.zero;
                }
                else
                {
                    material.EmissionType = UnityEngine.PathTracing.Core.MaterialPropertyType.Texture;
                    material.EmissionColor = Vector3.one;
                }

                perTexturePairMaterials[i] = material;
            }

            // Create all the unique transmission textures in bakeInput.transmissionData.
            Texture2D[] transmissiveTextures = new Texture2D[bakeInput.transmissionData.Length];
            for (int i = 0; i < bakeInput.transmissionData.Length; i++)
            {
                ref readonly TextureData transmissionData = ref bakeInput.transmissionData[i];
                ref readonly TextureProperties transmissionDataProperties = ref bakeInput.transmissionDataProperties[i];
                Texture2D transmissiveTexture = CreateTexture2DFromTextureData(in transmissionData, $"World (transmission) {i}");
                transmissiveTexture.wrapModeU = transmissionDataProperties.wrapModeU;
                transmissiveTexture.wrapModeV = transmissionDataProperties.wrapModeV;
                transmissiveTexture.filterMode = transmissionDataProperties.filterMode;
                allocatedObjects.Add(transmissiveTexture);
                transmissiveTextures[i] = transmissiveTexture;
            }

            // Certain material properties we can only determine by looking at individual submeshes of each instance.
            // Therefore, we must make a copy of the base material for each submesh. We create these materials here.
            perInstanceSubMeshMaterials = new MaterialHandle[bakeInput.instanceData.Length][];
            perInstanceSubMeshVisibility = new bool[bakeInput.instanceData.Length][];
            // To avoid needlessly creating duplicate materials, we also cache the materials we've already created:
            // Hashing texturePairIdx handles deduplicating by source mesh, scale and the set of source materials. We hash materialIdx
            // to identify the specific source material in the set associated with the texture pair (in case there are submeshes).
            Dictionary<(uint texturePairIdx, int materialIdx), MaterialHandle> materialCache = new();
            for (int instanceIdx = 0; instanceIdx < bakeInput.instanceData.Length; instanceIdx++)
            {
                // Get base (per-instance) material
                ref readonly InstanceData instanceData = ref bakeInput.instanceData[instanceIdx];
                uint texturePairIdx = bakeInput.instanceToTextureDataIndex[instanceIdx];
                ref readonly MaterialPool.MaterialDescriptor baseMaterial = ref perTexturePairMaterials[texturePairIdx];

                // Make space for per-submesh materials and visibility
                perInstanceSubMeshMaterials[instanceIdx] = new MaterialHandle[instanceData.subMeshMaterialIndices.Length];
                perInstanceSubMeshVisibility[instanceIdx] = new bool[instanceData.subMeshMaterialIndices.Length];

                // Extract per-subMesh materials
                for (int subMeshIdx = 0; subMeshIdx < instanceData.subMeshMaterialIndices.Length; subMeshIdx++)
                {
                    int subMeshMaterialIdx = instanceData.subMeshMaterialIndices[subMeshIdx];

                    // If we've already created this material, use it
                    if (materialCache.TryGetValue((texturePairIdx, subMeshMaterialIdx), out MaterialHandle existingHandle))
                    {
                        perInstanceSubMeshMaterials[instanceIdx][subMeshIdx] = existingHandle;
                        perInstanceSubMeshVisibility[instanceIdx][subMeshIdx] = true;
                        continue;
                    }

                    // Copy the base material
                    MaterialPool.MaterialDescriptor subMeshMaterial = baseMaterial;

                    // Get per-subMesh material properties, set them on the copy
                    if (-1 != subMeshMaterialIdx)
                    {
                        ref readonly MaterialData materialData = ref bakeInput.materialData[subMeshMaterialIdx];
                        subMeshMaterial.DoubleSidedGI = materialData.doubleSidedGI;

                        // Set transmission texture, if any
                        int transmissionDataIndex = bakeInput.materialToTransmissionDataIndex[subMeshMaterialIdx];
                        if (-1 != transmissionDataIndex)
                        {
                            subMeshMaterial.Transmission = transmissiveTextures[transmissionDataIndex];
                            ref readonly TextureProperties transmissionDataProperties = ref bakeInput.transmissionDataProperties[transmissionDataIndex];
                            subMeshMaterial.TransmissionScale = transmissionDataProperties.transmissionTextureST.scale;
                            subMeshMaterial.TransmissionOffset = transmissionDataProperties.transmissionTextureST.offset;
                            subMeshMaterial.TransmissionChannels = UnityEngine.PathTracing.Core.TransmissionChannels.RGB;
                            subMeshMaterial.PointSampleTransmission = transmissionDataProperties.filterMode == FilterMode.Point;
                        }

                        // Apply the stretching operation that LightBaker applies - ensures that the UV layout fills the entire UV space
                        if (instanceData.meshIndex >= 0)
                        {
                            Vector4 uvScaleOffset = bakeInput.meshData[instanceData.meshIndex].uvScaleOffset;
                            Vector2 uvScale = new Vector2(uvScaleOffset.x, uvScaleOffset.y);
                            Vector2 uvOffset = new Vector2(uvScaleOffset.z, uvScaleOffset.w);
                            subMeshMaterial.AlbedoScale = uvScale;
                            subMeshMaterial.AlbedoOffset = uvOffset;
                            subMeshMaterial.EmissionScale = uvScale;
                            subMeshMaterial.EmissionOffset = uvOffset;
                        }
                        else
                        {
                            subMeshMaterial.AlbedoScale = Vector2.one;
                            subMeshMaterial.AlbedoOffset = Vector2.zero;
                            subMeshMaterial.EmissionScale = Vector2.one;
                            subMeshMaterial.EmissionOffset = Vector2.zero;
                        }
                    }

                    MaterialHandle addedHandle = world.AddMaterial(in subMeshMaterial, UVChannel.UV1);
                    materialCache.Add((texturePairIdx, subMeshMaterialIdx), addedHandle);
                    perInstanceSubMeshMaterials[instanceIdx][subMeshIdx] = addedHandle;
                    perInstanceSubMeshVisibility[instanceIdx][subMeshIdx] = subMeshMaterialIdx != -1;
                }
            }
            Debug.Assert(allocatedObjects.Count == allocationCount + bakeInput.albedoData.Length * 2 + bakeInput.transmissionData.Length, "InjectMaterials allocated objects incorrectly");
        }

        internal static Mesh TerrainDataToMesh(in TerrainData terrainData, in HeightmapData heightmapData, in TerrainHoleData holeData)
        {
            var outMesh = TerrainToMesh.Convert(heightmapData.resolution, heightmapData.resolution, heightmapData.data, terrainData.heightmapScale, holeData.resolution, holeData.resolution, holeData.data);
            return outMesh;
        }

        internal static Mesh CreateTerrainQuadMesh()
        {
            // Match TerrainToMesh's extent convention: UV = vertex.xz / (resolution - 1).
            // The quad spans [0, 1] in both vertex space and UV so the lightmap atlas
            // region is fully covered (matches Progressive GPU).
            var mesh = new Mesh();
            mesh.vertices = new Vector3[] { new(0, 0, 0), new(1, 0, 0), new(1, 0, 1), new(0, 0, 1) };
            mesh.uv = new Vector2[] { new(0, 0), new(1, 0), new(1, 1), new(0, 1) };
            mesh.uv2 = new Vector2[] { new(0, 0), new(1, 0), new(1, 1), new(0, 1) };
            mesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            return mesh;
        }

        // Compute world-space bounds for a procedural terrain. The quad mesh used by procedural terrain
        // is in normalized UV space and has Y=0, so CalculateMeshBounds on it gives wrong results.
        // The terrain occupies (resolution-1) * heightmapScale.x in X, (resolution-1) * heightmapScale.z in Z,
        // and Y in [0, heightmapScale.y] (conservative — uses the maximum possible height).
        private static Bounds CalculateProceduralTerrainBounds(in HeightmapData heightMap, in LightBakerBridge.TerrainData terrainData, Matrix4x4 localToWorldMatrix4x4, out Vector3 meshMinVertex, out Vector3 meshMaxVertex)
        {
            float extentX = (heightMap.resolution - 1) * terrainData.heightmapScale.x;
            float extentZ = (heightMap.resolution - 1) * terrainData.heightmapScale.z;
            float maxY = terrainData.heightmapScale.y;
            Vector3[] corners = new Vector3[]
            {
                new(0,       0,    0),
                new(extentX, 0,    0),
                new(0,       0,    extentZ),
                new(extentX, 0,    extentZ),
                new(0,       maxY, 0),
                new(extentX, maxY, 0),
                new(0,       maxY, extentZ),
                new(extentX, maxY, extentZ),
            };
            return CalculateMeshBounds(corners, localToWorldMatrix4x4, out meshMinVertex, out meshMaxVertex);
        }

        private static Bounds CalculateMeshBounds(Vector3[] vertices, Matrix4x4 localToWorldMatrix4x4, out Vector3 meshMinVertex, out Vector3 meshMaxVertex)
        {
            meshMinVertex = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            meshMaxVertex = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (Vector3 vert in vertices)
            {
                // TODO: transform the bounding box instead of looping verts (https://jira.unity3d.com/browse/GFXFEAT-667)
                Vector3 v = localToWorldMatrix4x4.MultiplyPoint(vert);
                meshMinVertex = Vector3.Min(v, meshMinVertex);
                meshMaxVertex = Vector3.Max(v, meshMaxVertex);
            }
            Bounds meshBounds = new Bounds();
            meshBounds.SetMinMax(meshMinVertex, meshMaxVertex);
            return meshBounds;
        }

        internal static void ConvertInstancesAndMeshes(
            World world,
            in BakeInput bakeInput,
            in MaterialHandle[][] perInstanceSubMeshMaterials,
            in bool[][] perInstanceSubMeshVisibility,
            out Bounds sceneBounds,
            out Mesh[] meshes,
            out FatInstance[] fatInstances,
            List<UnityEngine.Object> allocatedObjects,
            uint renderingObjectLayer)
        {
            bool useProceduralTerrain = bakeInput.lightingSettings.enableHeightfieldRayTracing;

            // Extract meshes
            meshes = new Mesh[bakeInput.meshData.Length + bakeInput.terrainData.Length];
            int meshIndex = 0;
            for (int i = 0; i < bakeInput.meshData.Length; i++)
            {
                meshes[meshIndex] = MeshDataToMesh(in bakeInput.meshData[meshIndex]);
                meshes[meshIndex].name = $"{meshIndex}";
                meshIndex++;
            }

            // Extract terrains
            int terrainMeshOffset = meshIndex;
            for (int i = 0; i < bakeInput.terrainData.Length; i++)
            {
                if (useProceduralTerrain)
                {
                    meshes[meshIndex] = CreateTerrainQuadMesh();
                }
                else
                {
                    var heightMap = bakeInput.heightMapData[bakeInput.terrainData[i].heightMapIndex];
                    var holeMap = bakeInput.terrainData[i].terrainHoleIndex >= 0 ? bakeInput.terrainHoleData[bakeInput.terrainData[i].terrainHoleIndex] : new TerrainHoleData();
                    meshes[meshIndex] = TerrainDataToMesh(in bakeInput.terrainData[i], in heightMap, in holeMap);
                }
                meshIndex++;
            }

            // Add procedural terrain instances to the acceleration structure
            if (useProceduralTerrain && bakeInput.terrainData.Length > 0)
            {

                for (int i = 0; i < bakeInput.terrainData.Length; i++)
                {
                    ref readonly LightBakerBridge.TerrainData terrain = ref bakeInput.terrainData[i];
                    var heightMap = bakeInput.heightMapData[terrain.heightMapIndex];
                    var holeMap = terrain.terrainHoleIndex >= 0 ? bakeInput.terrainHoleData[terrain.terrainHoleIndex] : new TerrainHoleData();

                    if (terrain.terrainHoleIndex >= 0)
                        Debug.Assert(holeMap.resolution == heightMap.resolution + 1);

                    // Find the instance data for this terrain to get the transform and material
                    for (int inst = 0; inst < bakeInput.instanceData.Length; inst++)
                    {
                        if (bakeInput.instanceData[inst].terrainIndex != i)
                            continue;

                        ref readonly InstanceData instanceData = ref bakeInput.instanceData[inst];
                        float4x4 localToWorldFloat4x4 = instanceData.transform;
                        Matrix4x4 localToWorldMatrix4x4 = new Matrix4x4(localToWorldFloat4x4.c0, localToWorldFloat4x4.c1, localToWorldFloat4x4.c2, localToWorldFloat4x4.c3);

                        var materials = perInstanceSubMeshMaterials[inst];
                        MaterialHandle terrainMaterial = materials.Length > 0 ? materials[0] : MaterialHandle.Invalid;

                        uint mask = World.GetInstanceMask(
                            instanceData.castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                            true, RenderedGameObjectsFilter.OnlyStatic);

                        world.AddTerrainInstance(
                            heightMap.data,
                            heightMap.resolution,
                            terrain.heightmapScale,
                            holeMap.data,
                            holeMap.resolution,
                            terrainMaterial,
                            mask,
                            renderingObjectLayer,
                            localToWorldMatrix4x4);
                        break;
                    }
                }
            }

            // Compute the tight UV scale and offset for each mesh.
            Vector2[] uvBoundsSizes = new Vector2[meshes.Length];
            Vector2[] uvBoundsOffsets = new Vector2[meshes.Length];
            for (int i = 0; i < meshes.Length; ++i)
            {
                if (meshes[i].uv2.Length == 0)
                    LightmapIntegrationHelpers.ComputeUVBounds(meshes[i].uv, out uvBoundsSizes[i], out uvBoundsOffsets[i]);
                else
                    LightmapIntegrationHelpers.ComputeUVBounds(meshes[i].uv2, out uvBoundsSizes[i], out uvBoundsOffsets[i]);
            }

            // Baking specific settings
            RenderedGameObjectsFilter filter = RenderedGameObjectsFilter.OnlyStatic;
            const bool isStatic = true;

            Vector3 sceneMinVertex = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 sceneMaxVertex = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            // Extract instances
            List<FatInstance> fatInstanceList = new();
            for (int i = 0; i < bakeInput.instanceData.Length; i++)
            {
                ref readonly InstanceData instanceData = ref bakeInput.instanceData[i];

                // Get materials
                var materials = perInstanceSubMeshMaterials[i];
                var visibility = perInstanceSubMeshVisibility[i];

                // Get other instance data
                float4x4 localToWorldFloat4x4 = instanceData.transform;
                Matrix4x4 localToWorldMatrix4x4 = new Matrix4x4(localToWorldFloat4x4.c0, localToWorldFloat4x4.c1, localToWorldFloat4x4.c2, localToWorldFloat4x4.c3);
                ShadowCastingMode shadowCastingMode = instanceData.castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;

                int globalMeshIndex = instanceData.meshIndex >= 0 ? instanceData.meshIndex : terrainMeshOffset + instanceData.terrainIndex; // the mesh array is a concatenation of the meshes and terrain meshes - figure out the right index
                Debug.Assert(globalMeshIndex >= 0 && globalMeshIndex < meshes.Length);
                Mesh mesh = meshes[globalMeshIndex];
                Vector2 uvBoundsSize = uvBoundsSizes[globalMeshIndex];
                Vector2 uvBoundsOffset = uvBoundsOffsets[globalMeshIndex];

                // Calculate bounds. For procedural terrain the quad mesh is in normalized UV space,
                // so we must derive bounds from the heightmap extent and scale instead.
                bool isProceduralTerrain = useProceduralTerrain && instanceData.terrainIndex >= 0;
                Bounds meshBounds;
                Vector3 meshMinVertex, meshMaxVertex;
                if (isProceduralTerrain)
                {
                    ref readonly LightBakerBridge.TerrainData terrain = ref bakeInput.terrainData[instanceData.terrainIndex];
                    var heightMap = bakeInput.heightMapData[terrain.heightMapIndex];
                    meshBounds = CalculateProceduralTerrainBounds(in heightMap, in terrain, localToWorldMatrix4x4, out meshMinVertex, out meshMaxVertex);
                }
                else
                {
                    meshBounds = CalculateMeshBounds(mesh.vertices, localToWorldMatrix4x4, out meshMinVertex, out meshMaxVertex);
                }

                // Keep track of scene bounds as we go
                sceneMinVertex = Vector3.Min(sceneMinVertex, meshMinVertex);
                sceneMaxVertex = Vector3.Max(sceneMaxVertex, meshMaxVertex);

                // Get masks
                uint[] subMeshMasks = new uint[mesh.subMeshCount];
                for (int s = 0; s < mesh.subMeshCount; ++s)
                {
                    subMeshMasks[s] = visibility[s] ? World.GetInstanceMask(shadowCastingMode, isStatic, filter) : 0u;
                }

                // add instance
                var boundingSphere = new BoundingSphere();
                if (isProceduralTerrain)
                {
                    // For procedural terrain the mesh is a normalized-UV quad, so use the world-space
                    // bounds we just computed instead of mesh.bounds.
                    boundingSphere.position = meshBounds.center;
                    boundingSphere.radius = meshBounds.extents.magnitude;
                }
                else
                {
                    boundingSphere.position = localToWorldMatrix4x4.MultiplyPoint(mesh.bounds.center);
                    boundingSphere.radius = (localToWorldMatrix4x4.MultiplyPoint(mesh.bounds.extents) - boundingSphere.position).magnitude;
                }
                var lodIdentifier = new LodIdentifier(instanceData.lodGroup, instanceData.lodMask, instanceData.contributingLodLevel);
                var fatInstance = new FatInstance
                {
                    BoundingSphere = boundingSphere,
                    Mesh = mesh,
                    UVBoundsSize = uvBoundsSize,
                    UVBoundsOffset = uvBoundsOffset,
                    Materials = materials,
                    SubMeshMasks = subMeshMasks,
                    LocalToWorldMatrix = localToWorldMatrix4x4,
                    Bounds = meshBounds,
                    IsStatic = isStatic,
                    LodIdentifier = lodIdentifier,
                    ReceiveShadows = instanceData.receiveShadows,
                    Filter = filter,
                    RenderingObjectLayer = renderingObjectLayer,
                    EnableEmissiveSampling = true,
                    IsProceduralTerrain = isProceduralTerrain,
                    TerrainIndex = instanceData.terrainIndex
                };
                fatInstanceList.Add(fatInstance);
            }
            fatInstances = fatInstanceList.ToArray();
            Debug.Assert(fatInstances.Length == bakeInput.instanceData.Length);

            sceneBounds = new Bounds();
            sceneBounds.SetMinMax(sceneMinVertex, sceneMaxVertex);
        }

        internal static void PopulateWorld(InputExtraction.BakeInput input, UnityComputeWorld world, SamplingResources samplingResources, CommandBuffer cmd, bool autoEstimateLUTRange)
        {
            FatInstance[] fatInstances;
            BakeInputToWorldConversion.DeserializeAndInjectBakeInputData(world.PathTracingWorld, autoEstimateLUTRange, in input,
                out UnityEngine.Bounds sceneBounds, out world.Meshes, out fatInstances, out world.LightHandles,
                world.TemporaryObjects, UnityComputeWorld.RenderingObjectLayer);

            // Add instances to world
            Dictionary<int, List<LodInstanceBuildData>> lodInstances;
            Dictionary<Int32, List<ContributorLodInfo>> lodgroupToContributorInstances;
            WorldHelpers.AddContributingInstancesToWorld(world.PathTracingWorld, in fatInstances, out lodInstances, out lodgroupToContributorInstances);
            world.PathTracingWorld.Build(sceneBounds, cmd, ref world.ScratchBuffer, samplingResources, true, 1024, (int)input.bakeInput.GetLightingSettings().lightGridMaxCells);
        }

        internal static void DeserializeAndInjectBakeInputData(
            World world,
            bool autoEstimateLUTRange,
            in InputExtraction.BakeInput bakeInput,
            out Bounds sceneBounds,
            out Mesh[] meshes,
            out FatInstance[] fatInstances,
            out LightHandle[] lightHandles,
            List<UnityEngine.Object> allocatedObjects,
            uint renderingObjectLayer)
        {
            string bakeInputPath = $"Temp/TempNative.bakeInput";
            bool serializeSucceeded = LightBaking.LightBaker.Serialize(bakeInputPath, bakeInput.bakeInput);
            Debug.Assert(serializeSucceeded, $"Failed to serialize input to '{bakeInputPath}'.");

            LightBakerBridge.BakeInput conversionBakeInput;
            bool deserializeSucceeded = BakeInputSerialization.Deserialize(bakeInputPath, out conversionBakeInput);
            System.IO.File.Delete(bakeInputPath);
            Debug.Assert(deserializeSucceeded, $"Failed to deserialize input from '{bakeInputPath}'.");
            InjectBakeInputData(world, autoEstimateLUTRange, conversionBakeInput, out sceneBounds, out meshes, out fatInstances, out lightHandles, allocatedObjects, renderingObjectLayer);
        }

        internal static void InjectBakeInputData(
            World world,
            bool autoEstimateLUTRange,
            in BakeInput bakeInput,
            out Bounds sceneBounds,
            out Mesh[] meshes,
            out FatInstance[] fatInstances,
            out LightHandle[] lightHandles,
            List<UnityEngine.Object> allocatedObjects,
            uint renderingObjectLayer)
        {
            InjectAnalyticalLights(world, autoEstimateLUTRange, bakeInput, out lightHandles, allocatedObjects);
            InjectEnvironmentLight(world, bakeInput, allocatedObjects);
            InjectMaterials(world, bakeInput, out var perInstanceSubMeshMaterials, out var perInstanceSubMeshVisibility, allocatedObjects);
            ConvertInstancesAndMeshes(world, bakeInput, perInstanceSubMeshMaterials, perInstanceSubMeshVisibility, out sceneBounds, out meshes, out fatInstances, allocatedObjects, renderingObjectLayer);
        }
    }
}
