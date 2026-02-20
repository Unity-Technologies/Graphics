using System;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;
using AntialiasingMode = UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.AntialiasingMode;

namespace UnityEditor.Rendering.HighDefinition
{
    [InitializeOnLoad]
    static class HDAdditionalSceneViewSettings
    {
        const string k_UXMLResourcePath = "Packages/com.unity.render-pipelines.high-definition/Editor/RenderPipeline/Camera/HDAdditionalSceneViewSettings.uxml";

        [Serializable]
        class HDAdditionalData : SceneView.AdditionalSettings<HDRenderPipelineAsset, HDAdditionalCameraData>
        {
            [SerializeField] AntialiasingMode m_Antialiasing;
            [SerializeField] bool m_StopNaNs;
            [SerializeField] bool m_OverrideExposure;
            [SerializeField] float m_Exposure;

            public AntialiasingMode antialiasing
            {
                get => m_Antialiasing;
                set
                {
                    m_Antialiasing = value;
                    linkedComponent.antialiasing = value;
                }
            }

            public bool stopNaNs
            {
                get => m_StopNaNs;
                set
                {
                    m_StopNaNs = value;
                    linkedComponent.stopNaNs = value;
                }
            }

            public bool overrideExposure
            {
                get => m_OverrideExposure;
                set
                {
                    m_OverrideExposure = value;
                    linkedComponent.doesSceneViewOverrideExposure = value;
                }
            }

            public float exposure
            {
                get => m_Exposure;
                set
                {
                    m_Exposure = value;
                    linkedComponent.sceneViewOverrideExposureValue = value;
                }
            }

            public HDAdditionalData(HDAdditionalCameraData component)
            {
                Reset();
                SetLinkedComponent(component);
                Apply();
            }

            internal void SetLinkedComponent(HDAdditionalCameraData component)
                => linkedComponent = component;
            
            public override void Apply()
            {
                linkedComponent.antialiasing = m_Antialiasing;
                linkedComponent.stopNaNs = m_StopNaNs;
                linkedComponent.doesSceneViewOverrideExposure = m_OverrideExposure;
                linkedComponent.sceneViewOverrideExposureValue = m_Exposure;
            }

            public override void Reset()
            {
                m_Antialiasing = AntialiasingMode.None;
                m_StopNaNs = false;
                m_OverrideExposure = false;
                m_Exposure = 10;
            }
        }

        static HDAdditionalSceneViewSettings()
        {
            SceneView.onCameraCreated += EnsureAdditionalData;
            RenderPipelineManager.activeRenderPipelineCreated += AddHooks;
        }
        
        static HDAdditionalData GetSettings(SceneView sceneView) 
            => sceneView.GetAdditionalSettings<HDAdditionalData>();
        
        static void EnsureAdditionalData(SceneView sceneView)
        {
            if (!sceneView.camera.TryGetComponent(out HDAdditionalCameraData hdAdditionalCameraData))
                hdAdditionalCameraData = sceneView.camera.gameObject.AddComponent<HDAdditionalCameraData>();

            HDAdditionalData additionalData = sceneView.GetAdditionalSettings<HDAdditionalData>();
            if (additionalData == null)
            {
                additionalData = new HDAdditionalData(hdAdditionalCameraData);
                sceneView.AddAdditionalSettings(additionalData);
            }

            additionalData.SetLinkedComponent(hdAdditionalCameraData);
            additionalData.Apply();
        }

        static void AddHooks()
        {
            if (GraphicsSettings.currentRenderPipelineAssetType != typeof(HDRenderPipelineAsset))
                return;
            SceneViewCameraWindow.createAdditionalSettingsGUI += CreateGUI;
            SceneViewCameraWindow.bindAdditionalSettings += Bind;
            RenderPipelineManager.activeRenderPipelineDisposed += RemoveHooks;
        }

        public static void RemoveHooks()
        {
            SceneViewCameraWindow.createAdditionalSettingsGUI -= CreateGUI;
            SceneViewCameraWindow.bindAdditionalSettings -= Bind;
            RenderPipelineManager.activeRenderPipelineDisposed -= RemoveHooks;
        }

        static VisualElement CreateGUI(SceneView sceneView)
        {
            var root = new VisualElement();
            var visualTreeAsset = (VisualTreeAsset)EditorResources.Load<UnityEngine.Object>(k_UXMLResourcePath, isRequired: true);
            visualTreeAsset.CloneTree(root);

            var antiAliasing = root.Q<EnumField>("AntiAliasing");
            Assert.IsNotNull(antiAliasing);
            var taaWarning = root.Q<HelpBox>("TAAWarning");
            Assert.IsNotNull(taaWarning);
            var stopNaNs = root.Q<Toggle>("StopNaNs");
            Assert.IsNotNull(stopNaNs);
            var overrideExposure = root.Q<Toggle>("OverrideExposure");
            Assert.IsNotNull(overrideExposure);
            var exposureValue = root.Q<Slider>("ExposureValue");
            Assert.IsNotNull(exposureValue);
            
            // Links between VisualElement and callbacks
            antiAliasing.RegisterValueChangedCallback(evt =>
            {
                AntialiasingMode newValue = (AntialiasingMode)evt.newValue;
                GetSettings(sceneView).antialiasing = newValue;
                taaWarning.style.display = newValue == AntialiasingMode.TemporalAntialiasing ? DisplayStyle.Flex : DisplayStyle.None;
                sceneView.Repaint();
            });
            
            stopNaNs.RegisterValueChangedCallback(evt =>
            {
                GetSettings(sceneView).stopNaNs = evt.newValue;
                sceneView.Repaint();
            });
            
            overrideExposure.RegisterValueChangedCallback(evt =>
            {
                GetSettings(sceneView).overrideExposure = evt.newValue;
                exposureValue.SetEnabled(evt.newValue);
                sceneView.Repaint();
            });
            
            exposureValue.RegisterValueChangedCallback(evt =>
            {
                GetSettings(sceneView).exposure = evt.newValue;
                sceneView.Repaint();
            });
            
            return root;
        }

        static void Bind(SceneView sceneView, VisualElement root)
        {
            var antiAliasing = root.Q<EnumField>("AntiAliasing");
            var taaWarning = root.Q<HelpBox>("TAAWarning");
            var stopNaNs = root.Q<Toggle>("StopNaNs");
            var overrideExposure = root.Q<Toggle>("OverrideExposure");
            var exposureValue = root.Q<Slider>("ExposureValue");

            var settings = GetSettings(sceneView);
            taaWarning.style.display = settings.antialiasing == AntialiasingMode.TemporalAntialiasing ? DisplayStyle.Flex : DisplayStyle.None;
            exposureValue.SetEnabled(settings.overrideExposure);

            antiAliasing.SetValueWithoutNotify(settings.antialiasing);
            stopNaNs.SetValueWithoutNotify(settings.stopNaNs);
            overrideExposure.SetValueWithoutNotify(settings.overrideExposure);
            exposureValue.SetValueWithoutNotify(settings.exposure);
        }
    }
}