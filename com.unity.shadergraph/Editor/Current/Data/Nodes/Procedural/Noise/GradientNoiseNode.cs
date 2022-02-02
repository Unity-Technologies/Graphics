using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Noise", "Gradient Noise")]
    class GradientNoiseNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireMeshUV
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

        public GradientNoiseNode()
        {
            name = "Gradient Noise";
            synonyms = new string[] { "perlin noise" };
            UpdateNodeAfterDeserialization();
        }

        public enum HashType
        {
            Deterministic,
            LegacyMod,
        };
        static readonly string[] kHashFunctionPrefix =
        {
            "Hash_Tchou_2_1_",
            "Hash_LegacyMod_2_1_",
        };

        public override bool hasPreview => true;

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new UVMaterialSlot(UVSlotId, kUVSlotName, kUVSlotName, UVChannel.UV0));
            AddSlot(new Vector1MaterialSlot(ScaleSlotId, kScaleSlotName, kScaleSlotName, SlotType.Input, 10.0f));
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

            registry.ProvideFunction($"Unity_GradientNoise_{hashTypeString}_Dir_$precision", s =>
            {
                s.AppendLine($"$precision2 Unity_GradientNoise_{hashTypeString}_Dir_$precision($precision2 p)");
                using (s.BlockScope())
                {
                    s.AppendLine($"$precision x; {HashFunction}$precision(p, x);");
                    s.AppendLine("return normalize($precision2(x - floor(x + 0.5), abs(x) - 0.5));");
                }
            });

            registry.ProvideFunction($"Unity_GradientNoise_{hashTypeString}_$precision", s =>
            {
                s.AppendLine($"void Unity_GradientNoise_{hashTypeString}_$precision ($precision2 UV, $precision3 Scale, out $precision Out)");
                using (s.BlockScope())
                {
                    s.AppendLine("$precision2 p = UV * Scale;");
                    s.AppendLine("$precision2 ip = floor(p);");
                    s.AppendLine("$precision2 fp = frac(p);");
                    s.AppendLine($"$precision d00 = dot(Unity_GradientNoise_{hashTypeString}_Dir_$precision(ip), fp);");
                    s.AppendLine($"$precision d01 = dot(Unity_GradientNoise_{hashTypeString}_Dir_$precision(ip + $precision2(0, 1)), fp - $precision2(0, 1));");
                    s.AppendLine($"$precision d10 = dot(Unity_GradientNoise_{hashTypeString}_Dir_$precision(ip + $precision2(1, 0)), fp - $precision2(1, 0));");
                    s.AppendLine($"$precision d11 = dot(Unity_GradientNoise_{hashTypeString}_Dir_$precision(ip + $precision2(1, 1)), fp - $precision2(1, 1));");
                    s.AppendLine("fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);");
                    s.AppendLine("Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;");
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
            sb.AppendLine($"Unity_GradientNoise_{hashTypeString}_$precision({uv}, {scale}, {output});");
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
                // old nodes should select "LegacyMod" to replicate old behavior
                hashType = HashType.LegacyMod;
                ChangeVersion(1);
            }
        }
    }
}
