using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace UnityEngine.Rendering
{
    internal struct RenderersParameters
    {
        static private int s_uintSize = UnsafeUtility.SizeOf<uint>();

        [Flags]
        public enum Flags
        {
            None = 0,
            UseDeferredMaterialInstanceParameter = 1 << 0,
            UseBoundingSphereParameter = 1 << 1
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
            public static readonly int _DeferredMaterialInstanceData = Shader.PropertyToID("_DeferredMaterialInstanceData");
            public static readonly int _LodGroupIndexAndMask = Shader.PropertyToID("_LodGroupIndexAndMask");
        }
        [Flags]
        internal enum InstanceComponents : int
        {
            None = 0,
            BaseColor = 1 << 0,
            SpecCube0_HDR = 1 << 1,
            SHCoefficients = 1 << 2,
            LightmapIndex = 1 << 3,
            LightmapST = 1 << 4,
            ObjectToWorld = 1 << 5,
            WorldToObject = 1 << 6,
            MatrixPreviousM = 1 << 7,
            MatrixPreviousMI = 1 << 8,
            LodIndexAndMask = 1 << 9
        }

        internal static readonly InstanceComponents DefaultComponents = InstanceComponents.ObjectToWorld | InstanceComponents.WorldToObject | InstanceComponents.MatrixPreviousM | InstanceComponents.MatrixPreviousMI;
        internal static readonly InstanceComponents DefaultAndProbesComponents = DefaultComponents | InstanceComponents.SHCoefficients;
        internal static readonly InstanceComponents DefaultAndLightmapComponents = DefaultComponents | InstanceComponents.LightmapIndex | InstanceComponents.LightmapST;

        public static GPUInstanceDataBuffer CreateInstanceDataBuffer(int instanceCount, Flags flags = Flags.None)
        {
            using (var builder = new GPUInstanceDataBufferBuilder())
            {
                builder.AddComponent<Vector4>(ParamNames._BaseColor, isOverriden: false, isPerInstance: false);
                builder.AddComponent<Vector4>(ParamNames.unity_SpecCube0_HDR, isOverriden: false, isPerInstance: false);
                builder.AddComponent<SHCoefficients>(ParamNames.unity_SHCoefficients, isOverriden: true, isPerInstance: true);
                builder.AddComponent<Vector4>(ParamNames.unity_LightmapIndex, isOverriden: true, isPerInstance: true);
                builder.AddComponent<Vector4>(ParamNames.unity_LightmapST, isOverriden: true, isPerInstance: true);
                builder.AddComponent<PackedMatrix>(ParamNames.unity_ObjectToWorld, isOverriden: true, isPerInstance: true);
                builder.AddComponent<PackedMatrix>(ParamNames.unity_WorldToObject, isOverriden: true, isPerInstance: true);
                builder.AddComponent<PackedMatrix>(ParamNames.unity_MatrixPreviousM, isOverriden: true, isPerInstance: true);
                builder.AddComponent<PackedMatrix>(ParamNames.unity_MatrixPreviousMI, isOverriden: true, isPerInstance: true);
                if ((flags & Flags.UseDeferredMaterialInstanceParameter) != 0)
                {
                    builder.AddComponent<uint>(ParamNames._LodGroupIndexAndMask, isOverriden: true, isPerInstance: true);
                    builder.AddComponent<Vector4>(ParamNames._DeferredMaterialInstanceData, isOverriden: true, isPerInstance: true);
                }

                if ((flags & Flags.UseBoundingSphereParameter) != 0)
                    builder.AddComponent<Vector4>(ParamNames.unity_WorldBoundingSphere, isOverriden: true, isPerInstance: true);

                return builder.Build(instanceCount);
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
        public ParamInfo lodGroupIndexAndMask;
        public ParamInfo boundingSphere;
        public ParamInfo deferredMaterialInstanceData;

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
            lodGroupIndexAndMask = GetParamInfo(instanceDataBuffer, ParamNames._LodGroupIndexAndMask, assertOnFail: false);
            boundingSphere = GetParamInfo(instanceDataBuffer, ParamNames.unity_WorldBoundingSphere, assertOnFail: false);
            deferredMaterialInstanceData = GetParamInfo(instanceDataBuffer, ParamNames._DeferredMaterialInstanceData, assertOnFail: false);
        }
    }
}
