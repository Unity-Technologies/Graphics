using System;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.UIElements;
using UnityEditor.VFX;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System.Text;
using UnityEditor.SceneManagement;
using System.Globalization;

using PositionType = UnityEngine.UIElements.Position;

namespace UnityEditor.VFX.UI
{
    static class BoardPreferenceHelper
    {
        public enum Board
        {
            blackboard,
            componentBoard
        }


        const string rectPreferenceFormat = "vfx-{0}-rect";
        const string visiblePreferenceFormat = "vfx-{0}-visible";


        public static bool IsVisible(Board board, bool defaultState)
        {
            return EditorPrefs.GetBool(string.Format(visiblePreferenceFormat, board), defaultState);
        }

        public static void SetVisible(Board board, bool value)
        {
            EditorPrefs.SetBool(string.Format(visiblePreferenceFormat, board), value);
        }

        public static Rect LoadPosition(Board board, Rect defaultPosition)
        {
            string str = EditorPrefs.GetString(string.Format(rectPreferenceFormat, board));

            Rect blackBoardPosition = defaultPosition;
            if (!string.IsNullOrEmpty(str))
            {
                var rectValues = str.Split(',');

                if (rectValues.Length == 4)
                {
                    float x, y, width, height;
                    if (float.TryParse(rectValues[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
                        float.TryParse(rectValues[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y) &&
                        float.TryParse(rectValues[2], NumberStyles.Float, CultureInfo.InvariantCulture, out width) &&
                        float.TryParse(rectValues[3], NumberStyles.Float, CultureInfo.InvariantCulture, out height))
                    {
                        blackBoardPosition = new Rect(x, y, width, height);
                    }
                }
            }

            return blackBoardPosition;
        }

        public static void SavePosition(Board board, Rect r)
        {
            EditorPrefs.SetString(string.Format(rectPreferenceFormat, board), string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", r.x, r.y, r.width, r.height));
        }

        public static readonly Vector2 sizeMargin = Vector2.one * 30;

        public static bool ValidatePosition(GraphElement element, VFXView view, Rect defaultPosition)
        {
            Rect viewrect = view.contentRect;
            Rect rect = element.GetPosition();
            bool changed = false;

            if (!viewrect.Contains(rect.position))
            {
                Vector2 newPosition = defaultPosition.position;
                if (!viewrect.Contains(defaultPosition.position))
                {
                    newPosition = sizeMargin;
                }

                rect.position = newPosition;

                changed = true;
            }

            Vector2 maxSizeInView = viewrect.max - rect.position - sizeMargin;
            float newWidth = Mathf.Max(element.resolvedStyle.minWidth.value, Mathf.Min(rect.width, maxSizeInView.x));
            float newHeight = Mathf.Max(element.resolvedStyle.minHeight.value, Mathf.Min(rect.height, maxSizeInView.y));

            if (Mathf.Abs(newWidth - rect.width) > 1)
            {
                rect.width = newWidth;
                changed = true;
            }

            if (Mathf.Abs(newHeight - rect.height) > 1)
            {
                rect.height = newHeight;
                changed = true;
            }

            if (changed)
            {
                element.SetPosition(rect);
            }

            return false;
        }
    }


    class VFXComponentBoard : GraphElement, IControlledElement<VFXViewController>, IVFXMovable, IVFXResizable
    {
        VFXViewController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXViewController controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != value)
                {
                    if (m_Controller != null)
                    {
                        m_Controller.UnregisterHandler(this);
                    }
                    Clear();
                    m_Controller = value;

                    if (m_Controller != null)
                    {
                        m_Controller.RegisterHandler(this);
                    }
                }
            }
        }

        VFXView m_View;
        VFXUIDebug m_DebugUI;

        public VFXComponentBoard(VFXView view)
        {
            m_View = view;
            var tpl = VFXView.LoadUXML("VFXComponentBoard");

            tpl.CloneTree(contentContainer);

            contentContainer.AddStyleSheetPath("VFXComponentBoard");

            m_AttachButton = this.Query<Button>("attach");
            m_AttachButton.clickable.clicked += ToggleAttach;

            m_SelectButton = this.Query<Button>("select");
            m_SelectButton.clickable.clicked += Select;

            m_ComponentPath = this.Query<Label>("component-path");

            m_ComponentContainer = this.Query("component-container");
            m_ComponentContainerParent = m_ComponentContainer.parent;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachToPanel);

            m_Stop = this.Query<Button>("stop");
            m_Stop.clickable.clicked += EffectStop;
            m_Play = this.Query<Button>("play");
            m_Play.clickable.clicked += EffectPlay;
            m_Step = this.Query<Button>("step");
            m_Step.clickable.clicked += EffectStep;
            m_Restart = this.Query<Button>("restart");
            m_Restart.clickable.clicked += EffectRestart;

            m_PlayRateSlider = this.Query<Slider>("play-rate-slider");
            m_PlayRateSlider.lowValue = Mathf.Pow(VisualEffectControl.minSlider, 1 / VisualEffectControl.sliderPower);
            m_PlayRateSlider.highValue = Mathf.Pow(VisualEffectControl.maxSlider, 1 / VisualEffectControl.sliderPower);
            m_PlayRateSlider.RegisterValueChangedCallback(evt => OnEffectSlider(evt.newValue));
            m_PlayRateField = this.Query<IntegerField>("play-rate-field");
            m_PlayRateField.RegisterCallback<ChangeEvent<int>>(OnPlayRateField);

            m_PlayRateMenu = this.Query<Button>("play-rate-menu");
            m_PlayRateMenu.AddStyleSheetPathWithSkinVariant("VFXControls");

            m_PlayRateMenu.clickable.clicked += OnPlayRateMenu;

            m_ParticleCount = this.Query<Label>("particle-count");

            Button button = this.Query<Button>("on-play-button");
            button.clickable.clicked += () => SendEvent(VisualEffectAsset.PlayEventName);
            button = this.Query<Button>("on-stop-button");
            button.clickable.clicked += () => SendEvent(VisualEffectAsset.StopEventName);

            m_EventsContainer = this.Query("events-container");

            m_DebugModes = this.Query<Button>("debug-modes");
            m_DebugModes.clickable.clicked += OnDebugModes;

            Detach();
            this.AddManipulator(new Dragger { clampToParentEdges = true });

            capabilities |= Capabilities.Movable;

            RegisterCallback<MouseDownEvent>(OnMouseClick, TrickleDown.TrickleDown);

            style.position = PositionType.Absolute;

            SetPosition(BoardPreferenceHelper.LoadPosition(BoardPreferenceHelper.Board.componentBoard, defaultRect));
        }

