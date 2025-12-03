using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using Unity.Mathematics;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.UnifiedRayTracing
{
    internal sealed class AccelStructAdapter : IDisposable
    {
        private IRayTracingAccelStruct _accelStruct;
        AccelStructInstances _instances;

        internal AccelStructInstances Instances { get => _instances; }

        struct IdsOfInstances
        {
            public int IdOfInstance;
            public int AccelStructID;
        }

        private readonly Dictionary<UInt64, IdsOfInstances[]> _objectHandleToInstances = new();

        public AccelStructAdapter(IRayTracingAccelStruct accelStruct, GeometryPool geometryPool)
        {
            _accelStruct = accelStruct;
            _instances = new AccelStructInstances(geometryPool);
        }

        public AccelStructAdapter(IRayTracingAccelStruct accelStruct, RayTracingResources resources)
            : this(accelStruct, new GeometryPool(GeometryPoolDesc.NewDefault(), resources.geometryPoolKernels, resources.copyBuffer))
        { }

        public IRayTracingAccelStruct GetAccelerationStructure()
        {
            return _accelStruct;
        }

        public GeometryPool GeometryPool => _instances.geometryPool;

        public void Bind(CommandBuffer cmd, string propertyName, IRayTracingShader shader)
        {
            shader.SetAccelerationStructure(cmd, propertyName, _accelStruct);
            _instances.Bind(cmd, shader);
        }

        public void Dispose()
        {
            _instances?.Dispose();
            _instances = null;
            _accelStruct?.Dispose();
            _accelStruct = null;
            _objectHandleToInstances.Clear();
        }

        public void AddInstance(UInt64 objectHandle, Component meshRendererOrTerrain, Span<uint> perSubMeshMask, Span<uint> perSubMeshMaterialIDs, Span<bool> perSubMeshIsOpaque, uint renderingLayerMask)
        {
            if (meshRendererOrTerrain is Terrain terrain)
            {
                Debug.Assert(terrain.enabled, "Terrains are expected to be enabled.");
                TerrainDesc terrainDesc;
                terrainDesc.terrain = terrain;
                terrainDesc.localToWorldMatrix = terrain.transform.localToWorldMatrix;
                terrainDesc.mask = perSubMeshMask[0];
                terrainDesc.renderingLayerMask = renderingLayerMask;
                terrainDesc.materialID = perSubMeshMaterialIDs[0];
                terrainDesc.enableTriangleCulling = true;
                terrainDesc.frontTriangleCounterClockwise = false;
                AddInstance(objectHandle, terrainDesc);
            }
            else
            {
                var meshRenderer = (MeshRenderer)meshRendererOrTerrain;
                Debug.Assert(meshRenderer.enabled, "Mesh renderers are expected to be enabled.");
                Debug.Assert(!meshRenderer.isPartOfStaticBatch, "Mesh renderers are expected to not be part of static batch.");
                var mesh = meshRenderer.GetComponent<MeshFilter>().sharedMesh;
				AddInstance(objectHandle, mesh, meshRenderer.transform.localToWorldMatrix, perSubMeshMask, perSubMeshMaterialIDs, perSubMeshIsOpaque, renderingLayerMask);
            }
        }

        public void AddInstance(UInt64 objectHandle, Mesh mesh, Matrix4x4 localToWorldMatrix, Span<uint> perSubMeshMask, Span<uint> perSubMeshMaterialIDs, Span<bool> perSubMeshIsOpaque, uint renderingLayerMask)
        {
            int subMeshCount = mesh.subMeshCount;

            var instances = new IdsOfInstances[subMeshCount];
            for (int i = 0; i < subMeshCount; ++i)
            {
                var instanceDesc = new MeshInstanceDesc(mesh, i)
                {
                    localToWorldMatrix = localToWorldMatrix,
                    mask = perSubMeshMask[i],
                    opaqueGeometry = perSubMeshIsOpaque[i]
                };

                instances[i].IdOfInstance = _instances.AddInstance(instanceDesc, perSubMeshMaterialIDs[i], renderingLayerMask);
                instanceDesc.instanceID = (uint)instances[i].IdOfInstance;
                instances[i].AccelStructID = _accelStruct.AddInstance(instanceDesc);
            }

            _objectHandleToInstances.Add(objectHandle, instances);
        }

        private void AddInstance(UInt64 objectHandle, TerrainDesc terrainDesc)
        {
            List<IdsOfInstances> instanceHandles = new List<IdsOfInstances>();

            AddHeightmap(terrainDesc, ref instanceHandles);
            AddTrees(terrainDesc, ref instanceHandles);

            _objectHandleToInstances.Add(objectHandle, instanceHandles.ToArray());

        }

        void AddHeightmap(TerrainDesc terrainDesc, ref List<IdsOfInstances> instanceHandles)
        {
            var terrainMesh = TerrainToMesh.Convert(terrainDesc.terrain);
            var instanceDesc = new MeshInstanceDesc(terrainMesh);
            instanceDesc.localToWorldMatrix = terrainDesc.localToWorldMatrix;
            instanceDesc.mask = terrainDesc.mask;
            instanceDesc.enableTriangleCulling = terrainDesc.enableTriangleCulling;
            instanceDesc.frontTriangleCounterClockwise = terrainDesc.frontTriangleCounterClockwise;

            instanceHandles.Add(AddInstance(instanceDesc, terrainDesc.materialID, terrainDesc.renderingLayerMask));

        }

        void AddTrees(TerrainDesc terrainDesc, ref List<IdsOfInstances> instanceHandles)
        {
            TerrainData terrainData = terrainDesc.terrain.terrainData;
            Matrix4x4 terrainLocalToWorld = terrainDesc.localToWorldMatrix;
            Vector3 positionScale = Vector3.Scale(new Vector3(terrainData.heightmapResolution, 1.0f, terrainData.heightmapResolution), terrainData.heightmapScale);
            Vector3 positionOffset = terrainLocalToWorld.GetPosition();

            foreach (var treeInstance in terrainData.treeInstances)
            {
                var localToWorld = Matrix4x4.TRS(
                    positionOffset + Vector3.Scale(treeInstance.position, positionScale),
                    Quaternion.AngleAxis(treeInstance.rotation, Vector3.up),
                    new Vector3(treeInstance.widthScale, treeInstance.heightScale, treeInstance.widthScale));

                var prefab = terrainData.treePrototypes[treeInstance.prototypeIndex].prefab;

                GameObject go = prefab.gameObject;
                if (prefab.TryGetComponent<LODGroup>(out var lodGroup))
                {
                    var groups = lodGroup.GetLODs();
                    if (groups.Length != 0 && groups[0].renderers.Length != 0)
                        go = (groups[0].renderers[0] as MeshRenderer).gameObject;
                }
                if (!go.TryGetComponent<MeshFilter>(out var filter))
                    continue;

                var mesh = filter.sharedMesh;
                for (int i = 0; i < mesh.subMeshCount; ++i)
                {
                    var instanceDesc = new MeshInstanceDesc(mesh, i);
                    instanceDesc.localToWorldMatrix = localToWorld;
                    instanceDesc.mask = terrainDesc.mask;
                    instanceDesc.enableTriangleCulling = terrainDesc.enableTriangleCulling;
                    instanceDesc.frontTriangleCounterClockwise = terrainDesc.frontTriangleCounterClockwise;
                    instanceHandles.Add(AddInstance(instanceDesc, terrainDesc.materialID, 1u << prefab.gameObject.layer));
                }
            }
        }

        IdsOfInstances AddInstance(MeshInstanceDesc instanceDesc, uint materialID, uint renderingLayerMask)
        {
            IdsOfInstances res = new IdsOfInstances();
            res.IdOfInstance = _instances.AddInstance(instanceDesc, materialID, renderingLayerMask);
            instanceDesc.instanceID = (uint)res.IdOfInstance;
            res.AccelStructID = _accelStruct.AddInstance(instanceDesc);

            return res;
        }


        public void RemoveInstance(UInt64 objectHandle)
        {
            bool success = _objectHandleToInstances.TryGetValue(objectHandle, out var instances);
            Assert.IsTrue(success);

            foreach (var instance in instances)
            {
                _instances.RemoveInstance(instance.IdOfInstance);
                _accelStruct.RemoveInstance(instance.AccelStructID);
            }

            _objectHandleToInstances.Remove(objectHandle);
        }

        public void UpdateInstanceTransform(UInt64 objectHandle, Matrix4x4 localToWorldMatrix)
        {
            bool success = _objectHandleToInstances.TryGetValue(objectHandle, out var instances);
            Assert.IsTrue(success);

            foreach(var instance in instances)
            {
                _instances.UpdateInstanceTransform(instance.IdOfInstance, localToWorldMatrix);
                _accelStruct.UpdateInstanceTransform(instance.AccelStructID, localToWorldMatrix);
            }
        }

        public void UpdateInstanceMaterialIDs(UInt64 objectHandle, Span<uint> perSubMeshMaterialIDs)
        {
            bool success = _objectHandleToInstances.TryGetValue(objectHandle, out var instances);
            Assert.IsTrue(success);
            Assert.IsTrue(perSubMeshMaterialIDs.Length >= instances.Length);
            int i = 0;
            foreach (var instance in instances)
            {
                _instances.UpdateInstanceMaterialID(instance.IdOfInstance, perSubMeshMaterialIDs[i++]);
            }
        }

        public void UpdateInstanceMask(UInt64 objectHandle, Span<uint> perSubMeshMask)
        {
            bool success = _objectHandleToInstances.TryGetValue(objectHandle, out var instances);
            Assert.IsTrue(success);
            Assert.IsTrue(perSubMeshMask.Length >= instances.Length);
            int i = 0;
            foreach (var instance in instances)
            {
                _instances.UpdateInstanceMask(instance.IdOfInstance, perSubMeshMask[i]);
                _accelStruct.UpdateInstanceMask(instance.AccelStructID, perSubMeshMask[i]);
                i++;
            }
        }

        public void UpdateInstanceMask(UInt64 objectHandle, uint mask)
        {
            bool success = _objectHandleToInstances.TryGetValue(objectHandle, out var instances);
            Assert.IsTrue(success);

            var perSubMeshMask = new uint[instances.Length];
            Array.Fill(perSubMeshMask, mask);

            int i = 0;
            foreach (var instance in instances)
            {
                _instances.UpdateInstanceMask(instance.IdOfInstance, perSubMeshMask[i]);
                _accelStruct.UpdateInstanceMask(instance.AccelStructID, perSubMeshMask[i]);
                i++;
            }
        }

        public void Build(CommandBuffer cmd, ref GraphicsBuffer scratchBuffer)
        {
            RayTracingHelper.ResizeScratchBufferForBuild(_accelStruct, ref scratchBuffer);
            _accelStruct.Build(cmd, scratchBuffer);
        }

        public void NextFrame()
        {
            _instances.NextFrame();
        }

        public bool GetInstanceIDs(UInt64 rendererID, out int[] instanceIDs)
        {
            if (!_objectHandleToInstances.TryGetValue(rendererID, out IdsOfInstances[] instIDs))
            {
                // This should never happen as long as the renderer was already added to the acceleration structure
                instanceIDs = null;
                return false;
            }
            instanceIDs = Array.ConvertAll(instIDs, item => item.IdOfInstance);
            return true;
        }

    }

    internal struct TerrainDesc
    {
        public Terrain terrain;
        public Matrix4x4 localToWorldMatrix;
        public uint mask;
        public uint renderingLayerMask;
        public uint materialID;
        public bool enableTriangleCulling;
        public bool frontTriangleCounterClockwise;

        public TerrainDesc(Terrain terrain)
        {
            this.terrain = terrain;
            localToWorldMatrix = Matrix4x4.identity;
            mask = 0xFFFFFFFF;
            renderingLayerMask = 0xFFFFFFFF;
            materialID = 0;
            enableTriangleCulling = true;
            frontTriangleCounterClockwise = false;
        }
    }
}
