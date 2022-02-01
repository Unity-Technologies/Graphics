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
        [SerializeField] int m_Id;
        [SerializeField] List<int> m_ChildrenIds;
        [SerializeField] string m_Name;
        [SerializeField] string m_Help;
        [SerializeField] string[] m_Synonyms;
        [SerializeField] Texture2D m_Icon;
        [SerializeField] bool m_CollapseEmptyIcon = true;

        internal long lastSearchScore;
        internal string lastMatchedString;
        internal List<int> lastMatchedIndices;

        /// <summary>
        /// Index in the database.
        /// </summary>
        public int Id => m_Id;

        /// <summary>
        /// Name of the Item.
        /// </summary>
        public virtual string Name => m_Name;

        /// <summary>
        /// Path in the hierarchy of items.
        /// </summary>
        public string Path { get; private set; }

        public int ChildrenCapacity { get; set; }

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
        /// Might be used to find the item by an alternate name.
        /// </summary>
        public string[] Synonyms
        {
            get => m_Synonyms;
            set => m_Synonyms = value;
        }

        /// <summary>
        /// Depth of this item in the hierarchy.
        /// </summary>
        public int Depth => Parent?.Depth + 1 ?? 0;

        /// <summary>
        /// Custom User Data.
        /// </summary>
        public object UserData { get; set; }

        /// <summary>
        /// Icon associated with this item.
        /// </summary>
        public Texture2D Icon { get => m_Icon; set => m_Icon = value; }

        /// <summary>
        /// Whether the icon of this item should be collapsed or not if empty.
        /// </summary>
        public bool CollapseEmptyIcon { get => m_CollapseEmptyIcon; set => m_CollapseEmptyIcon = value; }

        /// <summary>
        /// Parent of this item in the hierarchy.
        /// </summary>
        public SearcherItem Parent { get; private set; }

        /// <summary>
        /// Whether this item has children or not.
        /// </summary>
        public bool HasChildren => Children.Count > 0;

        /// <summary>
        /// Database this items belongs to.
        /// </summary>
        public SearcherDatabaseBase Database { get; private set; }


        static (Func<SearcherItem, IEnumerable<string>> getSearchData, float ratio)[] s_SearchKeysRatios =
        {
            (si => Enumerable.Repeat(si.Name, 1), 1f),
            (si => Enumerable.Repeat(si.Path, 1), 0.5f),
            (si => si.Synonyms ?? Enumerable.Empty<string>(), 0.5f),
        };

        /// <summary>
        /// Data to apply search query on, with ratios of importance for each one
        /// </summary>
        public IEnumerable<(IEnumerable<string> searchData, float ratio)> SearchKeys =>
            s_SearchKeysRatios.Select(tu => (tu.getSearchData(this), tu.ratio));

        // the backing field gets serialized otherwise and triggers a "Serialization depth limit 7 exceeded" warning

        /// <summary>
        /// Children of this item in the hierarchy.
        /// </summary>
        [field: NonSerialized]
        public List<SearcherItem> Children { get; private set; }

        /// <summary>
        /// Instantiates a Searcher item.
        /// </summary>
        /// <param name="name">Name of the item.</param>
        /// <param name="help">Help content to display about the item.</param>
        /// <param name="children">Children of the item.</param>
        /// <param name="userData">Custom user data to store.</param>
        /// <param name="icon">Icon to display with this item.</param>
        /// <param name="collapseEmptyIcon">Whether this item icon should be collapsed or not if it's empty.</param>
        public SearcherItem(string name, string help = "", List<SearcherItem> children = null, object userData = null, Texture2D icon = null, bool collapseEmptyIcon = true)
        {
            m_Id = -1;
            Parent = null;
            Database = null;

            m_Name = name;
            m_Help = help;
            m_Icon = icon;
            UserData = userData;
            m_CollapseEmptyIcon = collapseEmptyIcon;

            Children = new List<SearcherItem>();
            if (children == null)
                return;

            Children = children;
            foreach (var child in children)
                child.OverwriteParent(this);
        }

        /// <summary>
        /// Add a Searcher Item as a child of this item.
        /// </summary>
        /// <param name="child">The children to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if the children to add was null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the item doesn't belong to a database.</exception>
        public void AddChild(SearcherItem child)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));

            if (Database != null)
                throw new InvalidOperationException(
                    "Cannot add more children to an item that was already used in a database.");

            Children = Children ?? new List<SearcherItem>(ChildrenCapacity > 0 ? ChildrenCapacity : 0);

            Children.Add(child);
            child.OverwriteParent(this);
        }

        internal void OverwriteId(int newId)
        {
            m_Id = newId;
        }

        void OverwriteParent(SearcherItem newParent)
        {
            Parent = newParent;
        }

        internal void OverwriteDatabase(SearcherDatabaseBase newDatabase)
        {
            Database = newDatabase;
        }

        internal void OverwriteChildrenIds(List<int> childrenIds)
        {
            m_ChildrenIds = childrenIds;
        }

        /// <summary>
        /// Build data for this item.
        /// Called during the Database Indexing.
        /// The item can be created with a lightweight representation and only gather expensive data when this is called.
        /// </summary>
        public virtual void Build()
        {
            GeneratePath();
        }

        internal void GeneratePath()
        {
            if (Parent != null)
                Path = Parent.Path + " ";
            else
                Path = string.Empty;
            Path += Name;
        }

        internal void ReInitAfterLoadFromFile()
        {
            if (Children == null)
                Children = new List<SearcherItem>(m_ChildrenIds.Count);

            foreach (var id in m_ChildrenIds)
            {
                var child = Database.IndexedItems[id];
                Children.Add(child);
                child.OverwriteParent(this);
            }

            GeneratePath();
        }

        /// <summary>
        /// String representation of this item.
        /// </summary>
        /// <returns>The representation of this item as a string.</returns>
        public override string ToString()
        {
            return $"{nameof(Id)}: {Id}, {nameof(Name)}: {Name}, {nameof(Depth)}: {Depth}";
        }
    }
}
