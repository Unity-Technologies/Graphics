using UnityEngine.Rendering;
using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering
{
    // Class holding parameters for the initialization of the Shadow System
    [Serializable]
    public class ShadowInitParameters
    {
        public const int kDefaultShadowAtlasSize = 4096;
        public const int kDefaultMaxPointLightShadows = 6;
        public const int kDefaultMaxSpotLightShadows = 12;
        public const int kDefaultMaxDirectionalLightShadows = 1;

        public int      shadowAtlasWidth = kDefaultShadowAtlasSize;
        public int      shadowAtlasHeight = kDefaultShadowAtlasSize;

        public int      maxPointLightShadows = kDefaultMaxPointLightShadows;
        public int      maxSpotLightShadows = kDefaultMaxSpotLightShadows;
        public int      maxDirectionalLightShadows = kDefaultMaxDirectionalLightShadows;
    }

    // Class used to pass parameters to the shadow system on a per frame basis.
    [Serializable]
    public class ShadowSettings
    {
        public const float kDefaultMaxShadowDistance = 1000.0f;
        public const float kDefaultDirectionalNearPlaneOffset = 5.0f;

        public bool     enabled = true;
        public float    maxShadowDistance = kDefaultMaxShadowDistance;
        public float    directionalLightNearPlaneOffset = kDefaultDirectionalNearPlaneOffset;
    }

    [GenerateHLSL]
    public enum GPUShadowType
    {
        Point,
        Spot,
        Directional,
        MAX,
        Unknown = MAX,
        All = Point | Spot | Directional
    };


    public enum ShadowAlgorithm // 6 bits
    {
        PCF,
        VSM,
        EVSM,
        MSM,
        PCSS,
        Custom = 32
    };

    public enum ShadowVariant // 3 bits
    {
        V0,
        V1,
        V2,
        V3,
        V4,
        V5,
        V6,
        V7
    }

    public enum ShadowPrecision // 1 bits
    {
        High,
        Low
    }

    [GenerateHLSL]
    public enum GPUShadowAlgorithm // 9 bits
    {
        PCF_1tap        = ShadowAlgorithm.PCF       << 3 | ShadowVariant.V0,
        PCF_9tap        = ShadowAlgorithm.PCF       << 3 | ShadowVariant.V1,
        PCF_tent_3x3    = ShadowAlgorithm.PCF       << 3 | ShadowVariant.V2,
        PCF_tent_5x5    = ShadowAlgorithm.PCF       << 3 | ShadowVariant.V3,
        PCF_tent_7x7    = ShadowAlgorithm.PCF       << 3 | ShadowVariant.V4,
        VSM             = ShadowAlgorithm.VSM       << 3,
        EVSM_2          = ShadowAlgorithm.EVSM      << 3 | ShadowVariant.V0,
        EVSM_4          = ShadowAlgorithm.EVSM      << 3 | ShadowVariant.V1,
        MSM_Ham         = ShadowAlgorithm.MSM       << 3 | ShadowVariant.V0,
        MSM_Haus        = ShadowAlgorithm.MSM       << 3 | ShadowVariant.V1,
        PCSS            = ShadowAlgorithm.PCSS      << 3 | ShadowVariant.V0,
        Custom          = ShadowAlgorithm.Custom    << 3
    }

    // Central location for storing various shadow constants and bitmasks. These can be used to pack enums into ints, for example.
    // These are all guaranteed to be positive, but C# doesn't like uints, so they're all ints.
    public static class ShadowConstants
    {
        public struct Counts
        {
            public const int k_ShadowAlgorithm = 64;
            public const int k_ShadowVariant   = 8;
            public const int k_ShadowPrecision = 2;
            public const int k_GPUShadowType   = 3;
        }
        public struct Bits
        {
            public const int k_ShadowAlgorithm    = 6;
            public const int k_ShadowVariant      = 3;
            public const int k_ShadowPrecision    = 1;
            public const int k_GPUShadowAlgorithm = k_ShadowAlgorithm + k_ShadowVariant + k_ShadowPrecision;
            public const int k_GPUShadowType      = 4;
        }

        public struct Masks
        {
            public const int k_ShadowAlgorithm     = (1 << Bits.k_ShadowAlgorithm) - 1;
            public const int k_ShadowVariant       = (1 << Bits.k_ShadowVariant) - 1;
            public const int k_ShadowPrecision     = (1 << Bits.k_ShadowPrecision) - 1;
            public const int k_GPUShadowAlgorithm  = (1 << Bits.k_GPUShadowAlgorithm) - 1;
            public const int k_GPUShadowType       = (1 << Bits.k_GPUShadowType) - 1;
        }
    }

    // Shadow Registry for exposing shadow features to the UI
    public class ShadowRegistry
    {
        public delegate void VariantDelegate(Light l, ShadowAlgorithm dataAlgorithm, ShadowVariant dataVariant, ShadowPrecision dataPrecision, ref int[] dataContainer);

        public delegate GPUShadowType ShadowLightTypeDelegate(Light l);

        // default implementation based on legacy Unity
        static public GPUShadowType ShadowLightType(Light l)
        {
            GPUShadowType shadowType = GPUShadowType.Unknown;

            switch (l.type)
            {
                case LightType.Spot:
                    shadowType = GPUShadowType.Spot;
                    break;
                case LightType.Directional:
                    shadowType = GPUShadowType.Directional;
                    break;
                case LightType.Point:
                    shadowType = GPUShadowType.Point;
                    break;
                    // area lights by themselves can't be mapped to any GPU type
            }

            return shadowType;
        }

        struct Dels
        {
            public VariantDelegate low;
            public VariantDelegate high;
        }
        struct Entry
        {
            public string   algorithmDesc;
            public int      variantsAvailable;
            public string[] variantDescs;
            public Dels[]   variantDels;
        }

        struct Override
        {
            public bool             enabled;
            public ShadowAlgorithm  algorithm;
            public ShadowVariant    variant;
            public ShadowPrecision  precision;
        }

        Override[] m_GlobalOverrides = new Override[(int)GPUShadowType.MAX];
        ShadowLightTypeDelegate m_shadowLightType;

        Dictionary<ShadowAlgorithm, Entry>[] m_Entries = new Dictionary<ShadowAlgorithm, Entry>[ShadowConstants.Counts.k_GPUShadowType]
        {
            new Dictionary<ShadowAlgorithm, Entry>(),
            new Dictionary<ShadowAlgorithm, Entry>(),
            new Dictionary<ShadowAlgorithm, Entry>()
        };

        // Init default delegate
        public ShadowRegistry() { m_shadowLightType = ShadowLightType;  }

        public void ClearRegistry()
        {
            foreach (var d in m_Entries)
                d.Clear();
        }

        public GPUShadowType GetShadowLightType(Light l)
        {
            return m_shadowLightType(l);
        }

        public void SetShadowLightTypeDelegate(ShadowLightTypeDelegate del)
        {
            m_shadowLightType = del;
        }

        public void Register(GPUShadowType type, ShadowPrecision precision, ShadowAlgorithm algorithm, string algorithmDescriptor, ShadowVariant[] variants, string[] variantDescriptors, VariantDelegate[] variantDelegates)
        {
            if (Validate(algorithmDescriptor, variants, variantDescriptors, variantDelegates))
                Register(m_Entries[(int)type], precision, algorithm, algorithmDescriptor, variants, variantDescriptors, variantDelegates);
        }

        private bool Validate(string algorithmDescriptor, ShadowVariant[] variants, string[] variantDescriptors, VariantDelegate[] variantDelegates)
        {
            if (string.IsNullOrEmpty(algorithmDescriptor))
            {
                Debug.LogError("Tried to register a shadow algorithm but the algorithm descriptor is empty.");
                return false;
            }
            if (variantDescriptors == null || variantDescriptors.Length == 0)
            {
                Debug.LogError("Tried to register a shadow algorithm (" + algorithmDescriptor + ") but the variant descriptors are empty. At least one variant descriptor is required for registration.");
                return false;
            }
            if (variantDescriptors.Length > ShadowConstants.Counts.k_ShadowVariant)
            {
                Debug.LogError("Tried to register a shadow algorithm (" + algorithmDescriptor + ") with more than the valid amount of variants. Variant count: " + variantDescriptors.Length + ", valid count: " + ShadowConstants.Counts.k_ShadowVariant + ".");
                return false;
            }
            if (variantDescriptors.Length != variants.Length)
            {
                Debug.LogError("Tried to register a shadow algorithm (" + algorithmDescriptor + ") but the length of variant descriptors (" + variantDescriptors.Length + ") does not match the length of variants (" + variants.Length + ").");
                return false;
            }
            if (variantDelegates.Length != variants.Length)
            {
                Debug.LogError("Tried to register a shadow algorithm (" + algorithmDescriptor + ") but the length of variant delegates (" + variantDelegates.Length + ") does not match the length of variants (" + variants.Length + ").");
                return false;
            }
            return true;
        }

        private void Register(Dictionary<ShadowAlgorithm, Entry> dict, ShadowPrecision precision, ShadowAlgorithm algorithm, string algorithmDescriptor, ShadowVariant[] variants, string[] variantDescriptors, VariantDelegate[] variantDelegates)
        {
            if (!dict.ContainsKey(algorithm))
            {
                Entry e;
                e.algorithmDesc     = algorithmDescriptor;
                e.variantsAvailable = variants.Length;
                e.variantDescs      = new string[ShadowConstants.Counts.k_ShadowVariant];
                e.variantDels       = new Dels[ShadowConstants.Counts.k_ShadowVariant];
                for (uint i = 0, cnt = (uint)variants.Length; i < cnt; ++i)
                {
                    e.variantDescs[(uint)variants[i]] = variantDescriptors[i];
                    if (precision == ShadowPrecision.Low)
                        e.variantDels[(uint)variants[i]].low = variantDelegates[i];
                    else
                        e.variantDels[(uint)variants[i]].high = variantDelegates[i];
                }
                dict.Add(algorithm, e);
            }
            else
            {
                var entry = dict[algorithm];
                for (uint i = 0, cnt = (uint)variants.Length; i < cnt; ++i)
                {
                    if (string.IsNullOrEmpty(entry.variantDescs[(uint)variants[i]]))
                    {
                        entry.variantsAvailable++;
                        entry.variantDescs[(uint)variants[i]]     = variantDescriptors[i];
                        entry.variantDels[(uint)variants[i]].low  = precision == ShadowPrecision.Low  ? variantDelegates[i] : null;
                        entry.variantDels[(uint)variants[i]].high = precision == ShadowPrecision.High ? variantDelegates[i] : null;
                    }
                    else if (precision == ShadowPrecision.Low && entry.variantDels[(uint)variants[i]].low == null)
                    {
                        entry.variantDels[(uint)variants[i]].low = variantDelegates[i];
                    }
                    else if (precision == ShadowPrecision.High && entry.variantDels[(uint)variants[i]].high == null)
                    {
                        entry.variantDels[(uint)variants[i]].high = variantDelegates[i];
                    }
                    else
                        Debug.Log("Tried to register variant " + variants[i] + " for algorithm " + algorithm + " with precision " + precision + ", but this variant is already registered. Skipping registration.");
                }
            }
        }

        public void Draw(Light l)
        {
            AdditionalShadowData asd = l.GetComponent<AdditionalShadowData>();
            Debug.Assert(asd != null, "Light has no valid AdditionalShadowData component attached.");

            GPUShadowType shadowType = GetShadowLightType(l);

            // check if this has supported shadows
            if ((int)shadowType >= ShadowConstants.Counts.k_GPUShadowType)
                return;

            int shadowAlgorithm;
            int shadowVariant;
            int shadowPrecision;
            bool globalOverride = m_GlobalOverrides[(int)shadowType].enabled;

            if (globalOverride)
            {
                shadowAlgorithm = (int)m_GlobalOverrides[(int)shadowType].algorithm;
                shadowVariant   = (int)m_GlobalOverrides[(int)shadowType].variant;
                shadowPrecision = (int)m_GlobalOverrides[(int)shadowType].precision;
            }
            else
                asd.GetShadowAlgorithm(out shadowAlgorithm, out shadowVariant, out shadowPrecision);

            DrawWidgets(l, shadowType, (ShadowAlgorithm)shadowAlgorithm, (ShadowVariant)shadowVariant, (ShadowPrecision)shadowPrecision, globalOverride);
        }

        void DrawWidgets(Light l, GPUShadowType shadowType, ShadowAlgorithm shadowAlgorithm, ShadowVariant shadowVariant, ShadowPrecision shadowPrecision, bool globalOverride)
        {
#if UNITY_EDITOR
            var          dict           = m_Entries[(int)shadowType];
            int[]        algoOptions    = new int[dict.Count];
            GUIContent[] algoDescs      = new GUIContent[dict.Count];
            int idx = 0;

            foreach (var entry in dict)
            {
                algoOptions[idx] = (int)entry.Key;
                algoDescs[idx]   = new GUIContent(entry.Value.algorithmDesc);
                idx++;
            }

            using (new UnityEditor.EditorGUI.DisabledGroupScope(globalOverride))
            {
                UnityEditor.EditorGUI.BeginChangeCheck();
                shadowAlgorithm = (ShadowAlgorithm)UnityEditor.EditorGUILayout.IntPopup(new GUIContent("Shadow Algorithm"), (int)shadowAlgorithm, algoDescs, algoOptions);
                if (UnityEditor.EditorGUI.EndChangeCheck())
                    shadowVariant = 0;
            }

            UnityEditor.EditorGUI.indentLevel++;
            Entry e = dict[shadowAlgorithm];

            int          varsAvailable  = e.variantsAvailable;
            int[]        varOptions     = new int[varsAvailable];
            GUIContent[] varDescs       = new GUIContent[varsAvailable];

            idx = 0;
            for (int writeIdx = 0; writeIdx < varsAvailable; idx++)
            {
                if (e.variantDels[idx].low != null || e.variantDels[idx].high != null)
                {
                    varOptions[writeIdx] = idx;
                    varDescs[writeIdx] = new GUIContent(e.variantDescs[idx]);
                    writeIdx++;
                }
            }

            UnityEditor.EditorGUILayout.BeginHorizontal();

            using (new UnityEditor.EditorGUI.DisabledGroupScope(globalOverride))
            {
                shadowVariant = (ShadowVariant)UnityEditor.EditorGUILayout.IntPopup(new GUIContent("Variant + Precision"), (int)shadowVariant, varDescs, varOptions);

                if (e.variantDels[(int)shadowVariant].low != null && e.variantDels[(int)shadowVariant].high != null)
                {
                    GUIContent[] precDescs   = new GUIContent[] { new GUIContent("High"), new GUIContent("Low") };
                    int[]        precOptions = new int[] { 0, 1 };
                    shadowPrecision = (ShadowPrecision)UnityEditor.EditorGUILayout.IntPopup((int)shadowPrecision, precDescs, precOptions, GUILayout.MaxWidth(65));
                }
                else
                {
                    using (new UnityEditor.EditorGUI.DisabledScope())
                    {
                        GUIContent[] precDescs   = new GUIContent[] { new GUIContent(e.variantDels[(int)shadowVariant].low == null ? "High" : "Low") };
                        int[]        precOptions = new int[] { e.variantDels[(int)shadowVariant].low == null ? 0 : 1 };
                        UnityEditor.EditorGUILayout.IntPopup(precOptions[0], precDescs, precOptions, GUILayout.MaxWidth(65));
                        shadowPrecision = (ShadowPrecision)precOptions[0];
                    }
                }
            }

            AdditionalShadowData asd = l.GetComponent<AdditionalShadowData>();
            GPUShadowAlgorithm packedAlgo = ShadowUtils.Pack(shadowAlgorithm, shadowVariant, shadowPrecision);
            int[] shadowData = null;
            if (!GUILayout.Button("Reset", GUILayout.MaxWidth(80.0f)))
                shadowData = asd.GetShadowData((int)packedAlgo);

            UnityEditor.EditorGUILayout.EndHorizontal();

            if (shadowPrecision == ShadowPrecision.Low)
                e.variantDels[(int)shadowVariant].low(l, shadowAlgorithm, shadowVariant, shadowPrecision, ref shadowData);
            else
                e.variantDels[(int)shadowVariant].high(l, shadowAlgorithm, shadowVariant, shadowPrecision, ref shadowData);
            asd.SetShadowAlgorithm((int)shadowAlgorithm, (int)shadowVariant, (int)shadowPrecision, (int)packedAlgo, shadowData);

            UnityEditor.EditorGUI.indentLevel--;
#endif
        }

        public void SetGlobalShadowOverride(GPUShadowType shadowType, ShadowAlgorithm shadowAlgorithm, ShadowVariant shadowVariant, ShadowPrecision shadowPrecision, bool enable)
        {
            m_GlobalOverrides[(int)shadowType].enabled   = enable;
            m_GlobalOverrides[(int)shadowType].algorithm = shadowAlgorithm;
            m_GlobalOverrides[(int)shadowType].variant   = shadowVariant;
            m_GlobalOverrides[(int)shadowType].precision = shadowPrecision;
        }

        void SetGlobalOverrideEnabled(GPUShadowType shadowType, bool enabled)
        {
            m_GlobalOverrides[(int)shadowType].enabled = enabled;
        }

        public bool GetGlobalShadowOverride(GPUShadowType shadowType, ref GPUShadowAlgorithm algo)
        {
            if (m_GlobalOverrides[(int)shadowType].enabled)
                algo = ShadowUtils.Pack(m_GlobalOverrides[(int)shadowType].algorithm, m_GlobalOverrides[(int)shadowType].variant, m_GlobalOverrides[(int)shadowType].precision);

            return m_GlobalOverrides[(int)shadowType].enabled;
        }
    }

    // This is the struct passed into shaders
    [GenerateHLSL]
    public struct ShadowData
    {
        // shadow texture related params (need to be set by ShadowmapBase and derivatives)
        public Vector4       proj;           // projection matrix value _00, _11, _22, _23
        public Vector3       pos;            // view matrix light position
        public Vector3       rot0;           // first column of view matrix rotation
        public Vector3       rot1;           // second column of view matrix rotation
        public Vector3       rot2;           // third column of view matrix rotation
        public Vector4       scaleOffset;    // scale and offset of shadowmap in atlas
        public Vector4       textureSize;    // the shadowmap's size in x and y. xy is texture relative, zw is viewport relative.
        public Vector4       texelSizeRcp;   // reciprocal of the shadowmap's texel size in x and y. xy is texture relative, zw is viewport relative.
        public uint          id;             // packed texture id, sampler id and slice idx
        public uint          shadowType;     // determines the shadow algorithm, i.e. which map to sample and how to interpret the data
        public uint          payloadOffset;  // if this shadow type requires additional data it can be fetched from a global Buffer<uint> at payloadOffset.
        public float         slice;          // shadowmap slice
        public Vector4       viewBias;       // x = min, y = max, z = scale, w = shadowmap texel size in world space at distance 1 from light
        public Vector4       normalBias;     // x = min, y = max, z = scale, w = enable/disable sample biasing
        public float         edgeTolerance;  // specifies the offset along either the normal or view vector used for calculating the edge leak fixup
        public Vector3       _pad;           // 16 byte padding
        public Matrix4x4     shadowToWorld;  // from light space matrix

        public void PackShadowmapId(uint texIdx, uint sampIdx)
        {
            Debug.Assert(texIdx  <= 0xff);
            Debug.Assert(sampIdx <= 0xff);
            id = texIdx << 24 | sampIdx << 16;
        }

        public void UnpackShadowmapId(out uint texIdx, out uint sampIdx)
        {
            texIdx = (id >> 24) & 0xff;
            sampIdx = (id >> 16) & 0xff;
        }

        public void PackShadowType(GPUShadowType type, GPUShadowAlgorithm algorithm)
        {
            shadowType = (uint)type << ShadowConstants.Bits.k_GPUShadowAlgorithm | (uint)algorithm;
        }
    };

    public struct FrameId
    {
        public int   frameCount;
        public float deltaT;
    }

    // -------------- Begin temporary structs that need to be replaced at some point ---------------
    public struct SamplerState
    {
#pragma warning disable 414 // CS0414 The private field '...' is assigned but its value is never used
        FilterMode      filterMode;
        TextureWrapMode wrapMode;
        uint            anisotropy;
#pragma warning restore 414

        public static SamplerState Default()
        {
            SamplerState defaultState;
            defaultState.filterMode = FilterMode.Bilinear;
            defaultState.wrapMode   = TextureWrapMode.Clamp;
            defaultState.anisotropy = 1;
            return defaultState;
        }

        // TODO: this should either contain the description for a sampler, or be replaced by a struct that does
        public static bool operator==(SamplerState lhs, SamplerState rhs)
        {
            return false; // TODO: Remove this once shared samplers are in
            //return lhs.filterMode == rhs.filterMode && lhs.wrapMode == rhs.wrapMode && lhs.anisotropy == rhs.anisotropy;
        }

        public static bool operator!=(SamplerState lhs, SamplerState rhs) { return !(lhs == rhs); }
        public override bool Equals(object obj) { return (obj is SamplerState) && (SamplerState)obj == this; }
        public override int GetHashCode() { /* TODO: implement this at some point */ throw new NotImplementedException(); }
    }

    public struct ComparisonSamplerState
    {
#pragma warning disable 414 // CS0414 The private field '...' is assigned but its value is never used
        FilterMode      filterMode;
        TextureWrapMode wrapMode;
        uint            anisotropy;
#pragma warning restore 414

        public static ComparisonSamplerState Default()
        {
            ComparisonSamplerState defaultState;
            defaultState.filterMode = FilterMode.Bilinear;
            defaultState.wrapMode   = TextureWrapMode.Clamp;
            defaultState.anisotropy = 1;
            return defaultState;
        }

        // TODO: this should either contain the description for a comparison sampler, or be replaced by a struct that does
        public static bool operator==(ComparisonSamplerState lhs, ComparisonSamplerState rhs)
        {
            return false;
            //return lhs.filterMode == rhs.filterMode && lhs.wrapMode == rhs.wrapMode && lhs.anisotropy == rhs.anisotropy;
        }

        public static bool operator!=(ComparisonSamplerState lhs, ComparisonSamplerState rhs) { return !(lhs == rhs); }
        public override bool Equals(object obj) { return (obj is ComparisonSamplerState) && (ComparisonSamplerState)obj == this; }
        public override int GetHashCode() { /* TODO: implement this at some point */ throw new NotImplementedException(); }
    }
    // -------------- End temporary structs that need to be replaced at some point ---------------

    // Helper struct for passing arbitrary data into shaders. Entry size is 4 ints to minimize load instructions
    // in shaders. This should really be int p[4] but C# doesn't let us do that.
    public struct ShadowPayload
    {
        public int p0;
        public int p1;
        public int p2;
        public int p3;

        public void Set(float v0, float v1, float v2, float v3)
        {
            p0 = ShadowUtils.Asint(v0);
            p1 = ShadowUtils.Asint(v1);
            p2 = ShadowUtils.Asint(v2);
            p3 = ShadowUtils.Asint(v3);
        }

        public void Set(int v0, int v1, int v2, int v3)
        {
            p0 = v0;
            p1 = v1;
            p2 = v2;
            p3 = v3;
        }

        public void Set(Vector4 v) { Set(v.x, v.y, v.z, v.w); }
    }

    // Class holding resource information that needs to be synchronized with shaders.
    public class ShadowContextStorage
    {
        public struct Init
        {
            public uint maxShadowDataSlots;
            public uint maxPayloadSlots;
            public uint maxTex2DArraySlots;
            public uint maxTexCubeArraySlots;
            public uint maxComparisonSamplerSlots;
            public uint maxSamplerSlots;
        }
        protected ShadowContextStorage(ref Init initializer)
        {
            m_ShadowDatas.Reserve(initializer.maxShadowDataSlots);
            m_Payloads.Reserve(initializer.maxPayloadSlots);
            m_Tex2DArray.Reserve(initializer.maxTex2DArraySlots);
            m_TexCubeArray.Reserve(initializer.maxTexCubeArraySlots);
            m_CompSamplers.Reserve(initializer.maxComparisonSamplerSlots);
            m_Samplers.Reserve(initializer.maxSamplerSlots);
        }

        // query functions to be used by the shadowmap
        public uint RequestTex2DArraySlot() { return m_Tex2DArray.Add(new RenderTargetIdentifier()); }
        public uint RequestTexCubeArraySlot() { return m_TexCubeArray.Add(new RenderTargetIdentifier()); }
        public uint RequestSamplerSlot(SamplerState ss)
        {
            uint idx;
            if (m_Samplers.FindFirst(out idx, ref ss))
                return idx;
            idx = m_Samplers.Count();
            m_Samplers.Add(ss);
            return idx;
        }

        public uint RequestSamplerSlot(ComparisonSamplerState css)
        {
            uint idx;
            if (m_CompSamplers.FindFirst(out idx, ref css))
                return idx;
            idx = m_CompSamplers.Count();
            m_CompSamplers.Add(css);
            return idx;
        }

        // setters called each frame on the shadowmap
        public void SetTex2DArraySlot(uint slot, RenderTargetIdentifier val)      { m_Tex2DArray[slot] = val; }
        public void SetTexCubeArraySlot(uint slot, RenderTargetIdentifier val)  { m_TexCubeArray[slot] = val; }

        protected VectorArray<ShadowData>             m_ShadowDatas   = new VectorArray<ShadowData>(0, false);
        protected VectorArray<ShadowPayload>          m_Payloads      = new VectorArray<ShadowPayload>(0, false);
        protected VectorArray<RenderTargetIdentifier> m_Tex2DArray    = new VectorArray<RenderTargetIdentifier>(0, true);
        protected VectorArray<RenderTargetIdentifier> m_TexCubeArray  = new VectorArray<RenderTargetIdentifier>(0, true);
        protected VectorArray<ComparisonSamplerState> m_CompSamplers  = new VectorArray<ComparisonSamplerState>(0, true);
        protected VectorArray<SamplerState>           m_Samplers      = new VectorArray<SamplerState>(0, true);
    }

    // Class providing hooks to do the actual synchronization
    public class ShadowContext : ShadowContextStorage
    {
        public delegate void SyncDel(ShadowContext sc);
        public delegate void BindDel(ShadowContext sc, CommandBuffer cb, ComputeShader computeShader, int computeKernel);
        public struct CtxtInit
        {
            public Init     storage;
            public SyncDel  dataSyncer;
            public BindDel  resourceBinder;
        }
        public ShadowContext(ref CtxtInit initializer) : base(ref initializer.storage)
        {
            Debug.Assert(initializer.dataSyncer != null && initializer.resourceBinder != null);
            m_DataSyncerDel = initializer.dataSyncer;
            m_ResourceBinderDel = initializer.resourceBinder;
        }

        public void ClearData() { m_ShadowDatas.Reset(); m_Payloads.Reset(); }
        // delegate that takes care of syncing data to the GPU
        public void SyncData() { m_DataSyncerDel(this); }
        // delegate that takes care of binding textures, buffers and samplers to shaders just before rendering
        public void BindResources(CommandBuffer cb, ComputeShader computeShader, int computeKernel) { m_ResourceBinderDel(this, cb, computeShader, computeKernel); }

        // the following functions are to be used by the bind and sync delegates
        public void GetShadowDatas(out ShadowData[] shadowDatas, out uint offset, out uint count)                           { shadowDatas   = m_ShadowDatas.AsArray(out offset, out count); }
        public void GetPayloads(out ShadowPayload[] payloads, out uint offset, out uint count)                              { payloads      = m_Payloads.AsArray(out offset, out count); }
        public void GetTex2DArrays(out RenderTargetIdentifier[] tex2DArrays, out uint offset, out uint count)               { tex2DArrays   = m_Tex2DArray.AsArray(out offset, out count); }
        public void GetTexCubeArrays(out RenderTargetIdentifier[] texCubeArrays, out uint offset, out uint count)           { texCubeArrays = m_TexCubeArray.AsArray(out offset, out count); }
        public void GetComparisonSamplerArrays(out ComparisonSamplerState[] compSamplers, out uint offset, out uint count)  { compSamplers  = m_CompSamplers.AsArray(out offset, out count); }
        public void GetSamplerArrays(out SamplerState[] samplerArrays, out uint offset, out uint count)                     { samplerArrays = m_Samplers.AsArray(out offset, out count); }

        private SyncDel m_DataSyncerDel;
        private BindDel m_ResourceBinderDel;
    }

    // Abstract base class for handling shadow maps.
    // Specific implementations managing atlases and the likes should inherit from this
    abstract public class ShadowmapBase
    {
        [Flags]
        public enum ShadowSupport
        {
            Point       = 1 << GPUShadowType.Point,
            Spot        = 1 << GPUShadowType.Spot,
            Directional = 1 << GPUShadowType.Directional
        }

        public struct ShadowRequest
        {
            private const byte k_IndexBits    = 24;
            private const byte k_FaceBits     = 32 - k_IndexBits;
            private const uint k_MaxIndex     = (1 << k_IndexBits) - 1;
            private const byte k_MaxFace      = (1 << k_FaceBits) - 1;
            public  const int  k_MaxFaceCount = k_FaceBits;

            // combined face mask and visible light index
            private uint m_MaskIndex;
            private  int m_ShadowTypeAndAlgorithm;
            // instance Id for this light
            public int   instanceId { get; set; }
            // shadow type of this light
            public GPUShadowType shadowType
            {
                get { return (GPUShadowType)(m_ShadowTypeAndAlgorithm >> ShadowConstants.Bits.k_GPUShadowAlgorithm); }
                set { m_ShadowTypeAndAlgorithm = ((int)value << ShadowConstants.Bits.k_GPUShadowAlgorithm) | (m_ShadowTypeAndAlgorithm & ShadowConstants.Masks.k_GPUShadowAlgorithm); }
            }
            public GPUShadowAlgorithm shadowAlgorithm
            {
                get { return (GPUShadowAlgorithm)(m_ShadowTypeAndAlgorithm & ShadowConstants.Masks.k_GPUShadowAlgorithm); }
                set { m_ShadowTypeAndAlgorithm = (m_ShadowTypeAndAlgorithm & ~(ShadowConstants.Masks.k_GPUShadowAlgorithm)) | (int)value; }
            }
            // index into the visible lights array
            public int index // use "int" and not "uint" as it is use to index inside List<>
            {
                get { return (int)(m_MaskIndex & k_MaxIndex); }
                set { m_MaskIndex = (uint)value & k_MaxIndex; }
            }
            // mask of which faces are requested:
            // - for spotlights the value is always 1
            // - for point lights the bit positions map to the faces as listed in the CubemapFace enum
            // - for directional lights the bit positions map to the individual cascades
            public uint facemask
            {
                get { return (m_MaskIndex >> k_IndexBits) & k_MaxFace; }
                set { m_MaskIndex = (m_MaskIndex & k_MaxIndex) | (value << k_IndexBits); }
            }
            public uint facecount
            {
                get
                {
                    uint fc = facemask;
                    uint count = 0;
                    while (fc != 0)
                    {
                        count += fc & 1;
                        fc >>= 1;
                    }
                    return count;
                }
            }
        }


        protected readonly uint                     m_Width;
        protected readonly uint                     m_Height;
        protected readonly uint                     m_Slices;
        protected readonly uint                     m_ShadowmapBits;
        protected readonly RenderTextureFormat      m_ShadowmapFormat;
        protected readonly SamplerState             m_SamplerState;
        protected readonly ComparisonSamplerState   m_CompSamplerState;
        protected readonly Vector4                  m_ClearColor;
        protected readonly float                    m_WidthRcp;
        protected readonly float                    m_HeightRcp;
        protected readonly uint                     m_MaxPayloadCount;
        protected readonly ShadowSupport            m_ShadowSupport;
        protected          CullResults              m_CullResults; // TODO: Temporary, due to CullResults dependency in ShadowUtils' matrix extraction code. Remove this member once that dependency is gone.


        public uint width { get { return m_Width; } }
        public uint height { get { return m_Height; } }
        public uint slices { get { return m_Slices; } }


        public struct BaseInit
        {
            public uint                     width;                      // width of the shadowmap
            public uint                     height;                     // height of the shadowmap
            public uint                     slices;                     // slices for the shadowmap
            public uint                     shadowmapBits;              // bit depth for native shadowmaps, or bitdepth for the temporary shadowmap if the shadowmapFormat is not native
            public RenderTextureFormat      shadowmapFormat;            // texture format of the shadowmap
            public SamplerState             samplerState;               // the desired sampler state for non-native shadowmaps
            public ComparisonSamplerState   comparisonSamplerState;     // the desired sampler state for native shadowmaps doing depth comparisons as well
            public Vector4                  clearColor;                 // the clear color used for non-native shadowmaps
            public uint                     maxPayloadCount;            // how many ints will be pushed into the payload buffer for each invocation of Reserve
            public ShadowSupport            shadowSupport;              // bitmask of all shadow types that this shadowmap supports
        };

        protected ShadowmapBase(ref BaseInit initializer)
        {
            m_Width             = initializer.width;
            m_Height            = initializer.height;
            m_Slices            = initializer.slices;
            m_ShadowmapBits     = initializer.shadowmapBits;
            m_ShadowmapFormat   = initializer.shadowmapFormat;
            m_SamplerState      = initializer.samplerState;
            m_CompSamplerState  = initializer.comparisonSamplerState;
            m_ClearColor        = initializer.clearColor;
            m_WidthRcp          = 1.0f / initializer.width;
            m_HeightRcp         = 1.0f / initializer.height;
            m_MaxPayloadCount   = initializer.maxPayloadCount;
            m_ShadowSupport     = initializer.shadowSupport;
        }

        protected bool IsNativeDepth()
        {
            return m_ShadowmapFormat == RenderTextureFormat.Shadowmap || m_ShadowmapFormat == RenderTextureFormat.Depth;
        }

        public void Register(ShadowRegistry registry)
        {
            int bit = 1;
            for (GPUShadowType i = GPUShadowType.Point; i < GPUShadowType.MAX; ++i, bit <<= 1)
            {
                if (((int)m_ShadowSupport & bit) != 0)
                    Register(i, registry);
            }
        }

        public ShadowSupport QueryShadowSupport() { return m_ShadowSupport; }
        public uint GetMaxPayload() { return m_MaxPayloadCount; }
        public void Assign(CullResults cullResults) { m_CullResults = cullResults; }            // TODO: Remove when m_CullResults is removed again
        abstract public bool Reserve(FrameId frameId, Camera camera, bool cameraRelativeRendering, ref ShadowData shadowData, ShadowRequest sr, uint width, uint height, ref VectorArray<ShadowData> entries, ref VectorArray<ShadowPayload> payloads, List<VisibleLight> lights);
        abstract public bool Reserve(FrameId frameId, Camera camera, bool cameraRelativeRendering, ref ShadowData shadowData, ShadowRequest sr, uint[] widths, uint[] heights, ref VectorArray<ShadowData> entries, ref VectorArray<ShadowPayload> payloads, List<VisibleLight> lights);
        abstract public bool ReserveFinalize(FrameId frameId, ref VectorArray<ShadowData> entries, ref VectorArray<ShadowPayload> payloads);
        abstract public void Update(FrameId frameId, ScriptableRenderContext renderContext, CommandBuffer cmd, CullResults cullResults, List<VisibleLight> lights);
        abstract public void ReserveSlots(ShadowContextStorage sc);
        abstract public void Fill(ShadowContextStorage cs);
        abstract public void CreateShadowmap();
        abstract protected void Register(GPUShadowType type, ShadowRegistry registry);
        abstract public void DisplayShadowMap(CommandBuffer cmd, Material debugMaterial, Vector4 scaleBias, uint slice, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, bool flipY);
    }

    public interface IShadowManager
    {
        // Warning: The shadowRequests array and shadowRequestsCount are modified by this function.
        //          When called the array contains the indices of lights requesting shadows,
        //          upon returning the array contains up to shadowRequestsCount valid shadow caster indices,
        //          whereas [shadowRequestsCount;originalRequestsCount) will hold all indices for lights that wanted to cast a shadow but got rejected.
        //          shadowDataIndices contains the offset into the shadowDatas array only for each shadow casting light, e.g. lights[shadowRequests[i]].shadowDataOffset = shadowDataIndices[i];
        //          shadowDatas contains shadowmap related basic parameters that can be passed to the shader.
        //          shadowPayloads contains implementation specific data that is accessed from the shader by indexing into an Buffer<int> using ShadowData.ShadowmapData.payloadOffset.
        //          This is the equivalent of a void pointer in the shader and there needs to be loader code that knows how to interpret the data.
        //          If there are no valid shadow casters all output arrays will be null, otherwise they will contain valid data that can be passed to shaders.
        void ProcessShadowRequests(FrameId frameId, CullResults cullResults, Camera camera, bool cameraRelativeRendering, List<VisibleLight> lights, ref uint shadowRequestsCount, int[] shadowRequests, out int[] shadowDataIndices);
        // Renders all shadows for lights the were deemed shadow casters after the last call to ProcessShadowRequests
        void RenderShadows(FrameId frameId, ScriptableRenderContext renderContext, CommandBuffer cmd, CullResults cullResults, List<VisibleLight> lights);
        // Debug function to display a shadow at the screen coordinate
        void DisplayShadow(CommandBuffer cmd, Material debugMaterial, int shadowIndex, uint faceIndex, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, bool flipY);
        void DisplayShadowMap(CommandBuffer cmd, Material debugMaterial, uint shadowMapIndex, uint sliceIndex, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, bool flipY);
        // Synchronize data with GPU buffers
        void SyncData();
        // Binds resources to shader stages just before rendering the lighting pass
        void BindResources(CommandBuffer cmd, ComputeShader computeShader, int computeKernel);
        // Fixes up some parameters within the cullResults
        void UpdateCullingParameters(ref ScriptableCullingParameters cullingParams);

        uint GetShadowMapCount();
        uint GetShadowMapSliceCount(uint shadowMapIndex);

        uint GetShadowRequestCount();

        uint GetShadowRequestFaceCount(uint requestIndex);

        int GetShadowRequestIndex(Light light);
    }

    abstract public class ShadowManagerBase : ShadowRegistry, IShadowManager
    {
        public  abstract void ProcessShadowRequests(FrameId frameId, CullResults cullResults, Camera camera, bool cameraRelativeRendering, List<VisibleLight> lights, ref uint shadowRequestsCount, int[] shadowRequests, out int[] shadowDataIndices);
        public  abstract void RenderShadows(FrameId frameId, ScriptableRenderContext renderContext, CommandBuffer cmd, CullResults cullResults, List<VisibleLight> lights);
        public  abstract void DisplayShadow(CommandBuffer cmd, Material debugMaterial, int shadowIndex, uint faceIndex, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, bool flipY);
        public  abstract void DisplayShadowMap(CommandBuffer cmd, Material debugMaterial, uint shadowMapIndex, uint sliceIndex, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, bool flipY);
        public  abstract void SyncData();
        public  abstract void BindResources(CommandBuffer cmd, ComputeShader computeShader, int computeKernel);
        public  abstract void UpdateCullingParameters(ref ScriptableCullingParameters cullingParams);
        // sort the shadow requests in descending priority - may only modify shadowRequests
        protected abstract void PrioritizeShadowCasters(Camera camera, List<VisibleLight> lights, uint shadowRequestsCount, int[] shadowRequests);
        // prune the shadow requests - may modify shadowRequests and shadowsCountshadowRequestsCount
        protected abstract void PruneShadowCasters(Camera camera, List<VisibleLight> lights, ref VectorArray<int> shadowRequests, ref VectorArray<ShadowmapBase.ShadowRequest> requestsGranted, out uint totalRequestCount);
        // allocate the shadow requests in the shadow map, only is called if shadowsCount > 0 - may modify shadowRequests and shadowsCount
        protected abstract bool AllocateShadows(FrameId frameId, Camera camera, bool cameraRelativeRendering, List<VisibleLight> lights, uint totalGranted, ref VectorArray<ShadowmapBase.ShadowRequest> grantedRequests, ref VectorArray<int> shadowIndices, ref VectorArray<ShadowData> shadowmapDatas, ref VectorArray<ShadowPayload> shadowmapPayload);

        public abstract uint GetShadowMapCount();
        public abstract uint GetShadowMapSliceCount(uint shadowMapIndex);
        public abstract uint GetShadowRequestCount();
        public abstract uint GetShadowRequestFaceCount(uint requestIndex);
        public abstract int GetShadowRequestIndex(Light light);
    }
} // end of namespace UnityEngine.Experimental.ScriptableRenderLoop
