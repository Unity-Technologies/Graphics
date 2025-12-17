using Unity.Collections;

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
        public static readonly int unity_RendererUserValuesPropertyEntry = Shader.PropertyToID("unity_RendererUserValuesPropertyEntry");

        public static readonly int[] DOTS_ST_WindParams = new int[(int)SpeedTreeWindParamIndex.MaxWindParamsCount];
        public static readonly int[] DOTS_ST_WindHistoryParams = new int[(int)SpeedTreeWindParamIndex.MaxWindParamsCount];

        static DefaultShaderPropertyID()
        {
            for (int i = 0; i < (int)SpeedTreeWindParamIndex.MaxWindParamsCount; ++i)
            {
                DOTS_ST_WindParams[i] = Shader.PropertyToID($"DOTS_ST_WindParam{i}");
                DOTS_ST_WindHistoryParams[i] = Shader.PropertyToID($"DOTS_ST_WindHistoryParam{i}");
            }
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
        public readonly GPUComponentHandle boundingSphere;
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

            boundingSphere = enableBoundingSpheresInstanceData
                ? archetypeManager.CreateComponent<Vector4>(DefaultShaderPropertyID.unity_WorldBoundingSphere, true)
                : default;

            speedTreeWind = new NativeArray<GPUComponentHandle>((int)SpeedTreeWindParamIndex.MaxWindParamsCount, Allocator.Persistent);
            speedTreeWindHistory = new NativeArray<GPUComponentHandle>((int)SpeedTreeWindParamIndex.MaxWindParamsCount, Allocator.Persistent);

            for (int i = 0; i < (int)SpeedTreeWindParamIndex.MaxWindParamsCount; ++i)
                speedTreeWind[i] = archetypeManager.CreateComponent<Vector4>(DefaultShaderPropertyID.DOTS_ST_WindParams[i], true);
            for (int i = 0; i < (int)SpeedTreeWindParamIndex.MaxWindParamsCount; ++i)
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
            };

            if (enableBoundingSpheresInstanceData)
                requiredComponentSet.Add(boundingSphere);

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
