using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    class InputRegisteringTests
    {
        static TestCaseData[] s_DuplicateTestCaseDatas =
        {
            new TestCaseData(new List<InputManagerEntry>()
                {
                    new() { name = "Another Action 1", kind = InputManagerEntry.Kind.Axis },
                    new() { name = "Duplicate Action", kind = InputManagerEntry.Kind.Axis },
                    new() { name = "Another Action 2", kind = InputManagerEntry.Kind.Axis },
                    new() { name = "Duplicate Action", kind = InputManagerEntry.Kind.Axis },
                    new() { name = "Duplicate Action", kind = InputManagerEntry.Kind.Axis },
                })
                .SetName("Given a set containing duplicate entries, when removing duplicate entries, only the first of each duplicate is kept")
                .Returns(new string[] { "Another Action 1", "Duplicate Action", "Another Action 2" }),
            new TestCaseData(new List<InputManagerEntry>()
                {
                    new() { name = "Duplicate Action", kind = InputManagerEntry.Kind.Axis },
                    new() { name = "Duplicate Action", kind = InputManagerEntry.Kind.Mouse },
                })
                .SetName("Given entries with duplicate names but different kinds, when removing duplicate entries, both are kept")
                .Returns(new string[] { "Duplicate Action", "Duplicate Action" }),
        };

        [Test, TestCaseSource(nameof(s_DuplicateTestCaseDatas))]
        public string[] GetEntriesWithoutDuplicates(List<InputManagerEntry> entries)
        {
            return InputRegistering.GetEntriesWithoutDuplicates(entries)
                .Select(e => e.name).ToArray();
        }

        static TestCaseData[] s_AlreadyRegisteredTestCaseDatas =
        {
            new TestCaseData(new List<InputManagerEntry>()
                {
                    new() { name = "New Action 1", kind = InputManagerEntry.Kind.Axis },
                    new() { name = "New Action 2", kind = InputManagerEntry.Kind.Mouse },
                    new() { name = "Old Action 1", kind = InputManagerEntry.Kind.Axis },
                    new() { name = "Old Action 2", kind = InputManagerEntry.Kind.Mouse },
                }, new List<(string, InputManagerEntry.Kind)>()
                {
                    ("Old Action 1", InputManagerEntry.Kind.Axis),
                    ("Old Action 2", InputManagerEntry.Kind.Mouse),
                    ("Old Action 3", InputManagerEntry.Kind.Mouse)
                })
                .SetName("Given a set of entries where some of them is already registered, when filtering out already registered entries, they are removed")
                .Returns(new string[] { "New Action 1", "New Action 2" }),
            new TestCaseData(new List<InputManagerEntry>()
                {
                    new() { name = "Old Action 1", kind = InputManagerEntry.Kind.Axis },
                    new() { name = "Old Action 2", kind = InputManagerEntry.Kind.Mouse },
                }, new List<(string, InputManagerEntry.Kind)>()
                {
                    ("Old Action 1", InputManagerEntry.Kind.Axis),
                    ("Old Action 2", InputManagerEntry.Kind.Axis),
                })
                .SetName("Given an entry with already registered name but different kind, when filtering out already registered entries, only ones with matching kind are removed")
                .Returns(new string[] { "Old Action 2" }),
        };


        [Test, TestCaseSource(nameof(s_AlreadyRegisteredTestCaseDatas))]
        public string[] GetEntriesWithoutAlreadyRegistered(List<InputManagerEntry> entries, List<(string, InputManagerEntry.Kind)> cachedEntries)
        {
            return InputRegistering.GetEntriesWithoutAlreadyRegistered(entries, cachedEntries)
                .Select(e => e.name).ToArray();
        }
    }
}
