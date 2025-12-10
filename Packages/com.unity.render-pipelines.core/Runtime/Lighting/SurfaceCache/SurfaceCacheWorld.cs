#if SURFACE_CACHE

using System;
using System.Collections.Generic;
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
            public float SpotAngle;
            public float InnerSpotAngle;
            public float Range;
        }

        internal struct DirectionalLight
        {
            internal Vector3 Direction;
            internal Vector3 Intensity;
        }

        internal struct SpotLight
        {
            internal Vector3 Position;
            internal Vector3 Direction;
            internal Vector3 Intensity;
            internal float Angle;
        }

        internal class LightSet
        {
            (LightHandle, DirectionalLight)? _directionalLightPair;
            (LightHandle, SpotLight)? _spotLightPair;
            Dictionary<LightHandle, LightType> _handleToTypeMap = new();
            LightHandleSet _handles = new();

            public DirectionalLight? DirectionalLight => _directionalLightPair.HasValue ? _directionalLightPair.Value.Item2 : null;
            public SpotLight? SpotLight => _spotLightPair.HasValue ? _spotLightPair.Value.Item2 : null;

            internal LightHandle Add(LightDescriptor desc)
            {
                var handle = _handles.Add();
                _handleToTypeMap[handle] = desc.Type;
                if (desc.Type == LightType.Directional && !_directionalLightPair.HasValue)
                {
                    var dirLight = new DirectionalLight()
                    {
                        Direction = desc.Transform.GetColumn(2),
                        Intensity = desc.LinearLightColor
                    };
                    _directionalLightPair = (handle, dirLight);
                }
                else if (desc.Type == LightType.Spot && !_spotLightPair.HasValue)
                {
                    var dirLight = new SpotLight()
                    {
                        Position = desc.Transform.GetPosition(),
                        Direction = desc.Transform.GetColumn(2),
                        Intensity = desc.LinearLightColor,
                        Angle = desc.SpotAngle
                    };
                    _spotLightPair = (handle, dirLight);
                }

                return handle;
            }

            internal void Remove(LightHandle handle)
            {
                Debug.Assert(_handleToTypeMap.ContainsKey(handle), "Unexpected light handle.");

                var type = _handleToTypeMap[handle];

                _handleToTypeMap.Remove(handle);
                _handles.Remove(handle);
                if (type == LightType.Directional && _directionalLightPair.HasValue && handle == _directionalLightPair.Value.Item1)
                {
                    _directionalLightPair = null;
                }
                if (type == LightType.Spot && _spotLightPair.HasValue && handle == _spotLightPair.Value.Item1)
                {
                    _spotLightPair = null;
                }
            }
        }

        private readonly InstanceHandleSet _instanceHandleSet = new();
        private readonly MaterialHandleSet _materialHandleSet = new();

        private LightSet _lights = new();
        private MaterialPool _materialPool;
        private AccelStructAdapter _rayTracingAccelerationStructure;
        private CubemapRender _cubemapRender;

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

        public SpotLight? GetSpotLight()
        {
            return _lights.SpotLight;
        }

        public void SetEnvironmentMaterial(Material mat)
        {
            _cubemapRender.SetMaterial(mat);
        }

        public void SetEnvironmentColor(Color color)
        {
            _cubemapRender.SetColor(color);
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
            UpdateLights(handles, lightDescs);
            return handles;
        }

        public void UpdateLights(LightHandle[] lightHandles, Span<LightDescriptor> lightDescriptors)
        {
            Debug.Assert(lightHandles.Length == lightDescriptors.Length);
            for (int i = 0; i < lightHandles.Length; i++)
            {
                ref readonly LightDescriptor descriptor = ref lightDescriptors[i];
                var handle = lightHandles[i];
                _lights.Remove(handle);
                lightHandles[i] = _lights.Add(descriptor);
            }
        }

        public void RemoveLights(Span<LightHandle> lightHandles)
        {
            foreach (var lightHandle in lightHandles)
            {
                _lights.Remove(lightHandle);
            }
        }

        public void Build(CommandBuffer cmdBuf, ref GraphicsBuffer scratchBuffer, uint envCubemapResolution, UnityEngine.Light sun)
        {
            Debug.Assert(_rayTracingAccelerationStructure != null);
            _materialPool.Build(cmdBuf);
            _rayTracingAccelerationStructure.Build(cmdBuf, ref scratchBuffer);
            _cubemapRender.Update(cmdBuf, sun, (int)envCubemapResolution);
        }
    }
}

#endif
