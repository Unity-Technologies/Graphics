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
        private VisualElement settingsContainer { get; set; }

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

        public override string title
        {
            get => controller?.name;
            set { }
        }

        protected float defaultLabelWidth { get; set; } = DefaultLabelWidth;
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

        protected virtual void OnPostLayout(GeometryChangedEvent e)
        {
            RefreshLayout();
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
            settingsContainer = this.Q("settings");

            // Remove useless child element to reduce number of VisualElements
            this.Q<VisualElement>("collapse-button")?.Clear();

            RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            RegisterCallback<FocusInEvent>(OnFocusIn);
            RegisterCallback<GeometryChangedEvent>(OnPostLayout);
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

        private void SyncSettings()
        {
            Profiler.BeginSample("VFXNodeUI.SyncSettings");
            var settings = controller.settings;
            var graphSettings = controller.model.GetSettings(false, VFXSettingAttribute.VisibleFlags.InGraph).ToArray();

            // Remove extra settings
            foreach (var propertyRM in m_Settings.ToArray())
            {
                if (graphSettings.All(x => string.Compare(x.field.Name, propertyRM.provider.name, StringComparison.OrdinalIgnoreCase) != 0))
                {
                    propertyRM.RemoveFromHierarchy();
                    m_Settings.Remove(propertyRM);
                }
            }

            // Add missing settings
            for (var i = 0; i < graphSettings.Length; i++)
            {
                var vfxSetting = graphSettings[i];
                if (m_Settings.All(x => string.Compare(x.provider.name, vfxSetting.name, StringComparison.OrdinalIgnoreCase) != 0))
                {
                    var setting = settings.Single(x => string.Compare(x.name, vfxSetting.field.Name, StringComparison.OrdinalIgnoreCase) == 0);
                    var propertyRM = AddSetting(setting);
                    settingsContainer.Insert(i, propertyRM);
                }
            }

            foreach (var propertyRM in m_Settings.ToArray())
            {
                propertyRM.Update();
            }

            hasSettings = m_Settings.Count > 0;
            if (settingsContainer != null)
            {
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

            Profiler.EndSample();
        }

        void SyncAnchors()
        {
            Profiler.BeginSample("VFXNodeUI.SyncAnchors");
            SyncAnchors(controller.outputPorts, outputContainer, false);
            SyncAnchors(controller.inputPorts, inputContainer, controller.HasActivationAnchor);
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

                    if (!existingAnchors.Remove(portController, out var anchor))
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

            if (hasActivationPort)
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

        protected void UpdateCollapse()
        {
            if (superCollapsed)
            {
                AddToClassList("superCollapsed");
            }
            else
            {
                RemoveFromClassList("superCollapsed");
            }

            if (controller.inputPorts.Count == (controller.HasActivationAnchor ? 1 : 0) && controller.outputPorts.Count == 0)
            {
                AddToClassList("cannot-expand");
            }
            else
            {
                RemoveFromClassList("cannot-expand");
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

        protected virtual void UpdateTitleUI()
        {
            titleContainer
                .Children()
                .OfType<Label>()
                .Where(x => x.name != "header-space")
                .ToList()
                .ForEach(titleContainer.Remove);
            var index = 0;
            foreach (var label in controller.title.SplitTextIntoLabels("setting"))
            {
                if (index == 0)
                    label.AddToClassList("first");
                titleContainer.Insert(index++, label);
            }
            titleContainer.Query<Label>().Last().AddToClassList("last");


            var spacer = titleContainer.Q<VisualElement>("spacer");
            if (spacer == null)
            {
                titleContainer.Insert(index, new VisualElement { name = "spacer" });
            }
        }

        protected virtual void SelfChange()
        {
            Profiler.BeginSample("VFXNodeUI.SelfChange");
            if (controller == null)
                return;

            UpdateTitleUI();

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
            Profiler.EndSample();
            Profiler.EndSample();

            UpdateCollapse();
            RefreshLayout();
        }

        protected virtual VFXDataAnchor InstantiateDataAnchor(VFXDataAnchorController ctrl, VFXNodeUI node)
        {
            return ctrl.direction == Direction.Input
                ? VFXEditableDataAnchor.Create(ctrl, node)
                : VFXOutputDataAnchor.Create(ctrl, node);
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

        private void GetPreferredSettingsWidths(ref float labelWidth, ref float controlWidth)
        {
            foreach (var setting in m_Settings)
            {
                labelWidth = Math.Max(labelWidth, setting.GetPreferredLabelWidth());
                controlWidth = Math.Max(controlWidth, setting.GetPreferredControlWidth());
            }
        }

        private void GetPreferredWidths(ref float labelWidth, ref float controlWidth)
        {
            foreach (var port in GetPorts(true, false).OfType<VFXEditableDataAnchor>())
            {
                // Skip because it's not visible
                if (!port.connected && !expanded)
                    continue;

                float portLabelWidth = port.GetPreferredLabelWidth();
                float portControlWidth = port.GetPreferredControlWidth();

                if (labelWidth < portLabelWidth)
                {
                    labelWidth = portLabelWidth;
                }
                if (controlWidth < portControlWidth)
                {
                    controlWidth = portControlWidth;
                }
            }
        }

        protected virtual void ApplyWidths(float labelWidth, float controlWidth)
        {
            foreach (var port in GetPorts(true, false).OfType<VFXEditableDataAnchor>())
            {
                port.SetLabelWidth(labelWidth);
            }
        }

        private void ApplySettingsWidths(float labelWidth)
        {
            foreach (var setting in m_Settings)
            {
                setting.SetLabelWidth(labelWidth);
            }
        }

        public const float DefaultLabelWidth = 148f;

        private PropertyRM AddSetting(VFXSettingController setting)
        {
            var rm = PropertyRM.Create(setting, defaultLabelWidth);
            if (rm != null)
            {
                m_Settings.Add(rm);
            }
            else
            {
                Debug.LogErrorFormat("Cannot create controller for {0}", setting.name);
            }

            return rm;
        }

        public void GetWidths(out float labelWidth, out float controlWidth)
        {
            var settingsLabelWidth = 0f;
            var inputsLabelWidth = 0f;
            controlWidth = 50f;
            // Settings are only visible when node is expanded
            if (expanded)
                GetPreferredSettingsWidths(ref settingsLabelWidth, ref controlWidth);
            GetPreferredWidths(ref inputsLabelWidth, ref controlWidth);
            labelWidth = Mathf.Max(settingsLabelWidth, inputsLabelWidth);
            if (labelWidth > 0)
                labelWidth = Mathf.Max(labelWidth, defaultLabelWidth);
        }

        protected virtual void RefreshLayout()
        {
            GetWidths(out var labelWidth, out var controlWidth);
            ApplySettingsWidths(labelWidth);
            ApplyWidths(labelWidth, controlWidth);
        }
    }
}
