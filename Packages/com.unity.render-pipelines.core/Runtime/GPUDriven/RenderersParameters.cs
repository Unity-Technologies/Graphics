using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace UnityEngine.Rendering
{
    [Flags]
    internal enum InstanceComponentGroup : uint
    {
        Default = 1 << 0,
        LightProbe = 1 << 1,
        Lightmap = 1 << 2,

        DefaultLightProbe = Default | LightProbe,
        DefaultLightmap = Default | Lightmap,
    }

    internal struct RenderersParameters
    {
        static private int s_uintSize = UnsafeUtility.SizeOf<uint>();

        [Flags]
        public enum Flags
        {
            None = 0,
            UseBoundingSphereParameter = 1 << 0
        }

        public static class ParamNames
        {
            public static readonly int _BaseColor = Shader.PropertyToID("_BaseColor");
            public static readonly int unity_SpecCube0_HDR = Shader.PropertyToID("unity_SpecCube0_HDR");
            public static readonly int unity_SHCoefficients = Shader.PropertyToID("unity_SHCoefficients");
            public static readonly int unity_LightmapIndex = Shader.PropertyToID("unity_LightmapIndex");
            public static readonly int unity_LightmapST = Shader.PropertyToID("unity_LightmapST");
            public static readonly int unity_ObjectToWorld = Shader.PropertyToID("unity_ObjectToWorld");
            public static readonly int unity_WorldToObject = Shader.PropertyToID("unity_WorldToObject");
            public static readonly int unity_MatrixPreviousM = Shader.PropertyToID("unity_MatrixPreviousM");
            public static readonly int unity_MatrixPreviousMI = Shader.PropertyToID("unity_MatrixPreviousMI");
            public static readonly int unity_WorldBoundingSphere = Shader.PropertyToID("unity_WorldBoundingSphere");
        }

        public static GPUInstanceDataBuffer CreateInstanceDataBuffer(Flags flags, in InstanceNumInfo instanceNumInfo)
        {
            using (var builder = new GPUInstanceDataBufferBuilder())
            {
                builder.AddComponent<Vector4>(ParamNames._BaseColor, isOverriden: false, isPerInstance: false, InstanceType.MeshRenderer);
                builder.AddComponent<Vector4>(ParamNames.unity_SpecCube0_HDR, isOverriden: false, isPerInstance: false, InstanceType.MeshRenderer);
                builder.AddComponent<SHCoefficients>(ParamNames.unity_SHCoefficients, isOverriden: true, isPerInstance: true, InstanceType.MeshRenderer, InstanceComponentGroup.LightProbe);
                builder.AddComponent<Vector4>(ParamNames.unity_LightmapIndex, isOverriden: true, isPerInstance: true, InstanceType.MeshRenderer, InstanceComponentGroup.Lightmap);
                builder.AddComponent<Vector4>(ParamNames.unity_LightmapST, isOverriden: true, isPerInstance: true, InstanceType.MeshRenderer, InstanceComponentGroup.Lightmap);
                builder.AddComponent<PackedMatrix>(ParamNames.unity_ObjectToWorld, isOverriden: true, isPerInstance: true, InstanceType.MeshRenderer);
                builder.AddComponent<PackedMatrix>(ParamNames.unity_WorldToObject, isOverriden: true, isPerInstance: true, InstanceType.MeshRenderer);
                builder.AddComponent<PackedMatrix>(ParamNames.unity_MatrixPreviousM, isOverriden: true, isPerInstance: true, InstanceType.MeshRenderer);
                builder.AddComponent<PackedMatrix>(ParamNames.unity_MatrixPreviousMI, isOverriden: true, isPerInstance: true, InstanceType.MeshRenderer);
                if ((flags & Flags.UseBoundingSphereParameter) != 0)
                {
                    builder.AddComponent<Vector4>(ParamNames.unity_WorldBoundingSphere, isOverriden: true, isPerInstance: true, InstanceType.MeshRenderer);
                }
                return builder.Build(instanceNumInfo);
            }
        }

        public struct ParamInfo
        {
            public int index;
            public int gpuAddress;
            public int uintOffset;
            public bool valid => index != 0;
        }

        public ParamInfo lightmapIndex;
        public ParamInfo lightmapScale;
        public ParamInfo localToWorld;
        public ParamInfo worldToLocal;
        public ParamInfo matrixPreviousM;
        public ParamInfo matrixPreviousMI;
        public ParamInfo shCoefficients;
        public ParamInfo boundingSphere;

        public RenderersParameters(in GPUInstanceDataBuffer instanceDataBuffer)
        {
            ParamInfo GetParamInfo(in GPUInstanceDataBuffer instanceDataBuffer, int paramNameIdx, bool assertOnFail = true)
            {
                int gpuAddress = instanceDataBuffer.GetGpuAddress(paramNameIdx, assertOnFail);
                int index = instanceDataBuffer.GetPropertyIndex(paramNameIdx, assertOnFail);
                return new ParamInfo()
                {
                    index = index,
                    gpuAddress = gpuAddress,
                    uintOffset = gpuAddress / s_uintSize
                };
            }

            lightmapIndex = GetParamInfo(instanceDataBuffer, ParamNames.unity_LightmapIndex);
            lightmapScale = GetParamInfo(instanceDataBuffer, ParamNames.unity_LightmapST);
            localToWorld = GetParamInfo(instanceDataBuffer, ParamNames.unity_ObjectToWorld);
            worldToLocal = GetParamInfo(instanceDataBuffer, ParamNames.unity_WorldToObject);
            matrixPreviousM = GetParamInfo(instanceDataBuffer, ParamNames.unity_MatrixPreviousM);
            matrixPreviousMI = GetParamInfo(instanceDataBuffer, ParamNames.unity_MatrixPreviousMI);
            shCoefficients = GetParamInfo(instanceDataBuffer, ParamNames.unity_SHCoefficients);
            boundingSphere = GetParamInfo(instanceDataBuffer, ParamNames.unity_WorldBoundingSphere, assertOnFail: false);
        }
    }
}
