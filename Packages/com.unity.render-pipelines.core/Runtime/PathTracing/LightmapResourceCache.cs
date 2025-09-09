using System;
using System.Collections.Generic;
using UnityEngine.PathTracing.Core;
using UnityEngine.PathTracing.Integration;
using UnityEngine.Rendering;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.PathTracing.Lightmapping
{
    internal class LightmapIntegrationResourceCache : IDisposable
    {
        private Dictionary<ulong, UVMesh> _meshToUVMesh = new();
        private Dictionary<ulong, UVAccelerationStructure> _meshToUVAccelerationStructure = new();
        private Dictionary<UVFallbackBufferKey, UVFallbackBuffer> _meshToUVFallbackBuffer = new();

        private struct UVFallbackBufferKey : IEquatable<UVFallbackBufferKey>
        {
            int width;
            int height;
            ulong meshInstanceID;

            public UVFallbackBufferKey(int width, int height, EntityId meshInstanceID)
            {
                this.width = width;
                this.height = height;
                this.meshInstanceID = Util.EntityIDToUlong(meshInstanceID);
            }

            public bool Equals(UVFallbackBufferKey other) =>
                width == other.width && height == other.height && meshInstanceID == other.meshInstanceID;
            public override bool Equals(object obj) => obj is UVFallbackBufferKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(width, height, meshInstanceID);
            public static bool operator ==(UVFallbackBufferKey left, UVFallbackBufferKey right) => left.Equals(right);
            public static bool operator !=(UVFallbackBufferKey left, UVFallbackBufferKey right) => !left.Equals(right);
        }

        public int UVMeshCount()
        {
            return _meshToUVMesh.Count;
        }

        public int UVAccelerationStructureCount()
        {
            return _meshToUVAccelerationStructure.Count;
        }

        public int UVFallbackBufferCount()
        {
            return _meshToUVFallbackBuffer.Count;
        }

        internal bool CacheIsHot(BakeInstance[] instances)
        {
            foreach (BakeInstance instance in instances)
            {
                // UVMesh
                ulong uvMeshHash = Util.EntityIDToUlong(instance.Mesh.GetEntityId());
                if (!_meshToUVMesh.TryGetValue(uvMeshHash, out UVMesh uvMesh))
                    return false;

                // UVAccelerationStructure
                ulong uvASHash = uvMeshHash;
                if (!_meshToUVAccelerationStructure.TryGetValue(uvASHash, out UVAccelerationStructure uvAS))
                    return false;

                // UVFallbackBuffer
                var uvFBKey = new UVFallbackBufferKey(instance.TexelSize.x, instance.TexelSize.y, uvMesh.Mesh.GetEntityId());
                if (!_meshToUVFallbackBuffer.TryGetValue(uvFBKey, out UVFallbackBuffer uvFB))
                    return false;
            }
            return true;
        }

        internal bool AddResources(
            BakeInstance[] instances,
            RayTracingContext context,
            CommandBuffer cmd,
            UVFallbackBufferBuilder uvFallbackBufferBuilder)
        {
            foreach (BakeInstance instance in instances)
            {
                // UVMesh
                ulong uvMeshHash = Util.EntityIDToUlong(instance.Mesh.GetEntityId());
                if (!_meshToUVMesh.TryGetValue(uvMeshHash, out UVMesh uvMesh))
                {
                    UVMesh newUVMesh = new();
                    if (!newUVMesh.Build(instance.Mesh))
                    {
                        newUVMesh?.Dispose();
                        return false;
                    }
                    _meshToUVMesh.Add(uvMeshHash, newUVMesh);
                    uvMesh = newUVMesh;
                }
                // UVAccelerationStructure
                ulong uvASHash = Util.EntityIDToUlong(uvMesh.Mesh.GetEntityId());
                if (!_meshToUVAccelerationStructure.TryGetValue(uvASHash, out UVAccelerationStructure uvAS))
                {
                    cmd.BeginSample("Build UVAccelerationStructure");
                    UVAccelerationStructure newUVAS = new();
                    newUVAS.Build(cmd, context, uvMesh, BuildFlags.None);
                    _meshToUVAccelerationStructure.Add(uvASHash, newUVAS);
                    uvAS = newUVAS;
                    cmd.EndSample("Build UVAccelerationStructure");
                }
                // UVFallbackBuffer
                var uvFBKey = new UVFallbackBufferKey(instance.TexelSize.x, instance.TexelSize.y, uvMesh.Mesh.GetEntityId());
                if (!_meshToUVFallbackBuffer.TryGetValue(uvFBKey, out UVFallbackBuffer uvFB))
                {
                    UVFallbackBuffer newUVFB = new();
                    if (!newUVFB.Build(
                        cmd,
                        uvFallbackBufferBuilder,
                        instance.TexelSize.x,
                        instance.TexelSize.y,
                        uvMesh))
                    {
                        newUVFB?.Dispose();
                        return false;
                    }
                    _meshToUVFallbackBuffer.Add(uvFBKey, newUVFB);
                    uvFB = newUVFB;
                }
            }
            return true;
        }

        internal void FreeResources(BakeInstance[] instancesToKeep)
        {
            // Build dictionary over resources to keep
            Dictionary<ulong, UVMesh> uvMeshesToKeep = new();
            Dictionary<ulong, UVAccelerationStructure> uvASToKeep = new();
            Dictionary<UVFallbackBufferKey, UVFallbackBuffer> uvFBToKeep = new();
            foreach (var instance in instancesToKeep)
            {
                ulong uvMeshHash = Util.EntityIDToUlong(instance.Mesh.GetEntityId());
                if (_meshToUVMesh.TryGetValue(uvMeshHash, out UVMesh uvMesh))
                {
                    uvMeshesToKeep.Add(uvMeshHash, uvMesh);
                    _meshToUVMesh.Remove(uvMeshHash);
                    ulong uvASHash = Util.EntityIDToUlong(uvMesh.Mesh.GetEntityId());
                    if (_meshToUVAccelerationStructure.TryGetValue(uvASHash, out UVAccelerationStructure uvAS))
                    {
                        uvASToKeep.Add(uvASHash, uvAS);
                        _meshToUVAccelerationStructure.Remove(uvASHash);
                        var uvFBKey = new UVFallbackBufferKey(instance.TexelSize.x, instance.TexelSize.y, uvMesh.Mesh.GetEntityId());
                        if (_meshToUVFallbackBuffer.TryGetValue(uvFBKey, out UVFallbackBuffer uvFB))
                        {
                            uvFBToKeep.Add(uvFBKey, uvFB);
                            _meshToUVFallbackBuffer.Remove(uvFBKey);
                        }
                    }
                }
            }

            // Dispose remaining resources
            Clear();

            // Restore resources to keep
            _meshToUVMesh = uvMeshesToKeep;
            _meshToUVAccelerationStructure = uvASToKeep;
            _meshToUVFallbackBuffer = uvFBToKeep;
        }

        internal bool GetResources(
            BakeInstance[] instances,
            out UVMesh[] uvMeshes,
            out UVAccelerationStructure[] uvAccelerationStructures,
            out UVFallbackBuffer[] uvFallbackBuffers)
        {
            List<UVMesh> uvMeshList = new();
            List<UVAccelerationStructure> uvAccelerationStructureList = new();
            List<UVFallbackBuffer> uvFallbackBufferList = new();
            uvMeshes = null;
            uvAccelerationStructures = null;
            uvFallbackBuffers = null;
            foreach (var instance in instances)
            {
                ulong uvMeshHash = Util.EntityIDToUlong(instance.Mesh.GetEntityId());
                if (!_meshToUVMesh.TryGetValue(uvMeshHash, out UVMesh uvMesh))
                    return false;
                uvMeshList.Add(uvMesh);
                ulong uvASHash = Util.EntityIDToUlong(uvMesh.Mesh.GetEntityId());
                if (!_meshToUVAccelerationStructure.TryGetValue(uvASHash, out UVAccelerationStructure uvAS))
                    return false;
                uvAccelerationStructureList.Add(uvAS);
                var uvFBKey = new UVFallbackBufferKey(instance.TexelSize.x, instance.TexelSize.y, uvMesh.Mesh.GetEntityId());
                if (!_meshToUVFallbackBuffer.TryGetValue(uvFBKey, out UVFallbackBuffer uvFB))
                    return false;
                uvFallbackBufferList.Add(uvFB);
            }
            uvMeshes = uvMeshList.ToArray();
            uvAccelerationStructures = uvAccelerationStructureList.ToArray();
            uvFallbackBuffers = uvFallbackBufferList.ToArray();
            return true;
        }

        private void Clear()
        {
            foreach (var uvMesh in _meshToUVMesh)
                uvMesh.Value.Dispose();
            foreach (var uvAccelerationStructure in _meshToUVAccelerationStructure)
                uvAccelerationStructure.Value.Dispose();
            foreach (var uvFallbackBuffer in _meshToUVFallbackBuffer)
                uvFallbackBuffer.Value.Dispose();
            _meshToUVMesh.Clear();
            _meshToUVAccelerationStructure.Clear();
            _meshToUVFallbackBuffer.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
