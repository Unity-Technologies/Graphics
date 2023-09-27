using System;
using System.Text.RegularExpressions;

namespace UnityEditor.VFX.UI
{
    static class VFXAttributeHelper
    {
        [Flags]
        public enum VFXAttributeMatch
        {
            None = 0x0,
            Name = 0x1,
            Type = 0x2,
            Casing = 0x4,
            PerfectMath = Name|Type|Casing,
        }

        private static readonly Regex s_VariadicMatch = new Regex("(.*)[X|Y|Z]", RegexOptions.Compiled);

        public struct Match
        {
            public Match(VFXAttribute attribute, VFXAttributeMatch status)
            {
                this.attribute = attribute;
                this.status = status;
            }

            public VFXAttribute attribute { get; }
            public VFXAttributeMatch status { get; }
        }

        public static Match GetMatch(VFXAttribute x, VFXAttribute y)
        {
            var status = VFXAttributeMatch.None;
            if (x.name == y.name)
            {
                status = VFXAttributeMatch.Name | VFXAttributeMatch.Casing;
                status = x.type == y.type ? status | VFXAttributeMatch.Type : status;
            }
            else if (string.Compare(x.name, y.name, StringComparison.OrdinalIgnoreCase) == 0)
            {
                status = VFXAttributeMatch.Name;
                status = x.type == y.type ? status | VFXAttributeMatch.Type : status;
            }

            return new Match(y, status);
        }

        public static bool IsMatching(string a, string b, bool matchVariadic)
        {
            if (string.Compare(a, b, StringComparison.OrdinalIgnoreCase) == 0)
                return true;
            if (matchVariadic)
            {
                if (s_VariadicMatch.Matches(a) is { Count: 1 } aMatches)
                {
                    return string.Compare(aMatches[0].Groups[1].Value, b, StringComparison.OrdinalIgnoreCase) == 0;
                }
                if (s_VariadicMatch.Matches(b) is { Count: 1 } bMatches)
                {
                    return string.Compare(bMatches[0].Groups[1].Value, a, StringComparison.OrdinalIgnoreCase) == 0;
                }
            }

            return false;
        }
    }
}
