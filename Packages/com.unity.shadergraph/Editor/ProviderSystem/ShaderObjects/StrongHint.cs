using System.Collections.Generic;
using System.Text;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    interface IStrongHint<T> where T : IShaderObject
    {
        string Key { get; }
        IReadOnlyCollection<string> Synonyms => null;
        IReadOnlyCollection<string> Conflicts => null;

        bool Process(bool found, string rawValue, T obj, IProvider provider, out object value, out string msg);
    }

    internal class HintRegistry<T> where T : IShaderObject
    {
        List<IStrongHint<T>> m_hints = new();
        Dictionary<string, List<string>> m_alternates = new();

        internal void RegisterStrongHint(IStrongHint<T> hint)
        {
            m_hints.Add(hint);
            var alternates = new List<string>(DisqualifyKey(hint.Key));

            if (hint.Synonyms != null)
                foreach(var synonym in hint.Synonyms)
                    alternates.AddRange(DisqualifyKey(synonym, true));

            if (alternates.Count > 0)
                m_alternates.Add(hint.Key, alternates);
        }

        // eg. (unity:engine:sg:HintKey) => engine:sg:HintKey, sg:HintKey, HintKey
        private static IEnumerable<string> DisqualifyKey(string key, bool inclusive = false)
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
            Dictionary<string, List<string>> conflictCases = new();

            // this is run per the registered hint types and not the hints on the object.
            foreach (var hint in m_hints)
            {
                // assess whether the object has the hint.
                bool hintFound = obj.Hints.TryGetValue(hint.Key, out var hintRawValue);
                bool hintValid = false;

                // try the alternate keys. It isn't strictly erroneous to have alternates also be present.
                if (!hintFound && m_alternates.ContainsKey(hint.Key))
                    foreach (var alternateKey in m_alternates[hint.Key])
                        if (hintFound = obj.Hints.TryGetValue(alternateKey, out hintRawValue))
                            break;

                // some hints may have default values and need to be processed whether they are found or not.
                if (hintValid = hint.Process(hintFound, hintRawValue, obj, provider, out var value, out var msg))
                {
                    values.Add(hint.Key, value);
                }

                if (!string.IsNullOrWhiteSpace(msg))
                    msgs.Add($"{hint.Key}: {msg}");

                // if they were processed or existed previously, we need to consider their conflict classes.
                if ((hintFound || hintValid) && hint.Conflicts != null && hint.Conflicts.Count > 0)
                {
                    foreach(var conflict in hint.Conflicts)
                    {
                        conflictCases.TryAdd(conflict, new());
                        conflictCases[conflict].Add(hint.Key);
                    }
                }
            }

            foreach(var caseKV in conflictCases)
            {
                // if there is only one hint in a conflict case, it isn't conflicted.
                if (caseKV.Value.Count <= 1)
                    continue;

                // otherwise it'll need a message and for the conflicting hints to be removed from the values.
                StringBuilder sb = new();
                bool first = true;
                sb.Append($"Conflicting hints of class '{caseKV.Key}' found, ignoring: ");
                foreach (var conflictKey in caseKV.Value)
                {
                    // Any key in conflict is removed and ignored.
                    values.Remove(conflictKey);
                    if (!first)
                        sb.Append(", ");
                    sb.Append($"{conflictKey}");
                    first = false;
                }
                msgs.Add(sb.ToString());
            }
        }
    }
}
