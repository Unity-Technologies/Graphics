using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    enum HashType
    {
        Original,
        Fastest,
        Deterministic
    };

    [Title("Procedural", "Noise", "Simple Noise")]
    class NoiseNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireMeshUV
    {
        // 0 original version
        // 1 add better looking / deterministic noise options
        public override int latestVersion => 1;

        public override IEnumerable<int> allowedNodeVersions => new int[] { 1 };

        const int UVSlotId = 0;
        const int ScaleSlotId = 1;
        const int OutSlotId = 2;
        const int PersistenceSlotId = 3;
        const int LacunaritySlotId = 4;
        const int BrightnessSlotId = 5;

        const string kUVSlotName = "UV";
        const string kScaleSlotName = "Scale";
        const string kOutSlotName = "Out";
        const string kPersistenceSlotName = "Persistence";
        const string kLacunaritySlotName = "Lacunarity";
        const string kBrightnessSlotName = "Brightness";

        public NoiseNode()
        {
            name = "Simple Noise";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview => true;

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new UVMaterialSlot(UVSlotId, kUVSlotName, kUVSlotName, UVChannel.UV0));
            AddSlot(new Vector1MaterialSlot(ScaleSlotId, kScaleSlotName, kScaleSlotName, SlotType.Input, 500.0f));
            AddSlot(new Vector1MaterialSlot(OutSlotId, kOutSlotName, kOutSlotName, SlotType.Output, 0.0f));
            AddSlot(new Vector1MaterialSlot(PersistenceSlotId, kPersistenceSlotName, kPersistenceSlotName, SlotType.Input, 2.0f));
            AddSlot(new Vector1MaterialSlot(LacunaritySlotId, kLacunaritySlotName, kLacunaritySlotName, SlotType.Input, 2.0f));
            AddSlot(new Vector1MaterialSlot(BrightnessSlotId, kBrightnessSlotName, kBrightnessSlotName, SlotType.Input, 0.875f));
            RemoveSlotsNameNotMatching(new[] { UVSlotId, ScaleSlotId, OutSlotId, LacunaritySlotId, PersistenceSlotId, BrightnessSlotId });
        }

        [SerializeField]
        private HashType m_HashType = HashType.Deterministic;

        [EnumControl("Hash Type")]
        public HashType hashType
        {
            get => m_HashType;
            set
            {
                if (m_HashType == value)
                    return;

                m_HashType = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        private int m_Octaves = 3;

        [IntegerControl("Octaves")]
        public int octaves
        {
            get => m_Octaves;
            set
            {
                if (m_Octaves == value)
                    return;

                if (value < 1)
                    value = 1;

                if (value > 16)
                    value = 16;

                m_Octaves = value;
                Dirty(ModificationScope.Graph);
            }
        }

        static readonly string[] kHashFunctionPrefix =
        {
            "Hash_LegacySine_2_1_",
            "Hash_BetterSine_2_1_",
            "Hash_Tchou_2_1_"
        };

        void IGeneratesFunction.GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.RequiresIncludePath("Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl");

            var hashTypeString = hashType.ToString();
            var HashFunction = kHashFunctionPrefix[(int) hashType];

            registry.ProvideFunction($"Unity_SimpleNoise_ValueNoise_{hashTypeString}_$precision", s =>
            {
                s.AppendLine($"$precision Unity_SimpleNoise_ValueNoise_{hashTypeString}_$precision ($precision2 uv)");
                using (s.BlockScope())
                {
                    s.AppendLine("$precision2 i = floor(uv);");
                    s.AppendLine("$precision2 f = frac(uv);");
                    s.AppendLine("f = f * f * (3.0 - 2.0 * f);");
                    s.AppendLine("uv = abs(frac(uv) - 0.5);");
                    s.AppendLine("$precision2 c0 = i + $precision2(0.0, 0.0);");
                    s.AppendLine("$precision2 c1 = i + $precision2(1.0, 0.0);");
                    s.AppendLine("$precision2 c2 = i + $precision2(0.0, 1.0);");
                    s.AppendLine("$precision2 c3 = i + $precision2(1.0, 1.0);");
                    s.AppendLine($"$precision r0; {HashFunction}$precision(c0, r0);");
                    s.AppendLine($"$precision r1; {HashFunction}$precision(c1, r1);");
                    s.AppendLine($"$precision r2; {HashFunction}$precision(c2, r2);");
                    s.AppendLine($"$precision r3; {HashFunction}$precision(c3, r3);");
                    s.AppendLine("$precision bottomOfGrid = lerp(r0, r1, f.x);");
                    s.AppendLine("$precision topOfGrid = lerp(r2, r3, f.x);");
                    s.AppendLine("$precision t = lerp(bottomOfGrid, topOfGrid, f.y);");
                    s.AppendLine("return t;");
                }
            });

            registry.ProvideFunction($"Unity_SimpleNoise{octaves}_" + hashTypeString + "_$precision", s =>
            {
                s.AppendLine($"void Unity_SimpleNoise{octaves}_{hashTypeString}_$precision($precision2 UV, $precision Scale, $precision Persistence, $precision Lacunarity, $precision Brightness, out $precision Out)");
                using (s.BlockScope())
                {
                    s.AppendLine("$precision freq = 1.0f;");
                    s.AppendLine("$precision amp = 1.0f;");
                    s.AppendLine("$precision total = 0.0f;");
                    s.AppendLine("UV.xy *= Scale;");
                    s.AppendLine("Out = 0.0f;");
                    for (int octave = 0; octave < octaves; octave++)
                    {
                        s.AppendLine($"Out += amp * Unity_SimpleNoise_ValueNoise_{hashTypeString}_$precision($precision2(UV.xy/freq));");
                        s.AppendLine("total += amp;");
                        s.AppendLine("freq = freq * Lacunarity;");   // 1.0, 2.0, 4.0      (Lacunarity = 2.0)
                        s.AppendLine("amp = amp * Persistence;");    // 0.125, 0.25, 0.5   (Persistence == 2.0)
                    }
                    s.AppendLine("Out = Out * Brightness / total;");
                }
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var hashTypeString = hashType.ToString();
            string uv = GetSlotValue(UVSlotId, generationMode);
            string scale = GetSlotValue(ScaleSlotId, generationMode);
            string lacunarity = GetSlotValue(LacunaritySlotId, generationMode);
            string persistence = GetSlotValue(PersistenceSlotId, generationMode);
            string brightness = GetSlotValue(BrightnessSlotId, generationMode);
            string output = GetVariableNameForSlot(OutSlotId);
            var outSlot = FindSlot<MaterialSlot>(OutSlotId);

            sb.AppendLine($"{outSlot.concreteValueType.ToShaderString(PrecisionUtil.Token)} {output};");
            sb.AppendLine($"Unity_SimpleNoise{octaves}_{hashTypeString}_$precision({uv}, {scale}, {persistence}, {lacunarity}, {brightness}, {output});");
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);
                var result = false;
                foreach (var slot in tempSlots)
                {
                    if (slot.RequiresMeshUV(channel))
                    {
                        result = true;
                        break;
                    }
                }

                tempSlots.Clear();
                return result;
            }
        }

        public override void OnAfterMultiDeserialize(string json)
        {
            if (sgVersion < 1)
            {
                // old nodes should select "Original" to replicate old behavior
                hashType = HashType.Original;
                ChangeVersion(1);
            }
        }
    }
}
