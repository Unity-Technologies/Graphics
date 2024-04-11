using System;
using System.Collections.Generic;
using System.IO;
using Unity.UI.Builder;
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

        private const string VFXTemplateWindowDocUrl = "https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@{0}/manual/Templates-window.html";
        private const string BuiltInCategory = "Default VFX Graph Templates";
        private const string EmptyTemplateName = "Empty VFX";
        private const string EmptyTemplateDescription = "Create a completely empty VFX asset";
        private const string LastSelectedGuidKey = "VFXTemplateWindow.LastSelectedGuid";
        private const string CreateNewVFXAssetTitle = "Create new VFX Asset";
        private const string InsertTemplateTitle = "Insert a template into current VFX Asset";

        private static readonly Dictionary<CreateMode, string> s_ModeToTitle = new ()
        {
            { CreateMode.CreateNew, CreateNewVFXAssetTitle },
            { CreateMode.Insert, InsertTemplateTitle },
            { CreateMode.None, CreateNewVFXAssetTitle },
        };

        private readonly List<TreeViewItemData<IVFXTemplateDescriptor>> m_TemplatesTree = new ();
        private readonly ISaveFileDialogHelper m_SaveFileDialogHelper = new SaveFileDialogHelper();

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
        private VFXTemplateDescriptor m_SelectedTemplate;

        private enum CreateMode
        {
            CreateNew,
            Insert,
            None,
        }

        public static void ShowCreateFromTemplate(VFXView vfxView, Action<string> callback) => ShowInternal(vfxView, CreateMode.CreateNew, callback);
        public static void ShowInsertTemplate(VFXView vfxView) => ShowInternal(vfxView, CreateMode.Insert);
        public static void PickTemplate(Action<string> callback) => ShowInternal(null, CreateMode.None, callback);

        private static void ShowInternal(VFXView vfxView, CreateMode mode, Action<string> callback = null)
        {
            var templateWindow = EditorWindow.GetWindow<VFXTemplateWindow>(true, s_ModeToTitle[mode], false);
            templateWindow.Setup(vfxView, mode, callback);
        }

        private void Setup(VFXView vfxView, CreateMode mode, Action<string> callback)
        {
            minSize = new Vector2(800, 300);
            m_VfxView = vfxView;
            m_UserCallback = callback;
            m_CurrentMode = mode;
            SetCallBack();
            LoadTemplates();
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
            m_ListOfTemplates.selectionChanged += OnSelectionChanged;
        }

        private void SetCallBack()
        {
            switch (m_CurrentMode)
            {
                case CreateMode.CreateNew:
                    m_VFXAssetCreationCallback = templatePath => CreateNewVisualEffect(templatePath, m_UserCallback);
                    break;
                case CreateMode.Insert:
                    m_VFXAssetCreationCallback = InsertTemplateInVisualEffect;
                    break;
                case CreateMode.None:
                    m_VFXAssetCreationCallback = m_UserCallback;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(m_CurrentMode), m_CurrentMode, null);
            }
        }

        private void OnOpenHelp() => Help.BrowseURL(string.Format(VFXTemplateWindowDocUrl, VFXHelpURLAttribute.version));

        private void LoadTemplates()
        {
            m_LastSelectedTemplateGuid = EditorPrefs.GetString(LastSelectedGuidKey);
            CollectTemplates();
            m_ListOfTemplates.ExpandAll();
        }

        private void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
        }

        private void OnBeforeAssemblyReload()
        {
            this.Close();
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
            var template = m_ListOfTemplates.selectedIndex != -1 ? (VFXTemplateDescriptor)m_ListOfTemplates.selectedItem : m_SelectedTemplate;
            m_LastSelectedTemplatePath = AssetDatabase.GUIDToAssetPath(template.assetGuid);
            m_VFXAssetCreationCallback?.Invoke(m_LastSelectedTemplatePath);
            Close();
            VFXAnalytics.GetInstance().OnSystemTemplateCreated(template.name);
            m_VfxView = null;
            m_VFXAssetCreationCallback = null;
        }

        private void CreateNewVisualEffect(string templatePath, Action<string> userCallback)
        {
            if (templatePath == null)
            {
                return;
            }

            var assetPath = m_SaveFileDialogHelper.OpenSaveFileDialog("", "New VFX", "vfx", "Create new VisualEffect Graph");
            if (!string.IsNullOrEmpty(assetPath))
            {
                VisualEffectAssetEditorUtility.CreateTemplateAsset(assetPath, templatePath);

                //Only null check on view is expected, it avoids GetViewWindow call
                //The resource can be invalidated due to previous write on same asset from CreateTemplateAsset
                if (m_VfxView != null)
                {
                    var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);
                    //If m_VfxView displayed resource is null but asset isn't
                    //GetWindow lambda already fallback to "No Asset" or already opened window
                    var window = VFXViewWindow.GetWindow(vfxAsset, true);
                    window.LoadAsset(vfxAsset, null);
                }

                userCallback?.Invoke(assetPath);
            }
        }

        private void InsertTemplateInVisualEffect(string templatePath)
        {
            if (!string.IsNullOrEmpty(templatePath))
            {
                if (GetViewWindow() is {} window)
                {
                    window.graphView.CreateTemplateSystem(templatePath, Vector2.zero, null);
                }
                else
                {
                    Close();
                }
            }
        }

        private VFXViewWindow GetViewWindow() => m_VfxView != null ? VFXViewWindow.GetWindow(m_VfxView) : null;

        private void OnSelectionChanged(IEnumerable<object> obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            var list = new List<object>(obj);
            if (list.Count == 1)
            {
                if (list[0] is VFXTemplateDescriptor template)
                {
                    m_SelectedTemplate = template;
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

            string ussClass;
            if (data is VFXTemplateDescriptor template)
            {
                item.Q<Image>("TemplateIcon").image = template.icon != null ? template.icon : m_CustomTemplateIcon;
                if (template.assetGuid == m_LastSelectedTemplateGuid)
                    m_ListOfTemplates.SetSelection(index);
                ussClass = "vfxtemplate-item";

                item.RegisterCallback<ClickEvent>(OnClickItem);
            }
            else
            {
                // This is a hack to put the expand/collapse button above the item so that we can interact with it
                var toggle = item.parent.parent.Q<Toggle>();
                toggle.BringToFront();
                ussClass = "vfxtemplate-section";
            }

            if (item.GetFirstAncestorWithClass("unity-tree-view__item") is { } parent)
            {
                parent.AddToClassList(ussClass);
            }
        }

        private void UnbindTemplateItem(VisualElement item, int index)
        {
            if (item.GetFirstAncestorWithClass("unity-tree-view__item") is { } parent)
            {
                parent.RemoveFromClassList("vfxtemplate-item");
                parent.RemoveFromClassList("vfxtemplate-section");
            }
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

            // This is to prevent collapse/expand if there's only one category
            if (templatesGroupedByCategory.Count == 1)
            {
                m_ListOfTemplates.AddToClassList("remove-toggle");
            }
            else
            {
                m_ListOfTemplates.RemoveFromClassList("remove-toggle");
            }

            var templates = new List<List<VFXTemplateDescriptor>>(templatesGroupedByCategory.Values);
            templates.Sort((listA, listB) => listA[0].order.CompareTo(listB[0].order));

            var id = 0;
            var lastSelectedTemplateFound = false;
            var fallBackTemplateAssetGuid = string.Empty;
            foreach (var group in templates)
            {
                var groupId = id++;
                var children = new List<TreeViewItemData<IVFXTemplateDescriptor>>(group.Count);
                foreach (var child in group)
                {
                    if (id == 2)
                        fallBackTemplateAssetGuid = child.assetGuid;
                    if (child.assetGuid == m_LastSelectedTemplateGuid)
                        lastSelectedTemplateFound = true;
                    children.Add(new TreeViewItemData<IVFXTemplateDescriptor>(id++, child));
                }
                var section = new TreeViewItemData<IVFXTemplateDescriptor>(groupId, new VFXTemplateSection(group[0].category), children);
                m_TemplatesTree.Add(section);
            }
            m_ListOfTemplates.SetRootItems(m_TemplatesTree);
            if (!lastSelectedTemplateFound)
            {
                m_LastSelectedTemplateGuid = fallBackTemplateAssetGuid;
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
