using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Profiling;
using System.Reflection;
using UnityEditor.VersionControl;
using UnityEditor.Searcher;

using PositionType = UnityEngine.UIElements.Position;
namespace UnityEditor.VFX.UI
{
    class VFXSearcherDatabase : SearcherDatabase
    {
        public VFXSearcherDatabase(IReadOnlyCollection<SearcherItem> db)
               : base(db)
        {
        }

        protected override bool Match(string query, SearcherItem item, out float score)
        {
            var filter = MatchFilter?.Invoke(query, item) ?? true;
            return Match(query, item.Name, out score) && filter;
        }

        static int NextSeparator(string s, int index)
        {
            for (; index < s.Length; index++)
                if (IsWhiteSpace(s[index])) // || char.IsUpper(s[index]))
                    return index;
            return -1;
        }

        static bool IsWhiteSpace(char c)
        {
            return c == ' ' || c == '\t';
        }

        static char ToLowerAsciiInvariant(char c)
        {
            if ('A' <= c && c <= 'Z')
                c |= ' ';
            return c;
        }

        static bool StartsWith(string s, int sStart, int sCount, string prefix, int prefixStart, int prefixCount)
        {
            if (prefixCount > sCount)
                return false;
            for (var i = 0; i < prefixCount; i++)
            {
                if (ToLowerAsciiInvariant(s[sStart + i]) != ToLowerAsciiInvariant(prefix[prefixStart + i]))
                    return false;
            }

            return true;
        }

        static bool Match(string query, string itemPath, out float score)
        {
            int queryPartStart = 0;
            int pathPartStart = 0;

            score = 0;
            var skipped = 0;
            do
            {
                // skip remaining spaces in path
                while (pathPartStart < itemPath.Length && IsWhiteSpace(itemPath[pathPartStart]))
                    pathPartStart++;

                // query is not done, nothing remaining in path, failure
                if (pathPartStart > itemPath.Length - 1)
                {
                    score = 0;
                    return false;
                }

                // skip query spaces. notice the + 1
                while (queryPartStart < query.Length && IsWhiteSpace(query[queryPartStart]))
                    queryPartStart++;

                // find next separator in query
                int queryPartEnd = query.IndexOf(' ', queryPartStart);
                if (queryPartEnd == -1)
                    queryPartEnd = query.Length; // no spaces, take everything remaining

                // next space, starting after the path part last char
                int pathPartEnd = NextSeparator(itemPath, pathPartStart + 1);
                if (pathPartEnd == -1)
                    pathPartEnd = itemPath.Length;


                int queryPartLength = queryPartEnd - queryPartStart;
                int pathPartLength = pathPartEnd - pathPartStart;
                bool match = StartsWith(itemPath, pathPartStart, pathPartLength,
                    query, queryPartStart, queryPartLength);

                pathPartStart = pathPartEnd;

                if (!match)
                {
                    skipped++;
                    continue;
                }

                score += queryPartLength / (float)Mathf.Max(1, pathPartLength);
                if (queryPartEnd == query.Length)
                {
                    int pathPartCount = 1;
                    while (-1 != pathPartStart)
                    {
                        pathPartStart = NextSeparator(itemPath, pathPartStart + 1);
                        pathPartCount++;
                    }

                    int queryPartCount = 1;
                    while (-1 != queryPartStart)
                    {
                        queryPartStart = NextSeparator(query, queryPartStart + 1);
                        pathPartCount++;
                    }

                    score *= queryPartCount / (float)pathPartCount;
                    score *= 1 / (1.0f + skipped);

                    return true; // successfully matched all query parts
                }

                queryPartStart = queryPartEnd + 1;
            } while (true);
        }
    }
}