        VisualElement m_ComponentContainerParent;

        public void ValidatePosition()
        {
            BoardPreferenceHelper.ValidatePosition(this, m_View, defaultRect);
        }

        static readonly Rect defaultRect = new Rect(200, 100, 300, 300);

        public override Rect GetPosition()
        {
            return new Rect(resolvedStyle.left, resolvedStyle.top, resolvedStyle.width, resolvedStyle.height);
        }

        public override void SetPosition(Rect newPos)
        {
            style.left = newPos.xMin;
            style.top = newPos.yMin;
            style.width = newPos.width;
            style.height = newPos.height;
        }

        void OnMouseClick(MouseDownEvent e)
        {
            m_View.SetBoardToFront(this);
        }

        void OnPlayRateMenu()
        {
            GenericMenu menu = new GenericMenu();
            foreach (var value in VisualEffectControl.setPlaybackValues)
            {
                menu.AddItem(EditorGUIUtility.TextContent(string.Format("{0}%", value)), false, SetPlayRate, value);
            }
            menu.DropDown(m_PlayRateMenu.worldBound);
        }

        void OnPlayRateField(ChangeEvent<int> e)
        {
            SetPlayRate(e.newValue);
        }

        void SetPlayRate(object value)
        {
            if (m_AttachedComponent == null)
                return;
            float rate = (float)((int)value) * VisualEffectControl.valueToPlayRate;
            m_AttachedComponent.playRate = rate;
            UpdatePlayRate();
        }

        void OnDebugModes()
        {
            GenericMenu menu = new GenericMenu();
            foreach (VFXUIDebug.Modes mode in Enum.GetValues(typeof(VFXUIDebug.Modes)))
            {
                menu.AddItem(EditorGUIUtility.TextContent(mode.ToString()), false, SetDebugMode, mode);
            }
            menu.DropDown(m_DebugModes.worldBound);
        }

