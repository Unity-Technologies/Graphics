using System.Linq;

namespace UnityEditor.ShaderGraph.Internal
{
    public class ConditionalPragma : IConditionalShaderString
    {        
        public Pragma pragma { get; }
        public FieldCondition[] fieldConditions { get; }
        public string value => pragma.value;

        public ConditionalPragma(Pragma pragma)
        {
            this.pragma = pragma;
            this.fieldConditions = null;
        }

        public ConditionalPragma(Pragma pragma, FieldCondition fieldCondition)
        {
            this.pragma = pragma;
            this.fieldConditions = new FieldCondition[] { fieldCondition };
        }

        public ConditionalPragma(Pragma pragma, FieldCondition[] fieldConditions)
        {
            this.pragma = pragma;
            this.fieldConditions = fieldConditions;
        }
    }

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

        public static Pragma Custom(string value)
        {
            return new Pragma($"#pragma {value}");
        }

        public static Pragma MultiCompileInstancing => new Pragma("#pragma multi_compile_instancing");
        public static Pragma MultiCompileFog => new Pragma("#pragma multi_compile_fog");
    }
}