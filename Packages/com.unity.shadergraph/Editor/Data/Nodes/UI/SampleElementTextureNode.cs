using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.Rendering.UITK.ShaderGraph;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    enum ElementTextureMipSamplingMode
    {
        Standard,
        LOD
    }

    [Title("UI", "Sample Element Texture")]
    [SubTargetFilter(typeof(IUISubTarget))]
    class SampleElementTextureNode : AbstractMaterialNode, IGeneratesFunction, IGeneratesBodyCode, IMayRequireUITK
    {
        const int kMinSampleCount = 1;
        const int kMaxSampleCount = 32;

        // Fixed-id inputs that don't scale with the sample count.
        public const int LODInputId = 4;
        public const int SamplerInputId = 5;

        const string kLODSlotName = "LOD";
        const string kSamplerSlotName = "Sampler";

        // The first 4 UV/Color slots keep their original ids so graphs authored before the count became
        // configurable (which always had 4 samples) keep their connections; extra slots use disjoint ranges.
        static int UVSlotId(int i) => i < 4 ? i : 1000 + i;
        static int ColorSlotId(int i) => i < 4 ? 10 + i : 2000 + i;

        public override bool hasPreview { get { return false; } }

        public SampleElementTextureNode()
        {
            name = "Sample Element Texture";
            synonyms = new string[] { };
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        private int m_SampleCount = 4;
        internal int sampleCount
        {
            get { return Mathf.Clamp(m_SampleCount, kMinSampleCount, kMaxSampleCount); }
            set
            {
                int clamped = Mathf.Clamp(value, kMinSampleCount, kMaxSampleCount);
                if (m_SampleCount == clamped)
                    return;
                m_SampleCount = clamped;
                UpdateNodeAfterDeserialization();
            }
        }

        [SerializeField]
        private ElementTextureMipSamplingMode m_MipSamplingMode = ElementTextureMipSamplingMode.Standard;
        internal ElementTextureMipSamplingMode mipSamplingMode
        {
            set { m_MipSamplingMode = value; UpdateNodeAfterDeserialization(); }
            get { return m_MipSamplingMode; }
        }

        public override void UpdateNodeAfterDeserialization()
        {
            int count = sampleCount;

            var ids = new List<int>();
            for (int i = 0; i < count; i++)
            {
                AddSlot(new Vector2MaterialSlot(UVSlotId(i), $"UV {i}", $"UV {i}", SlotType.Input, Vector2.zero));
                ids.Add(UVSlotId(i));
            }

            // Optional sampler override. When left unconnected the node, element texture's own sampler is used.
            AddSlot(new SamplerStateMaterialSlot(SamplerInputId, kSamplerSlotName, kSamplerSlotName, SlotType.Input));
            ids.Add(SamplerInputId);

            bool lod = m_MipSamplingMode == ElementTextureMipSamplingMode.LOD;
            if (lod)
            {
                AddSlot(new Vector1MaterialSlot(LODInputId, kLODSlotName, kLODSlotName, SlotType.Input, 0.0f));
                ids.Add(LODInputId);
            }

            for (int i = 0; i < count; i++)
            {
                AddSlot(new Vector4MaterialSlot(ColorSlotId(i), $"Color {i}", $"Color {i}", SlotType.Output, Vector4.zero));
                ids.Add(ColorSlotId(i));
            }

            // Lowering the count legitimately drops slots; suppress the "Removing Invalid MaterialSlot" warning.
            RemoveSlotsNameNotMatching(ids, supressWarnings: true);

            SetSlotOrder(ids);
        }

        // Per-sample expression used inside the dispatch macro. _Texture##index resolves to the element
        // texture slot picked by UIE_BRANCH; s/lod reference the function parameters when present.
        static string SampleExpression(int i, bool lod, bool hasSampler)
        {
            if (hasSampler)
                return lod
                    ? $"SAMPLE_TEXTURE2D_LOD(_Texture##index, s, uv{i}, lod)"
                    : $"SAMPLE_TEXTURE2D(_Texture##index, s, uv{i})";
            return lod
                ? $"UNITY_SAMPLE_TEX2D_LOD(_Texture##index, uv{i}, lod)"
                : $"UNITY_SAMPLE_TEX2D(_Texture##index, uv{i})";
        }

        // Name encodes the count and the sampling variant so graphs mixing configurations don't collide on
        // a single macro/function definition.
        static string FunctionName(int count, bool lod, bool hasSampler)
            => $"Unity_UIE_SampleElementTexture_{count}{(lod ? "_LOD" : "")}{(hasSampler ? "_S" : "")}";

        static string MacroName(int count, bool lod, bool hasSampler)
            => $"UIE_SAMPLE_{count}{(lod ? "_LOD" : "")}{(hasSampler ? "_S" : "")}";

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            int count = sampleCount;
            bool lod = m_MipSamplingMode == ElementTextureMipSamplingMode.LOD;
            bool hasSampler = IsSlotConnected(SamplerInputId);

            string fn = FunctionName(count, lod, hasSampler);
            string macro = MacroName(count, lod, hasSampler);

            registry.ProvideFunction(fn, sb =>
            {
                sb.AppendLine($"#define {macro}(index) \\");
                for (int i = 0; i < count; i++)
                {
                    string cont = (i < count - 1) ? " \\" : "";
                    sb.AppendLine($"    c{i} = {SampleExpression(i, lod, hasSampler)};{cont}");
                }
                sb.AppendLine("");

                var prms = new List<string> { "half index" };
                for (int i = 0; i < count; i++)
                    prms.Add($"float2 uv{i}");
                if (hasSampler)
                    prms.Add("SamplerState s");
                if (lod)
                    prms.Add("float lod");
                for (int i = 0; i < count; i++)
                    prms.Add($"out float4 c{i}");

                sb.AppendLine($"void {fn}({string.Join(", ", prms)})");
                using (sb.BlockScope())
                {
                    sb.AppendLine($"UIE_BRANCH({macro})");
                }
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            int count = sampleCount;

            if (generationMode == GenerationMode.Preview)
            {
                // In preview mode, return white
                for (int i = 0; i < count; i++)
                    sb.AppendLine("$precision4 {0} = $precision4(1, 1, 1, 1);", GetVariableNameForSlot(ColorSlotId(i)));
                return;
            }

            for (int i = 0; i < count; i++)
                sb.AppendLine("$precision4 {0};", GetVariableNameForSlot(ColorSlotId(i)));

            bool lod = m_MipSamplingMode == ElementTextureMipSamplingMode.LOD;
            bool hasSampler = IsSlotConnected(SamplerInputId);

            using (sb.BlockScope())
            {
                sb.AppendLine("half Unity_UIE_TextureSlotIndex = IN.typeTexSettings.y;");

                var args = new List<string> { "Unity_UIE_TextureSlotIndex" };
                for (int i = 0; i < count; i++)
                    args.Add(GetSlotValue(UVSlotId(i), generationMode));
                if (hasSampler)
                    args.Add($"{GetSlotValue(SamplerInputId, generationMode)}.samplerstate");
                if (lod)
                    args.Add(GetSlotValue(LODInputId, generationMode));
                for (int i = 0; i < count; i++)
                    args.Add(GetVariableNameForSlot(ColorSlotId(i)));

                sb.AppendLine($"{FunctionName(count, lod, hasSampler)}({string.Join(", ", args)});");

                sb.AppendLine("#if _UIE_FORCE_GAMMA");
                for (int i = 0; i < count; i++)
                {
                    string c = GetVariableNameForSlot(ColorSlotId(i));
                    sb.AppendLine("{0}.rgb = uie_linear_to_gamma({1}.rgb);", c, c);
                }
                sb.AppendLine("#endif");
            }
        }

        public bool RequiresUITK(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
