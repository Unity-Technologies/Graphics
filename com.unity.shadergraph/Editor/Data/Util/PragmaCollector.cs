using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityEditor.ShaderGraph
{
    abstract class AbstractShaderPragma
    {
        // Check if this pragma is a of another pragma. Some pragmas may work identical if they are present several
        // times and can be stripped while others do "something" each time they are used.
        public abstract bool IsDuplicate(AbstractShaderPragma other);
        public abstract string GetPragmaString();
    }

    // Just a list of string separated keywords
    class SimplePragma : AbstractShaderPragma
    {
        public SimplePragma(string PragmaName, IEnumerable<string> argumentKeywords)
        {
            keywords.Add("#pragma");
            keywords.Add(PragmaName);
            keywords.AddRange(argumentKeywords);
        }

        // By default we always emit the pragma 
        public override bool IsDuplicate(AbstractShaderPragma other) { return false; }

        public override string GetPragmaString()
        {
            return string.Join(" ", keywords);
        }

        List<string> keywords = new List<string>();

        public string pragmaname
        {
            get
            {
                return keywords[1];
            }
        }

        public IEnumerable<string> arguments
        {
            get
            {
                return keywords.Skip(2);
            }
        }
    }

    class MultiCompilePragma : SimplePragma
    {
        public MultiCompilePragma(IEnumerable<string> options) : base("multi_compile", options)
        {
        }

        public override bool IsDuplicate(AbstractShaderPragma other)
        {
            return (other is MultiCompilePragma) && (other as MultiCompilePragma).arguments.SequenceEqual(arguments);
        }
    }

    class ShaderFeaturePragma : SimplePragma
    {
        public ShaderFeaturePragma(IEnumerable<string> options) : base("shader_feature", options)
        {
        }

        public override bool IsDuplicate(AbstractShaderPragma other)
        {
            return (other is ShaderFeaturePragma) && (other as ShaderFeaturePragma).arguments.SequenceEqual(arguments);
        }
    }

    // Todo add build-in multicompiles, skip_variants, ....

    class PragmaCollector
    {
        public readonly List<AbstractShaderPragma> pragmas = new List<AbstractShaderPragma>();

        public void AddShaderPragma(AbstractShaderPragma newPragma)
        {
            // A equivalent pragma is already there
            if (pragmas.Any(x => x.IsDuplicate(newPragma)))
                return;
            pragmas.Add(newPragma);
        }

        public string GetPragmaLines(int baseIndentLevel)
        {
            var sb = new StringBuilder();
            foreach (var pragma in pragmas)
            {
                for (var i = 0; i < baseIndentLevel; i++)
                {
                    //sb.Append("\t");
                    sb.Append("    "); // unity convention use space instead of tab...
                }
                sb.AppendLine(pragma.GetPragmaString());
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            return GetPragmaLines(0);
        }
    }
}