        void SetDebugMode(object mode)
        {
            m_DebugUI.SetDebugMode((VFXUIDebug.Modes)mode, this);
        }

        void OnEffectSlider(float f)
        {
            if (m_AttachedComponent != null)
            {
                m_AttachedComponent.playRate = VisualEffectControl.valueToPlayRate * Mathf.Pow(f, VisualEffectControl.sliderPower);
                UpdatePlayRate();
            }
        }

        void EffectStop()
        {
            if (m_AttachedComponent != null)
                m_AttachedComponent.ControlStop();
            if (m_DebugUI != null)
                m_DebugUI.Notify(VFXUIDebug.Events.VFXStop);
        }

        void EffectPlay()
        {
            if (m_AttachedComponent != null)
                m_AttachedComponent.ControlPlayPause();
            if (m_DebugUI != null)
                m_DebugUI.Notify(VFXUIDebug.Events.VFXPlayPause);
        }

        void EffectStep()
        {
            if (m_AttachedComponent != null)
                m_AttachedComponent.ControlStep();
            if (m_DebugUI != null)
                m_DebugUI.Notify(VFXUIDebug.Events.VFXStep);
        }

        void EffectRestart()
        {
            if (m_AttachedComponent != null)
                m_AttachedComponent.ControlRestart();
            if (m_DebugUI != null)
                m_DebugUI.Notify(VFXUIDebug.Events.VFXReset);
        }

        public void OnVisualEffectComponentChanged(IEnumerable<VisualEffect> visualEffects)
        {
            OnSelectionChanged();
            if (m_AttachedComponent != null
                && visualEffects.Contains(m_AttachedComponent)
                && m_AttachedComponent.visualEffectAsset != controller.graph.visualEffectResource.asset)
            {
                //The Visual Effect Asset has been changed and is no longer valid, we don't want to modify capacity on the wrong graph. We have to detach.
                m_View.attachedComponent = null;
            }
        }

        void OnAttachToPanel(AttachToPanelEvent e)
        {
            OnSelectionChanged();
            Selection.selectionChanged += OnSelectionChanged;
        }

        void OnDetachToPanel(DetachFromPanelEvent e)
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        VisualEffect m_SelectionCandidate;

        VisualEffect m_AttachedComponent;

        public VisualEffect GetAttachedComponent()
        {
            return m_AttachedComponent;
        }

        void OnSelectionChanged()
        {
            if (Selection.activeGameObject != null && controller != null)
            {
                m_SelectionCandidate = null;
                m_SelectionCandidate = Selection.activeGameObject.GetComponent<VisualEffect>();
                if (m_SelectionCandidate != null && m_SelectionCandidate.visualEffectAsset != controller.graph.visualEffectResource.asset)
                {
                    m_SelectionCandidate = null;
                }
            }

            UpdateAttachButton();
        }

        bool m_LastKnownPauseState;
        void UpdatePlayButton()
        {
            if (m_AttachedComponent == null)
                return;

            if (m_LastKnownPauseState != m_AttachedComponent.pause)
            {
                m_LastKnownPauseState = m_AttachedComponent.pause;
                if (m_LastKnownPauseState)
                {
                    m_Play.AddToClassList("paused");
                }
                else
                {
                    m_Play.RemoveFromClassList("paused");
                }
            }
        }

        void UpdateAttachButton()
        {
            m_AttachButton.SetEnabled(m_SelectionCandidate != null || m_AttachedComponent != null && controller != null);

            m_AttachButton.text = m_AttachedComponent != null ? "Detach" : "Attach";
        }

        public void Detach()
        {
            if (m_AttachedComponent != null)
            {
                m_AttachedComponent.playRate = 1;
                m_AttachedComponent.pause = false;
            }
            m_AttachedComponent = null;
            if (m_UpdateItem != null)
            {
                m_UpdateItem.Pause();
            }
            m_ComponentContainer.RemoveFromHierarchy();
            m_ComponentPath.text = "";
            UpdateAttachButton();
            if (m_EventsContainer != null)
                m_EventsContainer.Clear();
            m_Events.Clear();
            m_SelectButton.visible = false;
            if (m_DebugUI != null)
                m_DebugUI.Clear();
        }

