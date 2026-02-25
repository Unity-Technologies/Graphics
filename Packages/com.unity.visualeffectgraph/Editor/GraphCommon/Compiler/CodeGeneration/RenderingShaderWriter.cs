using System;

namespace Unity.GraphCommon.LowLevel.Editor
{
    class RenderingShaderWriter : ShaderWriter
    {
        public override void Begin(string name)
        {
            base.Begin(name);
            WriteLine($"Shader \"{name}\"");
            OpenBlock();
        }
        public override string End()
        {
            CloseBlock();
            return base.End();
        }
        public void SubShaderBegin()
        {
            WriteLine("SubShader");
            OpenBlock();
            WriteLine("Tags { \"Queue\"=\"Transparent\" \"RenderType\"=\"Transparent\"  \"IgnoreProjector\"=\"True\"}");
            WriteLine("LOD 100");
            WriteLine("Cull Off");
            WriteLine("ZWrite Off");
            WriteLine("Blend SrcAlpha OneMinusSrcAlpha");
        }

        public void SubShaderEnd()
        {
            CloseBlock();
        }

        public void BeginHlslInclude()
        {
            WriteLine("HLSLINCLUDE");
        }

        public void EndHlslInclude()
        {
            WriteLine("ENDHLSL");
        }

        public void PassBegin(string name)
        {
            WriteLine("Pass");
            OpenBlock();
            WriteLine("HLSLPROGRAM");
            Pragma("target 5.0");
            Pragma("enable_d3d11_debug_symbols");
            Pragma("vertex VFXVertex");
            Pragma("fragment VFXFragment");

        }

        public void PassEnd()
        {
            WriteLine("ENDHLSL");
            CloseBlock();
        }

        public void WriteVertexFunction()
        {
            // TODO: the signature should be customizable
            WriteLine("Varyings VFXVertex(uint id : SV_VertexID, VertexInput input)");
        }

        public void WriteFragmentFunction()
        {
            WriteLine("float4 VFXFragment(Varyings input) : SV_Target");
        }

        public SubShaderScope CreateSubShaderScope() => new(this);
        public HlslIncludeScope CreateHlslIncludeScope() => new(this);
        public PassScope CreatePassScope(string name) => new(this, name);
        public readonly struct SubShaderScope : IDisposable
        {
            private readonly RenderingShaderWriter _writer;
            public SubShaderScope(RenderingShaderWriter writer)
            {
                _writer = writer;
                _writer.SubShaderBegin();
            }
            public void Dispose()
            {
                _writer.SubShaderEnd();
            }
        }

        public readonly struct HlslIncludeScope : IDisposable
        {
            private readonly RenderingShaderWriter _writer;
            public HlslIncludeScope(RenderingShaderWriter writer)
            {
                _writer = writer;
                _writer.BeginHlslInclude();
            }
            public void Dispose()
            {
                _writer.EndHlslInclude();
            }
        }

        public readonly struct PassScope : IDisposable
        {
            private readonly RenderingShaderWriter _writer;
            public PassScope(RenderingShaderWriter writer, string name)
            {
                _writer = writer;
                _writer.PassBegin(name);
            }
            public void Dispose()
            {
                _writer.PassEnd();
            }
        }
    }
}
