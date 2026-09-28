#if !UNITY_WEBGL_RENDERER_ONLY
using Unity.Collections;
using Unity.Mathematics;

namespace UnityEngine.Rendering
{
    // Move all propertyID static fields to a separate struct.
    // Burst does not support external calls like PropertyToID from static constructors.
    static internal class DefaultShaderPropertyID
    {
        public static readonly int unity_SHCoefficients = Shader.PropertyToID("unity_SHCoefficients");
        public static readonly int unity_LightmapST = Shader.PropertyToID("unity_LightmapST");
        public static readonly int unity_ObjectToWorld = Shader.PropertyToID("unity_ObjectToWorld");
        public static readonly int unity_WorldToObject = Shader.PropertyToID("unity_WorldToObject");
        public static readonly int unity_MatrixPreviousM = Shader.PropertyToID("unity_MatrixPreviousM");
        public static readonly int unity_MatrixPreviousMI = Shader.PropertyToID("unity_MatrixPreviousMI");
        public static readonly int unity_WorldBoundingSphere = Shader.PropertyToID("unity_WorldBoundingSphere");
        public static readonly int unity_LocalBounds = Shader.PropertyToID("unity_LocalBounds");
        public static readonly int unity_RendererUserValuesPropertyEntry = Shader.PropertyToID("unity_RendererUserValuesPropertyEntry");
        public static readonly int unity_LightProbeUsagePropertyEntry = Shader.PropertyToID("unity_LightProbeUsagePropertyEntry");

        public static readonly int[] DOTS_ST_WindParams = new int[InstanceDataSystem.k_STMaxWindParamsCount];
        public static readonly int[] DOTS_ST_WindHistoryParams = new int[InstanceDataSystem.k_STMaxWindParamsCount];

        static DefaultShaderPropertyID()
        {
            for (int i = 0; i < InstanceDataSystem.k_STMaxWindParamsCount; ++i)
            {
                DOTS_ST_WindParams[i] = Shader.PropertyToID($"DOTS_ST_WindParam{i}");
                DOTS_ST_WindHistoryParams[i] = Shader.PropertyToID($"DOTS_ST_WindHistoryParam{i}");
            }
        }
    }

    internal struct LocalBoundsGPUData
    {
        // Packed fp16 local AABB, 16 bytes (GLES requires component sizes to be multiples of 16).
        // x/y/z hold one axis each: low 16 bits = bounds min, high 16 bits = bounds max, w unused.
        // Min rounds toward -infinity and max toward +infinity so the unpacked box always contains the fp32 AABB;
        // values beyond the fp16 range saturate outward to +/-infinity,
        // which the occlusion kernel detects and treats as "always visible".
        public uint4 packedMinMax;

        public static LocalBoundsGPUData Pack(float3 center, float3 extents)
        {
            var boundsMin = center - extents;
            var boundsMax = center + extents;
            return new LocalBoundsGPUData
            {
                packedMinMax = new uint4(
                    F32ToF16Directed(boundsMin.x, false) | (F32ToF16Directed(boundsMax.x, true) << 16),
                    F32ToF16Directed(boundsMin.y, false) | (F32ToF16Directed(boundsMax.y, true) << 16),
                    F32ToF16Directed(boundsMin.z, false) | (F32ToF16Directed(boundsMax.z, true) << 16),
                    0u
                )
            };
        }

        // float -> fp16 bits with directed rounding, in pure integer arithmetic so the result does
        // not depend on the floating point environment
        // (math.f32tof16 rounds to nearest and Burst enables FTZ, which would flush fp16 subnormals).
        // Truncates the magnitude toward zero,
        // then bumps one ulp away from zero when bits were dropped and the rounding direction points away from zero;
        // incrementing the bit pattern walks subnormals -> normals -> infinity,
        // so out-of-range values saturate outward to +/-infinity as required.
        internal static uint F32ToF16Directed(float f32, bool roundUp)
        {
            uint bits = math.asuint(f32);
            uint sign = bits >> 31;
            uint absBits = bits & 0x7FFFFFFFu;

            if (absBits > 0x7F800000u) // NaN in, quiet NaN out
                return (sign << 15) | 0x7E00u;

            int exponent = (int)(absBits >> 23) - 127;
            uint mantissa = absBits & 0x7FFFFFu;

            uint magnitude;
            bool inexact;
            if (exponent >= 16) // above the fp16 range (including infinity)
            {
                magnitude = 0x7BFFu; // largest finite fp16 (65504)
                inexact = true;
            }
            else if (exponent >= -14) // fp16 normal range
            {
                magnitude = ((uint)(exponent + 15) << 10) | (mantissa >> 13);
                inexact = (mantissa & 0x1FFFu) != 0;
            }
            else if (exponent >= -24) // fp16 subnormal range
            {
                int shift = -exponent - 1; // 14 (exponent -15) .. 23 (exponent -24)
                uint full = 0x800000u | mantissa;
                magnitude = full >> shift;
                inexact = (full & ((1u << shift) - 1u)) != 0;
            }
            else // underflows even fp16 subnormals
            {
                magnitude = 0u;
                inexact = absBits != 0u;
            }

            bool roundAwayFromZero = roundUp == (sign == 0u);
            if (inexact && roundAwayFromZero)
                magnitude += 1u;

            return (sign << 15) | magnitude;
        }
    }

