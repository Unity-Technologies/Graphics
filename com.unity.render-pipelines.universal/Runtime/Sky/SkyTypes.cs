
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Rendering.Universal
{
    // TODO Replace these with hashes
    public enum SkyType
    {
        HDRI = 1,
        Procedural = 2,
        Gradient = 3,
        PhysicallyBased = 4,
    }

    public sealed class SkyTypesCatalog
    {
        static Dictionary<int, Type> m_SkyTypesDict = null;
        public static Dictionary<int, Type> skyTypesDict { get { if (m_SkyTypesDict == null) UpdateSkyTypes(); return m_SkyTypesDict; } }

        static void UpdateSkyTypes()
        {
            if (m_SkyTypesDict == null)
            {
                m_SkyTypesDict = new Dictionary<int, Type>();

                var skyTypes = CoreUtils.GetAllTypesDerivedFrom<SkySettings>().Where(t => !t.IsAbstract);
                foreach (Type skyType in skyTypes)
                {
                    var uniqueIDs = skyType.GetCustomAttributes(typeof(SkyUniqueID), false);
                    if (uniqueIDs.Length == 0)
                    {
                        Debug.LogWarningFormat($"Missing attribute SkyUniqueID on class {skyType}. Class won't be registered as an available sky.");
                        continue;
                    }

                    int uniqueID = ((SkyUniqueID)uniqueIDs[0]).uniqueID;
                    if (uniqueID == 0)
                    {
                        Debug.LogWarningFormat($"0 is a reserved SkyUniqueID and is used in class {skyType}. Class won't be registered as an available sky.");
                        continue;
                    }

                    Type value;
                    if (m_SkyTypesDict.TryGetValue(uniqueID, out value))
                    {
                        Debug.LogWarningFormat($"SkyUniqueID {uniqueID} used in class {skyType} is already used in class {value}. Class won't be registered as an available sky.");
                        continue;
                    }

                    m_SkyTypesDict.Add(uniqueID, skyType);
                }
            }
        }
    }
}
