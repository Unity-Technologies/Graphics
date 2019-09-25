using System.Text.RegularExpressions;

namespace UnityEditor.ShaderGraph
{
    public static class TextUtil
    {
        public static string PascalToLabel(this string pascalString)
        {
            return Regex.Replace(pascalString, "(\\B[A-Z])", " $1");
        }
    }
}
