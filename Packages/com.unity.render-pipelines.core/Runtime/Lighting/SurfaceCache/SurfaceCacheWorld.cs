using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.PathTracing.Core;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.Rendering
{
    using InstanceHandle = Handle<SurfaceCacheWorld.Instance>;
    using InstanceHandleSet = HandleSet<SurfaceCacheWorld.Instance>;

    using LightHandle = Handle<SurfaceCacheWorld.Light>;
    using LightHandleSet = HandleSet<SurfaceCacheWorld.Light>;

    using MaterialHandle = Handle<MaterialPool.MaterialDescriptor>;
    using MaterialHandleSet = HandleSet<MaterialPool.MaterialDescriptor>;

    internal class SurfaceCacheWorld : IDisposable
    {
        internal readonly struct Light { }
        internal readonly struct Instance { }

        internal struct LightDescriptor
        {
            public LightType Type;
            public Vector3 LinearLightColor;
            public Matrix4x4 Transform;
            public float ColorTemperature;
            public float OuterSpotAngle;
            public float InnerSpotAngle;
            public float Range;
        }

        internal struct DirectionalLight
        {
            internal Vector3 Direction;
            internal Vector3 Intensity;
        }

        internal struct PunctualLight
        {
            internal Vector3 Position;
            internal Vector3 Direction;
            internal Vector3 Intensity;
            internal float CosOuterAngle;
            internal float Range;
            // If innerAngle = outerAngle then 1/(cosInnerAngle-cosOuterAngle), otherwise 0.
            internal float AngleAttenuationValue1;
            // If innerAngle = outerAngle then cosOuterAngle/(cosOuterAngle-cosInnerAngle), otherwise 1.
            internal float AngleAttenuationValue2;
        }

        internal class LightSet : IDisposable
        {
            struct Light { }

            class LightList<T> : IDisposable where T : struct
            {
                private HandleSet<Light> _handles = new();
                private List<T> _list = new();
                private Dictionary<Handle<Light>, int> _handleToIndex = new();
                private Dictionary<int, Handle<Light>> _indexToHandle = new();
                private bool _gpuDirty = true;
                private GraphicsBuffer _buffer;

                internal uint Count => (uint)_list.Count;
                internal List<T> Values => _list;
                internal GraphicsBuffer Buffer => _buffer;

                internal Handle<Light> Add(T light)
                {
                    var handle = _handles.Add();
                    var index = _list.Count;
                    _handleToIndex[handle] = index;
                    _indexToHandle[index] = handle;
                    _list.Add(light);
                    _gpuDirty = true;
                    return handle;
                }

                internal void Update(Handle<Light> handle, T light)
                {
                    Debug.Assert(_handleToIndex.ContainsKey(handle));
                    _gpuDirty = true;
                    _list[_handleToIndex[handle]] = light;
                }

                internal void Remove(Handle<Light> handle)
                {
                    Debug.Assert(_handleToIndex.ContainsKey(handle));
                    _gpuDirty = true;
                    var swapIndex = _handleToIndex[handle];
                    _handleToIndex.Remove(handle);

                    int endIndex = _list.Count - 1;
                    Handle<Light> moveHandle = _indexToHandle[endIndex];

                    _list[swapIndex] = _list[endIndex];
                    _list.RemoveAt(endIndex);
                    _indexToHandle[swapIndex] = moveHandle;
                    _handleToIndex[moveHandle] = swapIndex;
                }

                internal void Commit(CommandBuffer cmd)
                {
                    if (_gpuDirty)
                    {
                        _gpuDirty = false;
                        if (_buffer == null || _buffer.count < _list.Count)
                        {
                            _buffer?.Dispose();
                            _buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, Math.Max(1, _list.Count), UnsafeUtility.SizeOf<T>());
                        }
                        cmd.SetBufferData(_buffer, _list);
                    }
                }

                public void Dispose()
                {
                    _buffer?.Dispose();
                }
            }

            LightHandleSet _handles = new();
            Dictionary<LightHandle, (LightType, Handle<Light>)> _handleToTypeAndSubHandleMap = new();
            LightList<PunctualLight> _punctualLights = new();
            LightList<DirectionalLight> _directionalLights = new();

            public DirectionalLight? DirectionalLight => 0 < _directionalLights.Count ? _directionalLights.Values[0] : null;
            public GraphicsBuffer PunctualLightBuffer => _punctualLights.Buffer;
            public uint PunctualLightCount => _punctualLights.Count;

            Handle<Light> AddToList(LightDescriptor desc)
            {
                if (desc.Type == LightType.Directional)
                    return _directionalLights.Add(ConvertDirectionalLight(desc));
                else if (desc.Type == LightType.Spot)
                    return _punctualLights.Add(ConvertSpotLight(desc));
                else if (desc.Type == LightType.Point)
                    return _punctualLights.Add(ConvertPointLight(desc));
                else
                    return Handle<Light>.Invalid;
            }

            void RemoveFromList(LightType type, Handle<Light> subHandle)
            {
                if (type == LightType.Directional)
                    _directionalLights.Remove(subHandle);
                else if (type == LightType.Spot || type == LightType.Point)
                    _punctualLights.Remove(subHandle);
            }

            void UpdateInList(Handle<Light> subHandle, LightDescriptor desc)
            {
                if (desc.Type == LightType.Directional)
                    _directionalLights.Update(subHandle, ConvertDirectionalLight(desc));
                else if (desc.Type == LightType.Spot)
                    _punctualLights.Update(subHandle, ConvertSpotLight(desc));
                else if (desc.Type == LightType.Point)
                    _punctualLights.Update(subHandle, ConvertPointLight(desc));
            }

            static Vector3 GetLightDirection(LightDescriptor desc)
            {
                return desc.Transform.GetColumn(2).normalized;
            }

            private static DirectionalLight ConvertDirectionalLight(LightDescriptor desc)
            {
                return new DirectionalLight()
                {
                    Direction = GetLightDirection(desc),
                    Intensity = desc.LinearLightColor
                };
            }

            private static PunctualLight ConvertSpotLight(LightDescriptor desc)
            {
                float cosOuterAngle = Mathf.Cos(desc.OuterSpotAngle * Mathf.Deg2Rad * 0.5f);
                float cosInnerAngle = Mathf.Cos(desc.InnerSpotAngle * Mathf.Deg2Rad * 0.5f);

                float angleAttenuationValue1;
                float angleAttenuationValue2;
                if (desc.InnerSpotAngle != desc.OuterSpotAngle)
                {
                    angleAttenuationValue1 = 1.0f / (cosInnerAngle - cosOuterAngle);
                    angleAttenuationValue2 = cosOuterAngle * -angleAttenuationValue1;
                }
                else
                {
                    angleAttenuationValue1 = 0;
                    angleAttenuationValue2 = 1;
                }

                return new PunctualLight()
                {
                    Position = desc.Transform.GetPosition(),
                    Direction = GetLightDirection(desc),
                    Intensity = desc.LinearLightColor,
                    CosOuterAngle = cosOuterAngle,
                    AngleAttenuationValue1 = angleAttenuationValue1,
                    AngleAttenuationValue2 = angleAttenuationValue2,
                    Range = desc.Range
                };
            }

            private static PunctualLight ConvertPointLight(LightDescriptor desc)
            {
                return new PunctualLight()
                {
                    Position = desc.Transform.GetPosition(),
                    Direction = Vector3.up, // doesn't matter
                    Intensity = desc.LinearLightColor,
                    CosOuterAngle = -1.0f, // cos(pi) = -1
                    Range = desc.Range
                };
            }

            static bool ShouldBeInList(LightDescriptor desc)
            {
                return desc.LinearLightColor != Vector3.zero;
            }

            internal void Update(LightHandle handle, LightDescriptor desc)
            {
                var (type, subHandle) = _handleToTypeAndSubHandleMap[handle];

                if (type == desc.Type)
                {
                    bool isInList = subHandle != Handle<Light>.Invalid;
                    if (ShouldBeInList(desc))
                    {
                        if (isInList)
                        {
                            UpdateInList(subHandle, desc);
                        }
                        else
                        {
                            subHandle = AddToList(desc);
                            _handleToTypeAndSubHandleMap[handle] = (type, subHandle);
                        }
                    }
                    else
                    {
                        if (isInList)
                        {
                            RemoveFromList(type, subHandle);
                            _handleToTypeAndSubHandleMap[handle] = (type, Handle<Light>.Invalid);
                        }
                    }
                }
                else
                {
                    var oldType = type;
                    var newType = desc.Type;
                    RemoveFromList(oldType, subHandle);
                    Handle<Light> newSubHandle = Handle<Light>.Invalid;
                    if (ShouldBeInList(desc))
                        newSubHandle = AddToList(desc);
                    _handleToTypeAndSubHandleMap[handle] = (newType, newSubHandle);
                }
            }

            internal LightHandle Add(LightDescriptor desc)
            {
                var handle = _handles.Add();
                Handle<Light> subHandle = Handle<Light>.Invalid;

                if (ShouldBeInList(desc))
                    subHandle = AddToList(desc);

                _handleToTypeAndSubHandleMap[handle] = (desc.Type, subHandle);
                return handle;
            }

            internal void Remove(LightHandle handle)
            {
                Debug.Assert(_handleToTypeAndSubHandleMap.ContainsKey(handle), "Unexpected light handle.");

                var (type, subHandle) = _handleToTypeAndSubHandleMap[handle];
                _handleToTypeAndSubHandleMap.Remove(handle);

                if (subHandle != Handle<Light>.Invalid)
                    RemoveFromList(type, subHandle);
            }

            internal void Commit(CommandBuffer cmd)
            {
                _directionalLights.Commit(cmd);
                _punctualLights.Commit(cmd);
            }

            public void Dispose()
            {
                _directionalLights.Dispose();
                _punctualLights.Dispose();
            }
        }

        private readonly InstanceHandleSet _instanceHandleSet = new();
        private readonly MaterialHandleSet _materialHandleSet = new();

        private LightSet _lights = new();
        private MaterialPool _materialPool;
        private AccelStructAdapter _rayTracingAccelerationStructure;
        private CubemapRender _cubemapRender;
        private float _environmentIntensityMultiplier = 1.0f;

        public void Init(RayTracingContext ctx, WorldResourceSet worldResources)
        {
            _materialPool = new MaterialPool(worldResources.SetAlphaChannelShader, worldResources.BlitCubemap, worldResources.BlitGrayScaleCookie);

            var options = new AccelerationStructureOptions()
            {
                buildFlags = BuildFlags.None, // TODO: Consider whether to use BuildFlags.MinimizeMemory once https://jira.unity3d.com/browse/UUM-54575 is fixed.
            };
            _rayTracingAccelerationStructure = new AccelStructAdapter(ctx.CreateAccelerationStructure(options), new GeometryPool(GeometryPoolDesc.NewDefault(), ctx.Resources.geometryPoolKernels, ctx.Resources.copyBuffer));

            _cubemapRender = new CubemapRender(worldResources.SkyBoxMesh, worldResources.SixFaceSkyBoxMesh);
        }

        public DirectionalLight? GetDirectionalLight()
        {
            return _lights.DirectionalLight;
        }

        public void SetEnvironmentMode(CubemapRender.Mode mode)
        {
            _cubemapRender.SetMode(mode);
        }

        public GraphicsBuffer GetPunctualLightBuffer()
        {
            return _lights.PunctualLightBuffer;
        }

        public uint GetPunctualLightCount()
        {
            return _lights.PunctualLightCount;
        }

        public void SetEnvironmentMaterial(Material mat)
        {
            _cubemapRender.SetMaterial(mat);
        }

        public void SetEnvironmentColor(Color color)
        {
            _cubemapRender.SetColor(color);
        }

        public void SetEnvironmentGradientColors(Color skyColor, Color equatorColor, Color groundColor)
        {
            _cubemapRender.SetFaceColor(CubemapFace.PositiveX, equatorColor);
            _cubemapRender.SetFaceColor(CubemapFace.NegativeX, equatorColor);
            _cubemapRender.SetFaceColor(CubemapFace.PositiveY, skyColor);
            _cubemapRender.SetFaceColor(CubemapFace.NegativeY, groundColor);
            _cubemapRender.SetFaceColor(CubemapFace.PositiveZ, equatorColor);
            _cubemapRender.SetFaceColor(CubemapFace.NegativeZ, equatorColor);
        }

        public void SetEnvironmentIntensityMultiplier(float multiplier)
        {
            _environmentIntensityMultiplier = multiplier;
        }

        public float GetEnvironmentIntensityMultiplier()
        {
            return _environmentIntensityMultiplier;
        }

        public ComputeBuffer GetMaterialListBuffer()
        {
            return _materialPool.MaterialBuffer;
        }

        public RenderTexture GetMaterialAlbedoTextures()
        {
            return _materialPool.AlbedoTextures;
        }

        public RenderTexture GetMaterialEmissionTextures()
        {
            return _materialPool.EmissionTextures;
        }

        public RenderTexture GetMaterialTransmissionTextures()
        {
            return _materialPool.TransmissionTextures;
        }

        public Texture GetEnvironmentTexture()
        {
            return _cubemapRender.GetCubemap();
        }

        public void Dispose()
        {
            _rayTracingAccelerationStructure?.Dispose();
            _materialPool?.Dispose();
            _cubemapRender?.Dispose();
            _lights.Dispose();
        }

        public AccelStructAdapter GetAccelerationStructure()
        {
            return _rayTracingAccelerationStructure;
        }

        public void RemoveInstance(InstanceHandle instance)
        {
            _rayTracingAccelerationStructure.RemoveInstance(instance.Value);
            _instanceHandleSet.Remove(instance);
        }

        public void RemoveMaterial(MaterialHandle materialHandle)
        {
            _materialHandleSet.Remove(materialHandle);
            _materialPool.RemoveMaterial(materialHandle.Value);
        }

        public MaterialHandle AddMaterial(in MaterialPool.MaterialDescriptor material, UVChannel albedoAndEmissionUVChannel)
        {
            MaterialHandle handle = _materialHandleSet.Add();
            _materialPool.AddMaterial(handle.Value, in material, albedoAndEmissionUVChannel);
            return handle;
        }

        public void UpdateMaterial(MaterialHandle materialHandle, in MaterialPool.MaterialDescriptor material, UVChannel albedoAndEmissionUVChannel)
        {
            _materialPool.UpdateMaterial(materialHandle.Value, in material, albedoAndEmissionUVChannel);
        }

        public InstanceHandle AddInstance(
            Mesh mesh,
            Span<MaterialHandle> materials,
            Span<uint> masks,
            in Matrix4x4 localToWorldMatrix)
        {
            Debug.Assert(mesh.vertexCount > 0);
            Debug.Assert(mesh.subMeshCount == materials.Length);
            Debug.Assert(mesh.subMeshCount == masks.Length);

            Span<uint> materialIndices = stackalloc uint[mesh.subMeshCount];
            Span<bool> isOpaque = stackalloc bool[mesh.subMeshCount];
            for (int i = 0; i < materials.Length; ++i)
            {
                if (materials[i] == MaterialHandle.Invalid)
                    continue;

                bool isTransmissive = false;
                _materialPool.GetMaterialInfo(materials[i].Value, out materialIndices[i], out isTransmissive);
                isOpaque[i] = !isTransmissive;
            }

            InstanceHandle instance = _instanceHandleSet.Add();
            _rayTracingAccelerationStructure.AddInstance(instance.Value, mesh, localToWorldMatrix, masks, materialIndices, isOpaque, 0);
            return instance;
        }

#if ENABLE_TERRAIN_MODULE
        public InstanceHandle AddInstance(
            Terrain terrain,
            MaterialHandle material,
            uint mask,
            in Matrix4x4 localToWorldMatrix)
        {
            Debug.Assert(terrain.terrainData != null);
            Debug.Assert(material != MaterialHandle.Invalid);

            Span<uint> masks = stackalloc uint[1] { mask };

            Span<uint> materialIndices = stackalloc uint[1];
            Span<bool> isOpaque = stackalloc bool[1];

            _materialPool.GetMaterialInfo(material.Value, out materialIndices[0], out bool isTransmissive);
            isOpaque[0] = false; // This property is not used for terrain, set to false.

            Component comp = terrain;
            InstanceHandle instance = _instanceHandleSet.Add();

            _rayTracingAccelerationStructure.AddInstance(instance.Value, comp, masks, materialIndices, isOpaque, terrain.renderingLayerMask);
            return instance;
        }
#endif

        public void UpdateInstanceTransform(InstanceHandle instance, Matrix4x4 localToWorldMatrix)
        {
            _rayTracingAccelerationStructure.UpdateInstanceTransform(instance.Value, localToWorldMatrix);
        }

        public void UpdateInstanceMask(InstanceHandle instance, Span<uint> perSubMeshMask)
        {
            _rayTracingAccelerationStructure.UpdateInstanceMask(instance.Value, perSubMeshMask);
        }

        public void UpdateInstanceMaterials(InstanceHandle instance, Span<MaterialHandle> materials)
        {
            Span<uint> materialIndices = stackalloc uint[materials.Length];
            for (int i = 0; i < materials.Length; ++i)
            {
                _materialPool.GetMaterialInfo(materials[i].Value, out materialIndices[i], out bool isTransmissive);
            }

            _rayTracingAccelerationStructure.UpdateInstanceMaterialIDs(instance.Value, materialIndices);
        }

        public LightHandle[] AddLights(Span<LightDescriptor> lightDescs)
        {
            LightHandle[] handles = new LightHandle[lightDescs.Length];
            for (int i = 0; i < lightDescs.Length; i++)
            {
                var handle = _lights.Add(lightDescs[i]);
                handles[i] = handle;
            }
            return handles;
        }

        public LightHandle AddLight(LightDescriptor lightDesc)
        {
            return _lights.Add(lightDesc);
        }

        public void UpdateLights(LightHandle[] lightHandles, Span<LightDescriptor> lightDescriptors)
        {
            Debug.Assert(lightHandles.Length == lightDescriptors.Length);
            for (int i = 0; i < lightHandles.Length; i++)
            {
                ref readonly LightDescriptor descriptor = ref lightDescriptors[i];
                var handle = lightHandles[i];
                _lights.Update(handle, descriptor);
            }
        }

        public void UpdateLight(LightHandle handle, LightDescriptor descriptor)
        {
            _lights.Update(handle, descriptor);
        }

        public void RemoveLights(Span<LightHandle> lightHandles)
        {
            foreach (var lightHandle in lightHandles)
            {
                _lights.Remove(lightHandle);
            }
        }

        public void RemoveLight(LightHandle handle)
        {
            _lights.Remove(handle);
        }

        public void Commit(CommandBuffer cmdBuf, ref GraphicsBuffer scratchBuffer, uint envCubemapResolution, UnityEngine.Light sun, out bool viewAndProjectionMatricesChanged)
        {
            Debug.Assert(_rayTracingAccelerationStructure != null);
            _materialPool.Build(cmdBuf);
            _rayTracingAccelerationStructure.Build(cmdBuf, ref scratchBuffer);
            _cubemapRender.Update(cmdBuf, sun, (int)envCubemapResolution, out viewAndProjectionMatricesChanged);
            _lights.Commit(cmdBuf);
        }
    }
}
