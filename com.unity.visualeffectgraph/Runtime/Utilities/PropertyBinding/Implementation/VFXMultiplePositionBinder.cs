using System.Collections.Generic;
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/Multiple Position Binder")]
    [VFXBinder("Point Cache/Multiple Position Binder")]
    class VFXMultiplePositionBinder : VFXBinderBase
    {
        [VFXPropertyBinding("UnityEngine.Texture2D"), UnityEngine.Serialization.FormerlySerializedAs("PositionMapParameter")]
        public ExposedProperty PositionMapProperty = "PositionMap";
        [VFXPropertyBinding("System.Int32"), UnityEngine.Serialization.FormerlySerializedAs("PositionCountParameter")]
        public ExposedProperty PositionCountProperty = "PositionCount";

        public GameObject[] Targets = null;
        public bool EveryFrame = false;

        private Texture2D positionMap;
        private int count = 0;

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateTexture();
        }

        public override bool IsValid(VisualEffect component)
        {
            return Targets != null &&
                component.HasTexture(PositionMapProperty) &&
                component.HasInt(PositionCountProperty);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            if (EveryFrame || Application.isEditor)
                UpdateTexture();

            component.SetTexture(PositionMapProperty, positionMap);
            component.SetInt(PositionCountProperty, count);
        }

        void UpdateTexture()
        {
            if (Targets == null || Targets.Length == 0)
                return;

            var candidates = new List<Vector3>();

            foreach (var obj in Targets)
            {
                if(obj != null)
                    candidates.Add(obj.transform.position);
            }

            count = candidates.Count;

            if (positionMap == null || positionMap.width != count)
            {
                positionMap = new Texture2D(count, 1, TextureFormat.RGBAFloat, false);
            }

            List<Color> colors = new List<Color>();
            foreach (var pos in candidates)
            {
                colors.Add(new Color(pos.x, pos.y, pos.z));
            }
            positionMap.name = gameObject.name + "_PositionMap";
            positionMap.filterMode = FilterMode.Point;
            positionMap.wrapMode = TextureWrapMode.Repeat;
            positionMap.SetPixels(colors.ToArray(), 0);
            positionMap.Apply();
        }

        public override string ToString()
        {
            return string.Format("Multiple Position Binder ({0} positions)", count);
        }
    }
}

