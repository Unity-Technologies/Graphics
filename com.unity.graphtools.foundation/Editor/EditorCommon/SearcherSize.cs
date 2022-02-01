using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Holds information about searcher dimensions.
    /// </summary>
    [Serializable]
    public struct SearcherSize
    {
        public static readonly SearcherSize defaultSearcherSize = new SearcherSize { Size = new Vector2(500, 400), RightLeftRatio = 1.0f };

        public Vector2 Size;
        public float RightLeftRatio;
    }

    /// <summary>
    /// Extension methods related to the searcher size.
    /// </summary>
    public static class PreferencesExtensionsForSearcherSize
    {
        static SerializedValueDictionary<string, SearcherSize> GetSizes(Preferences preferences)
        {
            SerializedValueDictionary<string, SearcherSize> sizes = null;
            var valueString = preferences.GetString(StringPref.SearcherSize);
            if (valueString != null)
            {
                sizes = JsonUtility.FromJson<SerializedValueDictionary<string, SearcherSize>>(valueString);
            }

            sizes ??= new SerializedValueDictionary<string, SearcherSize>();
            return sizes;
        }

        static void SaveSizes(Preferences preferences, SerializedValueDictionary<string, SearcherSize> sizes)
        {
            if (sizes != null)
            {
                var valueString = JsonUtility.ToJson(sizes);
                preferences.SetString(StringPref.SearcherSize, valueString);
            }
        }

        /// <summary>
        /// Gets the searcher window rect and left right ratio for <see cref="sizeName"/>.
        /// </summary>
        /// <param name="preferences">The object that contains size information.</param>
        /// <param name="sizeName">A string for the usage of the searcher.</param>
        public static SearcherSize GetSearcherSize(this Preferences preferences, string sizeName)
        {
            var sizes = GetSizes(preferences);
            if (string.IsNullOrEmpty(sizeName) || !sizes.TryGetValue(sizeName, out var size))
            {
                if (!sizes.TryGetValue("", out size))
                {
                    size = SearcherSize.defaultSearcherSize;
                }
            }
            return size;
        }

        /// <summary>
        /// Sets default searcher window size and left-right ratio for <see cref="sizeName"/>.
        /// </summary>
        /// <param name="preferences">The object that contains size information.</param>
        /// <param name="sizeName">A string for the usage of the searcher. Passing null for the usage will define the default for any searcher window.</param>
        /// <param name="size">The size of the window.</param>
        /// <param name="rightLeftRatio">The ratio between the left size and the right size (details) of the searcher.</param>
        public static void SetSearcherSize(this Preferences preferences, string sizeName, Vector2 size, float rightLeftRatio = 1.0f)
        {
            sizeName ??= "";

            var sizes = GetSizes(preferences);
            if (sizes.TryGetValue(sizeName, out var currentSize))
            {
                if (currentSize.Size == size && currentSize.RightLeftRatio == rightLeftRatio)
                {
                    return;
                }
            }

            sizes[sizeName] = new SearcherSize { Size = size, RightLeftRatio = rightLeftRatio };
            SaveSizes(preferences, sizes);
        }

        /// <summary>
        /// Sets searcher window size and left-right ratio for <see cref="sizeName"/>, if it is not already set.
        /// </summary>
        /// <param name="preferences">The object that contains size information.</param>
        /// <param name="sizeName">A string for the usage of the searcher. Passing null for the usage will define the default for any searcher window.</param>
        /// <param name="size">The size of the window.</param>
        /// <param name="rightLeftRatio">The ratio between the left size and the right size (details) of the searcher.</param>
        public static void SetInitialSearcherSize(this Preferences preferences, string sizeName, Vector2 size, float rightLeftRatio = 1.0f)
        {
            sizeName ??= "";

            var sizes = GetSizes(preferences);
            if (!sizes.TryGetValue(sizeName, out _))
            {
                sizes[sizeName] = new SearcherSize { Size = size, RightLeftRatio = rightLeftRatio };
                SaveSizes(preferences, sizes);
            }
        }

        internal static void ResetSearcherSizes(this Preferences preferences)
        {
            var sizes = GetSizes(preferences);
            sizes.Clear();
            SaveSizes(preferences, sizes);
        }
    }
}