        public void Attach(VisualEffect effect = null)
        {
            VisualEffect target = effect != null ? effect : m_SelectionCandidate;
            if (target != null)
            {
                m_SelectionCandidate = target; // allow reattaching if effet != null;
                m_AttachedComponent = target;
                UpdateAttachButton();
                m_LastKnownPauseState = !m_AttachedComponent.pause;
                UpdatePlayButton();

                if (m_UpdateItem == null)
                    m_UpdateItem = schedule.Execute(Update).Every(100);
                else
                    m_UpdateItem.Resume();
                if (m_ComponentContainer.parent == null)
                    m_ComponentContainerParent.Add(m_ComponentContainer);
                UpdateEventList();
                m_SelectButton.visible = true;

                var debugMode = VFXUIDebug.Modes.None;
                if (m_DebugUI != null)
                {
                    debugMode = m_DebugUI.GetDebugMode();
                    m_DebugUI.Clear();
                }

                m_DebugUI = new VFXUIDebug(m_View);
                m_DebugUI.SetVisualEffect(m_AttachedComponent);
                m_DebugUI.SetDebugMode(debugMode, this, true);
            }
        }

        public void SendEvent(string name)
        {
            if (m_AttachedComponent != null)
            {
                m_AttachedComponent.SendEvent(name);
            }
        }

        IVisualElementScheduledItem m_UpdateItem;


        float m_LastKnownPlayRate = -1;


        int m_LastKnownParticleCount = -1;

        void Update()
        {
            if (m_AttachedComponent == null || controller == null)
            {
                Detach();
                return;
            }

            string path = m_AttachedComponent.name;

            UnityEngine.Transform current = m_AttachedComponent.transform.parent;
            while (current != null)
            {
                path = current.name + " > " + path;
                current = current.parent;
            }

            if (EditorSceneManager.loadedSceneCount > 1)
            {
                path = m_AttachedComponent.gameObject.scene.name + " : " + path;
            }

            if (m_ComponentPath.text != path)
                m_ComponentPath.text = path;

            if (m_ParticleCount != null)
            {
                int newParticleCount = 0;//m_AttachedComponent.aliveParticleCount
                if (m_LastKnownParticleCount != newParticleCount)
                {
                    m_LastKnownParticleCount = newParticleCount;
                    m_ParticleCount.text = m_LastKnownParticleCount.ToString();
                }
            }

            UpdatePlayRate();
            UpdatePlayButton();
        }

        void UpdatePlayRate()
        {
            if (m_LastKnownPlayRate != m_AttachedComponent.playRate)
            {
                m_LastKnownPlayRate = m_AttachedComponent.playRate;
                float playRateValue = m_AttachedComponent.playRate * VisualEffectControl.playRateToValue;
                m_PlayRateSlider.value = Mathf.Pow(playRateValue, 1 / VisualEffectControl.sliderPower);
                if (m_PlayRateField != null && !m_PlayRateField.HasFocus())
                    m_PlayRateField.value = Mathf.RoundToInt(playRateValue);
            }
        }

        void ToggleAttach()
        {
            if (!object.ReferenceEquals(m_AttachedComponent, null))
            {
                Detach();
            }
            else
            {
                Attach();
            }
        }

        void Select()
        {
            if (m_AttachedComponent != null)
            {
                Selection.activeObject = m_AttachedComponent;
            }
        }

        Button m_AttachButton;
        Button m_SelectButton;
        Label m_ComponentPath;
        VisualElement m_ComponentContainer;
        VisualElement m_EventsContainer;

        Button m_Stop;
        Button m_Play;
        Button m_Step;
        Button m_Restart;


        Slider m_PlayRateSlider;
        IntegerField m_PlayRateField;

        Button m_PlayRateMenu;
        Button m_DebugModes;

        Label m_ParticleCount;

        public new void Clear()
        {
            Detach();
        }

        void IControlledElement.OnControllerChanged(ref ControllerChangedEvent e)
        {
            UpdateEventList();
        }

        static readonly string[] staticEventNames = new string[] { VisualEffectAsset.PlayEventName, VisualEffectAsset.StopEventName };


        static bool IsDefaultEvent(string evt)
        {
            return evt == VisualEffectAsset.PlayEventName || evt == VisualEffectAsset.StopEventName;
        }

        IEnumerable<String> GetEventNames()
        {
            foreach (var context in controller.contexts.Select(t => t.model).OfType<VFXContext>())
            {
                foreach (var name in RecurseGetEventNames(context))
                    yield return name;
            }
        }

