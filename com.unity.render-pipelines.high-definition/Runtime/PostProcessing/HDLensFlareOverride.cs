using UnityEngine;

namespace UnityEngine.Rendering.HighDefinition
{
    [ExecuteInEditMode]
    [AddComponentMenu("Rendering/HD Lens Flare Source Override")]
    public class HDLensFlareOverride : MonoBehaviour
    {
        public SRPLensFlareData LensFlareData;

        private void OnEnable()
        {
            if (LensFlareData)
                SRPLensFlareCommon.Instance.AddData(LensFlareData);
        }

        private void OnDisable()
        {
            if (LensFlareData)
                SRPLensFlareCommon.Instance.RemoveData(LensFlareData);
        }

        //public void Start()
        //{
        //    if (LensFlareData)
        //    {
        //        LensFlareData.WorldPosition = transform.position;
        //        SRPLensFlareCommon.Instance.AddData(LensFlareData);
        //    }
        //}

        public void Update()
        {
            if (LensFlareData)
            {
                LensFlareData.WorldPosition = transform.position;
            }
        }
    }
}
