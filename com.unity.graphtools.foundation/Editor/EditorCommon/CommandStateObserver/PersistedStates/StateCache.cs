using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    // Inspired by UnityEditor.StateCache<T>, which (1) is internal and (2) is lacking the
    // ability to hold different state types.
    sealed class StateCache : IDisposable
    {
        struct FileData
        {
            public string Data;
            public bool IsDirty;
        }

        Dictionary<string, FileData> m_InMemoryCache = new Dictionary<string, FileData>();
        string m_CacheFolder;

        public StateCache(string cacheFolder)
        {
            if (string.IsNullOrEmpty(cacheFolder))
                throw new ArgumentException(nameof(cacheFolder) + " cannot be null or empty string", cacheFolder);

            if (cacheFolder.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                throw new ArgumentException("Cache folder path has invalid path characters: '" + cacheFolder + "'");
            }

            cacheFolder = ConvertSeparatorsToUnity(cacheFolder);
            if (!cacheFolder.EndsWith("/"))
            {
                Debug.LogError("The cache folder path should end with a forward slash: '/'. Path: " + cacheFolder + ". Fixed up.");
                cacheFolder += "/";
            }
            if (cacheFolder.StartsWith("/"))
            {
                Debug.LogError("The cache folder path should not start with a forward slash: '/'. Path: " + cacheFolder + ". Fixed up."); // since on OSX a leading '/' means the root directory
                cacheFolder = cacheFolder.TrimStart('/');
            }

            m_CacheFolder = cacheFolder;
        }

        /// <inheritdoc />
        ~StateCache() {
            Flush();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Flush();
            GC.SuppressFinalize(this);
        }

        static string ConvertSeparatorsToUnity(string path)
        {
            return path.Replace('\\', '/');
        }

        bool FileExists(string path)
        {
            return m_InMemoryCache.TryGetValue(path, out _) || File.Exists(path);
        }

        string ReadFile(string path)
        {
            if (!m_InMemoryCache.TryGetValue(path, out var data))
            {
                try
                {

                    var content = File.ReadAllText(path, Encoding.UTF8);
                    m_InMemoryCache[path] = new FileData { Data = content, IsDirty = false };
                    TruncateCache();
                    return content;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading file {path}. Error: {e}");
                    return null;
                }
            }

            return data.Data;
        }

        void WriteFile(string path, string data)
        {
            m_InMemoryCache[path] = new FileData { Data = data, IsDirty = true };
            TruncateCache();
        }

        void TruncateCache()
        {
            if (m_InMemoryCache.Count > 64)
            {
                Flush();
            }
        }

        public TComponent GetState<TComponent>(Hash128 key, Func<TComponent> defaultValueCreator = null) where TComponent : class, IStateComponent
        {
            ThrowIfInvalid(key);

            TComponent obj = null;
            var filePath = GetFilePathForKey(key);
            if (FileExists(filePath))
            {
                var serializedData = ReadFile(filePath);
                if (serializedData != null)
                {
                    try
                    {
                        obj = StateComponentHelper.Deserialize<TComponent>(serializedData);
                    }
                    catch (ArgumentException exception)
                    {
                        Debug.LogError($"Invalid file content for {filePath}. Removing file. Error: {exception}");

                        // Remove invalid content
                        RemoveState(key);
                        obj = null;
                    }
                }
            }

            return obj ?? defaultValueCreator?.Invoke();
        }

        public void StoreState(Hash128 key, IStateComponent stateComponent)
        {
            var filePath = GetFilePathForKey(key);
            var serializedData = StateComponentHelper.Serialize(stateComponent);
            WriteFile(filePath, serializedData);
        }

        public void RemoveState(Hash128 key)
        {
            ThrowIfInvalid(key);

            string filePath = GetFilePathForKey(key);
            m_InMemoryCache.Remove(filePath);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        public void Flush()
        {
            bool noErr = true;
            foreach (var kv in m_InMemoryCache)
            {
                if (!kv.Value.IsDirty)
                    continue;

                try
                {
                    var directory = Path.GetDirectoryName(kv.Key);
                    if (directory != null)
                    {
                        Directory.CreateDirectory(directory);
                        File.WriteAllText(kv.Key, kv.Value.Data, Encoding.UTF8);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error saving file {kv.Key}. Error: {e}");
                    noErr = false;
                }
            }

            if (noErr)
            {
                m_InMemoryCache.Clear();
            }
        }

        static void ThrowIfInvalid(Hash128 key)
        {
            if (!key.isValid)
                throw new ArgumentException("Hash128 key is invalid: " + key);
        }

        internal string GetFilePathForKey(Hash128 key)
        {
            // Hashed folder structure to ensure we scale with large amounts of state files.
            // See: https://medium.com/eonian-technologies/file-name-hashing-creating-a-hashed-directory-structure-eabb03aa4091
            string hexKey = key.ToString();
            string hexFolder = hexKey.Substring(0, 2) + "/";
            return m_CacheFolder + hexFolder + hexKey + ".json";
        }
    }
}
