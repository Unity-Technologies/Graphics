using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using UnityEngine.Rendering.UIGen;

namespace UnityEditor.Rendering.UIGen
{
    public class DebugMenuIntegration
    {
        public class Documents
        {
            [MustUseReturnValue]
            public bool WriteToDisk(
                BindableViewExtensions.DiskLocation location,
                [NotNullWhen(false)] out Exception error
            )
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// generate runtime and editor integration
        /// </summary>
        /// <param name="documents"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        [MustUseReturnValue]
        public static bool GenerateIntegration(
            [NotNullWhen(true)] out Documents documents,
            [NotNullWhen(false)] out Exception error
        )
        {
            throw new NotImplementedException();
        }
    }
}
