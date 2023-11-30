#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;

namespace UnityEngine.Rendering
{
    internal static partial class RenderPipelineGraphicsSettingsStripper
    {
        public class Report : IDisposable
        {
            internal static string k_OutputPath = "Temp/graphics-settings-stripping.json";

            [Serializable]
            class SettingsStrippingInfo
            {
                public string type;
                public bool isAvailableInPlayerBuild;
                public bool strippersDefined;
            }

            [Serializable]
            class Export
            {
                public uint totalSettings = 0;
                public uint totalSettingsOnPlayer = 0;
                public List<SettingsStrippingInfo> settings = new();
            }

            public Report()
            {
                try
                {
                    if (File.Exists(k_OutputPath))
                        File.Delete(k_OutputPath);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            Export m_Data = new();

            public void AddStrippedSetting(Type settingsType, bool isAvailableInPlayerBuild, bool strippersDefined)
            {
                m_Data.totalSettings++;
                if (isAvailableInPlayerBuild)
                {
                    m_Data.totalSettingsOnPlayer++;
                }

                m_Data.settings.Add(new SettingsStrippingInfo()
                {
                    type = settingsType.AssemblyQualifiedName,
                    isAvailableInPlayerBuild = isAvailableInPlayerBuild,
                    strippersDefined = strippersDefined
                });
            }

            static void ExportStrippingInfo(string path, Export data)
            {
                try
                {
                    File.WriteAllText(path, JsonUtility.ToJson(data, true));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            public void Dispose()
            {
                ExportStrippingInfo(k_OutputPath, m_Data);
            }
        }
    }
}
#endif
