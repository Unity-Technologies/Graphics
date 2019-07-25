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
        private Dictionary<VFXModel, string> m_UnindexedNames = new Dictionary<VFXModel, string>();
        private Dictionary<string, List<int>> m_DuplicatesIndices = new Dictionary<string, List<int>>();


        public static string ExtractName(VFXModel system)
        {
            return system.systemName;
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
                .Select(c =>  c.GetData() != null ? c.GetData() as VFXModel : c)
                .Distinct().ToList();

            Init(systems);
        }

        /* Handling user modifications */
        public static void UIUpdate(VFXSystemBorder systemBorder, string newName)
        {
            systemBorder.controller.title = newName;
            systemBorder.title = systemBorder.controller.title;
        }

        public static void UIUpdate(VFXContextUI contextUI, string newName)
        {
            if (contextUI.controller.model.contextType == VFXContextType.Spawner)
            {
                contextUI.controller.model.systemName = newName;
            }
        }

        public void Init(IEnumerable<VFXModel> models)
        {
            m_UnindexedNames = new Dictionary<VFXModel, string>();
            m_DuplicatesIndices = new Dictionary<string, List<int>>();

            foreach (var system in models)
            {
                //Debug.Log("Init: " + RuntimeHelpers.GetHashCode(system));
                if (!(system is VFXDataParticle || system is VFXContext))
                    continue;
                var systemName = ExtractName(system);
                if (string.IsNullOrEmpty(systemName))
                    systemName = DefaultSystemName;
                
                var unindexedName = SysRegex.Replace(systemName, IndexPattern, "");
                system.systemName = unindexedName;
                if (!m_UnindexedNames.ContainsKey(system))
                    Debug.LogError("Init: key lost. system.systemName: " + system.systemName);
            }
        }

        /// <summary>
        /// Removed every registered system that is not in models.
        /// </summary>
        public void Sanitize(IEnumerable<VFXModel> models)
        {
            if (models != null)
            {
                var systems = models.OfType<VFXContext>().Select(context => context.GetData() != null ? context.GetData() as VFXModel : context as VFXModel);
                List<VFXModel> registeredModels = new List<VFXModel>(m_UnindexedNames.Keys);
                foreach (var registeredModel in registeredModels)
                {
                    if (!systems.Contains(registeredModel))
                        RemoveSystem(registeredModel, true);
                }
            }
            else
            {
                m_UnindexedNames.Clear();
                m_DuplicatesIndices.Clear();
            }
        }

        /// <summary>
        /// Registers a system name, eventually corrects it so it is unique, and returns it.
        /// If an indexed name is supplied, index will not be considered, and will probably change even if it is a correct one.
        /// I'm the index master >:-)
        /// </summary>
        public string AddAndCorrect(VFXModel system, string wishedName)
        {
            //Debug.Log("AAC: " + RuntimeHelpers.GetHashCode(system));
            if (string.IsNullOrEmpty(wishedName))
                wishedName = DefaultSystemName;
            var unindexedName = SysRegex.Replace(wishedName, IndexPattern, "");

            RemoveSystem(system);

            m_UnindexedNames[system] = unindexedName;

            return MakeUnique(unindexedName);
        }

        public void RemoveSystem(VFXModel system, bool removeFromUnindexNames = true)
        {
            // if system is not of type VFXDataParticle, or if it is not a spawner of type VFXContext, abort.
            if (!(system is VFXDataParticle))
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
            }
        }

        private string MakeUnique(string unindexedName)
        {
            int index = -1;

            List<int> unavailableIndices;
            m_DuplicatesIndices.TryGetValue(unindexedName, out unavailableIndices);
            if (unavailableIndices != null)
            {
                unavailableIndices.Sort();// TODO: no need to sort each time, if every element is added in a sorted list
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
                unavailableIndices = new List<int>();
                m_DuplicatesIndices[unindexedName] = unavailableIndices;
                index = 0;
            }

            var format = "{0} ({1})";
            var newName = index == 0 ? unindexedName : string.Format(format, unindexedName, index);

            unavailableIndices.Add(index);
            return newName;
        }

    }
}
