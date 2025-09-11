using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static UnityEditor.Rendering.MaterialUpgrader;

namespace UnityEditor.Rendering.Tools.Tests
{
    class MaterialUpgraderTests
    {
        public class MaterialUpgradeTestCase
        {
            public List<MaterialInfo> Materials { get; set; }
            public HashSet<string> Upgraders { get; set; }
            public HashSet<string> IgnoredShaders { get; set; }
            public List<MaterialUpgradeEntry> ExpectedUpgradeEntries { get; set; }
            public string ExpectedLog { get; set; }
            public override string ToString() => ExpectedLog?.Split('\n').FirstOrDefault() ?? "Case";
        }

        public static IEnumerable TestCases
        {
            get
            {
                // Case 1: Upgradable
                var info1 = new MaterialInfo { ShaderName = "Standard" };
                yield return new TestCaseData(new MaterialUpgradeTestCase
                {
                    Materials = new() { info1 },
                    Upgraders = new() { "Standard" },
                    IgnoredShaders = new(),
                    ExpectedUpgradeEntries = new()
                    {
                        new MaterialUpgradeEntry
                        {
                            MaterialInfo = info1,
                            AvailableForUpgrade = true,
                            NotAvailableForUpgradeReason = ""
                        }
                    },
                    ExpectedLog = "Testing" + Environment.NewLine + "Upgrading material:  using shader: Standard" + Environment.NewLine
                }).SetName("Material is upgradable");

                // Case 2: Not upgradable
                var info2 = new MaterialInfo { ShaderName = "Legacy/Diffuse" };
                yield return new TestCaseData(new MaterialUpgradeTestCase
                {
                    Materials = new() { info2 },
                    Upgraders = new(),
                    IgnoredShaders = new(),
                    ExpectedUpgradeEntries = new()
                    {
                        new()
                        {
                            MaterialInfo = info2,
                            AvailableForUpgrade = false,
                            NotAvailableForUpgradeReason = MaterialUpgrader.GenerateReason(info2)
                        }
                    },
                    ExpectedLog = "Testing" + Environment.NewLine + $"Skipping material:  - {MaterialUpgrader.GenerateReason(info2)}" + Environment.NewLine
                }).SetName("Material is not upgradable");

                // Case 3: Ignored
                var info3 = new MaterialInfo { ShaderName = "Unlit/Color" };
                yield return new TestCaseData(new MaterialUpgradeTestCase
                {
                    Materials = new() { info3 },
                    Upgraders = new() { "Unlit/Color" },
                    IgnoredShaders = new() { "Unlit/Color" },
                    ExpectedUpgradeEntries = new(),
                    ExpectedLog = string.Empty
                }).SetName("Material is ignored");

                // Case 4: Variant
                var info4 = new MaterialInfo
                {
                    Name = "Standard Variant",
                    BaseMaterialName = "Standard Base",
                    ShaderName = "Standard",
                    IsVariant = true
                };
                yield return new TestCaseData(new MaterialUpgradeTestCase
                {
                    Materials = new() { info4 },
                    Upgraders = new() { "Standard" },
                    IgnoredShaders = new(),
                    ExpectedUpgradeEntries = new()
                    {
                        new MaterialUpgradeEntry
                        {
                            MaterialInfo = info4,
                            AvailableForUpgrade = false,
                            NotAvailableForUpgradeReason = MaterialUpgrader.GenerateReason(info4)
                        }
                    },
                    ExpectedLog = "Testing" + Environment.NewLine + $"Skipping material: Standard Variant - {MaterialUpgrader.GenerateReason(info4)}" + Environment.NewLine
                }).SetName("Material is a variant and skipped");
            }
        }

        [TestCaseSource(nameof(TestCases))]
        public void FetchUpgradeOptionsTest(MaterialUpgradeTestCase testCase)
        {
            var result = MaterialUpgrader.FetchUpgradeOptions(
                testCase.Upgraders,
                testCase.IgnoredShaders,
                testCase.Materials).ToList();

            Assert.That(result, Is.EqualTo(testCase.ExpectedUpgradeEntries));
        }

        [TestCaseSource(nameof(TestCases))]
        public void PerformUpgradeTest(MaterialUpgradeTestCase testCase)
        {
            var entries = MaterialUpgrader.FetchUpgradeOptions(
                testCase.Upgraders,
                testCase.IgnoredShaders,
                testCase.Materials).ToList();

            var log = MaterialUpgrader.PerformUpgradeInternal(
                entries, null, testCase.IgnoredShaders, "Testing", false, UpgradeFlags.None);

            Assert.That(log, Is.EqualTo(testCase.ExpectedLog));
        }
    }
}
