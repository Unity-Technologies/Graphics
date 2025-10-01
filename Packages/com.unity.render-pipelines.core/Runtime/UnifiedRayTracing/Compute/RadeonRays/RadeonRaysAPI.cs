using System;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace UnityEngine.Rendering.RadeonRays
{
    enum IndexFormat { Int32 = 0, Int16 };

    internal struct MeshBuildInfo
    {
        public GraphicsBuffer vertices;
        public int verticesStartOffset; // in DWORD
        public uint vertexCount;
        public uint vertexStride; // in DWORD
        public int baseVertex;

        public GraphicsBuffer triangleIndices;
        public int indicesStartOffset; // in DWORD
        public int baseIndex;
        public IndexFormat indexFormat;
        public uint triangleCount;
    }

    internal struct MeshBuildMemoryRequirements
    {
        public ulong buildScratchSizeInDwords;
        public ulong bvhSizeInDwords;
        public ulong bvhLeavesSizeInDwords;
    }

    internal struct SceneBuildMemoryRequirements
    {
        public ulong buildScratchSizeInDwords;
    }

    internal class SceneMemoryRequirements
    {
        public ulong buildScratchSizeInDwords;
        public ulong[] bottomLevelBvhSizeInNodes;
        public uint[] bottomLevelBvhOffsetInNodes;
        public ulong[] bottomLevelBvhLeavesSizeInNodes;
        public uint[] bottomLevelBvhLeavesOffsetInNodes;

        public ulong totalBottomLevelBvhSizeInNodes;
        public ulong totalBottomLevelBvhLeavesSizeInNodes;
    }

    [System.Flags]
    internal enum BuildFlags
    {
        None = 0,
        PreferFastBuild = 1 << 0
    }

    internal enum RayQueryType
    {
        ClosestHit,
        AnyHit
    }

    internal enum RayQueryOutputType
    {
        FullHitData,
        InstanceID
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Transform
    {
        public float4 row0;
        public float4 row1;
        public float4 row2;


        public Transform(float4 row0, float4 row1, float4 row2)
        {
            this.row0 = row0;
            this.row1 = row1;
            this.row2 = row2;
        }

        public static Transform Identity()
        {
            return new Transform(
                new float4(1.0f, 0.0f, 0.0f, 0.0f),
                new float4(0.0f, 1.0f, 0.0f, 0.0f),
                new float4(0.0f, 0.0f, 1.0f, 0.0f));
        }

        public static Transform Translation(float3 translation)
        {
            return new Transform(
                new float4(1.0f, 0.0f, 0.0f, translation.x),
                new float4(0.0f, 1.0f, 0.0f, translation.y),
                new float4(0.0f, 0.0f, 1.0f, translation.z));
        }

        public static Transform Scale(float3 scale)
        {
            return new Transform(
                new float4(scale.x, 0.0f, 0.0f, 0.0f),
                new float4(0.0f, scale.y, 0.0f, 0.0f),
                new float4(0.0f, 0.0f, scale.z, 0.0f));
        }

        public static Transform TRS(float3 translation, float3 rotation, float3 scale)
        {
            var rot = float3x3.Euler(rotation);
            rot.c0 *= scale.x;
            rot.c1 *= scale.y;
            rot.c2 *= scale.z;

            return new Transform(
                    new float4(rot.c0.x, rot.c1.x, rot.c2.x, translation.x),
                    new float4(rot.c0.y, rot.c1.y, rot.c2.y, translation.y),
                    new float4(rot.c0.z, rot.c1.z, rot.c2.z, translation.z));
        }

        public Transform Inverse()
        {
            float3x3 m = new float3x3();
            m[0] = new float3(row0.x, row1.x, row2.x);
            m[1] = new float3(row0.y, row1.y, row2.y);
            m[2] = new float3(row0.z, row1.z, row2.z);

            m = math.inverse(m);
            var t = -math.mul(m, new float3(row0.w, row1.w, row2.w));

            Transform res;
            res.row0 = new float4(m[0].x, m[1].x, m[2].x, t.x);
            res.row1 = new float4(m[0].y, m[1].y, m[2].y, t.y);
            res.row2 = new float4(m[0].z, m[1].z, m[2].z, t.z);

            return res;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BvhNode
    {
        public uint child0; // MSB set for leaf nodes
        public uint child1; // MSB set for leaf nodes
        public uint parent;
        public uint update;

        public float3 aabb0_min;
        public float3 aabb0_max;
        public float3 aabb1_min;
        public float3 aabb1_max;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BvhHeader
    {
        public uint internalNodeCount;
        public uint leafNodeCount;
        public uint root;
        public uint unused;

        public float3 globalAabbMin;
        public float3 globalAabbMax;
        public uint3 unused3;
        public uint3 unused4;
    }

    internal struct Instance
    {
        public uint meshAccelStructOffset;
        public uint instanceMask;
        public uint vertexOffset;
        public uint meshAccelStructLeavesOffset;
        public bool triangleCullingEnabled;
        public bool invertTriangleCulling;
        public uint userInstanceID;
        public bool isOpaque;
        public Transform localToWorldTransform;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct InstanceInfo
    {
        public int blasOffset;
        public int instanceMask;
        public int vertexOffset;
        public int indexOffset;
        public uint disableTriangleCulling;
        public uint invertTriangleCulling;
        public uint userInstanceID;
        public int isOpaque;
        public Transform worldToLocalTransform;
        public Transform localToWorldTransform;
    }

    internal sealed class RadeonRaysShaders
    {
        public ComputeShader bitHistogram;
        public ComputeShader blockReducePart;
        public ComputeShader blockScan;
        public ComputeShader buildHlbvh;
        public ComputeShader restructureBvh;
        public ComputeShader scatter;

#if UNITY_EDITOR
        public static RadeonRaysShaders LoadFromPath(string kernelFolderPath)
        {
            var res = new RadeonRaysShaders();

            res.bitHistogram =           UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(Path.Combine(kernelFolderPath, "bit_histogram.compute"));
            res.blockReducePart =        UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(Path.Combine(kernelFolderPath, "block_reduce_part.compute"));
            res.blockScan =              UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(Path.Combine(kernelFolderPath, "block_scan.compute"));
            res.buildHlbvh =             UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(Path.Combine(kernelFolderPath, "build_hlbvh.compute"));
            res.restructureBvh =         UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(Path.Combine(kernelFolderPath, "restructure_bvh.compute"));
            res.scatter =                UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(Path.Combine(kernelFolderPath, "scatter.compute"));

            return res;
        }
#endif
    }

    internal class RadeonRaysAPI : IDisposable
    {
        readonly HlbvhBuilder buildBvh;
        readonly HlbvhTopLevelBuilder buildTopLevelBvh;
        readonly RestructureBvh restructureBvh;

        public const GraphicsBuffer.Target BufferTarget = GraphicsBuffer.Target.Structured;

        public RadeonRaysAPI(RadeonRaysShaders shaders)
        {
            buildBvh = new HlbvhBuilder(shaders);
            buildTopLevelBvh = new HlbvhTopLevelBuilder(shaders);
            restructureBvh = new RestructureBvh(shaders);
        }
        public void Dispose()
        {
            restructureBvh.Dispose();
        }

        static public int BvhInternalNodeSizeInDwords() { return Marshal.SizeOf<BvhNode>() / 4; }
        static public int BvhInternalNodeSizeInBytes() { return Marshal.SizeOf<BvhNode>(); }
        static public int BvhLeafNodeSizeInBytes() { return Marshal.SizeOf<uint4>(); }
        static public int BvhLeafNodeSizeInDwords() { return Marshal.SizeOf<uint4>() / 4; }

        public void BuildMeshAccelStruct(
            CommandBuffer cmd,
            MeshBuildInfo buildInfo, BuildFlags buildFlags,
            GraphicsBuffer scratchBuffer,
            in BottomLevelLevelAccelStruct result)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
                buildFlags |= BuildFlags.PreferFastBuild;

            buildBvh.Execute(cmd,
                buildInfo.vertices, buildInfo.verticesStartOffset, buildInfo.vertexStride,
                buildInfo.triangleIndices, buildInfo.indicesStartOffset, buildInfo.baseIndex, buildInfo.indexFormat, buildInfo.triangleCount,
                scratchBuffer, in result);

            if ((buildFlags & BuildFlags.PreferFastBuild) == 0)
            {
                restructureBvh.Execute(cmd,
                    buildInfo.vertices, buildInfo.verticesStartOffset, buildInfo.vertexStride, buildInfo.triangleCount,
                    scratchBuffer, in result);
            }
        }

        public MeshBuildMemoryRequirements GetMeshBuildMemoryRequirements(MeshBuildInfo buildInfo, BuildFlags buildFlags)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
                buildFlags |= BuildFlags.PreferFastBuild;

            var result = new MeshBuildMemoryRequirements();
            result.bvhSizeInDwords = buildBvh.GetResultDataSizeInDwords(buildInfo.triangleCount);
            result.bvhLeavesSizeInDwords = buildInfo.triangleCount * (ulong)RadeonRaysAPI.BvhLeafNodeSizeInDwords();

            result.buildScratchSizeInDwords = buildBvh.GetScratchDataSizeInDwords(buildInfo.triangleCount);

            ulong restructureScratchSize = ((buildFlags & BuildFlags.PreferFastBuild) == 0) ? restructureBvh.GetScratchDataSizeInDwords(buildInfo.triangleCount) : 0;
            result.buildScratchSizeInDwords = math.max(result.buildScratchSizeInDwords, restructureScratchSize);

            return result;
        }

        public TopLevelAccelStruct BuildSceneAccelStruct(
                CommandBuffer cmd,
                GraphicsBuffer meshAccelStructsBuffer,
                Instance[] instances,
                GraphicsBuffer scratchBuffer)
        {
            var accelStruct = new TopLevelAccelStruct();

            if (instances.Length == 0)
            {
                buildTopLevelBvh.CreateEmpty(ref accelStruct);
                return accelStruct;
            }

            buildTopLevelBvh.AllocateResultBuffers((uint)instances.Length, ref accelStruct);

            var instancesInfos = new InstanceInfo[instances.Length];
            for (uint i = 0; i < instances.Length; ++i)
            {
                instancesInfos[i] = new InstanceInfo
                {
                    blasOffset = (int)instances[i].meshAccelStructOffset,
                    instanceMask = (int)instances[i].instanceMask,
                    vertexOffset = (int)instances[i].vertexOffset,
                    indexOffset = (int)instances[i].meshAccelStructLeavesOffset,
                    localToWorldTransform = instances[i].localToWorldTransform,
                    disableTriangleCulling = instances[i].triangleCullingEnabled ? 0 : (1u << 30),
                    invertTriangleCulling = instances[i].invertTriangleCulling ? (1u << 31) : 0,
                    userInstanceID = instances[i].userInstanceID,
                    isOpaque = instances[i].isOpaque ? 1 : 0
                    // worldToLocal computed in the shader
                };
            }
            accelStruct.instanceInfos.SetData(instancesInfos);
            accelStruct.bottomLevelBvhs = meshAccelStructsBuffer;
            accelStruct.instanceCount = (uint)instances.Length;

            buildTopLevelBvh.Execute(cmd, scratchBuffer, ref accelStruct);

            return accelStruct;
        }

        public TopLevelAccelStruct CreateSceneAccelStructBuffers(
                GraphicsBuffer meshAccelStructsBuffer,
                uint tlasSizeInDwords,
                Instance[] instances)
        {
            var accelStruct = new TopLevelAccelStruct();

            if (instances.Length == 0)
            {
                buildTopLevelBvh.CreateEmpty(ref accelStruct);
                return accelStruct;
            }

            var instancesInfos = new InstanceInfo[instances.Length];
            for (uint i = 0; i < instances.Length; ++i)
            {
                instancesInfos[i] = new InstanceInfo
                {
                    blasOffset = (int)instances[i].meshAccelStructOffset,
                    instanceMask = (int)instances[i].instanceMask,
                    vertexOffset = (int)instances[i].vertexOffset,
                    indexOffset = (int)instances[i].meshAccelStructLeavesOffset,
                    localToWorldTransform = instances[i].localToWorldTransform,
                    disableTriangleCulling = instances[i].triangleCullingEnabled ? 0 : (1u << 30),
                    invertTriangleCulling = instances[i].invertTriangleCulling ? (1u << 31) : 0,
                    userInstanceID = instances[i].userInstanceID,
                    worldToLocalTransform = instances[i].localToWorldTransform.Inverse()
                };
            }

            accelStruct.instanceInfos = new GraphicsBuffer(TopLevelAccelStruct.instanceInfoTarget, instances.Length, Marshal.SizeOf<InstanceInfo>());
            accelStruct.instanceInfos.SetData(instancesInfos);
            accelStruct.bottomLevelBvhs = meshAccelStructsBuffer;
            accelStruct.topLevelBvh = new GraphicsBuffer(TopLevelAccelStruct.topLevelBvhTarget, (int)tlasSizeInDwords / BvhInternalNodeSizeInDwords(), Marshal.SizeOf<BvhNode>());
            accelStruct.instanceCount = (uint)instances.Length;

            return accelStruct;
        }

        public SceneBuildMemoryRequirements GetSceneBuildMemoryRequirements(uint instanceCount)
        {
            var result = new SceneBuildMemoryRequirements();
            result.buildScratchSizeInDwords = buildTopLevelBvh.GetScratchDataSizeInDwords(instanceCount);

            return result;
        }

        public SceneMemoryRequirements GetSceneMemoryRequirements(MeshBuildInfo[] buildInfos, BuildFlags buildFlags)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
                buildFlags |= BuildFlags.PreferFastBuild;

            var requirements = new SceneMemoryRequirements();
            requirements.buildScratchSizeInDwords = 0;

            requirements.bottomLevelBvhSizeInNodes = new ulong[buildInfos.Length];
            requirements.bottomLevelBvhOffsetInNodes = new uint[buildInfos.Length];

            requirements.bottomLevelBvhLeavesSizeInNodes = new ulong[buildInfos.Length];
            requirements.bottomLevelBvhLeavesOffsetInNodes = new uint[buildInfos.Length];

            int i = 0;
            uint bvhOffset = 0;
            uint bvhLeavesOffset = 0;
            foreach (var buildInfo in buildInfos)
            {
                var meshRequirements = GetMeshBuildMemoryRequirements(buildInfo, buildFlags);

                requirements.buildScratchSizeInDwords = math.max(requirements.buildScratchSizeInDwords, meshRequirements.buildScratchSizeInDwords);

                requirements.bottomLevelBvhSizeInNodes[i] = meshRequirements.bvhSizeInDwords / (ulong)RadeonRaysAPI.BvhInternalNodeSizeInDwords();
                requirements.bottomLevelBvhOffsetInNodes[i] = bvhOffset;

                requirements.bottomLevelBvhLeavesSizeInNodes[i] = meshRequirements.bvhLeavesSizeInDwords / (ulong)RadeonRaysAPI.BvhLeafNodeSizeInDwords();
                requirements.bottomLevelBvhLeavesOffsetInNodes[i] = bvhLeavesOffset;

                bvhOffset += (uint)(meshRequirements.bvhSizeInDwords / (ulong)RadeonRaysAPI.BvhInternalNodeSizeInDwords());
                bvhLeavesOffset += (uint)(meshRequirements.bvhLeavesSizeInDwords / (ulong)RadeonRaysAPI.BvhLeafNodeSizeInDwords());
                i++;
            }

            requirements.totalBottomLevelBvhSizeInNodes = bvhOffset;
            requirements.totalBottomLevelBvhLeavesSizeInNodes = bvhLeavesOffset;

            ulong topLevelScratchSize = buildTopLevelBvh.GetScratchDataSizeInDwords((uint)buildInfos.Length);
            requirements.buildScratchSizeInDwords = math.max(requirements.buildScratchSizeInDwords, topLevelScratchSize);

            return requirements;
        }

        static public ulong GetTraceMemoryRequirements(uint rayCount)
        {
            const uint kStackSize = 64u;
            return kStackSize* rayCount;
        }

    }
}
