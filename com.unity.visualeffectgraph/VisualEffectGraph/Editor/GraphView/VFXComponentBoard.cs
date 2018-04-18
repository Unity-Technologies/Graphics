using System;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Experimental.UIElements;
using UnityEditor.VFX;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System.Text;
using UnityEditor.SceneManagement;

namespace  UnityEditor.VFX.UI
{
    static class BoardPreferenceHelper
    {
       public  enum Board
        {
            blackboard,
            componentBoard
        }


        const string rectPreferenceFormat = "vfx-{0}-rect";
        const string visiblePreferenceFormat = "vfx-{0}-visible";


        public static bool IsVisible(Board board,bool defaultState)
        {
            return EditorPrefs.GetBool(string.Format(visiblePreferenceFormat,board),defaultState);
        }

        public static void SetVisible(Board board, bool value)
        {
            EditorPrefs.SetBool(string.Format(visiblePreferenceFormat,board),value);
        }

        public static Rect LoadPosition(Board board, Rect defaultPosition)
        {
            string str = EditorPrefs.GetString(string.Format(rectPreferenceFormat,board));

            Rect blackBoardPosition = defaultPosition;;
            if (!string.IsNullOrEmpty(str))
            {
                var rectValues = str.Split(',');

                if (rectValues.Length == 4)
                {
                    float x, y, width, height;
                    if (float.TryParse(rectValues[0], out x) && float.TryParse(rectValues[1], out y) && float.TryParse(rectValues[2], out width) && float.TryParse(rectValues[3], out height))
                    {
                        blackBoardPosition = new Rect(x, y, width, height);
                    }
                }
            }

            return blackBoardPosition;
        }

