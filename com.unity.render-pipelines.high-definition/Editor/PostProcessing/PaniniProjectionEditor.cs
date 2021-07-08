using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeComponentEditor(typeof(PaniniProjection))]
    sealed class PaniniProjectionEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Distance;
        SerializedDataParameter m_CropToFit;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<PaniniProjection>(serializedObject);

            m_Distance = Unpack(o.Find(x => x.distance));
            m_CropToFit = Unpack(o.Find(x => x.cropToFit));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Distance);
            using (new IndentLevelScope())
                PropertyField(m_CropToFit);
        }
    }
}
