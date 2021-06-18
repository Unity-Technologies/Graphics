using System.Reflection;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Random", "Integer Hash")]
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
        private const string kInputSlotName = "coord";

        // Output slots
        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "hash";

        // Local state
        public enum HashType
        {
            Tchou_2_3,
            Tchou_3_3,
            PCG_2_3,
            PCG_3_3,
            LegacySine_2_1,
            LegacySine_2_2
            //PCG_2_2,
            //PCG_2_4,
            //PCG_4_4,
            //IQ_2_3,
            //Wang_2_1
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
            new Description("Hash_Tchou_2_3_float", 2, 3),
            new Description("Hash_Tchou_3_3_float", 3, 3),
            new Description("Hash_PCG_2_3_float", 2, 3),
            new Description("Hash_PCG_3_3_float", 3, 3),
            new Description("Hash_LegacySine_2_1_float", 2, 1),
            new Description("Hash_LegacySine_2_2_float", 2, 2)
        };

        [SerializeField]
        private HashType m_HashType = HashType.Tchou_2_3;

        [EnumControl("Hash")]
        public HashType hashType
        {
            get { return m_HashType; }
            set
            {
                if (m_HashType == value)
                    return;

                m_HashType = value;
                Dirty(ModificationScope.Topological);
                UpdateNodeAfterDeserialization();
            }
        }

        Description hashDescription => k_hashDescriptions[(int)hashType];

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

        /*
                static string Unity_IntegerHash_PCG_2_2(
                    [Slot(0, Binding.None)] Vector2 coord,     // TODO: Binding.PixelCoord, integer types
                    [Slot(2, Binding.None)] out Vector2 Out)
                {
                    Out = Vector2.zero;
                    return
        @"
        {
            // double conversion to preserve negative numbers
            int2 i = (int2) floor(coord);
            uint2 v = (uint2) i;
            v = v * 1664525u + 1013904223u;

            v.x += v.y * 1664525u;
            v.y += v.x * 1664525u;

            v = v ^ (v>>16u);

            v.x += v.y * 1664525u;
            v.y += v.x * 1664525u;

            v = v ^ (v>>16u);

            Out = v * (1.0/float(0xffffffff));
        }";
                }

                static string Unity_IntegerHash_PCG_2_3(
                    [Slot(0, Binding.None)] Vector2 coord,     // TODO: Binding.PixelCoord, integer types
                    [Slot(2, Binding.None)] out Vector3 Out)
                {
                    Out = Vector3.zero;
                    return
        @"
        {
            // double conversion to preserve negative numbers
            int2 i = (int2) floor(coord);
            uint2 v2 = (uint2) i;
            uint3 v = uint3(v2.xy, v2.x ^ v2.y);    // convert 2d input to 3d

            // pcg 3->3
            v = v * 1664525u + 1013904223u;

            v.x += v.y*v.z;
            v.y += v.z*v.x;
            v.z += v.x*v.y;

            v ^= v >> 16u;

            v.x += v.y*v.z;
            v.y += v.z*v.x;
            v.z += v.x*v.y;

            Out = v * (1.0/float(0xffffffff));
        }";
                }

                static string Unity_IntegerHash_PCG_3_3(
                    [Slot(0, Binding.None)] Vector3 coord,     // TODO: Binding.PixelCoord, integer types
                    [Slot(2, Binding.None)] out Vector3 Out)
                {
                    Out = Vector3.zero;
                    return
        @"
        {
            // double conversion to preserve negative numbers
            int3 i = (int3) floor(coord);
            uint3 v = (uint3) i;

            // pcg 3->3
            v = v * 1664525u + 1013904223u;

            v.x += v.y*v.z;
            v.y += v.z*v.x;
            v.z += v.x*v.y;

            v ^= v >> 16u;

            v.x += v.y*v.z;
            v.y += v.z*v.x;
            v.z += v.x*v.y;

            Out = v * (1.0/float(0xffffffff));
        }";
                }

                static string Unity_IntegerHash_Tchou_2_3(
            [Slot(0, Binding.None)] Vector2 coord,     // TODO: Binding.PixelCoord, integer types
            [Slot(2, Binding.None)] out Vector3 Out)
                {
                    Out = Vector3.zero;
                    return
        @"
        {
            // double conversion to preserve negative numbers
            int2 i = (int2) floor(coord);
            uint2 v2 = (uint2) i;
            uint3 v = uint3(v2.xy, v2.x ^ v2.y);    // convert 2d input to 3d

            // tchou 3->3
            v.x += v.y*v.z;     // 2    (1 mul)
            v.x *= 0x27d4eb2du; // 1    (1 mul)   
            v.x ^= v.x >> 4u;   // 2    // bit mix -- useful if you want fully 'random' looking bits
            v.y += v.z ^ v.x;     // 2    
            v.y ^= v.y >> 15u;  // 2    // bit mix -- useful if you want fully 'random' looking bits
            v.y *= 0x27d4eb2du; // 1    (1 mul)
            v.z += v.x ^ v.y;   // 2
            v.x += v.z;         // 1

            Out = v * (1.0/float(0xffffffff));
        }";
                }

                static string Unity_IntegerHash_Tchou_3_3(
        [Slot(0, Binding.None)] Vector3 coord,
        [Slot(2, Binding.None)] out Vector3 Out)
                {
                    Out = Vector3.zero;
                    return
        @"
        {
            // double conversion to preserve negative numbers
            int3 i = (int3) floor(coord);
            uint3 v = (uint3) i;

            // tchou 3->3
            // v.z += v.x ^ v.y;       // 2
            v.x += v.y * v.z;       // 2    (1 mul)
            v.x *= 0x27d4eb2du;     // 1    (1 mul)   
            v.x ^= v.x >> 4u;       // 2    // bit mix -- useful if you want fully 'random' looking bits
            v.y += v.z ^ v.x;       // 2    
            v.y ^= v.y >> 15u;      // 2    // bit mix -- useful if you want fully 'random' looking bits
            v.y *= 0x27d4eb2du;     // 1    (1 mul)
            v.z += v.x ^ v.y;       // 2
            v.x += v.z;             // 1

            Out = v * (1.0/float(0xffffffff));
        }";
                }

                static string Unity_IntegerHash_PCG_2_4(
            [Slot(0, Binding.None)] Vector2 coord,     // TODO: Binding.PixelCoord, integer types
            [Slot(2, Binding.None)] out Vector4 Out)
                {
                    Out = Vector4.zero;
                    return
        @"
        {
            // double conversion to preserve negative numbers
            int2 i = (int2) floor(coord);
            uint2 v2 = (uint2) i;
            uint4 v = uint4(v2.xy, v2.x ^ v2.y, v2.x + v2.y);    // convert 2d input to 4d

            // pcg 4->4
            v = v * 1664525u + 1013904223u;

            v.x += v.y*v.w;
            v.y += v.z*v.x;
            v.z += v.x*v.y;
            v.w += v.y*v.z;

            v ^= v >> 16u;

            v.x += v.y*v.w;
            v.y += v.z*v.x;
            v.z += v.x*v.y;
            v.w += v.y*v.z;

            Out = v * (1.0/float(0xffffffff));
        }";
                }

                static string Unity_IntegerHash_PCG_4_4(
                    [Slot(0, Binding.None)] Vector4 coord,     // TODO: Binding.PixelCoord, integer types
                    [Slot(2, Binding.None)] out Vector4 Out)
                {
                    Out = Vector4.zero;
                    return
        @"
        {
            // double conversion to preserve negative numbers
            int4 i = (int4) floor(coord);
            uint4 v = (uint4) i;

            // pcg 4->4
            v = v * 1664525u + 1013904223u;

            v.x += v.y*v.w;
            v.y += v.z*v.x;
            v.z += v.x*v.y;
            v.w += v.y*v.z;

            v ^= v >> 16u;

            v.x += v.y*v.w;
            v.y += v.z*v.x;
            v.z += v.x*v.y;
            v.w += v.y*v.z;

            Out = v * (1.0/float(0xffffffff));
        }";
                }

                static string Unity_IntegerHash_IQInt_2_3(
                    [Slot(0, Binding.None)] Vector2 coord,     // TODO: Binding.PixelCoord, integer types
                    [Slot(2, Binding.None)] out Vector3 Out)
                {
                    Out = Vector2.zero;
                    return
        @"
        {
            // double conversion to preserve negative numbers
            int2 i = (int2) floor(coord);
            uint2 v2 = (uint2) i;
            uint3 x = uint3(v2.xy, v2.x ^ v2.y);    // convert 2d input to 3d

            const uint k = 1103515245u;

            x = ((x>>8U) ^ x.yzx)*k;
            x = ((x>>8U) ^ x.yzx)*k;
            x = ((x>>8U) ^ x.yzx)*k;

            Out = x * (1.0/float(0xffffffff));
        }";
                }

                static string Unity_IntegerHash_Wang_2_1(
                    [Slot(0, Binding.None)] Vector2 coord,     // TODO: Binding.PixelCoord, integer types
                    [Slot(2, Binding.None)] out Vector1 Out)
                {
                    return
        @"
        {
            // double conversion to preserve negative numbers
            int2 i = (int2) floor(coord);
            uint2 v2 = (uint2) i;
            uint v = v2.x + 461 * v2.y;

            v = (v ^ 61u) ^ (v >> 16u);
            v *= 9u;
            v ^= v >> 4u;
            v *= 0x27d4eb2du;
            v ^= v >> 15u;

            Out = v * (1.0/float(0xffffffff));
        }";
            }
        */
    }
}
