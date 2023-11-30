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
        Wind = 1 << 1,
        LightProbe = 1 << 2,
        Lightmap = 1 << 3,

        DefaultWind = Default | Wind,
        DefaultLightProbe = Default | LightProbe,
        DefaultLightmap = Default | Lightmap,
        DefaultWindLightProbe = Default | Wind | LightProbe,
        DefaultWindLightmap = Default | Wind | Lightmap,
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

            public static readonly int _ST_WindVector = Shader.PropertyToID("_ST_WindVector");
            public static readonly int _ST_WindGlobal = Shader.PropertyToID("_ST_WindGlobal");
            public static readonly int _ST_WindBranch = Shader.PropertyToID("_ST_WindBranch");
            public static readonly int _ST_WindBranchTwitch = Shader.PropertyToID("_ST_WindBranchTwitch");
            public static readonly int _ST_WindBranchWhip = Shader.PropertyToID("_ST_WindBranchWhip");
            public static readonly int _ST_WindBranchAnchor = Shader.PropertyToID("_ST_WindBranchAnchor");
            public static readonly int _ST_WindBranchAdherences = Shader.PropertyToID("_ST_WindBranchAdherences");
            public static readonly int _ST_WindTurbulences = Shader.PropertyToID("_ST_WindTurbulences");
            public static readonly int _ST_WindLeaf1Ripple = Shader.PropertyToID("_ST_WindLeaf1Ripple");
            public static readonly int _ST_WindLeaf1Tumble = Shader.PropertyToID("_ST_WindLeaf1Tumble");
            public static readonly int _ST_WindLeaf1Twitch = Shader.PropertyToID("_ST_WindLeaf1Twitch");
            public static readonly int _ST_WindLeaf2Ripple = Shader.PropertyToID("_ST_WindLeaf2Ripple");
            public static readonly int _ST_WindLeaf2Tumble = Shader.PropertyToID("_ST_WindLeaf2Tumble");
            public static readonly int _ST_WindLeaf2Twitch = Shader.PropertyToID("_ST_WindLeaf2Twitch");
            public static readonly int _ST_WindFrondRipple = Shader.PropertyToID("_ST_WindFrondRipple");
            public static readonly int _ST_WindAnimation = Shader.PropertyToID("_ST_WindAnimation");
            public static readonly int _ST_WindVectorHistory = Shader.PropertyToID("_ST_WindVectorHistory");
            public static readonly int _ST_WindGlobalHistory = Shader.PropertyToID("_ST_WindGlobalHistory");
            public static readonly int _ST_WindBranchHistory = Shader.PropertyToID("_ST_WindBranchHistory");
            public static readonly int _ST_WindBranchTwitchHistory = Shader.PropertyToID("_ST_WindBranchTwitchHistory");
            public static readonly int _ST_WindBranchWhipHistory = Shader.PropertyToID("_ST_WindBranchWhipHistory");
            public static readonly int _ST_WindBranchAnchorHistory = Shader.PropertyToID("_ST_WindBranchAnchorHistory");
            public static readonly int _ST_WindBranchAdherencesHistory = Shader.PropertyToID("_ST_WindBranchAdherencesHistory");
            public static readonly int _ST_WindTurbulencesHistory = Shader.PropertyToID("_ST_WindTurbulencesHistory");
            public static readonly int _ST_WindLeaf1RippleHistory = Shader.PropertyToID("_ST_WindLeaf1RippleHistory");
            public static readonly int _ST_WindLeaf1TumbleHistory = Shader.PropertyToID("_ST_WindLeaf1TumbleHistory");
            public static readonly int _ST_WindLeaf1TwitchHistory = Shader.PropertyToID("_ST_WindLeaf1TwitchHistory");
            public static readonly int _ST_WindLeaf2RippleHistory = Shader.PropertyToID("_ST_WindLeaf2RippleHistory");
            public static readonly int _ST_WindLeaf2TumbleHistory = Shader.PropertyToID("_ST_WindLeaf2TumbleHistory");
            public static readonly int _ST_WindLeaf2TwitchHistory = Shader.PropertyToID("_ST_WindLeaf2TwitchHistory");
            public static readonly int _ST_WindFrondRippleHistory = Shader.PropertyToID("_ST_WindFrondRippleHistory");
            public static readonly int _ST_WindAnimationHistory = Shader.PropertyToID("_ST_WindAnimationHistory");
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

                //@ Most of SpeedTree parameters could be packed in fp16. Do later.
                builder.AddComponent<Vector4>(ParamNames._ST_WindVector, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindGlobal, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindBranch, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindBranchTwitch, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindBranchWhip, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindBranchAnchor, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindBranchAdherences, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindTurbulences, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindLeaf1Ripple, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindLeaf1Tumble, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindLeaf1Twitch, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindLeaf2Ripple, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindLeaf2Tumble, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindLeaf2Twitch, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindFrondRipple, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindAnimation, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindVectorHistory, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindGlobalHistory, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindBranchHistory, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindBranchTwitchHistory, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindBranchWhipHistory, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindBranchAnchorHistory, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindBranchAdherencesHistory, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindTurbulencesHistory, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindLeaf1RippleHistory, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindLeaf1TumbleHistory, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindLeaf1TwitchHistory, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindLeaf2RippleHistory, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindLeaf2TumbleHistory, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindLeaf2TwitchHistory, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindFrondRippleHistory, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);
                builder.AddComponent<Vector4>(ParamNames._ST_WindAnimationHistory, isOverriden: true, isPerInstance: true, InstanceType.SpeedTree, InstanceComponentGroup.Wind);

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

        public ParamInfo windVector;
        public ParamInfo windGlobal;
        public ParamInfo windBranchAdherences;
        public ParamInfo windBranch;
        public ParamInfo windBranchTwitch;
        public ParamInfo windBranchWhip;
        public ParamInfo windBranchAnchor;
        public ParamInfo windTurbulences;
        public ParamInfo windLeaf1Ripple;
        public ParamInfo windLeaf1Tumble;
        public ParamInfo windLeaf1Twitch;
        public ParamInfo windLeaf2Ripple;
        public ParamInfo windLeaf2Tumble;
        public ParamInfo windLeaf2Twitch;
        public ParamInfo windFrondRipple;
        public ParamInfo windAnimation;
        public ParamInfo windVectorHistory;
        public ParamInfo windGlobalHistory;
        public ParamInfo windBranchAdherencesHistory;
        public ParamInfo windBranchHistory;
        public ParamInfo windBranchTwitchHistory;
        public ParamInfo windBranchWhipHistory;
        public ParamInfo windBranchAnchorHistory;
        public ParamInfo windTurbulencesHistory;
        public ParamInfo windLeaf1RippleHistory;
        public ParamInfo windLeaf1TumbleHistory;
        public ParamInfo windLeaf1TwitchHistory;
        public ParamInfo windLeaf2RippleHistory;
        public ParamInfo windLeaf2TumbleHistory;
        public ParamInfo windLeaf2TwitchHistory;
        public ParamInfo windFrondRippleHistory;
        public ParamInfo windAnimationHistory;

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

            windVector = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindVector);
            windGlobal = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindGlobal);
            windBranchAdherences = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindBranchAdherences);
            windBranch = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindBranch);
            windBranchTwitch = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindBranchTwitch);
            windBranchWhip = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindBranchWhip);
            windBranchAnchor = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindBranchAnchor);
            windTurbulences = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindTurbulences);
            windLeaf1Ripple = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindLeaf1Ripple);
            windLeaf1Tumble = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindLeaf1Tumble);
            windLeaf1Twitch = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindLeaf1Twitch);
            windLeaf2Ripple = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindLeaf2Ripple);
            windLeaf2Tumble = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindLeaf2Tumble);
            windLeaf2Twitch = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindLeaf2Twitch);
            windFrondRipple = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindFrondRipple);
            windAnimation = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindAnimation);
            windVectorHistory = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindVectorHistory);
            windGlobalHistory = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindGlobalHistory);
            windBranchAdherencesHistory = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindBranchAdherencesHistory);
            windBranchHistory = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindBranchHistory);
            windBranchTwitchHistory = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindBranchTwitchHistory);
            windBranchWhipHistory = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindBranchWhipHistory);
            windBranchAnchorHistory = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindBranchAnchorHistory);
            windTurbulencesHistory = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindTurbulencesHistory);
            windLeaf1RippleHistory = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindLeaf1RippleHistory);
            windLeaf1TumbleHistory = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindLeaf1TumbleHistory);
            windLeaf1TwitchHistory = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindLeaf1TwitchHistory);
            windLeaf2RippleHistory = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindLeaf2RippleHistory);
            windLeaf2TumbleHistory = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindLeaf2TumbleHistory);
            windLeaf2TwitchHistory = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindLeaf2TwitchHistory);
            windFrondRippleHistory = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindFrondRippleHistory);
            windAnimationHistory = GetParamInfo(instanceDataBuffer, ParamNames._ST_WindAnimationHistory);
        }
    }
}
