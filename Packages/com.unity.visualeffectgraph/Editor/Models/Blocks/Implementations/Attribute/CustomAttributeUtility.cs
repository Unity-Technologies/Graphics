using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    static class CustomAttributeUtility
    {
        private static readonly Regex s_NameValidationRegex = new Regex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
        private static readonly Regex s_NameReplaceRegex = new Regex("[A-Za-z_][A-Za-z0-9_]*", RegexOptions.Compiled);

        public enum Signature
        {
            Float,
            Vector2,
            Vector3,
            Vector4,
            Bool,
            Uint,
            Int
        }

        internal static VFXValueType GetValueType(Signature signature)
        {
            switch (signature)
            {
                default:
                case Signature.Float: return VFXValueType.Float;
                case Signature.Vector2: return VFXValueType.Float2;
                case Signature.Vector3: return VFXValueType.Float3;
                case Signature.Vector4: return VFXValueType.Float4;
                case Signature.Int: return VFXValueType.Int32;
                case Signature.Uint: return VFXValueType.Uint32;
                case Signature.Bool: return VFXValueType.Boolean;
            }
        }

        internal static Signature GetSignature(VFXValueType valueType)
        {
            switch (valueType)
            {
                default:
                case VFXValueType.Float: return Signature.Float;
                case VFXValueType.Float2: return Signature.Vector2;
                case VFXValueType.Float3: return Signature.Vector3;
                case VFXValueType.Float4: return Signature.Vector4;
                case VFXValueType.Int32: return Signature.Int;
                case VFXValueType.Uint32: return Signature.Uint;
                case VFXValueType.Boolean: return Signature.Bool;
            }
        }

        internal static string MakeShaderCompatibleName(string name)
        {
            var validName = new StringBuilder();
            foreach (var match in s_NameReplaceRegex.Matches(name))
            {
                validName.Append(((Match)match).Value);
            }

            return validName.ToString();
        }

        internal static bool IsShaderCompilableName(string name)
        {
            return s_NameValidationRegex.IsMatch(name);
        }
    }
}
