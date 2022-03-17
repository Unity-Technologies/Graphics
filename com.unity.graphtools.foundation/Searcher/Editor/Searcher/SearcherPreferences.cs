using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    /// <summary>
    /// Helper class to handle searcher preferences per searcher tool.
    /// <remarks>Internal class for automated tests purposes.</remarks>
    /// </summary>
    class SearcherPreferences
    {
        /// <summary>
        /// The data serialized in the EditorPrefs as json for each Searcher tool.
        /// <remarks>Internal class for automated tests purposes.</remarks>
        /// </summary>
        [Serializable]
        internal class DataPerTool
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="DataPerTool"/> class.
            /// </summary>
            public DataPerTool()
            {
                favoritesPerContext = new List<SerializableValue<List<string>>>();
                boolPrefs = new List<SerializableValue<bool>>();
                intPrefs = new List<SerializableValue<int>>();
                stringPrefs = new List<SerializableValue<string>>();
            }

            [Serializable]
            public class SerializableValue<T>
            {
                public string key;
                public T value;
            }

            [SerializeField]
            public List<SerializableValue<List<string>>> favoritesPerContext;
            [SerializeField]
            public List<SerializableValue<bool>> boolPrefs;
            [SerializeField]
            public List<SerializableValue<int>> intPrefs;
            [SerializeField]
            public List<SerializableValue<string>> stringPrefs;

            public IReadOnlyList<string> GetFavorites(string context)
            {
                return Get(context, k_EmptyFavorites, favoritesPerContext);
            }

            public void SetFavorite(string context, string itemPath, bool setFavorite = true)
            {
                var favorites = GetFavorites(context).ToList();
                if (setFavorite)
                    favorites.Add(itemPath);
                else
                    favorites.Remove(itemPath);
                Set(context, favorites, favoritesPerContext);
            }

            public bool GetBool(string key, bool defaultValue)
            {
                return Get(key, defaultValue, boolPrefs);
            }

            public void SetBool(string key, bool value)
            {
                Set(key, value, boolPrefs);
            }

            public int GetInt(string key, int defaultValue)
            {
                return Get(key, defaultValue, intPrefs);
            }

            public void SetInt(string key, int value)
            {
                Set(key, value, intPrefs);
            }

            public string GetString(string key, string defaultValue)
            {
                return Get(key, defaultValue, stringPrefs);
            }

            public void SetString(string key, string value)
            {
                Set(key, value, stringPrefs);
            }

            internal T Get<T>(string key, T defaultValue, List<SerializableValue<T>> list)
            {
                int keyIndex = GetIndexForKey(key, list);
                if (keyIndex == -1)
                    return defaultValue;
                return list[keyIndex].value;
            }

            internal void Set<T>(string key, T value, List<SerializableValue<T>> list)
            {
                int keyIndex = GetIndexForKey(key, list);
                var newTuple = new SerializableValue<T>() { key = key, value = value };
                if (keyIndex == -1)
                    list.Add(newTuple);
                else
                    list[keyIndex] = newTuple;
            }

            int GetIndexForKey<T>(string key, List<SerializableValue<T>> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].key == key)
                    {
                        return i;
                    }
                }
                return -1;
            }

            static readonly List<string> k_EmptyFavorites = new List<string>();
        }

        /// <summary>
        /// The key used in <see cref="EditorPrefs"/> to store preferences.
        /// </summary>
        public string PreferenceKey => $"SearcherPreferences.{ToolName}";

        /// <summary>
        /// The name of the Searcher Tool accessing preferences.
        /// </summary>
        public string ToolName { get; }

        /// <summary>
        /// The name of the context in which the searcher was created.
        /// </summary>
        public string Context { get; }

        static Dictionary<string, DataPerTool> s_CachedPrefs = new Dictionary<string, DataPerTool>();

        /// <summary>
        /// Used in tests as tests delete their own EditorPrefs keys to cleanup.
        /// </summary>
        internal static void InvalidateCache()
        {
            s_CachedPrefs = new Dictionary<string, DataPerTool>();
        }

        DataPerTool ToolPref
        {
            get => s_CachedPrefs[ToolName];
            set => s_CachedPrefs[ToolName] = value;
        }

        static readonly string k_PreviewTogglePrefName = "PreviewToggle";

        /// <summary>
        /// Gets or Sets the Visibility of the preview panel in the searcher.
        /// </summary>
        public bool PreviewVisibility
        {
            get => ToolPref.GetBool(k_PreviewTogglePrefName, false);
            set
            {
                ToolPref.SetBool(k_PreviewTogglePrefName, value);
                Save();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearcherPreferences"/> class.
        /// </summary>
        /// <param name="toolName">The name of the Searcher Tool accessing preferences.</param>
        /// <param name="context">The name of the context in which the searcher was created.</param>
        public SearcherPreferences(string toolName, string context)
        {
            ToolName = toolName;
            Context = context;
            Load();
        }

        /// <summary>
        /// Get a list of all favorite items in the current tool and context.
        /// </summary>
        /// <returns>A list of all favorite items in the current tool and context by their path.</returns>
        public IReadOnlyList<string> GetFavorites()
        {
            return ToolPref.GetFavorites(Context);
        }

        /// <summary>
        /// Adds or remove a favorite in the current tool and context.
        /// </summary>
        /// <param name="itemPath">The path of the item to be favorite</param>
        /// <param name="setFavorite">If true, adds a favorite. Removes from favorites otherwise.</param>
        public void SetFavorite(string itemPath, bool setFavorite)
        {
            ToolPref.SetFavorite(Context, itemPath, setFavorite);
            Save();
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            return ToolPref.GetBool(key, defaultValue);
        }

        public void SetBool(string key, bool value)
        {
            ToolPref.SetBool(key, value);
            Save();
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            return ToolPref.GetInt(key, defaultValue);
        }

        public void SetInt(string key, int value)
        {
            ToolPref.SetInt(key, value);
            Save();
        }

        public string GetString(string key, string defaultValue = "")
        {
            return ToolPref.GetString(key, defaultValue);
        }

        public void SetString(string key, string value)
        {
            ToolPref.SetString(key, value);
            Save();
        }

        /// <summary>
        /// Clear all favorites for the current tool and context.
        /// </summary>
        public void ClearFavorites()
        {
            var favoritesPerContext = ToolPref.favoritesPerContext;
            favoritesPerContext.RemoveAll(f => f.key == Context);
            Save();
        }

        void Load()
        {
            if (!s_CachedPrefs.ContainsKey(ToolName))
            {
                ToolPref = RetrievePrefs(PreferenceKey);
            }
        }

        void Save()
        {
            var value = JsonUtility.ToJson(ToolPref);
            EditorPrefs.SetString(PreferenceKey, value);
        }

        internal static DataPerTool RetrievePrefs(string preferenceKey)
        {
            if (EditorPrefs.HasKey(preferenceKey))
            {
                var prefStr = EditorPrefs.GetString(preferenceKey, "");
                var prefs = JsonUtility.FromJson<DataPerTool>(prefStr);
                if (prefs != null)
                    return prefs;
            }

            return new DataPerTool();
        }
    }
}
