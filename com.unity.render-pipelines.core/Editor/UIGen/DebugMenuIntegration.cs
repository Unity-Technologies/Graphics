using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp;
using UnityEngine.Rendering.UIGen;

namespace UnityEditor.Rendering.UIGen
{
    public class DebugMenuIntegration
    {
        public class Documents
        {
            CSharpSyntaxTree m_EditorWindowCode;

            [MustUseReturnValue]
            public bool WriteToDisk(
                BindableViewExtensions.DiskLocation location,
                [NotNullWhen(false)] out Exception error
            )
            {
                throw new NotImplementedException();
            }
        }

        public struct Parameters
        {
            public string uiViewType;
            public string uiViewContextType;
            public string editorMenuPath;
            public string editorWindowName;
        }

        /// <summary>
        /// generate runtime and editor integration
        /// </summary>
        /// <param name="documents"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        [MustUseReturnValue]
        public static bool GenerateIntegration(
            Parameters parameters,
            [NotNullWhen(true)] out Documents documents,
            [NotNullWhen(false)] out Exception error
        )
        {
            documents = default;

            if (!parameters.uiViewType.FailIfNullOrEmpty(nameof(parameters.uiViewType), out error)
                || !parameters.uiViewContextType.FailIfNullOrEmpty(nameof(parameters.uiViewType), out error)
                || !parameters.editorMenuPath.FailIfNullOrEmpty(nameof(parameters.uiViewType), out error)
                || !parameters.editorWindowName.FailIfNullOrEmpty(nameof(parameters.uiViewType), out error))
                return false;


            return true;
        }
    }
}
