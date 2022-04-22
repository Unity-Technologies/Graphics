using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.AssetImporters;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [ScriptedImporter(version: 1, ext: "vuss", importQueueOffset: 1100)]
    class VersionedUSSImporter : ScriptedImporter
    {
        class StylesheetVersionInfo
        {
            public string MinVersion;
            public string MaxVersion;
        }

        /// <inheritdoc />
        public override void OnImportAsset(AssetImportContext ctx)
        {
            string contents = string.Empty;

            try
            {
                contents = File.ReadAllText(ctx.assetPath);
            }
            catch (IOException exc)
            {
                ctx.LogImportError($"IOException : {exc.Message}");
            }
            finally
            {
                var v = new StylesheetVersionInfo();

                using (var reader = new StringReader(contents))
                {
                    var firstLine = reader.ReadLine();
                    if (firstLine != null && firstLine.StartsWith("/*") && firstLine.EndsWith("*/"))
                    {
                        var comment = firstLine.Substring(2, firstLine.Length - 4).Trim();
                        if (!string.IsNullOrEmpty(comment))
                        {
                            try
                            {
                                v = JsonUtility.FromJson<StylesheetVersionInfo>(comment);
                            }
                            catch (Exception e)
                            {
                                ctx.LogImportError($"Exception : {e.Message}");
                            }
                        }
                    }
                }

                bool doImport = v == null;
                if (v != null)
                {
                    try
                    {
                        var regex = new Regex(@"20([0-9][0-9])\.([0-9]+)\.([0-9]+)");

                        var match = regex.Match(Application.unityVersion);
                        var curMajor = int.Parse(match.Groups[1].Value);
                        var curMinor = int.Parse(match.Groups[2].Value);
                        var curRevision = int.Parse(match.Groups[3].Value);

                        var minMajor = int.MinValue;
                        var minMinor = int.MinValue;
                        var minRevision = int.MinValue;
                        if (!string.IsNullOrEmpty(v.MinVersion))
                        {
                            match = regex.Match(v.MinVersion);
                            minMajor = int.Parse(match.Groups[1].Value);
                            minMinor = int.Parse(match.Groups[2].Value);
                            minRevision = int.Parse(match.Groups[3].Value);
                        }

                        var maxMajor = int.MaxValue;
                        var maxMinor = int.MaxValue;
                        var maxRevision = int.MaxValue;
                        if (!string.IsNullOrEmpty(v.MaxVersion))
                        {
                            match = regex.Match(v.MaxVersion);
                            maxMajor = int.Parse(match.Groups[1].Value);
                            maxMinor = int.Parse(match.Groups[2].Value);
                            maxRevision = int.Parse(match.Groups[3].Value);
                        }

                        if (curMajor >= minMajor && curMajor <= maxMajor)
                        {
                            if (curMinor >= minMinor && curMinor <= maxMinor)
                            {
                                if (curRevision >= minRevision && curRevision <= maxRevision)
                                {
                                    doImport = true;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        ctx.LogImportError($"Exception : {e.Message}");
                    }
                }

                StyleSheet asset = ScriptableObject.CreateInstance<StyleSheet>();
                asset.hideFlags = HideFlags.NotEditable;

                if (doImport && !string.IsNullOrEmpty(contents))
                {
                    GraphViewStaticBridge.ImportStyleSheet(ctx, asset, contents);
                }

                // make sure to produce a style sheet object in all cases
                ctx.AddObjectToAsset("stylesheet", asset);
                ctx.SetMainObject(asset);
            }
        }
    }
}
