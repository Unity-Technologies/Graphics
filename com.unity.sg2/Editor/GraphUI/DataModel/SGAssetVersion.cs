
using System;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [Serializable]
    public class SGAssetVersion : IComparable<SGAssetVersion>
    {
        public readonly int Major;
        public readonly int Minor;
        public readonly int Patch;
        public readonly string Suffix;

        public SGAssetVersion(int major, int minor, int patch, string suffix)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Suffix = suffix;
        }

        public SGAssetVersion(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Suffix = null;
        }

        public int CompareTo(SGAssetVersion other)
        {
            var result = Major.CompareTo(other.Major);
            if (result != 0)
                return result;
            result = Minor.CompareTo(other.Minor);
            if (result != 0)
                return result;
            result = Patch.CompareTo(other.Patch);
            if (result != 0)
                return result;
            else
            {
                if (Suffix == null)
                {
                    return other.Suffix == null ? 0 : -1;
                }
                else
                    return Suffix.CompareTo(other.Suffix);
            }

        }

        public override string ToString()
        {
            var result = $"{Major}.{Minor}.{Patch}";
            if (Suffix != null)
            {
                result += Suffix;
            }
            return result;
        }
    }
}
