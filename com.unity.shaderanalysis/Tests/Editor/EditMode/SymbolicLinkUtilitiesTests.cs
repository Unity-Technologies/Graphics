using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using UnityEditor.ShaderAnalysis.Internal;

namespace UnityEditor.Experimental.ShaderTools.Internal.Tests
{
    public class SymbolicLinkUtilitiesTests
    {
        [Test]
        public void IsSymbolicLink()
        {
            var root = new DirectoryInfo(Path.GetTempPath());
            root.Create();

            var sourceDir = root.CreateSubdirectory("SourceDir");
            var targetDir = new DirectoryInfo(Path.Combine(root.FullName, "TargetDir"));

            // make link
            var process = Process.Start("cmd.exe", $"/C mklink /D {targetDir.FullName} {sourceDir.FullName}");
            process.WaitForExit();

            Assert.True(targetDir.IsSymbolicLink());
            Assert.False(sourceDir.IsSymbolicLink());
        }

        [Test]
        public void AreSymbolicLinks()
        {
            var root = new DirectoryInfo(Path.GetTempPath());
            root.Create();

            var sourceDir = root.CreateSubdirectory("SourceDir");
            var targetDir = new DirectoryInfo(Path.Combine(root.FullName, "TargetDir"));
            var targetDir2 = new DirectoryInfo(Path.Combine(root.FullName, "TargetDir2"));

            // make links
            var process = Process.Start("cmd.exe", $"/C mklink /D \"{targetDir.FullName}\" \"{sourceDir.FullName}\" & mklink /D \"{targetDir2.FullName}\" \"{sourceDir.FullName}\"");
            process.WaitForExit();

            Assert.True(new DirectoryInfo[] { targetDir, targetDir2 }.AreSymbolicLinks());
            Assert.False(new DirectoryInfo[] { sourceDir, targetDir, targetDir2 }.AreSymbolicLinks());
            Assert.False(new DirectoryInfo[] { sourceDir, targetDir2 }.AreSymbolicLinks());
            Assert.False(new DirectoryInfo[] { targetDir2, sourceDir }.AreSymbolicLinks());
        }

        [Test]
        public void CreateSymbolicLink()
        {
            var root = new DirectoryInfo(Path.GetTempPath());
            root.Create();

            var sourceDir = root.CreateSubdirectory("SourceDir");
            var targetDir = new DirectoryInfo(Path.Combine(root.FullName, "TargetDir"));

            // make link
            targetDir.CreateSymbolicLink(sourceDir);

            Assert.True(targetDir.IsSymbolicLink());
            Assert.False(sourceDir.IsSymbolicLink());
        }

        [Test]
        public void CreateSymbolicLinks()
        {
            var root = new DirectoryInfo(Path.GetTempPath());
            root.Create();

            var sourceDir = root.CreateSubdirectory("SourceDir");
            var targetDir = new DirectoryInfo(Path.Combine(root.FullName, "TargetDir"));
            var targetDir2 = new DirectoryInfo(Path.Combine(root.FullName, "TargetDir2"));

            // make link
            SymbolicLinkUtilities.CreateSymbolicLinks(new[] { targetDir, targetDir2 }, new[] { sourceDir, sourceDir });

            Assert.True(new DirectoryInfo[] { targetDir, targetDir2 }.AreSymbolicLinks());
        }
    }
}