        IEnumerable<String> RecurseGetEventNames(VFXContext context)
        {
            if (context is VFXBasicEvent)
            {
                if (!IsDefaultEvent(name))
                    yield return (context as VFXBasicEvent).eventName;
            }
            else if (context is VFXSubgraphContext)
            {
                foreach (var subContext in (context as VFXSubgraphContext).subChildren.OfType<VFXContext>())
                {
                    foreach (var name in RecurseGetEventNames(subContext))
                        yield return name;
                }
            }
        }

        public void UpdateEventList()
        {
            if (m_AttachedComponent == null)
            {
                if (m_EventsContainer != null)
                    m_EventsContainer.Clear();
                m_Events.Clear();
            }
            else
            {
                var eventNames = GetEventNames().ToArray();

                foreach (var removed in m_Events.Keys.Except(eventNames).ToArray())
                {
                    var ui = m_Events[removed];
                    m_EventsContainer.Remove(ui);
                    m_Events.Remove(removed);
                }

                foreach (var added in eventNames.Except(m_Events.Keys).ToArray())
                {
                    var tpl = VFXView.LoadUXML("VFXComponentBoard-event");

                    tpl.CloneTree(m_EventsContainer);

                    VFXComponentBoardEventUI newUI = m_EventsContainer.Children().Last() as VFXComponentBoardEventUI;
                    if (newUI != null)
                    {
                        newUI.Setup();
                        newUI.name = added;
                        m_Events.Add(added, newUI);
                    }
                }

                if (!m_Events.Values.Any(t => t.nameHasFocus))
                {
                    SortEventList();
                }
            }
        }

        void SortEventList()
        {
            var eventNames = m_Events.Keys.OrderBy(t => t);
            //Sort events
            VFXComponentBoardEventUI prev = null;
            foreach (var eventName in eventNames)
            {
                VFXComponentBoardEventUI current = m_Events[eventName];
                if (current != null)
                {
                    if (prev == null)
                    {
                        current.SendToBack();
                    }
                    else
                    {
                        current.PlaceInFront(prev);
                    }
                    prev = current;
                }
            }
        }

        Dictionary<string, VFXComponentBoardEventUI> m_Events = new Dictionary<string, VFXComponentBoardEventUI>();

        public override void UpdatePresenterPosition()
        {
            BoardPreferenceHelper.SavePosition(BoardPreferenceHelper.Board.componentBoard, GetPosition());
        }

        public void OnMoved()
        {
            BoardPreferenceHelper.SavePosition(BoardPreferenceHelper.Board.componentBoard, GetPosition());
        }

        void IVFXResizable.OnStartResize() {}
        public void OnResized()
        {
            BoardPreferenceHelper.SavePosition(BoardPreferenceHelper.Board.componentBoard, GetPosition());
        }
    }
    class VFXComponentBoardEventUIFactory : UxmlFactory<VFXComponentBoardEventUI>
    {}
    class VFXComponentBoardEventUI : VisualElement
    {
        public VFXComponentBoardEventUI()
        {
        }

        public void Setup()
        {
            m_EventName = this.Query<TextField>("event-name");
            m_EventName.isDelayed = true;
            m_EventName.RegisterCallback<ChangeEvent<string>>(OnChangeName);
            m_EventSend = this.Query<Button>("event-send");
            m_EventSend.clickable.clicked += OnSend;
        }

        void OnChangeName(ChangeEvent<string> e)
        {
            var board = GetFirstAncestorOfType<VFXComponentBoard>();
            if (board != null)
            {
                board.controller.ChangeEventName(m_Name, e.newValue);
            }
        }

        public bool nameHasFocus
        {
            get { return m_EventName.HasFocus(); }
        }

        public new string name
        {
            get
            {
                return m_Name;
            }

            set
            {
                m_Name = value;
                if (m_EventName != null)
                {
                    if (!m_EventName.HasFocus())
                        m_EventName.SetValueWithoutNotify(m_Name);
                }
            }
        }

        string      m_Name;
        TextField   m_EventName;
        Button      m_EventSend;

        void OnSend()
        {
            var board = GetFirstAncestorOfType<VFXComponentBoard>();
            if (board != null)
            {
                board.SendEvent(m_Name);
            }
        }
    }
}
