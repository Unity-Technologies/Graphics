using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.ProviderSystem.Hints
{
    internal static class Common
    {
        internal const string kDisplayName = "sg:DisplayName";
        internal const string kTooltip = "sg:Tooltip";
    }

    internal class DisplayName<T> : IStrongHint<T> where T : IShaderObject
    {
        public string Key { get; private set; }
        public IReadOnlyCollection<string> Synonyms { get; } = new string[] { "Label", "Title" };
        string fallback;
        internal DisplayName(string name = Common.kDisplayName, string fallback = null) { Key = name; this.fallback = fallback; }
        public bool Process(bool found, string rawValue, T obj, IProvider provider, out object value, out string msg)
        {
            msg = null;
            value = found ? rawValue : fallback ?? obj.Name;
            return true; // We always resolve a display name.
        }
    }

    internal class Tooltip<T> : IStrongHint<T> where T : IShaderObject
    {
        public string Key { get; private set; }
        public IReadOnlyCollection<string> Synonyms { get; } = new [] { "summary" };
        internal Tooltip(string name = Common.kTooltip) { Key = name; }
        public bool Process(bool found, string rawValue, T obj, IProvider provider, out object value, out string msg)
        {
            msg = null;
            value = rawValue;
            return found;
        }
    }

    internal class Flag<T> : IStrongHint<T> where T : IShaderObject
    {
        public string Key { get; private set; }
        public IReadOnlyCollection<string> Conflicts { get; private set; }

        internal Flag(string name) { Key = name; Conflicts = null; }
        internal Flag(string name, string conflict) { Key = name; Conflicts = new[] { conflict }; }
        internal Flag(string name, string[] conflicts) { Key = name; Conflicts = conflicts; }

        public bool Process(bool found, string rawValue, T obj, IProvider provider, out object value, out string msg)
        {
            msg = null;
            value = rawValue;
            if (!string.IsNullOrWhiteSpace(rawValue))
                msg = $"Expects an empty argument string.";
            return found;
        }
    }
}
