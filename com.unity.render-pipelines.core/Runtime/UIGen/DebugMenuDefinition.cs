using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering.UIGen
{
    // use it on the data source
    // must be on a static class
    [AttributeUsage(AttributeTargets.Class)]
    public class DeriveDebugMenuAttribute : Attribute
    {

    }

    // Use it on a property to display it in the debug menu
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DebugMenuPropertyAttribute : Attribute
    {
        public string primaryCategory { get; }
        public string secondaryCategory { get; }
        public string displayName { get; }
        public string tooltip { get; }

        public DebugMenuPropertyAttribute(
            [DisallowNull] string primaryCategory,
            [AllowNull] string secondaryCategory = null,
            [AllowNull] string displayName = null,
            [AllowNull] string tooltip = null
        )
        {
            this.primaryCategory = primaryCategory;
            this.secondaryCategory = secondaryCategory;
            this.displayName = displayName;
            this.tooltip = tooltip;
        }
    }
}

