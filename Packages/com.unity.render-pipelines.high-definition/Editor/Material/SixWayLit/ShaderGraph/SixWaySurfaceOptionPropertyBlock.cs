using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class SixWaySurfaceOptionPropertyBlock : SurfaceOptionPropertyBlock
    {
        HDSixWayData sixWayData;

        class Styles
        {
            public static GUIContent receiveShadows = new GUIContent("Receive Shadows", "Receive Shadows");
            public static GUIContent useColorAbsorption = new GUIContent("Use Color Absorption", "Use Color Absorption");
        }

        public SixWaySurfaceOptionPropertyBlock(HDSixWayData sixWayData) : base(Features.Unlit)
            => this.sixWayData = sixWayData;

        protected override void CreatePropertyGUI()
        {
            base.CreatePropertyGUI();
            AddProperty(Styles.receiveShadows, () => sixWayData.receiveShadows, (newValue) => sixWayData.receiveShadows = newValue);
            AddProperty(Styles.useColorAbsorption, () => sixWayData.useColorAbsorption, (newValue) => sixWayData.useColorAbsorption = newValue);
        }
    }
}
