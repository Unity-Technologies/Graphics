using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text;
using UnityEngine.Assertions;

namespace UnityEditor.ShaderAnalysis.Internal
{
    public static class SymbolicLinkUtilities
    {
        public static bool IsSymbolicLink(string path)
        {
            var p = new ProcessStartInfo("cmd.exe")
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                Arguments = $"/C fsutil reparsepoint query \"{path}\" | find \"Symbolic Link\" >nul && echo true || echo false"
            };
            var proc = new Process
            {
                StartInfo = p
            };
            proc.Start();
            var stdout = proc.StandardOutput;
            proc.WaitForExit();
            var stdoutContent = stdout.ReadToEnd();
            return stdoutContent.Contains("true");
        }

        public static bool IsSymbolicLink(this DirectoryInfo directory) => IsSymbolicLink(directory.FullName);

        public static bool CreateSymbolicLink(this DirectoryInfo link, DirectoryInfo target)
        {
            var p = new ProcessStartInfo("cmd.exe")
            {
                Arguments = $"/C mklink /D {link.FullName} {target.FullName} & pause",
                Verb = "runas"
            };
            var proc = new Process
            {
                StartInfo = p
            };
            proc.Start();
            proc.WaitForExit();
            return proc.ExitCode == 0;
        }

        public static bool AreSymbolicLinks(this DirectoryInfo[] links)
        {
            Assert.IsNotNull(links);

            var args = new StringBuilder();
            args.Append("/C");
            for (int i = 0; i < links.Length; ++i)
            {
                if (i > 0)
                    args.Append(" &&");

                args.Append($" fsutil reparsepoint query \"{links[i]}\" | find \"Symbolic Link\" >nul");
            }

            args.Append(" && echo true || echo false");

            var p = new ProcessStartInfo("cmd.exe")
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                Arguments = args.ToString()
            };
            var proc = new Process
            {
                StartInfo = p
            };
            proc.Start();
            var stdout = proc.StandardOutput;
            proc.WaitForExit();
            var stdoutContent = stdout.ReadToEnd();
            return stdoutContent.Contains("true");
        }

        public static bool CreateSymbolicLinks(DirectoryInfo[] links, DirectoryInfo[] targets)
        {
            Assert.IsNotNull(links);
            Assert.IsNotNull(targets);
            Assert.AreEqual(links.Length, targets.Length);

            var args = new StringBuilder();
            args.Append("/C");
            for (int i = 0; i < links.Length; ++i)
            {
                if (i > 0)
                    args.Append(" &");

                args.Append($" mklink /D \"{links[i]}\" \"{targets[i]}\"");
            }

            var p = new ProcessStartInfo("cmd.exe")
            {
                Arguments = args.ToString(),
                Verb = !IsUserAdministrator() ? "runas" : string.Empty,
            };
            var proc = new Process
            {
                StartInfo = p
            };
            proc.Start();
            proc.WaitForExit();
            return proc.ExitCode == 0;
        }

        static bool IsUserAdministrator()
        {
            //bool value to hold our return value
            bool isAdmin;
            WindowsIdentity user = null;
            try
            {
                //get the currently logged in user
                user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException)
            {
                isAdmin = false;
            }
            catch (Exception)
            {
                isAdmin = false;
            }
            finally
            {
                if (user != null)
                    user.Dispose();
            }
            return isAdmin;
        }
    }
}
