using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Profiling;

using PositionType = UnityEngine.UIElements.Position;

namespace UnityEditor.VFX.UI
{
    class VFXNodeUI : Node, IControlledElement, ISettableControlledElement<VFXNodeController>, IVFXMovable
    {

        bool m_Selected;
        VFXNodeController m_Controller;
        readonly List<PropertyRM> m_Settings = new();
        protected VisualElement m_SettingsDivider;


        static string UXMLResourceToPackage(string resourcePath)
        {
            return VisualEffectAssetEditorUtility.editorResourcesPath + "/" + resourcePath + ".uxml";
        }

        public VFXNodeUI() : base(UXMLResourceToPackage("uxml/VFXNode"))
        {
            styleSheets.Add(EditorGUIUtility.Load("StyleSheets/GraphView/Node.uss") as StyleSheet);
            Initialize();
        }

        public VFXNodeUI(string template) : base(UXMLResourceToPackage(template))
        {
            Initialize();
        }

        public virtual bool superCollapsed => controller.superCollapsed;
        public VisualElement settingsContainer { get; private set; }
        public virtual bool hasSettingDivider => true;

        public override bool expanded
        {
            get => base.expanded;
            set
            {
                if (base.expanded == value)
                    return;

                base.expanded = value;
                controller.expanded = value;
                UpdateActivationPortPositionIfAny();
            }
        }

        protected bool hasSettings { get; private set; }
        Controller IControlledElement.controller => m_Controller;

        public delegate void SelectionEvent(bool selfSelected);

        public event SelectionEvent onSelectionDelegate;
        public void OnMoved()
        {
            controller.position = GetPosition().position;
        }

        public VFXNodeController controller
        {
            get => m_Controller;
            set
            {
                m_Controller?.UnregisterHandler(this);
                m_Controller = value;
                OnNewController();
                m_Controller?.RegisterHandler(this);
            }
        }

        protected virtual void OnNewController()
        {
            if (controller != null)
                viewDataKey = $"NodeID-{controller.model.GetInstanceID()}";
        }

        public void OnSelectionMouseDown(MouseDownEvent e)
        {
            var gv = GetFirstAncestorOfType<VFXView>();
            if (IsSelected(gv))
            {
                if (e.actionKey)
                {
                    Unselect(gv);
                }
            }
            else
            {
                Select(gv, e.actionKey);
            }
        }

        void OnFocusIn(FocusInEvent e)
        {
            var gv = GetFirstAncestorOfType<VFXView>();
            if (!IsSelected(gv))
                Select(gv, false);
            e.StopPropagation();
        }

        void OnPointerEnter(PointerEnterEvent e)
        {
            e.StopPropagation();
        }

        void OnPointerLeave(PointerLeaveEvent e)
        {
            e.StopPropagation();
        }

        public override void OnSelected()
        {
            base.OnSelected();
            m_Selected = true;
            onSelectionDelegate?.Invoke(m_Selected);
        }

        public override void OnUnselected()
        {
            m_Selected = false;
            onSelectionDelegate?.Invoke(m_Selected);
            base.OnUnselected();
        }

        void Initialize()
        {
            this.AddStyleSheetPath("VFXNode");
            AddToClassList("VFXNodeUI");

            RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            RegisterCallback<FocusInEvent>(OnFocusIn);
        }

        public virtual void OnControllerChanged(ref ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                Profiler.BeginSample(GetType().Name + "::SelfChange()");
                SelfChange();
                Profiler.EndSample();
            }
            else if (e.controller is VFXDataAnchorController)
            {
                RefreshExpandedState();
            }
        }

        protected virtual bool HasPosition() => true;

        protected virtual void SyncSettings()
        {
            Profiler.BeginSample("VFXNodeUI.SyncSettings");
            var settings = controller.settings;
            if (settingsContainer == null && settings != null)
            {
                settingsContainer = this.Q("settings");

                m_SettingsDivider = this.Q("settings-divider");

                foreach (var setting in settings)
                {
                    AddSetting(setting);
                }
            }
            if (settingsContainer != null)
            {
                var activeSettings = controller.model.GetSettings(false, VFXSettingAttribute.VisibleFlags.InGraph).ToList();
                m_Settings.ForEach(x => x.RemoveFromHierarchy());

                hasSettings = false;
                for (int i = 0; i < m_Settings.Count; ++i)
                {
                    PropertyRM prop = m_Settings[i];
                    if (prop != null && activeSettings.Any(s => s.field.Name == settings[i].name))
                    {
                        hasSettings = true;
                        settingsContainer.Add(prop);
                        prop.Update();
                    }
                }

                if (hasSettings)
                {
                    RemoveFromClassList("nosettings");
                    settingsContainer.RemoveFromClassList("nosettings");
                }
                else
                {
                    AddToClassList("nosettings");
                    settingsContainer.AddToClassList("nosettings");
                }
            }

            if (m_SettingsDivider != null)
                m_SettingsDivider.visible = hasSettingDivider && hasSettings;
            Profiler.EndSample();
        }

        void SyncAnchors()
        {
            Profiler.BeginSample("VFXNodeUI.SyncAnchors");
            SyncAnchors(controller.inputPorts, inputContainer, controller.HasActivationAnchor);
            SyncAnchors(controller.outputPorts, outputContainer, false);
            Profiler.EndSample();
        }

