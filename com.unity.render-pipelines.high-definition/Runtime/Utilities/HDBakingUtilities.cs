using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    internal static class HDBakingUtilities
    {
        const string k_HDProbeAssetFormat = "{0}-{1}.exr";
        static readonly Regex k_HDProbeAssetRegex = new Regex(@"(?<type>ReflectionProbe|PlanarProbe)-(?<index>\d+)\.exr");

        public enum SceneObjectCategory
        {
            ReflectionProbe
        }

        public static string HDProbeAssetPattern(ProbeSettings.ProbeType type)
        {
            return string.Format("{0}-*.exr", type);
        }

        public static string GetBakedTextureDirectory(SceneManagement.Scene scene)
        {
            var scenePath = scene.path;
            if (string.IsNullOrEmpty(scenePath))
                return string.Empty;

            var cacheDirectoryName = Path.GetFileNameWithoutExtension(scenePath);
            var cacheDirectory = Path.Combine(Path.GetDirectoryName(scenePath), cacheDirectoryName);
            return cacheDirectory;
        }

        public static string GetBakedTextureFilePath(HDProbe probe)
        {
            return GetBakedTextureFilePath(
                probe.settings.type,
                SceneObjectIDMap.GetOrCreateSceneObjectID(
                    probe.gameObject, SceneObjectCategory.ReflectionProbe
                ),
                probe.gameObject.scene
            );
        }

        public static bool TryParseBakedProbeAssetFileName(
            string filename,
            out ProbeSettings.ProbeType type,
            out int index
        )
        {
            var match = k_HDProbeAssetRegex.Match(filename);
            if (!match.Success)
            {
                type = default(ProbeSettings.ProbeType);
                index = 0;
                return false;
            }

            type = (ProbeSettings.ProbeType)Enum.Parse(typeof(ProbeSettings.ProbeType), match.Groups["type"].Value);
            index = int.Parse(match.Groups["index"].Value);
            return true;
        }

        public static string GetBakedTextureFilePath(
            ProbeSettings.ProbeType probeType,
            int index,
            SceneManagement.Scene scene
        )
        {
            var cacheDirectory = GetBakedTextureDirectory(scene);
            var targetFile = Path.Combine(
                cacheDirectory,
                string.Format(k_HDProbeAssetFormat, probeType, index)
            );
            return targetFile;
        }

        public static void CreateParentDirectoryIfMissing(string path)
        {
            var fileInfo = new FileInfo(path);
            if (!fileInfo.Directory.Exists)
                fileInfo.Directory.Create();
        }

        public static bool TrySerializeToDisk<T>(T renderData, string filePath)
        {
            CreateParentDirectoryIfMissing(filePath);

            var serializer = new XmlSerializer(typeof(T));
            FileStream fileStream = null;
            try
            {
                fileStream = new FileStream(filePath, FileMode.Create);
                serializer.Serialize(fileStream, renderData);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
            finally
            {
                if (fileStream != null)
                    fileStream.Dispose();
            }
            return true;
        }

        public static bool TryDeserializeFromDisk<T>(string filePath, out T renderData)
        {
            if (!File.Exists(filePath))
            {
                renderData = default(T);
                return false;
            }

            var serializer = new XmlSerializer(typeof(T));
            FileStream fileStream = null;
            try
            {
                fileStream = new FileStream(filePath, FileMode.Open);
                renderData = (T)serializer.Deserialize(fileStream);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                renderData = default(T);
                return false;
            }
        }
    }
}
