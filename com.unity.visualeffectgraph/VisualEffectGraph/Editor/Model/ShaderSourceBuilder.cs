using UnityEngine;
using UnityEngine.Experimental.VFX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Experimental;

namespace UnityEditor.Experimental
{
    public class ShaderSourceBuilder
    {
        // Generic builder method
        public void Write<T>(T t)
        {
            m_Builder.Append(t);
        }

        public void WriteLine<T>(T t)
        {
            Write(t);
            WriteLine();
        }

        public void WriteLine()
        {
            m_Builder.AppendLine();
            WriteIndent();
        }

        public void EnterScope()
        {
            Indent();
            WriteLine('{');
        }

        public void ExitScope()
        {
            Deindent();
            m_Builder[m_Builder.Length - 1] = '}'; // Replace last '\t' by '}'
            WriteLine();
        }

        public void ExitScopeStruct()
        {
            Deindent();
            m_Builder[m_Builder.Length - 1] = '}'; // Replace last '\t' by '}'
            WriteLine(';');
        }

        public override string ToString()
        {
            return m_Builder.ToString();
        }

        // Shader helper methods
        public void WriteAttributeBuffer(AttributeBuffer attributeBuffer)
        {
            Write("struct Attribute");
            WriteLine(attributeBuffer.Index);

            EnterScope();

            for (int i = 0; i < attributeBuffer.Count; ++i)
            {
                WriteType(attributeBuffer[i].m_Type);
                Write(" ");
                Write(attributeBuffer[i].m_Name);
                WriteLine(";");
            }

            if (attributeBuffer.GetSizeInBytes() == 12)
                WriteLine("float _PADDING_;");

            ExitScopeStruct();
            WriteLine();
        }

        public void WriteCBuffer(string cbufferName, HashSet<VFXValue> uniforms, Dictionary<VFXValue, string> uniformsToName)
        {
            if (uniforms.Count > 0)
            {
                Write("CBUFFER_START(");
                Write(cbufferName);
                WriteLine(")");

                foreach (var uniform in uniforms)
                {
                    Write('\t');
                    WriteType(uniform.ValueType);
                    Write(" ");
                    Write(uniformsToName[uniform]);
                    WriteLine(";");
                }

                WriteLine("CBUFFER_END");
                WriteLine();
            }
        }

        public void WriteSamplers(HashSet<VFXValue> samplers, Dictionary<VFXValue, string> samplersToName)
        {
            foreach (var sampler in samplers)
            {
                if (sampler.ValueType == VFXValueType.kTexture2D)
                    Write("sampler2D ");
                else if (sampler.ValueType == VFXValueType.kTexture3D)
                    Write("sampler3D ");
                else
                    continue;

                Write(samplersToName[sampler]);
                WriteLine(";");
                WriteLine();
            }
        }

        public void WriteType(VFXValueType type)
        {
            // tmp transform texture to sampler TODO This must be handled directly in C++ conversion array
            if (type == VFXValueType.kTexture2D)
                Write("sampler2D");
            else if (type == VFXValueType.kTexture3D)
                Write("sampler3D");
            else
                Write(VFXValue.TypeToName(type));
        }

        public void WriteFunction(VFXBlockModel block, Dictionary<Hash128, string> functions)
        {
            if (!functions.ContainsKey(block.Desc.Hash)) // if not already defined
            {
                // generate function name
                string name = new string((from c in block.Desc.Name where char.IsLetterOrDigit(c) select c).ToArray());
                functions[block.Desc.Hash] = name;

                string source = block.Desc.Source;

                // function signature
                Write("void ");
                Write(name);
                Write("(");

                char separator = ' ';
                foreach (var arg in block.Desc.Attributes)
                {
                    Write(separator);
                    separator = ',';

                    if (arg.m_Writable)
                        Write("inout ");
                    WriteType(arg.m_Type);
                    Write(" ");
                    Write(arg.m_Name);
                }

                List<VFXNamedValue> namedValues = new List<VFXNamedValue>();
                for (int i = 0; i < block.GetNbSlots(); ++i)
                {
                    VFXPropertySlot slot = block.GetSlot(i);

                    namedValues.Clear();
                    slot.CollectNamedValues(namedValues);
                    foreach (var arg in namedValues)
                    {
                        Write(separator);
                        separator = ',';

                        WriteType(arg.m_Value.ValueType);
                        Write(" ");
                        Write(arg.m_Name);
                    }
                }

                if ((block.Desc.Flags & VFXBlockDesc.Flag.kHasRand) != 0)
                {
                    Write(separator);
                    separator = ',';
                    Write("inout uint seed");
                }

                if ((block.Desc.Flags & VFXBlockDesc.Flag.kHasKill) != 0)
                {
                    Write(separator);
                    separator = ',';
                    Write("inout bool kill");
                }

                WriteLine(")");

                // function body
                EnterScope();

                source = source.TrimStart(new char[] {'\t'}); // TODO Fix that from importer (no need for first '\t')

                Write(source);
                WriteLine();

                ExitScope();
                WriteLine();
            }
        }

