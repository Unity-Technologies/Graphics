using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;
using Unity.Mathematics;

namespace UnityEngine.ShaderGraph
{
    [Serializable]
    public class ConstantComputer : ScriptableObject
    {
        [Serializable]
        public struct Arg
        {
            public int type; // 0 - 3
            public int index;
        }

        [Serializable]
        public struct Instruction
        {
            [NonSerialized] public MethodInfo method;
            public string methodName;
            public string typeName;
            public Arg[] inputs;
            public Arg[] outputs;
        }

        [Serializable]
        public struct ColorProperty
        {
            public Color color;
            public int index;
        }

        [Serializable]
        public struct Property
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

            var isGamma = QualitySettings.activeColorSpace == ColorSpace.Gamma;
            foreach (var colorProperty in m_Colors)
            {
                var vec = isGamma ? colorProperty.color : colorProperty.color.linear;
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

        public void Construct(Instruction[] instructions, float4[] vecs, ColorProperty[] colors, Property[] inputProperties, Property[] outputProperties)
        {
            m_Instructions = instructions;
            m_Vecs = vecs;
            m_Colors = colors;
            m_Inputs = inputProperties;
            m_Outputs = outputProperties;
        }
    }
}
