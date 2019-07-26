using UnityEngine;
using UnityEditor.VFX;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.UI;
using UnityEditor;
using NUnit.Framework;
using System.Runtime.CompilerServices;

using SysRegex = System.Text.RegularExpressions.Regex;

namespace UnityEditor.VFX
{
    internal class VFXSystemNames
    {

        public static readonly string DefaultSystemName = "System";

        private static readonly string IndexPattern = @" (\(([0-9])*\))$";
        //private Dictionary<VFXModel, string> m_UnindexedNames = new Dictionary<VFXModel, string>();
        //private Dictionary<string, List<int>> m_DuplicatesIndices = new Dictionary<string, List<int>>();


        private Dictionary<VFXModel, int> m_SystemToIndex = new Dictionary<VFXModel, int>();

        public static string GetSystemName(VFXModel model)
        {
            var data = model as VFXData;
            // general case
            if (data != null)
            {
                return data.title;
            }

            // special case for spawners
            var context = model as VFXContext;
            if (context != null)
            {
                if (context.contextType == VFXContextType.Spawner)
                    return context.label;
                else
                {
                    var contextData = context.GetData();
                    if (contextData != null)
                        return contextData.title;
                }
                    
            }

            Debug.LogError("model not associated to a system");
            return null;
        }

        public static void SetSystemName(VFXModel model, string name)
        {
            var data = model as VFXData;
            if (data != null)
            {
                data.title = name;
                return;
            }

            var context = model as VFXContext;
            if (context != null)
            {
                if (context.contextType == VFXContextType.Spawner)
                {
                    context.label = name;
                    return;
                }
                else
                {
                    var contextData = context.GetData();
                    if (contextData != null)
                    {
                        contextData.title = name;
                        return;
                    }
                }
            }

            Debug.LogError("model not associated to a system");
        }

        public string GetUniqueSystemName(VFXModel model)
        {
            int index;
            if (m_SystemToIndex.TryGetValue(model, out index))
            {
                var wishedName = GetSystemName(model);
                if (!string.IsNullOrEmpty(wishedName))
                {
                    var format = "{0} ({1})";
                    var newName = index == 0 ? wishedName : string.Format(format, wishedName, index);
                    return newName;
                }
            }
            Debug.LogError("GetUniqueSystemName::Error: model not registered");
            return string.Empty;
        }

        public static int ExtractIndex(string name)
        {
            if (SysRegex.IsMatch(name, IndexPattern))
            {
                var afterOpeningBracket = name.LastIndexOf('(') + 1;
                var closingBracket = name.LastIndexOf(')');
                var index = name.Substring(afterOpeningBracket, closingBracket - afterOpeningBracket);
                return int.Parse(index);
            }
            return 0;
        }

        public void Sync(VFXGraph graph)
        {
            var models = new HashSet<ScriptableObject>();
            graph.CollectDependencies(models, false);

            var systems = models.OfType<VFXContext>()
                .Where(c => c.contextType == VFXContextType.Spawner || c.GetData() != null)
                .Select(c => c.contextType == VFXContextType.Spawner ? c as VFXModel : c.GetData())
                .Distinct().ToList();

            Init(systems);
        }

        private void Init(IEnumerable<VFXModel> models)
        {
            m_SystemToIndex.Clear();

            foreach (var system in models)
            {
                /*if (!(system is VFXDataParticle || system is VFXContext))
                    continue;*/

                var systemName = GetSystemName(system);
                if (string.IsNullOrEmpty(systemName))
                {
                    SetSystemName(system, DefaultSystemName);
                    systemName = GetSystemName(system);
                }

                var index = GetIndex(systemName);
                m_SystemToIndex[system] = index;
            }
            Debug.Log("Init");
        }

        /// <summary>
        /// Registers a system name, eventually corrects it so it is unique, and returns it.
        /// If an indexed name is supplied, index will not be considered, and will probably change even if it is a correct one.
        /// I'm the index master >:-)
        /// </summary>
        public string AddAndCorrect(VFXGraph graph, VFXModel system, string wishedName)
        {
            //Debug.Log("AAC: " + RuntimeHelpers.GetHashCode(system));
            /*if (string.IsNullOrEmpty(wishedName))
                wishedName = DefaultSystemName;
            var unindexedName = SysRegex.Replace(wishedName, IndexPattern, "");

            RemoveSystem(graph, system);

            m_UnindexedNames[system] = unindexedName;

            return GetIndex(unindexedName);*/

            return string.Empty;
        }

        public void RemoveSystem(VFXGraph graph, VFXModel system, bool removeFromUnindexNames = true)
        {
            // if system is not of type VFXDataParticle, or if it is not a spawner of type VFXContext, abort.
            /*if (!(system is VFXDataParticle))
            {
                var context = system as VFXContext;
                if (context == null || context.contextType != VFXContextType.Spawner)
                    return;
            }
            string unindexedName;
            m_UnindexedNames.TryGetValue(system, out unindexedName);
            if (!string.IsNullOrEmpty(unindexedName))
            {
                List<int> duplicateIndices;
                m_DuplicatesIndices.TryGetValue(unindexedName, out duplicateIndices);
                if (duplicateIndices != null)
                {
                    int index = ExtractIndex(ExtractName(system));
                    duplicateIndices.Remove(index);
                    if (duplicateIndices.Count() == 0)
                        m_DuplicatesIndices.Remove(unindexedName);
                }
                if (removeFromUnindexNames)
                    m_UnindexedNames.Remove(system);
            }*/

        }

        private int GetIndex(string unindexedName)
        {
            int index = -1;

            List<int> unavailableIndices = m_SystemToIndex.Where(pair => GetSystemName(pair.Key) == unindexedName).Select(pair => pair.Value).ToList();
            //m_DuplicatesIndices.TryGetValue(unindexedName, out unavailableIndices);
            if (unavailableIndices != null && unavailableIndices.Count() > 0)
            {
                unavailableIndices.Sort();
                for (int i = 0; i < unavailableIndices.Count(); ++i)
                    if (i != unavailableIndices[i])
                    {
                        index = i;
                        break;
                    }

                if (index == -1)
                    index = unavailableIndices[unavailableIndices.Count() - 1] + 1;
            }
            else
            {
                index = 0;
            }

            return index;
        }

    }
}