    internal struct DefaultGPUComponents
    {
        public readonly GPUComponentHandle shCoefficients;
        public readonly GPUComponentHandle lightmapScaleOffset;
        public readonly GPUComponentHandle objectToWorld;
        public readonly GPUComponentHandle worldToObject;
        public readonly GPUComponentHandle matrixPreviousM;
        public readonly GPUComponentHandle matrixPreviousMI;
        public readonly GPUComponentHandle rendererUserValues;
        public readonly GPUComponentHandle lightProbeUsages;
        public readonly GPUComponentHandle boundingSphere;
        public readonly GPUComponentHandle localBoundsAABB;
        public readonly NativeArray<GPUComponentHandle> speedTreeWind;
        public readonly NativeArray<GPUComponentHandle> speedTreeWindHistory;

        public readonly GPUComponentSet requiredComponentSet;
        public readonly GPUComponentSet lightProbesComponentSet;
        public readonly GPUComponentSet speedTreeComponentSet;
        public readonly GPUComponentSet defaultGOComponentSet;
        public readonly GPUComponentSet defaultSpeedTreeComponentSet;

        public readonly GPUArchetypeHandle defaultGOArchetype;

        public DefaultGPUComponents(ref GPUArchetypeManager archetypeManager, bool enableBoundingSpheresInstanceData)
        {
            shCoefficients = archetypeManager.CreateComponent<SHCoefficients>(DefaultShaderPropertyID.unity_SHCoefficients, true);
            lightmapScaleOffset = archetypeManager.CreateComponent<Vector4>(DefaultShaderPropertyID.unity_LightmapST, true);
            objectToWorld = archetypeManager.CreateComponent<PackedMatrix>(DefaultShaderPropertyID.unity_ObjectToWorld, true);
            worldToObject = archetypeManager.CreateComponent<PackedMatrix>(DefaultShaderPropertyID.unity_WorldToObject, true);
            matrixPreviousM = archetypeManager.CreateComponent<PackedMatrix>(DefaultShaderPropertyID.unity_MatrixPreviousM, true);
            matrixPreviousMI = archetypeManager.CreateComponent<PackedMatrix>(DefaultShaderPropertyID.unity_MatrixPreviousMI, true);
            rendererUserValues = archetypeManager.CreateComponent<uint>(DefaultShaderPropertyID.unity_RendererUserValuesPropertyEntry, true);
            lightProbeUsages = archetypeManager.CreateComponent<uint>(DefaultShaderPropertyID.unity_LightProbeUsagePropertyEntry, true);

            boundingSphere = enableBoundingSpheresInstanceData
                ? archetypeManager.CreateComponent<Vector4>(DefaultShaderPropertyID.unity_WorldBoundingSphere, true)
                : default;

            localBoundsAABB = enableBoundingSpheresInstanceData
                ? archetypeManager.CreateComponent<LocalBoundsGPUData>(DefaultShaderPropertyID.unity_LocalBounds, true)
                : default;

            speedTreeWind = new NativeArray<GPUComponentHandle>(InstanceDataSystem.k_STMaxWindParamsCount, Allocator.Persistent);
            speedTreeWindHistory = new NativeArray<GPUComponentHandle>(InstanceDataSystem.k_STMaxWindParamsCount, Allocator.Persistent);

            for (int i = 0; i < InstanceDataSystem.k_STMaxWindParamsCount; ++i)
                speedTreeWind[i] = archetypeManager.CreateComponent<Vector4>(DefaultShaderPropertyID.DOTS_ST_WindParams[i], true);
            for (int i = 0; i < InstanceDataSystem.k_STMaxWindParamsCount; ++i)
                speedTreeWindHistory[i] = archetypeManager.CreateComponent<Vector4>(DefaultShaderPropertyID.DOTS_ST_WindHistoryParams[i], true);

            //@ Investigate how to avoid uploading the previous matrices for everything.
            // It should only be needed when using object motion vectors.
            requiredComponentSet = new GPUComponentSet()
            {
                objectToWorld,
                worldToObject,
                matrixPreviousM,
                matrixPreviousMI,
                rendererUserValues,
                lightProbeUsages,
            };

            if (enableBoundingSpheresInstanceData)
            {
                requiredComponentSet.Add(boundingSphere);
                requiredComponentSet.Add(localBoundsAABB);
            }

            lightProbesComponentSet = new GPUComponentSet()
            {
                shCoefficients,
            };

            speedTreeComponentSet = new GPUComponentSet();

            for (int i = 0; i < speedTreeWind.Length; ++i)
                speedTreeComponentSet.Add(speedTreeWind[i]);
            for (int i = 0; i < speedTreeWindHistory.Length; ++i)
                speedTreeComponentSet.Add(speedTreeWindHistory[i]);

            defaultGOComponentSet = requiredComponentSet;

            defaultSpeedTreeComponentSet = defaultGOComponentSet;
            defaultSpeedTreeComponentSet.Add(lightmapScaleOffset);
            defaultSpeedTreeComponentSet.AddSet(speedTreeComponentSet);

            defaultGOArchetype = archetypeManager.GetOrCreateArchetype(defaultGOComponentSet);
        }

        public void Dispose()
        {
            speedTreeWind.Dispose();
            speedTreeWindHistory.Dispose();
        }
    }
}

#endif // !UNITY_WEBGL_RENDERER_ONLY
