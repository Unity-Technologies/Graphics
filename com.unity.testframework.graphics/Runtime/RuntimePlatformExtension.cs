using System;
using System.Globalization;
using UnityEngine;

namespace UnityEngine.TestTools.Graphics
{
    internal static class RuntimePlatformExtension
    {
        // this method is required to generate backward compatible unique string values for duplicated RuntimePlatform enum values
        public static string ToUniqueString(this RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.OSXPlayer:

                    if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(SystemInfo.processorType, "Apple M1", CompareOptions.IgnoreCase) >= 0)
                    {
                        return "OSXPlayer_AppleSilicon";
                    }
                    else
                    {
                        return platform.ToString();
                    }
                case RuntimePlatform.OSXEditor:

                    if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(SystemInfo.processorType, "Apple M1", CompareOptions.IgnoreCase) >= 0)
                    {
                        return "OSXEditor_AppleSilicon";
                    }
                    else
                    {
                        return platform.ToString();
                    }
                case RuntimePlatform.WSAPlayerX86: //duplicate RuntimePlatform.MetroPlayerX86
                    return "MetroPlayerX86";
                case RuntimePlatform.WSAPlayerX64: //duplicate RuntimePlatform.MetroPlayerX64
                    return "MetroPlayerX64";
                case RuntimePlatform.WSAPlayerARM: //duplicate RuntimePlatform.MetroPlayerARM
                    return "MetroPlayerARM";
                default:
                    return platform.ToString();
            }
        }
    }
}
