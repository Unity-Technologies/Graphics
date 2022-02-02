using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.ShaderAnalysis.Internal
{
    static class PackagesUtilities
    {
        public const string PackageSymbolicLinkFolder = "Library/ShaderAnalysis/PackageSymlinks";

        /// <summary>
        /// Create a symlink to local packages so shader includes will work properly.
        /// </summary>
        internal static void CreateProjectLocalPackagesSymlinks()
        {
            var rootProjectPath = new FileInfo(Application.dataPath).Directory;
            var symlinkFolder = new DirectoryInfo(Path.Combine(rootProjectPath.FullName, $"{PackageSymbolicLinkFolder}/Packages"));
            if (!symlinkFolder.Exists)
                symlinkFolder.Create();
            var includeFolder = symlinkFolder.Parent;

            var packages = AllPackages;
            var localPackagesCount = FilterLocalPackagesInPlace(packages);
            if (localPackagesCount > 0)
                CreateSymlinksFor(packages, localPackagesCount, includeFolder);
        }

        static void CreateSymlinksFor(
            PackageManager.PackageInfo[] packages,
            int localPackagesCount,
            DirectoryInfo rootPath
        )
        {
            var links = new DirectoryInfo[localPackagesCount];
            var targets = new DirectoryInfo[localPackagesCount];
            for (int i = 0; i < localPackagesCount; ++i)
            {
                var package = packages[i];
                Assert.AreEqual(PackageSource.Local, package.source);

                links[i] = new DirectoryInfo(Path.GetFullPath(Path.Combine(rootPath.FullName, package.assetPath)));
                targets[i] = new DirectoryInfo(package.resolvedPath);
            }

            if (!links.AreSymbolicLinks())
            {
                if (!SymbolicLinkUtilities.CreateSymbolicLinks(links, targets))
                    throw new Exception("Failed to create packages symlinks in ProjectFolder/Library/ShaderAnalysis/PackageSymlinks/Packages/*. Please check if you have sufficient privileges to create symlinks or manually create the symlinks for each package.");
            }
        }

        static int FilterLocalPackagesInPlace(
            PackageManager.PackageInfo[] packages
        )
        {
            var count = packages.Length;
            var index = 0;
            while (index < count)
            {
                if (packages[index].source != PackageSource.Local)
                {
                    packages[index] = packages[count - 1];
                    --count;
                }
                else
                    ++index;
            }
            return count;
        }

        static void RemoveAllSymlinks(DirectoryInfo packagesPath)
        {
            foreach (var directory in packagesPath.GetDirectories())
            {
                if (directory.IsSymbolicLink())
                    directory.Delete();
            }
        }

        static PackageManager.PackageInfo[] AllPackages => PackageManager.PackageInfo.GetAllRegisteredPackages();
    }
}