        public static void SavePosition(Board board,Rect r)
        {
            EditorPrefs.SetString(string.Format(rectPreferenceFormat,board), string.Format("{0},{1},{2},{3}", r.x, r.y, r.width, r.height));
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
        
        public VFXComponentBoard(VFXView view)
        {
            m_View = view;
            var tpl = EditorGUIUtility.Load(UXMLHelper.GetUXMLPath("uxml/VFXComponentBlackboardSection.uxml")) as VisualTreeAsset;

            tpl.CloneTree(contentContainer, new Dictionary<string, VisualElement>());

            contentContainer.AddStyleSheetPath("VFXComponentBlackboardSection");

            m_AttachButton = this.Query<Button>("attach");
            m_AttachButton.clickable.clicked += ToggleAttach;

            m_ComponentPath = this.Query<Label>("component-path");

            m_ComponentContainer = this.Query("component-container");

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
            m_PlayRateSlider.lowValue = Mathf.Pow(VisualEffectControl.minSlider,1/VisualEffectControl.sliderPower);
            m_PlayRateSlider.highValue = Mathf.Pow(VisualEffectControl.maxSlider,1/VisualEffectControl.sliderPower);
            m_PlayRateSlider.valueChanged += OnEffectSlider;
            m_PlayRateField = this.Query<IntegerField>("play-rate-field");

            m_PlayRateMenu = this.Query<Button>("play-rate-menu");
            m_PlayRateMenu.AddStyleSheetPathWithSkinVariant("VFXControls");

            m_PlayRateMenu.clickable.clicked += OnPlayRateMenu;

            m_ParticleCount = this.Query<Label>("particle-count");

            Detach();
            this.AddManipulator(new Dragger { clampToParentEdges = true });

            capabilities |= Capabilities.Movable;

            RegisterCallback<ControllerChangedEvent>(ControllerChanged);

            RegisterCallback<MouseDownEvent>(OnMouseClick,Capture.Capture);

            SetPosition(BoardPreferenceHelper.LoadPosition(BoardPreferenceHelper.Board.componentBoard, new Rect(200, 100, 300, 300)));
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

        void SetPlayRate(object value)
        {
            if( m_AttachedComponent == null)
                return;
            float rate = (float)((int)value)  * VisualEffectControl.valueToPlayRate;
            m_AttachedComponent.playRate = rate;
            UpdatePlayRate();
        }

        void OnEffectSlider(float f)
        {
            if( m_AttachedComponent != null)
            {
                m_AttachedComponent.playRate = VisualEffectControl.valueToPlayRate * Mathf.Pow(f,VisualEffectControl.sliderPower);
                UpdatePlayRate();
            }
        }
        void EffectStop()
        {
            if( m_AttachedComponent != null)
                m_AttachedComponent.ControlStop();
        }
        void EffectPlay()
        {
            if( m_AttachedComponent != null)
                m_AttachedComponent.ControlPlayPause();
        }

        void EffectStep()
        {
            if( m_AttachedComponent != null)
                m_AttachedComponent.ControlStep();
        }
        void EffectRestart()
        {
            if( m_AttachedComponent != null)
                m_AttachedComponent.ControlRestart();
        }
        void OnAttachToPanel(AttachToPanelEvent e)
        {
            Selection.selectionChanged += OnSelectionChanged;
        }
        void OnDetachToPanel(DetachFromPanelEvent e)
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }


        VisualEffect m_SelectionCandidate;

        VisualEffect m_AttachedComponent;

        void OnSelectionChanged()
        {
            m_SelectionCandidate = null;
            if(Selection.activeGameObject != null && controller != null)
            {
                m_SelectionCandidate = Selection.activeGameObject.GetComponent<VisualEffect>();
                if( m_SelectionCandidate != null && m_SelectionCandidate.visualEffectAsset != controller.model)
                {
                    m_SelectionCandidate = null;
                }
            }

            UpdateAttachButton();
        }


        bool m_LastKnownPauseState;
        void UpdatePlayButton()
        {
            if( m_AttachedComponent == null )
                return;

            if( m_LastKnownPauseState != m_AttachedComponent.pause)
            {
                m_LastKnownPauseState = m_AttachedComponent.pause;
                if( m_LastKnownPauseState )
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

        void Detach()
        {
            if( m_AttachedComponent != null)
            {
                m_AttachedComponent.playRate = 1;
                m_AttachedComponent.pause = false;
            }
            m_AttachedComponent = null;
            if( m_UpdateItem != null)
            {
                m_UpdateItem.Pause();
            }
            m_ComponentContainer.RemoveFromHierarchy();
            UpdateAttachButton();
        }

        void Attach()
        {
            if( m_SelectionCandidate != null)
            {
                m_AttachedComponent = m_SelectionCandidate;
                UpdateAttachButton();
                m_LastKnownPauseState = !m_AttachedComponent.pause;
                UpdatePlayButton();

                if( m_UpdateItem == null)
                    m_UpdateItem = schedule.Execute(Update).Every(100);
                else
                    m_UpdateItem.Resume();
            }
            if( m_ComponentContainer.parent == null)
                Add(m_ComponentContainer);
        }

        IVisualElementScheduledItem m_UpdateItem;


        float m_LastKnownPlayRate = -1;


        int m_LastKnownParticleCount = -1;

        void Update()
        {
            if( m_AttachedComponent == null || controller == null)
            {
                Detach();
                return;
            }

            string path = m_AttachedComponent.name;
            
            UnityEngine.Transform current = m_AttachedComponent.transform.parent;
            while(current != null)
            {
                path = current.name + " > " + path;
            }

            if( EditorSceneManager.loadedSceneCount > 1)
            {
                path = m_AttachedComponent.gameObject.scene.name + " : " + path;
            }

            if( m_ComponentPath.text != path)
                m_ComponentPath.text = path;

            if( m_LastKnownParticleCount != m_AttachedComponent.aliveParticleCount)
            {
                m_LastKnownParticleCount = m_AttachedComponent.aliveParticleCount;
                m_ParticleCount.text = m_LastKnownParticleCount.ToString();
            }

            UpdatePlayRate();
            UpdatePlayButton();
        }

        void UpdatePlayRate()
        {
            if( m_LastKnownPlayRate != m_AttachedComponent.playRate)
            {
                m_LastKnownPlayRate = m_AttachedComponent.playRate;
                float playRateValue = m_AttachedComponent.playRate * VisualEffectControl.playRateToValue;
                m_PlayRateSlider.value = Mathf.Pow(playRateValue,1/ VisualEffectControl.sliderPower);
                if( ! m_PlayRateField.HasFocus())
                    m_PlayRateField.value = Mathf.RoundToInt(playRateValue);
            }
        }

        void ToggleAttach()
        {
            if( ! object.ReferenceEquals(m_AttachedComponent,null))
            {
                Detach();
            }
            else
            {
                Attach();
            }
        }

        Button m_AttachButton;
        Label m_ComponentPath;
        VisualElement m_ComponentContainer;

        Button m_Stop;
        Button m_Play;
        Button m_Step;
        Button m_Restart;


        Slider m_PlayRateSlider;
        IntegerField m_PlayRateField;

        Button m_PlayRateMenu;

        Label m_ParticleCount;

        public new void Clear()
        {
            Detach();
        }
        public void ControllerChanged(ControllerChangedEvent e)
        {
        }
        public void OnMoved()
        {
            BoardPreferenceHelper.SavePosition(BoardPreferenceHelper.Board.componentBoard,GetPosition());
        }
        void IVFXResizable.OnStartResize(){}
        public void OnResized()
        {
            BoardPreferenceHelper.SavePosition(BoardPreferenceHelper.Board.componentBoard,GetPosition());
        }
    }
}
