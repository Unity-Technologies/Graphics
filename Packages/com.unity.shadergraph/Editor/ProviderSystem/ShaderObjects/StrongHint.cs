using System.Collections.Generic;
using System.Text;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    interface IStrongHint<T> where T : IShaderObject
    {
        string Key { get; }

        // Indicate that this hint should attempt to be processed even if it wasn't found;
        // this allows hints like 'DisplayName' to be able to handle determining a fallback.
        bool AlwaysProcess => false;
        bool AllowDisqualifiedSynonyms => true;

        IReadOnlyCollection<string> Synonyms => null;
        IReadOnlyCollection<string> Conflicts => null;

        bool Process(bool found, string rawValue, T obj, IProvider provider, out object value, out string msg, string actualHintKey);
    }

    internal class HintRegistry<T> where T : IShaderObject
    {
        Dictionary<string, IStrongHint<T>> m_hintsByKey = new();
        Dictionary<string, List<(string StrongHintKey, int Priority)>> m_synonymMap = new();

        internal void RegisterStrongHint(IStrongHint<T> hint)
        {
            // add the new hint;
            m_hintsByKey.Add(hint.Key, hint);

            // add the hint's actual key as a synonym with highest priority
            int priority = 0;
            if (!m_synonymMap.TryAdd(hint.Key, new() { (hint.Key, priority) }))
                m_synonymMap[hint.Key].Add((hint.Key, priority));

            if (!hint.AllowDisqualifiedSynonyms)
                return;

            // build a list of alternates by disqualifying the hint.key.
            var alternates = new List<string>(DisqualifyKey(hint.Key));

            // Look through the hints synonyms and also add them and their disqualifications.
            if (hint.Synonyms != null)
                foreach(var synonym in hint.Synonyms)
                    alternates.AddRange(DisqualifyKey(synonym, true));

            // for each subsequent alternate, the priority decreases.
            foreach(var alternate in alternates)
            {
                priority++;
                // Since a single synonym could be used by multiple hints, we need to register
                // each StrongHint.Key to the synonym.
                if (!m_synonymMap.TryAdd(alternate, new() { (hint.Key, priority) }))
                    m_synonymMap[alternate].Add((hint.Key, priority));
            }
        }

        // eg. (unity:engine:sg:HintKey) => engine:sg:HintKey, sg:HintKey, HintKey
        internal static IEnumerable<string> DisqualifyKey(string key, bool inclusive = false)
        {
            if (inclusive)
                yield return key;

            for (int i = 0; i < key.Length - 1; ++i)
            {
                if (key[i] == ':')
                {
                    int j;
                    // find next non ':' index.
                    for (j = i + 1; j < key.Length && key[j] == ':'; ++j);

                    string candidate = key[j..];
                    if (!string.IsNullOrEmpty(candidate))
                        yield return candidate;
                }
            }
        }

        internal void ProcessObject(T obj, IProvider provider, out Dictionary<string, object> values, out List<string> msgs)
        {
            values = new();
            msgs = new();

            Dictionary<string, (string Synonym, int Priority)> foundHints = new();
            Dictionary<string, HashSet<string>> conflictCases = new();
            HashSet<string> conflictedHints = new();

            // prepass, match the discovered hints to strong hint keys- this allows us to determine which strong hints shouldn't be skipped.
            foreach(var rawHintKey in obj.Hints.Keys)
            {
                if (m_synonymMap.TryGetValue(rawHintKey, out var matches))
                {
                    foreach(var hint in matches)
                    {
                        // track priority so that we don't accidentally match a a synonym over the actual key.
                        if (!foundHints.TryAdd(hint.StrongHintKey, (rawHintKey, hint.Priority)))
                        {
                            // newly found synonym is higher priority (lower number), so use that instead.
                            if (hint.Priority < foundHints[hint.StrongHintKey].Priority)
                                foundHints[hint.StrongHintKey] = (rawHintKey, hint.Priority);
                        }

                        // we can also determine which conflict classes we'll run into here.
                        if (m_hintsByKey[hint.StrongHintKey].Conflicts != null)
                            foreach (var conflictClass in m_hintsByKey[hint.StrongHintKey].Conflicts)
                                if (!conflictCases.TryAdd(conflictClass, new HashSet<string>() { hint.StrongHintKey }))
                                    conflictCases[conflictClass].Add(hint.StrongHintKey);
                    }
                }
            }

            // Conflict cases allow us to early out processing hints.
            foreach(var caseKV in conflictCases)
            {
                if (caseKV.Value.Count == 1)
                    continue;

                // Aggregate conflicted hints so that we know which aren't being processed because they are in conflict.
                conflictedHints.UnionWith(caseKV.Value);

                // Build conflict message.
                StringBuilder sb = new();
                bool first = true;
                sb.Append($"Conflicting hints of class '{caseKV.Key}' found, ignoring: ");
                foreach (var conflictKey in caseKV.Value)
                {
                    if (!first)
                        sb.Append(", ");
                    sb.Append($"'{conflictKey}'");
                    first = false;
                }
                msgs.Add(sb.ToString());
            }

            // Now that we know which hints are both in use and valid, we can skip the ones not in use.
            foreach (var strongHint in m_hintsByKey.Values)
            {
                if (conflictedHints.Contains(strongHint.Key))
                    continue;

                string synonymUsed = null;
                string rawHintValue = null;
                bool found;
                bool shouldProcess = strongHint.AlwaysProcess;


                if (found = foundHints.TryGetValue(strongHint.Key, out var rawHintData))
                {
                    shouldProcess = true;
                    synonymUsed = rawHintData.Synonym;
                    rawHintValue = obj.Hints[synonymUsed];
                }

                if (!shouldProcess)
                    continue;

                if (strongHint.Process(found, rawHintValue, obj, provider, out var value, out var msg, synonymUsed))
                    values.Add(strongHint.Key, value);

                if (!string.IsNullOrWhiteSpace(msg))
                    msgs.Add($"{strongHint.Key}: {msg}");
            }
        }
    }
}
