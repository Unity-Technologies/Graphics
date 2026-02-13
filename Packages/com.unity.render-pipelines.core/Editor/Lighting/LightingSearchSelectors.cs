using System;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Lighting
{
    static class CoreLightingSearchSelectors
    {
        internal const string k_SceneProvider = "scene";
        internal const string k_BakingSetPath = "BakingSets/";
        internal const string k_VolumePath = "Volume/";
        internal const string k_LightPath = "Light/";
        internal const string k_LightShapePath = k_LightPath + "Shape";
        internal const string k_BakingModePath = k_BakingSetPath + "BakingMode";
        internal const string k_SkyOcclusionBakingSamplesPath = k_BakingSetPath + "SkyOcclusionBakingSamples";
        internal const string k_VolumeModePath = k_VolumePath + "Mode";
        internal const string k_VolumeProfilePath = k_VolumePath + "Profile";

        const string k_StyleSheetPath = "StyleSheets/LightingSearchSelectors.uss";
        const int k_DefaultSearchSelectorPriority = 99;
        const int k_MinBakingSamples = 1;
        const int k_MaxBakingSamples = 8192;
        static StyleSheet s_StyleSheet;

        static StyleSheet LoadStyleSheet()
        {
            if (s_StyleSheet == null)
            {
                s_StyleSheet = EditorGUIUtility.Load(k_StyleSheetPath) as StyleSheet;
            }
            return s_StyleSheet;
        }

        [SearchSelector(k_VolumeModePath, provider: k_SceneProvider, priority: k_DefaultSearchSelectorPriority)]
        static object VolumeModeSearchSelector(SearchSelectorArgs args)
        {
            var go = args.current.ToObject<GameObject>();
            if (go == null)
                return null;

            return LightingSearchDataAccessors.GetVolumeMode(go);
        }

        [SearchSelector(k_VolumeProfilePath, provider: k_SceneProvider, priority: k_DefaultSearchSelectorPriority)]
        static object VolumeProfileSearchSelector(SearchSelectorArgs args)
        {
            var go = args.current.ToObject<GameObject>();
            if (go == null)
                return null;

            return LightingSearchDataAccessors.GetVolumeProfile(go);
        }

        [SearchColumnProvider(k_BakingModePath)]
        public static void BakingModeSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var bakingSet = args.item.ToObject() as ProbeVolumeBakingSet;
                if (bakingSet == null)
                    return null;

                return LightingSearchDataAccessors.GetBakingMode(bakingSet);
            };
            column.setter = args =>
            {
                if (args.value is not string)
                    return;

                var bakingSet = args.item.ToObject() as ProbeVolumeBakingSet;
                if (bakingSet == null)
                    return;

                LightingSearchDataAccessors.SetBakingMode(bakingSet, (string)args.value);
            };
            column.cellCreator = _ =>
            {
                var dropdown = new DropdownField
                {
                    choices = new System.Collections.Generic.List<string> { "Baking Set", "Single Scene" }
                };
                return dropdown;
            };
            column.binder = (args, ve) =>
            {
                var bakingSet = args.item.ToObject() as ProbeVolumeBakingSet;
                if (bakingSet == null)
                {
                    ve.visible = false;
                    return;
                }

                var dropdown = (DropdownField)ve;
                var mode = (string)args.value;

                ve.visible = true;
                dropdown.SetValueWithoutNotify(mode);
            };
        }

        [SearchColumnProvider(k_SkyOcclusionBakingSamplesPath)]
        public static void SkyOcclusionBakingSamplesSearchColumnProvider(SearchColumn column)
        {
            static int SamplesToSliderValue(int samples)
            {
                int value = k_MinBakingSamples;
                while (value < samples && value < k_MaxBakingSamples)
                {
                    value *= 2;
                }
                return value;
            }

            column.getter = args =>
            {
                var bakingSet = args.item.ToObject() as ProbeVolumeBakingSet;
                if (bakingSet == null)
                    return null;

                return LightingSearchDataAccessors.GetSkyOcclusionBakingSamples(bakingSet);
            };
            column.setter = args =>
            {
                if (args.value is not int)
                    return;

                var bakingSet = args.item.ToObject() as ProbeVolumeBakingSet;
                if (bakingSet == null)
                    return;

                int samples = (int)args.value;
                int roundedSamplesValue = SamplesToSliderValue(samples);

                LightingSearchDataAccessors.SetSkyOcclusionBakingSamples(bakingSet, roundedSamplesValue);
            };
            column.cellCreator = _ =>
            {
                var container = new VisualElement();
                container.AddToClassList("core-lighting-search-baking-samples");

                var slider = new SliderInt(k_MinBakingSamples, k_MaxBakingSamples);
                slider.AddToClassList("core-lighting-search-baking-samples__slider");

                var integerField = new IntegerField();
                integerField.AddToClassList("core-lighting-search-baking-samples__text-field");

                container.Add(slider);
                container.Add(integerField);

                var styleSheet = LoadStyleSheet();
                if (styleSheet != null)
                    container.styleSheets.Add(styleSheet);

                return container;
            };
            column.binder = (args, ve) =>
            {
                var bakingSet = args.item.ToObject() as ProbeVolumeBakingSet;
                if (bakingSet == null)
                {
                    ve.visible = false;
                    return;
                }

                var samples = (int)args.value;

                ve.visible = true;
                var slider = ve.Q<SliderInt>();
                var textField = ve.Q<IntegerField>();

                slider.SetValueWithoutNotify(samples);
                textField.SetValueWithoutNotify(samples);
            };
        }

        [SearchColumnProvider(k_VolumeModePath)]
        public static void VolumeModeSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return null;

                return LightingSearchDataAccessors.GetVolumeMode(go);
            };
            column.setter = args =>
            {
                if (args.value is not string)
                    return;

                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return;

                LightingSearchDataAccessors.SetVolumeMode(go, (string)args.value);
            };
            column.cellCreator = _ =>
            {
                var dropdown = new DropdownField
                {
                    choices = new System.Collections.Generic.List<string> { "Global", "Local" }
                };
                return dropdown;
            };
            column.binder = (args, ve) =>
            {
                var go = args.item.ToObject<GameObject>();
                if (go == null || !go.TryGetComponent<Volume>(out _))
                {
                    ve.visible = false;
                    return;
                }

                var dropdown = (DropdownField)ve;
                var mode = (string)args.value;

                ve.visible = true;
                dropdown.SetValueWithoutNotify(mode);
            };
        }

        [SearchColumnProvider(k_VolumeProfilePath)]
        public static void VolumeProfileSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return null;

                return LightingSearchDataAccessors.GetVolumeProfile(go);
            };
            column.setter = args =>
            {
                if (args.value is not VolumeProfile)
                    return;

                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return;

                LightingSearchDataAccessors.SetVolumeProfile(go, (VolumeProfile)args.value);
            };
            column.cellCreator = _ =>
            {
                var field = new UnityEditor.UIElements.ObjectField { objectType = typeof(VolumeProfile) };
                field.AddToClassList("core-lighting-search-volume-profile");

                var styleSheet = LoadStyleSheet();
                if (styleSheet != null)
                    field.styleSheets.Add(styleSheet);

                return field;
            };
            column.binder = (args, ve) =>
            {
                var go = args.item.ToObject<GameObject>();
                if (go == null || !go.TryGetComponent<Volume>(out _))
                {
                    ve.visible = false;
                    return;
                }

                var objectField = (UnityEditor.UIElements.ObjectField)ve;
                var profile = args.value as VolumeProfile;

                ve.visible = true;
                objectField.SetValueWithoutNotify(profile);
            };
        }

        [SearchColumnProvider(k_LightShapePath)]
        internal static void LightShapeSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return null;

                if (!go.TryGetComponent<Light>(out var light))
                    return null;

                return LightingSearchDataAccessors.GetLightShape(go);
            };
            column.setter = args =>
            {
                if (args.value == null || !args.value.GetType().IsEnum)
                    return;

                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return;

                LightingSearchDataAccessors.SetLightShape(go, (LightType)args.value);
            };
            column.cellCreator = _ => new LightShapeField();
            column.binder = (args, ve) =>
            {
                var field = (LightShapeField)ve;
                if (args.value is LightType lightType)
                {
                    if (IsLightShapeApplicable(lightType))
                    {
                        field.visible = true;
                        field.SetValueWithoutNotify(lightType);
                    }
                    else
                    {
                        field.visible = false;
                    }
                }
                else
                {
                    field.visible = false;
                }
            };
        }

        static class LightingSearchDataAccessors
        {
            internal static string GetBakingMode(ProbeVolumeBakingSet bakingSet)
            {
                return bakingSet.singleSceneMode ? "Single Scene" : "Baking Set";
            }

            internal static void SetBakingMode(ProbeVolumeBakingSet bakingSet, string mode)
            {
                bakingSet.singleSceneMode = (mode == "Single Scene");
            }

            internal static int GetSkyOcclusionBakingSamples(ProbeVolumeBakingSet bakingSet)
            {
                return bakingSet.skyOcclusionBakingSamples;
            }

            internal static void SetSkyOcclusionBakingSamples(ProbeVolumeBakingSet bakingSet, int samples)
            {
                bakingSet.skyOcclusionBakingSamples = samples;
            }

            internal static string GetVolumeMode(GameObject go)
            {
                if (!go.TryGetComponent<Volume>(out var volume))
                    return null;

                return volume.isGlobal ? "Global" : "Local";
            }

            internal static void SetVolumeMode(GameObject go, string mode)
            {
                if (!go.TryGetComponent<Volume>(out var volume))
                    return;

                volume.isGlobal = (mode == "Global");
            }

            internal static VolumeProfile GetVolumeProfile(GameObject go)
            {
                if (!go.TryGetComponent<Volume>(out var volume))
                    return null;

                return volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile;
            }

            internal static void SetVolumeProfile(GameObject go, VolumeProfile profile)
            {
                if (!go.TryGetComponent<Volume>(out var volume))
                    return;

                volume.sharedProfile = profile;
            }

            internal static LightType? GetLightShape(GameObject go)
            {
                if (!go.TryGetComponent<Light>(out var light))
                    return null;

                return light.type;
            }

            internal static void SetLightShape(GameObject go, LightType value)
            {
                if (!go.TryGetComponent<Light>(out var light))
                    return;

                if (IsLightShapeApplicable(value))
                {
                    light.type = value;
                }
            }
        }

        internal static bool IsLightShapeApplicable(LightType lightType)
        {
            return IsAreaLight(lightType);
        }

        static bool IsAreaLight(LightType lightType)
        {
            return lightType is LightType.Rectangle or LightType.Disc or LightType.Tube;
        }

        enum AreaLightShape
        {
            Rectangle = LightType.Rectangle,
            Disc = LightType.Disc,
            Tube = LightType.Tube
        }

        class LightShapeField : EnumField
        {
            LightType m_Value;

            public new LightType value
            {
                get => m_Value;
                set
                {
                    if (m_Value != value)
                    {
                        m_Value = value;
                        UpdateEnumField();
                    }
                }
            }

            public LightShapeField() : base(AreaLightShape.Rectangle)
            {
                m_Value = LightType.Rectangle;

                AddToClassList("core-lighting-search-light-shape");

                var styleSheet = LoadStyleSheet();
                if (styleSheet != null)
                    styleSheets.Add(styleSheet);
            }

            public void SetValueWithoutNotify(LightType newValue)
            {
                if (m_Value == newValue)
                    return;

                m_Value = newValue;
                UpdateEnumField();
            }

            void UpdateEnumField()
            {
                if (IsAreaLight(m_Value))
                {
                    Init((AreaLightShape)m_Value, false);
                }
            }
        }
    }
}
