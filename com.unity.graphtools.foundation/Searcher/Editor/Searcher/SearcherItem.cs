using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    /// <summary>
    /// Represents an item to display in the searcher.
    /// </summary>
    [PublicAPI]
    [Serializable]
    public class SearcherItem
    {
        [SerializeField] string m_CategoryPath;
        [SerializeField] string m_Name;
        [SerializeField] string m_Help;
        [SerializeField] string[] m_Synonyms;
        [SerializeField] string m_StyleName;
        [SerializeField] int m_Priority;

        public static readonly char CategorySeparator = '/';

        /// <summary>
        /// Name of the item.
        /// <remarks>Used to find the item during search.</remarks>
        /// </summary>
        public virtual string Name => m_Name ?? "";

        /// <summary>
        /// Full name of the item including its path as a searchable string.
        /// e.g. path separated by spaces rather than /.
        /// <example>"Food Fruits Berries Strawberry"</example>
        /// </summary>
        public string SearchableFullName
        {
            get => FullName.Replace(CategorySeparator, ' ');
        }

        /// <summary>
        /// Name of the item including its category path.
        /// <example>"Food/Fruits/Berries/Strawberry"</example>
        /// <remarks>This will set the <see cref="CategoryPath"/> and <see cref="Name"/> properties.</remarks>
        /// </summary>
        public string FullName
        {
            get => CategoryPath == "" ? Name : Name == "" ? CategoryPath : CategoryPath + CategorySeparator + Name;
            set
            {
                var success = ExtractPathAndNameFromFullName(value, out m_CategoryPath, out m_Name);
                if (!success)
                    Debug.LogWarning($"error parsing SearcherItem fullname '{value}'.Category path set to '{m_CategoryPath}' and name set to '{m_Name}'");
            }
        }

        public string[] GetParentCategories() => CategoryPath.Split(CategorySeparator);

        /// <summary>
        /// The category in which this item belongs, in a directory format.
        /// <example>"Food/Fruits/Berries"</example>
        /// </summary>
        public string CategoryPath
        {
            get => m_CategoryPath ?? "";
            set => m_CategoryPath = value;
        }

        /// <summary>
        /// Help content to display about this item.
        /// </summary>
        public string Help
        {
            get => m_Help;
            set => m_Help = value;
        }

        /// <summary>
        /// Synonyms of this item.
        /// <remarks> Might be used to find the item by an alternate name.</remarks>
        /// </summary>
        public string[] Synonyms
        {
            get => m_Synonyms;
            set => m_Synonyms = value;
        }

        /// <summary>
        /// Custom User Data.
        /// </summary>
        [Obsolete("You should create your own class inheriting from SearcherItem. (2021-09-21)")]
        public object UserData { get; set; }

        /// <summary>
        /// Custom name used to generate USS styles when creating UI for this item.
        /// </summary>
        public string StyleName
        {
            get => m_StyleName;
            set => m_StyleName = value;
        }

        /// <summary>
        /// Number to allow some items to come before others.
        /// <remarks>The lower, the higher the priority is.</remarks>
        /// </summary>
        public int Priority
        {
            get => m_Priority;
            set => m_Priority = value;
        }

        static (Func<SearcherItem, IEnumerable<string>> getSearchData, float ratio)[] s_SearchKeysRatios =
        {
            (si => Enumerable.Repeat(si.Name, 1), 1f),
            (si => Enumerable.Repeat(si.SearchableFullName, 1), 0.5f),
            (si => si.Synonyms ?? Enumerable.Empty<string>(), 0.5f),
        };

        /// <summary>
        /// Data to apply search query on, with ratios of importance for each one.
        /// </summary>
        public IEnumerable<(IEnumerable<string> searchData, float ratio)> SearchKeys =>
            s_SearchKeysRatios.Select(tu => (tu.getSearchData(this), tu.ratio));

        /// <summary>
        /// Initializes a new instance of the <see cref="SearcherItem"/> class.
        /// </summary>
        public SearcherItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearcherItem"/> class.
        /// </summary>
        /// <param name="name">The name used to search the item.</param>
        public SearcherItem(string name)
        {
            m_Name = name;
        }

        static bool ExtractPathAndNameFromFullName(string fullName, out string path, out string name)
        {
            path = "";
            name = fullName;
            var nameParts = fullName.Split(CategorySeparator);
            if (nameParts.Length > 1)
            {
                name = nameParts[nameParts.Length - 1];
                path = fullName.Substring(0, fullName.Length - name.Length - 1);
                return true;
            }
            return nameParts.Length == 1;
        }

        /// <summary>
        /// Instantiates a Searcher item.
        /// </summary>
        /// <param name="name">Name of the item.</param>
        /// <param name="help">Help content to display about the item.</param>
        /// <param name="children">Children of the item.</param>
        /// <param name="userData">Custom user data to store.</param>
        /// <param name="icon">Icon to display with this item.</param>
        /// <param name="collapseEmptyIcon">Whether this item icon should be collapsed or not if it's empty.</param>
        /// <param name="styleName">Custom name used to generate USS styles when creating UI for this item.</param>
        [Obsolete("SearcherItems don't have children, as they aren't used to represent categories anymore. Specify CategoryPath such as 'Food/Fruits', or FullName such as 'Food/Fruits/Apple'")]
        public SearcherItem(string name, string help, List<SearcherItem> children, object userData = null, Texture2D icon = null, bool collapseEmptyIcon = true, string styleName = null)
        {
        }

        /// <summary>
        /// Add a Searcher Item as a child of this item.
        /// </summary>
        /// <param name="child">The children to add.</param>
        [Obsolete("SearcherItems don't have children, as they aren't used to represent categories anymore. Specify CategoryPath such as 'Food/Fruits', or FullName such as 'Food/Fruits/Apple'.")]
        public void AddChild(SearcherItem child)
        {
        }

        /// <summary>
        /// Build data for this item.
        /// Called during the Database Indexing.
        /// The item can be created with a lightweight representation and only gather expensive data when this is called.
        /// </summary>
        public virtual void Build()
        {
        }

        /// <summary>
        /// String representation of this item.
        /// </summary>
        /// <returns>The representation of this item as a string.</returns>
        public override string ToString()
        {
            return $"{nameof(FullName)}: {FullName}";
        }
    }
}
