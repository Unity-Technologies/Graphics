using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Noise", "Simple Noise")]
    class NoiseNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireMeshUV
    {
        // 0 original version
        // 1 add deterministic noise option
        public override int latestVersion => 1;
        public override IEnumerable<int> allowedNodeVersions => new int[] { 1 };

        public const int UVSlotId = 0;
        public const int ScaleSlotId = 1;
        public const int OutSlotId = 2;

        const string kUVSlotName = "UV";
        const string kScaleSlotName = "Scale";
        const string kOutSlotName = "Out";

        public NoiseNode()
        {
            name = "Simple Noise";
            synonyms = new string[] { "value noise" };
            UpdateNodeAfterDeserialization();
        }

        public enum HashType
        {
            Deterministic,
            LegacySine,
        };
        static readonly string[] kHashFunctionPrefix =
        {
            "Hash_Tchou_2_1_",
            "Hash_LegacySine_2_1_",
        };

        public override bool hasPreview => true;

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new UVMaterialSlot(UVSlotId, kUVSlotName, kUVSlotName, UVChannel.UV0));
            AddSlot(new Vector1MaterialSlot(ScaleSlotId, kScaleSlotName, kScaleSlotName, SlotType.Input, 500.0f));
            AddSlot(new Vector1MaterialSlot(OutSlotId, kOutSlotName, kOutSlotName, SlotType.Output, 0.0f));
            RemoveSlotsNameNotMatching(new[] { UVSlotId, ScaleSlotId, OutSlotId });
        }

        [SerializeField]
        private HashType m_HashType = HashType.Deterministic;

        [EnumControl("Hash Type")]
        public HashType hashType
        {
            get
            {
                if (((int)m_HashType < 0) || ((int)m_HashType >= kHashFunctionPrefix.Length))
                    return (HashType)0;
                return m_HashType;
            }
            set
            {
                if (m_HashType == value)
                    return;

                m_HashType = value;
                Dirty(ModificationScope.Graph);
            }
        }

        void IGeneratesFunction.GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.RequiresIncludePath("Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl");

            var hashType = this.hashType;
            var hashTypeString = hashType.ToString();
            var HashFunction = kHashFunctionPrefix[(int)hashType];

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

            registry.ProvideFunction($"Unity_SimpleNoise_" + hashTypeString + "_$precision", s =>
            {
                s.AppendLine($"void Unity_SimpleNoise_{hashTypeString}_$precision($precision2 UV, $precision Scale, out $precision Out)");
                using (s.BlockScope())
                {
                    s.AppendLine("$precision freq, amp;");
                    s.AppendLine("Out = 0.0f;");
                    for (int octave = 0; octave < 3; octave++)
                    {
                        s.AppendLine($"freq = pow(2.0, $precision({octave}));");
                        s.AppendLine($"amp = pow(0.5, $precision(3-{octave}));");
                        s.AppendLine($"Out += Unity_SimpleNoise_ValueNoise_{hashTypeString}_$precision($precision2(UV.xy*(Scale/freq)))*amp;");
                    }
                }
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var hashType = this.hashType;
            var hashTypeString = hashType.ToString();
            string uv = GetSlotValue(UVSlotId, generationMode);
            string scale = GetSlotValue(ScaleSlotId, generationMode);
            string output = GetVariableNameForSlot(OutSlotId);
            var outSlot = FindSlot<MaterialSlot>(OutSlotId);

            sb.AppendLine($"{outSlot.concreteValueType.ToShaderString(PrecisionUtil.Token)} {output};");
            sb.AppendLine($"Unity_SimpleNoise_{hashTypeString}_$precision({uv}, {scale}, {output});");
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
                // old nodes should select "LegacySine" to replicate old behavior
                hashType = HashType.LegacySine;
                ChangeVersion(1);
            }
        }
    }
}
