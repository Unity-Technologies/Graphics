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
                    Directory.CreateDirectory(Path.GetDirectoryName(editorPath));
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

        public readonly struct Parameters
        {
            public readonly string uiViewEditorType;
            public readonly string uiViewType;
            public readonly string uiViewContextType;
            public readonly string editorMenuPath;
            public readonly string editorWindowName;

            public Parameters(
                [DisallowNull] string uiViewEditorType,
                [DisallowNull] string uiViewType,
                [DisallowNull] string uiViewContextType,
                [DisallowNull] string editorMenuPath,
                [DisallowNull] string editorWindowName
            )
            {
                this.uiViewEditorType = uiViewEditorType;
                this.uiViewType = uiViewType;
                this.uiViewContextType = uiViewContextType;
                this.editorMenuPath = editorMenuPath;
                this.editorWindowName = editorWindowName;
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
                @$"using System;
using UnityEditor;
using UnityEditor.Rendering.UIGen;
using UnityEngine;

public class {parameters.uiViewEditorType}: UIViewEditorWindow<{parameters.uiViewType}, I{parameters.uiViewContextType}, {parameters.uiViewContextType}>
{{
    [MenuItem(""{parameters.editorMenuPath}"")]
    static void ShowWindow()
    {{
        var window = GetWindow<{parameters.uiViewEditorType}>();
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
