using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using UnityEngine.Rendering.UIGen;

namespace UnityEditor.Rendering.UIGen
{
    public class DebugMenuIntegration
    {
        public class Documents
        {
            string m_EditorCodeFileName;
            CSharpSyntaxTree m_EditorWindowCode;

            Documents(
                [DisallowNull] string editorCodeFileName,
                [DisallowNull] CSharpSyntaxTree editorWindowCode
            )
            {
                m_EditorCodeFileName = editorCodeFileName;
                m_EditorWindowCode = editorWindowCode;
            }

            [MustUseReturnValue]
            public bool WriteToDisk(
                GenerationTargetLocations locations,
                [NotNullWhen(false)] out Exception error
            )
            {
                if (!locations.GetEditorPathFor(m_EditorCodeFileName, out var editorPath, out error))
                    return false;

                try
                {
                    File.WriteAllText(editorPath, m_EditorWindowCode.ToString());
                }
                catch (Exception e)
                {
                    error = e;
                    return false;
                }

                return true;
            }

            public static bool From(
                [DisallowNull] CSharpSyntaxTree editorCode,
                [DisallowNull] string editorCodeFilename,
                [NotNullWhen(true)] out Documents documents,
                [NotNullWhen(false)] out Exception error
            )
            {
                documents = new Documents(editorCodeFilename, editorCode);
                error = default;
                return true;
            }
        }

        public struct Parameters
        {
            public string uiViewEditorType;
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

            if (!parameters.uiViewEditorType.FailIfNullOrEmpty(nameof(parameters.uiViewEditorType), out error)
                || !parameters.uiViewType.FailIfNullOrEmpty(nameof(parameters.uiViewType), out error)
                || !parameters.uiViewContextType.FailIfNullOrEmpty(nameof(parameters.uiViewContextType), out error)
                || !parameters.editorMenuPath.FailIfNullOrEmpty(nameof(parameters.editorMenuPath), out error)
                || !parameters.editorWindowName.FailIfNullOrEmpty(nameof(parameters.editorWindowName), out error))
                return false;

            var syntaxTree = (CSharpSyntaxTree) SyntaxFactory.ParseSyntaxTree(
                @$"public class {parameters.uiViewType}: UnityEngine.Rendering.UIGen.UIView<{parameters.uiViewType}, {parameters.uiViewContextType}>
{{
    [MenuItem(""{parameters.editorMenuPath}"")]
    static void Show()
    {{
        var window = GetWindow<{parameters.uiViewType}>();
        window.titleContent = new GUIContent(""{parameters.editorWindowName}"");
    }}
}}"
            );
            if (syntaxTree == null)
            {
                error = new Exception("Failed to create syntax tree").WithStackTrace();
                return false;
            }

            if (!Documents.From(syntaxTree, $"{parameters.uiViewEditorType}.cs", out documents, out error))
                return false;

            return true;
        }
    }
}
