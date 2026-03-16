using System.Text;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.ProviderSystem.Hints
{
    internal static class Func
    {
        internal const string kProviderKey = "sg:ProviderKey";

        internal const string kReturnDisplayName = "sg:ReturnDisplayName";

        internal const string kSearchTerms = "sg:SearchTerms";
        internal const string kSearchName = "sg:SearchName";
        internal const string kSearchCategory = "sg:SearchCategory";

        internal const string kPrecision = "sg:DynamicPrecision";

        // Not yet implemented.
        internal const string kGroupKey = "sg:GroupKey";
        internal const string kReturnTooltip = "sg:ReturnTooltip";
        internal const string kDocumentationLink = "sg:HelpURL";

        // ProviderKey associated hints, Not yet implemented.
        internal const string kDeprecated = "sg:Deprecated";
        internal const string kObsolete = "sg:Obsolete";
        internal const string kVersion = "sg:Version";
    }

    class ProviderKey : IStrongHint<IShaderFunction>
    {
        public string Key => Func.kProviderKey;
        public bool AlwaysProcess => true;
        public bool AllowDisqualifiedSynonyms => false;
        public bool Process(bool found, string rawValue, IShaderFunction obj, IProvider provider, out object value, out string msg, string actualHintKey)
        {
            string sourcePath = AssetDatabase.GUIDToAssetPath(provider.AssetID);

            value = provider.ProviderKey;
            msg = !found
                ? $"Expected; but none found for '{provider.ProviderKey}' in '{sourcePath}'."
                : null;

            return true;
        }
    }

    class SearchName : IStrongHint<IShaderFunction>
    {
        public string Key => Func.kSearchName;
        public bool AlwaysProcess => true;
        public bool Process(bool found, string rawValue, IShaderFunction obj, IProvider provider, out object value, out string msg, string actualHintKey)
        {
            msg = null;
            value = found ? rawValue : ShaderObjectUtils.QualifySignature(obj, false, true);
            return true;
        }
    }

    class SearchTerms : IStrongHint<IShaderFunction>
    {
        public string Key => Func.kSearchTerms;
        public bool AlwaysProcess => true;
        public bool Process(bool found, string rawValue, IShaderFunction obj, IProvider provider, out object value, out string msg, string actualHintKey)
        {
            msg = null;
            value = found ? HeaderUtils.LazyTokenString(rawValue) : new string[] { obj.Name };
            return true;
        }
    }

    class SearchCategory : IStrongHint<IShaderFunction>
    {
        public string Key => Func.kSearchCategory;
        public bool AlwaysProcess => true;
        public IReadOnlyCollection<string> Synonyms { get; } = new[] { "Category" };
        public bool Process(bool found, string rawValue, IShaderFunction obj, IProvider provider, out object value, out string msg, string actualHintKey)
        {
            msg = null;
            if (found)
            {
                value = rawValue;
            }
            else
            {
                StringBuilder catsb = new();
                foreach(var name in obj.Namespace)
                {
                    catsb.Append($"/{name}");
                }
                var category = catsb.ToString();
                if (!string.IsNullOrWhiteSpace(category))
                {
                    value = $"Reflected by Namespace{category}";
                }
                else if (provider.AssetID != default)
                {
                    var path = AssetDatabase.GUIDToAssetPath(provider.AssetID);
                    value = $"Reflected by Path/{path}";
                }
                else
                {
                    value = "Uncategorized";
                }
            }
            return true;
        }
    }
}
