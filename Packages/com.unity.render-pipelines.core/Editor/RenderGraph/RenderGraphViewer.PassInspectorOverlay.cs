using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    public partial class RenderGraphViewer
    {
        [Icon("Packages/com.unity.render-pipelines.core/Editor/Icons/Processed/PassInspector Icon.asset")]
        [Overlay(typeof(RenderGraphViewer), ViewId, "Pass Inspector", defaultLayout = Layout.Panel,
            defaultDockZone = DockZone.RightColumn, defaultDockPosition = DockPosition.Bottom)]
        class PassInspectorOverlay : OverlayBase, ITransientOverlay
        {
            public const string ViewId = "RenderGraphViewer.PassInspectorOverlay";

            const string k_TemplatePath =
                "Packages/com.unity.render-pipelines.core/Editor/UXML/RenderGraphViewer.PassInspector.uxml";

            static class Classes
            {
                public const string kSubHeaderText = "sub-header-text";
                public const string kAttachmentInfoItem = "attachment-info__item";
                public const string kAttachmentInfoLineBreak = "attachment-info-line-break";
            }

            static readonly string[] k_PassTypeNames =
                { "Legacy Render Pass", "Unsafe Render Pass", "Raster Render Pass", "Compute Pass" };

            public bool visible => true;

            public override VisualElement CreatePanelContent()
            {
                Init("Pass Inspector");

                if (root == null)
                {
                    var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TemplatePath);
                    root = visualTreeAsset.Instantiate();
                    SetDisplayState(DisplayState.Empty);

                    var themeStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(EditorGUIUtility.isProSkin
                        ? k_DarkStylePath
                        : k_LightStylePath);
                    root.styleSheets.Add(themeStyleSheet);
                }

                return root;
            }

            public void ClearContents()
            {
                if (root == null)
                    return;

                SetDisplayState(DisplayState.Empty);
            }

            public void PopulateContents(RenderGraphViewer viewer, List<int> passes)
            {
                if (root == null)
                    return;

                SetDisplayState(DisplayState.Populated);

                var scrollView = root.Q<ScrollView>();
                scrollView.Clear();

                void CreateTextElement(VisualElement parent, string text, string className = null)
                {
                    var textElement = new TextElement();
                    textElement.text = text;
                    if (className != null)
                        textElement.AddToClassList(className);
                    parent.Add(textElement);
                }

                // Native pass info (duplicated for each pass so just look at the first)
                var firstPass = viewer.m_CurrentDebugData.passList[passes[0]];

                CreateTextElement(scrollView, "Native Render Pass Info", Classes.kSubHeaderText);

                if (firstPass.nrpInfo.nativePassInfo != null)
                {
                    if (firstPass.nrpInfo.nativePassInfo.mergedPassIds.Count == 1)
                        CreateTextElement(scrollView, "Native Pass was created from Raster Render Pass.");
                    else if (firstPass.nrpInfo.nativePassInfo.mergedPassIds.Count > 1)
                        CreateTextElement(scrollView,
                            $"Native Pass was created by merging {firstPass.nrpInfo.nativePassInfo.mergedPassIds.Count} Raster Render Passes.");
                    scrollView.Add(new TextElement
                    {
                        text = $"<b>Pass break reasoning:</b> {firstPass.nrpInfo.nativePassInfo.passBreakReasoning}"
                    });
                }
                else
                {
                    var pass = viewer.m_CurrentDebugData.passList[passes[0]];
                    var msg = $"This is a {k_PassTypeNames[(int) pass.type]}. Only Raster Render Passes can be merged.";
                    msg = msg.Replace("a Unsafe", "an Unsafe");
                    CreateTextElement(scrollView, msg);
                    CreateTextElement(scrollView, "Attachments", Classes.kSubHeaderText);
                    CreateTextElement(scrollView, "No attachments.");
                }

                CreateTextElement(scrollView, "Render Graph Pass Info", Classes.kSubHeaderText);
                foreach (int passId in passes)
                {
                    var pass = viewer.m_CurrentDebugData.passList[passId];
                    Debug.Assert(pass.nrpInfo != null); // This overlay currently assumes NRP compiler
                    var passFoldout = new Foldout();
                    passFoldout.text = $"{pass.name} ({k_PassTypeNames[(int) pass.type]})";
                    passFoldout.AddToClassList(Classes.kAttachmentInfoItem);

                    var lineBreak = new VisualElement();
                    lineBreak.AddToClassList(Classes.kAttachmentInfoLineBreak);
                    passFoldout.Add(lineBreak);

                    CreateTextElement(passFoldout,
                        $"Attachment dimensions: {pass.nrpInfo.width}x{pass.nrpInfo.height}x{pass.nrpInfo.volumeDepth}");
                    CreateTextElement(passFoldout, $"Has depth attachment: {pass.nrpInfo.hasDepth}");
                    CreateTextElement(passFoldout, $"MSAA samples: {pass.nrpInfo.samples}");
                    CreateTextElement(passFoldout, $"Async compute: {pass.async}");

                    scrollView.Add(passFoldout);
                }

                if (firstPass.nrpInfo.nativePassInfo != null)
                {
                    CreateTextElement(scrollView, "Attachments", Classes.kSubHeaderText);
                    foreach (var attachmentInfo in firstPass.nrpInfo.nativePassInfo.attachmentInfos)
                    {
                        var attachmentFoldout = new Foldout();
                        attachmentFoldout.text = attachmentInfo.resourceName;
                        attachmentFoldout.AddToClassList(Classes.kAttachmentInfoItem);
                        var lineBreak = new VisualElement();
                        lineBreak.AddToClassList(Classes.kAttachmentInfoLineBreak);
                        attachmentFoldout.Add(lineBreak);

                        attachmentFoldout.Add(new TextElement
                        {
                            text = $"<b>Load action:</b> {attachmentInfo.loadAction} ({attachmentInfo.loadReason})"
                        });
                        attachmentFoldout.Add(new TextElement
                        {
                            text = $"<b>Store action:</b> {attachmentInfo.storeAction} ({attachmentInfo.storeReason})"
                        });

                        scrollView.Add(attachmentFoldout);
                    }
                    if (firstPass.nrpInfo.nativePassInfo.attachmentInfos.Count == 0)
                        CreateTextElement(scrollView, "No attachments.");
                }
            }
        }
    }
}
