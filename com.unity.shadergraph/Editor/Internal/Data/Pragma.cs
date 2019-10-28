using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph.Internal
{
    public class Pragma
    {
        public string value { get; }

        Pragma(string value)
        {
            this.value = value;
        }

        public static Pragma Target(double value)
        {
            return new Pragma($"#pragma target {string.Format("{0:0.0}", value)}");
        }

        public static Pragma Vertex(string value)
        {
            return new Pragma($"#pragma vertex {value}");
        }

        public static Pragma Fragment(string value)
        {
            return new Pragma($"#pragma fragment {value}");
        }

        public static Pragma Geometry(string value)
        {
            return new Pragma($"#pragma geometry {value}");
        }

        public static Pragma Hull(string value)
        {
            return new Pragma($"#pragma hull {value}");
        }

        public static Pragma Domain(string value)
        {
            return new Pragma($"#pragma domain {value}");
        }

        public static Pragma OnlyRenderers(Platform[] renderers)
        {
            var rendererStrings = renderers.Select(x => x.ToShaderString());
            return new Pragma($"#pragma only_renderers {string.Join(" ", rendererStrings)}");
        }

        public static Pragma ExcludeRenderers(Platform[] renderers)
        {
            var rendererStrings = renderers.Select(x => x.ToShaderString());
            return new Pragma($"#pragma exclude_renderers {string.Join(" ", rendererStrings)}");
        }

        public static Pragma PreferHlslCC(Platform[] renderers)
        {
            var rendererStrings = renderers.Select(x => x.ToShaderString());
            return new Pragma($"#pragma prefer_hlslcc {string.Join(" ", rendererStrings)}");
        }

        public static Pragma MultiCompileInstancing => new Pragma("#pragma multi_compile_instancing");
        public static Pragma MultiCompileFog => new Pragma("#pragma multi_compile_fog");
    }

    public class PragmaCollection : IEnumerable<ConditionalPragma>
    {
        private readonly List<ConditionalPragma> m_Pragmas;

        public PragmaCollection()
        {
            m_Pragmas = new List<ConditionalPragma>();
        }

        public void Add(Pragma pragma)
        {
            m_Pragmas.Add(new ConditionalPragma(pragma, null));
        }

        public void Add(Pragma pragma, FieldCondition fieldCondition)
        {
            m_Pragmas.Add(new ConditionalPragma(pragma, new FieldCondition[]{ fieldCondition }));
        }

        public void Add(Pragma pragma, FieldCondition[] fieldConditions)
        {
            m_Pragmas.Add(new ConditionalPragma(pragma, fieldConditions));
        }

        public IEnumerator<ConditionalPragma> GetEnumerator()
        {
            return m_Pragmas.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class ConditionalPragma : IConditionalShaderString
    {        
        public Pragma pragma { get; }
        public FieldCondition[] fieldConditions { get; }
        public string value => pragma.value;

        public ConditionalPragma(Pragma pragma, FieldCondition[] fieldConditions)
        {
            this.pragma = pragma;
            this.fieldConditions = fieldConditions;
        }
    }
}