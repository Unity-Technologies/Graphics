using System.Reflection;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Noise", "Integer Hash")]
    class IntegerHashNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        public IntegerHashNode()
        {
            name = "Integer Hash";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview { get { return true; } }

        // Input slots
        private const int kInputSlotId = 1;
        private const string kInputSlotName = "Coord";

        // Output slots
        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Hash";

        // Local state
        public enum HashType
        {
            Tchou_2_1,
            Tchou_2_3,
            Tchou_3_1,
            Tchou_3_3,
        };

        struct Description
        {
            public string functionName;
            public int inputDimension;
            public int outputDimension;
            public Description(string f, int i, int o) { functionName = f; inputDimension = i; outputDimension = o; }
        };

        static Description[] k_hashDescriptions =
        {
            new Description("Hash_Tchou_2_1_float", 2, 1),
            new Description("Hash_Tchou_2_3_float", 2, 3),
            new Description("Hash_Tchou_3_1_float", 3, 1),
            new Description("Hash_Tchou_3_3_float", 3, 3),
        };

        [SerializeField]
        private HashType m_HashType = HashType.Tchou_2_3;

        [EnumControl("Hash")]
        public HashType hashType
        {
            get
            {
                return m_HashType;
            }
            set
            {
                if (m_HashType == value)
                    return;

                m_HashType = value;
                Dirty(ModificationScope.Topological);
                UpdateNodeAfterDeserialization();
            }
        }

        Description hashDescription => ((int) hashType >= 0 && (int) hashType < k_hashDescriptions.Length) ? k_hashDescriptions[(int)hashType] : k_hashDescriptions[0];

        public MaterialSlot CreateVectorSlot(int dimension, int id, string name, SlotType slotType, Vector4 value = default)
        {
            MaterialSlot slot = null;
            switch (dimension)
            {
                case 1:
                    slot = new Vector1MaterialSlot(id, name, name, slotType, value.x);
                    break;
                case 2:
                    slot = new Vector2MaterialSlot(id, name, name, slotType, value);
                    break;
                case 3:
                    slot = new Vector3MaterialSlot(id, name, name, slotType, value);
                    break;
                case 4:
                    slot = new Vector4MaterialSlot(id, name, name, slotType, value);
                    break;
            }
            return slot;
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            var desc = hashDescription;

            MaterialSlot inputSlot = CreateVectorSlot(desc.inputDimension, kInputSlotId, kInputSlotName, SlotType.Input);
            AddSlot(inputSlot);

            MaterialSlot outputSlot = CreateVectorSlot(desc.outputDimension, kOutputSlotId, kOutputSlotName, SlotType.Output);
            AddSlot(outputSlot);

            RemoveSlotsNameNotMatching(new[]
            {
                kInputSlotId,
                kOutputSlotId
            });
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.RequiresIncludePath("Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl");
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var desc = hashDescription;

            var outputVar = GetVariableNameForSlot(kOutputSlotId);
            var input = GetSlotValue(kInputSlotId, generationMode);

            sb.AppendLine($"$precision{desc.outputDimension} {outputVar};");
            sb.AppendLine($"{desc.functionName}({input}, {outputVar});");
        }
    }
}