        void SyncAnchors(ReadOnlyCollection<VFXDataAnchorController> ports, VisualElement container, bool hasActivationPort)
        {
            // Check whether resync is needed
            bool needsResync = false;
            if (ports.Count != container.childCount) // first check expected number match
                needsResync = true;
            else
            {
                for (int i = 0; i < ports.Count; ++i) // Then compare expected anchor one by one
                {
                    VFXDataAnchor anchor = container[i] as VFXDataAnchor;

                    if (ports[i] == null)
                        throw new NullReferenceException("VFXDataAnchorController should not be null at index " + i);

                    if (anchor?.controller != ports[i])
                    {
                        needsResync = true;
                        break;
                    }
                }
            }

            if (needsResync)
            {
                var existingAnchors = container.Children().Cast<VFXDataAnchor>()
                    .Union(titleContainer.Query<VFXDataAnchor>().ToList())
                    .ToDictionary(t => t.controller, t => t);
                container.Clear();
                for (int i = 0; i < ports.Count; ++i)
                {
                    VFXDataAnchorController portController = ports[i];

                    if (existingAnchors.TryGetValue(portController, out var anchor))
                        existingAnchors.Remove(portController);
                    else
                        anchor = InstantiateDataAnchor(portController, this); // new anchor

                    if (hasActivationPort && i == 1 || !hasActivationPort && i == 0)
                    {
                        anchor.AddToClassList("first");
                    }
                    else
                    {
                        anchor.RemoveFromClassList("first");
                    }

                    container.Add(anchor);
                }

                // delete no longer used anchors
                foreach (var anchor in existingAnchors.Values)
                {
                    GetFirstAncestorOfType<VFXView>()?.RemoveAnchorEdges(anchor);
                    anchor.parent?.Remove(anchor);
                }
            }

            UpdateActivationPortPositionIfAny(); // Needed to account for expanded state change in case of undo/redo
        }

        private void UpdateActivationPortPosition(VFXDataAnchor anchor)
        {
            if (anchor.controller.isSubgraphActivation)
                anchor.AddToClassList("subgraphblock");

            titleContainer.AddToClassList("activationslot");
            anchor.AddToClassList("activationslot");
            AddToClassList("activationslot");
        }

        private bool UpdateActivationPortPositionIfAny()
        {
            if (controller.HasActivationAnchor)
            {
                var anchorController = controller.inputPorts[0];
                var anchor = inputContainer.Children()
                    .Cast<VFXDataAnchor>()
                    .SingleOrDefault(x => x.controller == anchorController);

                if (anchor != null)
                {
                    anchor.RemoveFromHierarchy();
                    titleContainer.Insert(0, anchor);
                }
                else
                {
                    anchor = titleContainer.Q<VFXDataAnchor>();
                }
                if (anchor != null)
                {
                    UpdateActivationPortPosition(anchor);
                    return true;
                }
            }

            return false;
        }

        public void ForceUpdate()
        {
            SelfChange();
        }

        public void UpdateCollapse()
        {
            if (superCollapsed)
            {
                AddToClassList("superCollapsed");
            }
            else
            {
                RemoveFromClassList("superCollapsed");
            }
        }

        public void AssetMoved()
        {
            title = controller.title;

            m_Settings.ForEach(x => x.UpdateGUI(true));

            foreach (VFXEditableDataAnchor input in GetPorts(true, false).OfType<VFXEditableDataAnchor>())
            {
                input.AssetMoved();
            }
        }

        protected virtual void SelfChange()
        {
            Profiler.BeginSample("VFXNodeUI.SelfChange");
            if (controller == null)
                return;

            title = controller.title;

            if (HasPosition())
            {
                style.position = PositionType.Absolute;
                style.left = controller.position.x;
                style.top = controller.position.y;
            }

            base.expanded = controller.expanded;

            SyncSettings();
            SyncAnchors();
            Profiler.BeginSample("VFXNodeUI.SelfChange The Rest");
            RefreshExpandedState();
            RefreshLayout();
            Profiler.EndSample();
            Profiler.EndSample();


            UpdateCollapse();
        }

        public virtual VFXDataAnchor InstantiateDataAnchor(VFXDataAnchorController controller, VFXNodeUI node)
        {
            return controller.direction == Direction.Input
                ? VFXEditableDataAnchor.Create(controller, node)
                : VFXOutputDataAnchor.Create(controller, node);
        }

        public IEnumerable<VFXDataAnchor> GetPorts(bool input, bool output)
        {
            if (input)
            {
                foreach (var child in inputContainer.Children().OfType<VFXDataAnchor>())
                {
                    yield return child;
                }

                if (titleContainer.Q<VFXDataAnchor>() is { } activationSlot)
                {
                    yield return activationSlot;
                }
            }
            if (output)
            {
                foreach (var child in outputContainer.Children().OfType<VFXDataAnchor>())
                {
                    yield return child;
                }
            }
        }

        public virtual void GetPreferedSettingsWidths(ref float labelWidth, ref float controlWidth)
        {
            foreach (var setting in m_Settings)
            {
                if (setting.parent == null)
                    continue;
                labelWidth = Math.Max(labelWidth, setting.GetPreferredLabelWidth() + 5);
                controlWidth = Math.Max(controlWidth, setting.GetPreferredControlWidth());
            }
        }

        public virtual void GetPreferedWidths(ref float labelWidth, ref float controlWidth)
        {
        }

        public virtual void ApplyWidths(float labelWidth, float controlWidth)
        {
        }

        public virtual void ApplySettingsWidths(float labelWidth, float controlWidth)
        {
            foreach (var setting in m_Settings)
            {
                setting.SetLabelWidth(labelWidth);
            }
        }

        public const int DefaultLabelWidth = 148;

        protected void AddSetting(VFXSettingController setting)
        {
            var rm = PropertyRM.Create(setting, DefaultLabelWidth);
            if (rm != null)
            {
                m_Settings.Add(rm);
            }
            else
            {
                Debug.LogErrorFormat("Cannot create controller for {0}", setting.name);
            }
        }

        public virtual void RefreshLayout()
        {
        }
    }
}
