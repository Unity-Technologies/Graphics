using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Experimental;
using UnityEditor.VFX.UI;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    internal interface IVFXTemplateDescriptor
    {
        string header { get; }
    }

    internal class VFXTemplateWindow : EditorWindow
    {
        internal interface ISaveFileDialogHelper
        {
            string OpenSaveFileDialog(string title, string defaultName, string extension, string message);
        }

        private class SaveFileDialogHelper : ISaveFileDialogHelper
        {
            public string OpenSaveFileDialog(string title, string defaultName, string extension, string message) => EditorUtility.SaveFilePanelInProject(title, defaultName, extension, message);
        }

        private class VFXTemplateSection : IVFXTemplateDescriptor
        {
            public VFXTemplateSection(string text)
            {
                header = text;
            }
            public string header { get; }
        }

        private const string VFXTemplateWindowDocUrl = "https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@15.0/manual/index.html";
        private const string BuiltInCategory = "Default VFX Graph Templates";
        private const string EmptyTemplateName = "Empty VFX";
        private const string EmptyTemplateDescription = "Create a completely empty VFX asset";
        private const string LastSelectedGuidKey = "VFXTemplateWindow.LastSelectedGuid";
        private const string CreateNewVFXAssetTitle = "Create new VFX Asset";
        private const string InsertTemplateTitle = "Insert a template into current VFX Asset";

        private readonly List<TreeViewItemData<IVFXTemplateDescriptor>> m_TemplatesTree = new ();
        private readonly ISaveFileDialogHelper m_SaveFileDialogHelper;

        private TreeView m_ListOfTemplates;
        private Texture2D m_CustomTemplateIcon;
        private Image m_DetailsScreenshot;
        private Label m_DetailsTitle;
        private Label m_DetailsDescription;
        private VisualTreeAsset m_ItemTemplate;
        private Action<string> m_VFXAssetCreationCallback;
        private string m_LastSelectedTemplatePath;
        private int m_LastSelectedIndex;
        private CreateMode m_CurrentMode;
        private Action<string> m_UserCallback;
        private string m_LastSelectedTemplateGuid;
        private VFXView m_VfxView;
        private VisualEffectResource m_EditedResource;

        private enum CreateMode
        {
            CreateNew,
            Insert,
            None,
        }

        public VFXTemplateWindow()
        {
            this.m_SaveFileDialogHelper = new SaveFileDialogHelper();
        }

        public static void ShowCreateFromTemplate(VFXView vfxView, Action<string> callback) => ShowInternal(vfxView, CreateMode.CreateNew, callback);
        public static void ShowInsertTemplate(VFXView vfxView) => ShowInternal(vfxView, CreateMode.Insert);
        public static void PickTemplate(Action<string> callback) => ShowInternal(null, CreateMode.None, callback);

        private static void ShowInternal(VFXView vfxView, CreateMode mode, Action<string> callback = null)
        {
            var templateWindow = EditorWindow.GetWindowDontShow<VFXTemplateWindow>();
            templateWindow.minSize = new Vector2(800, 300);
            templateWindow.m_VfxView = vfxView;
            templateWindow.m_EditedResource = vfxView != null ? vfxView.controller?.graph?.visualEffectResource : null;
            templateWindow.m_CurrentMode = mode;
            templateWindow.m_UserCallback = callback;
            templateWindow.ShowUtility();
        }

        private void CreateGUI()
        {
            m_ItemTemplate = VFXView.LoadUXML("VFXTemplateItem");
            var tpl = VFXView.LoadUXML("VFXTemplateWindow");
            tpl.CloneTree(rootVisualElement);
            rootVisualElement.AddStyleSheetPath("VFXTemplateWindow");

            rootVisualElement.name = "VFXTemplateWindowRoot";
            rootVisualElement.Q<Button>("CreateButton").clicked += OnCreate;
            rootVisualElement.Q<Button>("CancelButton").clicked += OnCancel;

            m_CustomTemplateIcon = EditorGUIUtility.LoadIcon(Path.Combine(VisualEffectGraphPackageInfo.assetPackagePath, "Editor/Templates/UI/CustomVFXGraph@256.png"));

            m_DetailsScreenshot = rootVisualElement.Q<Image>("Screenshot");
            m_DetailsScreenshot.scaleMode = ScaleMode.ScaleAndCrop;
            m_DetailsTitle = rootVisualElement.Q<Label>("Title");
            m_DetailsDescription = rootVisualElement.Q<Label>("Description");

            var helpButton = rootVisualElement.Q<Button>("HelpButton");
            helpButton.clicked += OnOpenHelp;
            var helpImage = helpButton.Q<Image>("HelpImage");
            helpImage.image = EditorGUIUtility.LoadIcon(EditorResources.iconsPath + "_Help.png");

            m_ListOfTemplates = rootVisualElement.Q<TreeView>("ListOfTemplates");
            m_ListOfTemplates.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;

            m_ListOfTemplates.makeItem = CreateTemplateItem;
            m_ListOfTemplates.bindItem = BindTemplateItem;
            m_ListOfTemplates.unbindItem = UnbindTemplateItem;

            switch (m_CurrentMode)
            {
                case CreateMode.CreateNew:
                    titleContent.text = CreateNewVFXAssetTitle;
                    m_VFXAssetCreationCallback = templatePath => CreateNewVisualEffect(templatePath, m_UserCallback);
                    break;
                case CreateMode.Insert:
                    titleContent.text = InsertTemplateTitle;
                    m_VFXAssetCreationCallback = InsertTemplateInVisualEffect;
                    break;
                case CreateMode.None:
                    titleContent.text = CreateNewVFXAssetTitle;
                    m_VFXAssetCreationCallback = m_UserCallback;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(m_CurrentMode), m_CurrentMode, null);
            }
            LoadTemplates();
        }

        private void OnOpenHelp() => Help.BrowseURL(VFXTemplateWindowDocUrl);

        private void LoadTemplates()
        {
            CollectTemplates();

            m_ListOfTemplates.ExpandAll();
            m_ListOfTemplates.selectionChanged += OnSelectionChanged;
            m_LastSelectedTemplateGuid = EditorPrefs.GetString(LastSelectedGuidKey);
        }

        private void OnDestroy()
        {
            EditorPrefs.SetString(LastSelectedGuidKey, m_LastSelectedTemplateGuid);
        }

        private void OnCancel()
        {
            m_LastSelectedTemplatePath = null;
            m_VFXAssetCreationCallback?.Invoke(m_LastSelectedTemplatePath);
            Close();
        }

        private void OnCreate()
        {
            var template = (VFXTemplateDescriptor)m_ListOfTemplates.selectedItem;
            m_LastSelectedTemplatePath = AssetDatabase.GUIDToAssetPath(template.assetGuid);
            m_VFXAssetCreationCallback?.Invoke(m_LastSelectedTemplatePath);
            Close();
            VFXAnalytics.GetInstance().OnSystemTemplateCreated(template.name);
            m_EditedResource = null;
            m_VfxView = null;
            m_VFXAssetCreationCallback = null;
        }

        private void CreateNewVisualEffect(string templatePath, Action<string> userCallback)
        {
            if (string.IsNullOrEmpty(templatePath))
            {
                return;
            }

            var assetPath = m_SaveFileDialogHelper.OpenSaveFileDialog("", "New VFX", "vfx", "Create new VisualEffect Graph");
            if (!string.IsNullOrEmpty(assetPath))
            {
                VisualEffectAssetEditorUtility.CreateTemplateAsset(assetPath, templatePath);

                if (GetView() is {} window)
                {
                    var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);
                    window.LoadAsset(vfxAsset, null);
                }

                userCallback?.Invoke(assetPath);
            }
        }

        private void InsertTemplateInVisualEffect(string templatePath)
        {
            if (!string.IsNullOrEmpty(templatePath))
            {
                if (GetView() is {} window)
                {
                    window.graphView.CreateTemplateSystem(templatePath, Vector2.zero, null);
                }
                else
                {
                    Close();
                }
            }
        }

        private VFXViewWindow GetView()
        {
            if (m_VfxView != null)
            {
                return VFXViewWindow.GetWindow(m_VfxView);
            }

            return m_EditedResource != null ? VFXViewWindow.GetWindow(m_EditedResource.asset) : null;
        }

        private void OnSelectionChanged(IEnumerable<object> obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            var list = new List<object>(obj);
            if (list.Count == 1)
            {
                if (list[0] is VFXTemplateDescriptor template)
                {
                    m_DetailsTitle.text = template.name;
                    m_DetailsDescription.text = template.description;
                    m_LastSelectedTemplateGuid = template.assetGuid;
                    m_LastSelectedIndex = m_ListOfTemplates.selectedIndex;
                    m_DetailsScreenshot.image = template.thumbnail;
                    // Maybe set a placeholder screenshot when null
                }
                else
                {
                    m_ListOfTemplates.selectedIndex = m_LastSelectedIndex;
                }
            }
            else
            {
                throw new NotSupportedException("Cannot select multiple templates");
            }
        }

        private void BindTemplateItem(VisualElement item, int index)
        {
            var data = m_ListOfTemplates.GetItemDataForIndex<IVFXTemplateDescriptor>(index);
            var label = item.Q<Label>("TemplateName");
            label.text = data.header;

            if (data is VFXTemplateDescriptor template)
            {
                item.Q<Image>("TemplateIcon").image = template.icon != null ? template.icon : m_CustomTemplateIcon;
                if (template.assetGuid == m_LastSelectedTemplateGuid)
                    m_ListOfTemplates.SetSelection(index);

                item.AddToClassList("vfxtemplate-item");
                item.RemoveFromClassList("vfxtemplate-section");
                item.RegisterCallback<ClickEvent>(OnClickItem);
            }
            else
            {
                // This is a hack to put the expand/collapse button above the item so that we can interact with it
                var toggle = item.parent.parent.Q<Toggle>();
                toggle.BringToFront();
                item.AddToClassList("vfxtemplate-section");
                item.RemoveFromClassList("vfxtemplate-item");
            }
        }

        private void UnbindTemplateItem(VisualElement item, int index)
        {
            item.UnregisterCallback<ClickEvent>(OnClickItem);
        }

        private void OnClickItem(ClickEvent evt)
        {
            if (evt.clickCount == 2 && m_ListOfTemplates.selectedItem != null)
            {
                OnCreate();
            }
        }

        private VisualElement CreateTemplateItem() => m_ItemTemplate.Instantiate();

        private void CollectTemplates()
        {
            m_TemplatesTree.Clear();

            var vfxAssetsGuid = AssetDatabase.FindAssets("t:VisualEffectAsset");
            var allTemplates = new List<VFXTemplateDescriptor>(vfxAssetsGuid.Length);

            foreach (var guid in vfxAssetsGuid)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (VFXTemplateHelper.TryGetTemplate(assetPath, out var template))
                {
                    var isBuiltIn = assetPath.StartsWith(VisualEffectAssetEditorUtility.templatePath);
                    template.category = isBuiltIn ? BuiltInCategory : template.category;
                    template.order =  isBuiltIn ? 0 : 1;
                    template.assetGuid = guid;
                    if (isBuiltIn)
                    {
                        template.icon = GetSkinIcon(template.icon);
                    }
                    allTemplates.Add(template);
                }
            }

            if (m_CurrentMode != CreateMode.Insert)
            {
                allTemplates.Add(MakeEmptyTemplate());
            }

            var id = 0;
            var templatesGroupedByCategory = new Dictionary<string, List<VFXTemplateDescriptor>>();
            foreach (var template in allTemplates)
            {
                if (templatesGroupedByCategory.TryGetValue(template.category, out var list))
                {
                    list.Add(template);
                }
                else
                {
                    list = new List<VFXTemplateDescriptor> { template };
                    templatesGroupedByCategory[template.category] = list;
                }
            }

            var templates = new List<List<VFXTemplateDescriptor>>(templatesGroupedByCategory.Values);
            templates.Sort((listA, listB) => listA[0].order.CompareTo(listB[0].order));

            foreach (var group in templates)
            {
                var groupId = id++;
                var children = new List<TreeViewItemData<IVFXTemplateDescriptor>>(group.Count);
                foreach (var child in group)
                {
                    children.Add(new TreeViewItemData<IVFXTemplateDescriptor>(id++, child));
                }
                var section = new TreeViewItemData<IVFXTemplateDescriptor>(groupId, new VFXTemplateSection(group[0].category), children);
                m_TemplatesTree.Add(section);
            }
            m_ListOfTemplates.SetRootItems(m_TemplatesTree);

            if (m_ListOfTemplates.selectedItem == null && m_TemplatesTree.Count > 0)
            {
                m_ListOfTemplates.SetSelection(0);
            }
        }

        private Texture2D GetSkinIcon(Texture2D templateIcon)
        {
            if (EditorGUIUtility.skinIndex == 0)
            {
                return templateIcon;
            }

            var path = AssetDatabase.GetAssetPath(templateIcon);
            return EditorGUIUtility.LoadIcon(path);
        }

        private VFXTemplateDescriptor MakeEmptyTemplate()
        {
            return new VFXTemplateDescriptor
            {
                name = EmptyTemplateName,
                icon = EditorGUIUtility.LoadIcon(Path.Combine(VisualEffectGraphPackageInfo.assetPackagePath, "Editor/Templates/UI/EmptyTemplate@256.png")),
                thumbnail = EditorGUIUtility.LoadIcon(Path.Combine(VisualEffectGraphPackageInfo.assetPackagePath, "Editor/Templates/UI/3d_Empty.png")),
                category = BuiltInCategory,
                description = EmptyTemplateDescription,
                assetGuid = "empty",
            };
        }
    }
}
