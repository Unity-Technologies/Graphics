using System;
using UnityEngine.Assertions;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Attribute to create a Searcher Item out of a <see cref="IGraphElementModel"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SearcherItemAttribute : Attribute
    {
        /// <summary>
        /// Type of Stencil to use to create the element.
        /// </summary>
        public Type StencilType { get; }
        /// <summary>
        /// Path of the item in the searcher.
        /// </summary>
        public string Path { get; }
        /// <summary>
        /// Search context where this item should figure.
        /// </summary>
        public SearcherContext Context { get; }

        /// <summary>
        /// Style to apply to this item.
        /// </summary>
        /// <remarks>Allows UI to apply custom styles to elements made with this item.</remarks>
        public string StyleName { get; }

        /// <summary>
        /// Initializes a new instance of the SearcherItemAttribute class.
        /// </summary>
        /// <param name="stencilType">Type of Stencil to use to create the element.</param>
        /// <param name="context">Search context where this item should figure.</param>
        /// <param name="path">Path of the item in the searcher.</param>
        /// <param name="styleName">Style name to give to this item.</param>
        public SearcherItemAttribute(Type stencilType, SearcherContext context, string path, string styleName = null)
        {
            Assert.IsTrue(
                stencilType.IsSubclassOf(typeof(Stencil)),
                $"Parameter stencilType is type of {stencilType.FullName} which is not a subclass of {typeof(Stencil).FullName}");

            StencilType = stencilType;
            Path = path;
            Context = context;
            StyleName = styleName;
        }
    }
}
