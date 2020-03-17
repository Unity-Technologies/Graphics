using UnityEngine;
using UnityEditor.VFX;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.UI;
using UnityEditor;
using System.Runtime.CompilerServices;

using SysRegex = System.Text.RegularExpressions.Regex;

namespace UnityEditor.VFX
{
    internal class VFXSystemNames
    {
        public static readonly string DefaultSystemName = "System";

        private static readonly string IndexPattern = @" (\(([0-9])*\))$";
        private Dictionary<VFXModel, int> m_SystemToIndex = new Dictionary<VFXModel, int>();

        public static string GetSystemName(VFXModel model)
        {
            // general case
            var data = model as VFXData;
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
            return null;
        }

        public static void SetSystemName(VFXModel model, string name)
        {
            // general case
            var data = model as VFXData;
            if (data != null)
            {
                data.title = name;
                return;
            }

            // special case for spawner
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
        }

        private static string GetSystemUnindexedName(VFXModel model)
        {
            var name = GetSystemName(model);
            return string.IsNullOrEmpty(name) ? name : SysRegex.Replace(name, IndexPattern, "");
        }

        private static int ExtractIndex(string name)
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

        public string GetUniqueSystemName(VFXModel model)
        {
            int index;
            if (m_SystemToIndex.TryGetValue(model, out index))
            {
                var wishedName = GetSystemUnindexedName(model);
                if (string.IsNullOrEmpty(wishedName))
                    wishedName = DefaultSystemName;
                var format = "{0} ({1})";
                var newName = index == 0 ? wishedName : string.Format(format, wishedName, index);
                return newName;
            }
            if (!(model is VFXSubgraphContext))
                throw new InvalidOperationException("SystemNames : Model is not registered " + model);
            return GetSystemName(model);
        }

        public void Sync(VFXGraph graph)
        {
            var models = new HashSet<ScriptableObject>();
            graph.CollectDependencies(models, false);

            var systems = models.OfType<VFXContext>()
                .Where(c => c.contextType == VFXContextType.Spawner || c.GetData() != null)
                .Select(c => c.contextType == VFXContextType.Spawner ? c as VFXModel : c.GetData())
                .Distinct();

            Init(systems);
        }

        public void Init(IEnumerable<VFXModel> models)
        {
            m_SystemToIndex.Clear();
            foreach (var system in models)
            {
                var systemName = GetSystemUnindexedName(system);
                var index = GetIndex(systemName);
                m_SystemToIndex[system] = index;
            }
        }

        private int GetIndex(string unindexedName)
        {
            int index = -1;

            IEnumerable<int> unavailableIndices;
            if (string.IsNullOrEmpty(unindexedName) || unindexedName == DefaultSystemName)
                unavailableIndices = m_SystemToIndex.Where(pair => (string.IsNullOrEmpty(GetSystemUnindexedName(pair.Key)) || GetSystemUnindexedName(pair.Key) == DefaultSystemName)).Select(pair => pair.Value);
            else
                unavailableIndices = m_SystemToIndex.Where(pair => GetSystemUnindexedName(pair.Key) == unindexedName).Select(pair => pair.Value);
            if (unavailableIndices.Any())
            {
                var unavailableIndicesList = unavailableIndices.ToList();
                unavailableIndicesList.Sort();
                for (int i = 0; i < unavailableIndicesList.Count(); ++i)
                    if (i != unavailableIndicesList[i])
                    {
                        index = i;
                        break;
                    }

                if (index == -1)
                    index = unavailableIndicesList.Last() + 1;
            }
            else
            {
                index = 0;
            }

            return index;
        }
    }
}
