using UnityEngine;

namespace UnityEditor.Experimental.Rendering
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<GlobalLightLoopSettingsUI, SerializedGlobalLightLoopSettings>;

    class GlobalLightLoopSettingsUI : BaseUI<SerializedGlobalLightLoopSettings>
    {
        static GlobalLightLoopSettingsUI()
        {
            Inspector = CED.Group(
                SectionCookies,
                CED.space,
                SectionReflection,
                CED.space,
                SectionSky
            );
        }

        public static readonly CED.IDrawer Inspector;

        public static readonly CED.IDrawer SectionCookies = CED.Action(Drawer_SectionCookies);
        public static readonly CED.IDrawer SectionReflection = CED.Action(Drawer_SectionReflection);
        public static readonly CED.IDrawer SectionSky = CED.Action(Drawer_SectionSky);

        public GlobalLightLoopSettingsUI()
            : base(0)
        {

        }

        static void Drawer_SectionCookies(GlobalLightLoopSettingsUI s, SerializedGlobalLightLoopSettings d, Editor o)
        {
            EditorGUILayout.LabelField(_.GetContent("Cookies"), EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(d.cookieTexArraySize, _.GetContent("Texture Array Size"));
            EditorGUILayout.PropertyField(d.cubeCookieTexArraySize, _.GetContent("Cubemap Array Size"));
            EditorGUILayout.PropertyField(d.pointCookieSize, _.GetContent("Point Cookie Size"));
            EditorGUILayout.PropertyField(d.spotCookieSize, _.GetContent("Spot Cookie Size"));
            --EditorGUI.indentLevel;
        }

        static void Drawer_SectionReflection(GlobalLightLoopSettingsUI s, SerializedGlobalLightLoopSettings d, Editor o)
        {
            EditorGUILayout.LabelField(_.GetContent("Reflection"), EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(d.reflectionCacheCompressed, _.GetContent("Compress Reflection Probe Cache"));
            EditorGUILayout.PropertyField(d.reflectionCubemapSize, _.GetContent("Reflection Cubemap Size"));
            EditorGUILayout.PropertyField(d.reflectionProbeCacheSize, _.GetContent("Probe Cache Size"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(d.planarReflectionCacheCompressed, _.GetContent("Compress Planar Reflection Probe Cache"));
            EditorGUILayout.PropertyField(d.planarReflectionCubemapSize, _.GetContent("Planar Reflection Texture Size"));
            EditorGUILayout.PropertyField(d.planarReflectionProbeCacheSize, _.GetContent("Planar Probe Cache Size"));
            EditorGUILayout.PropertyField(d.maxPlanarReflectionProbes, _.GetContent("Max Planar Probe Per Frame"));
            d.maxPlanarReflectionProbes.intValue = Mathf.Max(1, d.maxPlanarReflectionProbes.intValue);
            --EditorGUI.indentLevel;
        }

        static void Drawer_SectionSky(GlobalLightLoopSettingsUI s, SerializedGlobalLightLoopSettings d, Editor o)
        {
            EditorGUILayout.LabelField(_.GetContent("Sky"), EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(d.skyReflectionSize, _.GetContent("Sky Reflection Size"));
            EditorGUILayout.PropertyField(d.skyLightingOverrideLayerMask, _.GetContent("Sky Lighting Override Mask|This layer mask will define in which layers the sky system will look for sky settings volumes for lighting override"));
            --EditorGUI.indentLevel;
        }
    }
}
