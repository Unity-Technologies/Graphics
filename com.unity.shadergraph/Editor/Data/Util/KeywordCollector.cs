using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityEditor.ShaderGraph
{
    class KeywordCollector
    {
        public readonly List<ShaderKeyword> keywords = new List<ShaderKeyword>();

        public void AddShaderKeyword(ShaderKeyword chunk)
        {
            if (keywords.Any(x => x.referenceName == chunk.referenceName))
                return;
            keywords.Add(chunk);
        }

        public void GetKeywordsDeclaration(ShaderStringBuilder builder, GenerationMode mode)
        {
            foreach (var keyword in keywords)
            {
                if(mode == GenerationMode.Preview)
                    builder.AppendLine(keyword.GetKeywordPreviewDeclarationString());
                else
                    builder.AppendLine(keyword.GetKeywordDeclarationString());
            }
            builder.AppendNewLine();
        }
    }
}
