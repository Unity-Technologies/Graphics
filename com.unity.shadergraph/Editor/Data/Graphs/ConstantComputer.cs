using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;
using Unity.Mathematics;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class ConstantComputer : ScriptableObject
    {
        [Serializable]
        struct Arg
        {
            public int type; // 0 - 3
            public int index;
        }

        [Serializable]
        struct Instruction
        {
            [NonSerialized] public MethodInfo method;
            public string methodName;
            public string typeName;
            public Arg[] inputs;
            public Arg[] outputs;
        }

        [Serializable]
        struct ColorProperty
        {
            public Color color;
            public int index;
        }

        [Serializable]
        struct Property
        {
            public string name;
            public bool isFloat;
            public int index;
        }

        [SerializeField]
        private Instruction[] m_Instructions;

        [SerializeField]
        private float4[] m_Vecs;

        [SerializeField]
        private ColorProperty[] m_Colors;

        [SerializeField]
        private Property[] m_Inputs;

        [SerializeField]
        private Property[] m_Outputs;

        public IEnumerable<(string name, bool isFloat)> InputNames
        {
            get
            {
                foreach (var e in m_Inputs)
                    yield return (e.name, e.isFloat);
            }
        }

        public IEnumerable<(string name, bool isFloat)> OutputNames
        {
            get
            {
                foreach (var e in m_Outputs)
                    yield return (e.name, e.isFloat);
            }
        }

        public void SetInput(string name, float value)
        {
            var index = Array.FindIndex(m_Inputs, i => i.name == name && i.isFloat);
            if (index < 0)
                throw new ArgumentException($"Input name {name} is not found.");
            m_Vecs[m_Inputs[index].index] = value;
        }

        public void SetInput(string name, Vector4 value)
        {
            var index = Array.FindIndex(m_Inputs, i => i.name == name && !i.isFloat);
            if (index < 0)
                throw new ArgumentException($"Input name {name} is not found.");
            m_Vecs[m_Inputs[index].index] = value;
        }

        public void Execute()
        {
            for (int i = 0; i < m_Instructions.Length; ++i)
            {
                ref var inst = ref m_Instructions[i];
                if (inst.method == null)
                {
                    inst.method = Type.GetType(inst.typeName).GetMethod(inst.methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (inst.method == null)
                        throw new Exception($"Cannot resolve method {inst.typeName}.{inst.method}");
                }
            }

            var isgamma = PlayerSettings.colorSpace == ColorSpace.Gamma;
            foreach (var colorProperty in m_Colors)
            {
                var vec = isgamma ? colorProperty.color : colorProperty.color.linear;
                m_Vecs[colorProperty.index] = math.float4(vec.r, vec.g, vec.b, vec.a);
            }

            foreach (var inst in m_Instructions)
            {
                var args = new object[inst.inputs.Length + inst.outputs.Length];
                for (int i = 0; i < inst.inputs.Length; ++i)
                {
                    var arg = inst.inputs[i];
                    var vec = m_Vecs[arg.index];
                    if (arg.type == 0)
                        args[i] = Float(vec.x);
                    else if (arg.type == 1)
                        args[i] = Float2(vec.x, vec.y);
                    else if (arg.type == 2)
                        args[i] = Float3(vec.x, vec.y, vec.z);
                    else if (arg.type == 3)
                        args[i] = Float4(vec.x, vec.y, vec.z, vec.w);
                }
                inst.method.Invoke(null, args);
                for (int i = 0; i < inst.outputs.Length; ++i)
                {
                    var arg = inst.outputs[i];
                    var outVar = args[i + inst.inputs.Length];
                    if (arg.type == 0)
                        m_Vecs[arg.index] = math.float4(((Float)outVar).Value.Value, 0, 0, 0);
                    else if (arg.type == 1)
                        m_Vecs[arg.index] = math.float4(((Float2)outVar).Value.Value, 0, 0);
                    else if (arg.type == 2)
                        m_Vecs[arg.index] = math.float4(((Float3)outVar).Value.Value, 0);
                    else if (arg.type == 3)
                        m_Vecs[arg.index] = ((Float4)outVar).Value.Value;
                }
            }
        }

        public Vector4 GetOutput(string name)
        {
            var index = Array.FindIndex(m_Outputs, i => i.name == name);
            if (index < 0)
                throw new ArgumentException($"Output name {name} is not found.");
            return m_Vecs[m_Outputs[index].index];
        }

        private static AbstractShaderProperty GetShaderPropertyFromNode(PropertyNode node)
            => node.owner.properties.FirstOrDefault(x => x.guid == node.propertyGuid);

        internal static ConstantComputer Gather(AbstractMaterialNode root)
        {
            Debug.Assert(root.isStatic);
            Debug.Assert(root is TimeNode || root is PropertyNode || root is CodeFunctionNode);

            var nodes = new List<AbstractMaterialNode>();
            Graphing.NodeUtils.DepthFirstCollectNodesFromNode(nodes, root);

            var nodeGuidToInstructionIndex = new Dictionary<Guid, int>();
            var instructions = new List<Instruction>(nodes.Count);
            var vecs = new List<float4>();
            var colors = new List<ColorProperty>();
            var inputs = new List<Property>();
            var outputs = new List<Property>();

            foreach (var node in nodes.OfType<CodeFunctionNode>())
            {
                // Code function node
                var func = node.Method;
                if (func.GetCustomAttribute<CodeFunctionNode.HlslCodeGenAttribute>() == null)
                    throw new Exception("Unity can only create ConstantComputer from HlslCodeGen function nodes.");

                var inputArgs = new List<Arg>();
                var outputArgs = new List<Arg>();

                var slots = new List<MaterialSlot>();
                node.GetSlots(slots);
                foreach (var param in func.GetParameters())
                {
                    var arg = new Arg();
                    var paramType = param.ParameterType;
                    if (paramType.IsByRef)
                        paramType = paramType.GetElementType();
                    if (paramType == typeof(Float))
                        arg.type = 0;
                    else if (paramType == typeof(Float2))
                        arg.type = 1;
                    else if (paramType == typeof(Float3))
                        arg.type = 2;
                    else if (paramType == typeof(Float4))
                        arg.type = 3;

                    var slotId = param.GetCustomAttribute<CodeFunctionNode.SlotAttribute>().slotId;
                    var slot = slots.Find(s => s.id == slotId);
                    var edge = node.owner.GetEdges(slot.slotReference).FirstOrDefault();
                    if (edge == null && param.IsOut)
                        continue;

                    if (!param.IsOut)
                    {
                        // input
                        if (edge != null)
                        {
                            var fromNode = node.owner.GetNodeFromGuid<AbstractMaterialNode>(edge.outputSlot.nodeGuid);
                            Debug.Assert(nodes.Contains(fromNode));
                            if (fromNode is PropertyNode || fromNode is TimeNode)
                            {
                                string inputName;
                                bool isFloat;
                                if (fromNode is PropertyNode propertyNode)
                                {
                                    inputName = GetShaderPropertyFromNode(propertyNode).referenceName;
                                    isFloat = GetShaderPropertyFromNode(propertyNode).propertyType == PropertyType.Vector1;
                                }
                                else
                                {
                                    inputName = fromNode.GetVariableNameForSlot(edge.outputSlot.slotId);
                                    isFloat = true;
                                }

                                var inputIndex = inputs.FindIndex(i => i.name == inputName);
                                if (inputIndex < 0)
                                {
                                    inputIndex = inputs.Count;
                                    inputs.Add(new Property() { name = inputName, isFloat = isFloat, index = vecs.Count});
                                    vecs.Add(float4.zero);
                                }
                                arg.index = inputs[inputIndex].index;
                            }
                            else if (fromNode is ColorNode colorNode)
                            {
                                arg.index = vecs.Count;
                                vecs.Add(float4.zero);
                                colors.Add(new ColorProperty() { color = colorNode.color.color, index = arg.index });
                            }
                            else
                            {
                                arg.index = -1;
                                var fromFuncNode = fromNode as CodeFunctionNode;
                                var fromInstruction = instructions[nodeGuidToInstructionIndex[fromNode.guid]];
                                var fromParams = fromInstruction.method.GetParameters();
                                for (int i = 0; i < fromParams.Length; ++i)
                                {
                                    if (fromParams[i].GetCustomAttribute<CodeFunctionNode.SlotAttribute>().slotId == edge.outputSlot.slotId)
                                    {
                                        arg.index = fromInstruction.outputs[i - fromInstruction.inputs.Length].index;
                                        break;
                                    }
                                }
                                Debug.Assert(arg.index != -1);
                            }
                        }
                        else
                        {
                            // default
                            // TODO: deduplicate by hashing the value
                            arg.index = vecs.Count;
                            if (slot is Vector1MaterialSlot vec1Slot)
                                vecs.Add(math.float4(vec1Slot.value));
                            else if (slot is Vector2MaterialSlot vec2Slot)
                                vecs.Add(math.float4(vec2Slot.value, 0, 0));
                            else if (slot is Vector3MaterialSlot vec3Slot)
                                vecs.Add(math.float4(vec3Slot.value, 0));
                            else if (slot is Vector4MaterialSlot vec4Slot)
                                vecs.Add(math.float4(vec4Slot.value));
                            else
                                Debug.Assert(false);
                        }
                        inputArgs.Add(arg);
                    }
                    else
                    {
                        // output
                        arg.index = vecs.Count;
                        vecs.Add(float4.zero);
                        outputArgs.Add(arg);

                        if (node == root)
                            outputs.Add(new Property() { name = node.GetVariableNameForSlot(slotId), isFloat = slot.concreteValueType == ConcreteSlotValueType.Vector1, index = arg.index });
                    }
                }

                var instruction = new Instruction()
                {
                    method = node.Method,
                    methodName = node.Method.Name,
                    typeName = node.Method.DeclaringType?.FullName,
                    inputs = inputArgs.ToArray(),
                    outputs = outputArgs.ToArray()
                };
                instructions.Add(instruction);
                nodeGuidToInstructionIndex.Add(node.guid, instructions.Count - 1);
            }

            var constantComputer = ScriptableObject.CreateInstance<ConstantComputer>();
            constantComputer.m_Instructions = instructions.ToArray();
            constantComputer.m_Vecs = vecs.ToArray();
            constantComputer.m_Colors = colors.ToArray();
            constantComputer.m_Inputs = inputs.ToArray();
            constantComputer.m_Outputs = outputs.ToArray();

            return constantComputer;
        }
    }
}