        public void WriteFunctionCall(
            VFXBlockModel block,
            Dictionary<Hash128, string> functions,
            ShaderMetaData data)
        {
            Dictionary<VFXValue, string> paramToName = data.paramToName;
            Dictionary<VFXAttribute, AttributeBuffer> attribToBuffer = data.attribToBuffer;

            Write(functions[block.Desc.Hash]);
            Write("(");

            char separator = ' ';
            foreach (var arg in block.Desc.Attributes)
            {
                Write(separator);
                separator = ',';

                int index = attribToBuffer[arg].Index;
                Write("attrib");
                Write(index);
                Write(".");
                Write(arg.m_Name);
            }

            List<VFXNamedValue> namedValues = new List<VFXNamedValue>();
            for (int i = 0; i < block.GetNbSlots(); ++i)
            {
                VFXPropertySlot slot = block.GetSlot(i);

                namedValues.Clear();
                slot.CollectNamedValues(namedValues);
                foreach (var arg in namedValues)
                {
                    Write(separator);
                    separator = ',';
                    Write(paramToName[arg.m_Value]);
                }
            }

            if ((block.Desc.Flags & VFXBlockDesc.Flag.kHasRand) != 0)
            {
                Write(separator);
                separator = ',';
                WriteAttrib(CommonAttrib.Seed, data);
            }

            if ((block.Desc.Flags & VFXBlockDesc.Flag.kHasKill) != 0)
            {
                Write(separator);
                separator = ',';
                Write("kill");
            }

            WriteLine(");");
        }

        public void WriteAddPhaseShift(ShaderMetaData data)
        {
            WritePhaseShift('+', data);
        }

        public void WriteRemovePhaseShift(ShaderMetaData data)
        {
            WritePhaseShift('-', data);
        }

        private void WritePhaseShift(char op, ShaderMetaData data)
        {
            WriteAttrib(CommonAttrib.Position, data);
            Write(" ");
            Write(op);
            Write("= (");
            WriteAttrib(CommonAttrib.Phase, data);
            Write(" * deltaTime) * ");
            WriteAttrib(CommonAttrib.Velocity, data);
            WriteLine(";");
        }

        public void WriteAttrib(VFXAttribute attrib, ShaderMetaData data)
        {
            int attribIndex = data.attribToBuffer[attrib].Index;
            Write("attrib");
            Write(attribIndex);
            Write(".");
            Write(attrib.m_Name);
        }

        public void WriteKernelHeader(string name)
        {
            WriteLine("[numthreads(NB_THREADS_PER_GROUP,1,1)]");
            Write("void ");
            Write(name);
            WriteLine("(uint3 id : SV_DispatchThreadID)");
            EnterScope();
        }

        // Private stuff
        private void Indent()
        {
            ++m_Indent;
        }

        private void Deindent()
        {
            if (m_Indent == 0)
                throw new InvalidOperationException("Cannot de-indent as current indentation is 0");

            --m_Indent;
        }

        private void WriteIndent()
        {
            for (int i = 0; i < m_Indent; ++i)
                m_Builder.Append('\t');
        }

        private StringBuilder m_Builder = new StringBuilder();
        private int m_Indent = 0;
    }
}
