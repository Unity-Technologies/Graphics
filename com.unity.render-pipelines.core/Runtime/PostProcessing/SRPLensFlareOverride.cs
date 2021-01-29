
namespace UnityEngine
{
    [ExecuteInEditMode]
    [AddComponentMenu("Rendering/SRP Lens Flare Source Override")]
    public sealed class SRPLensFlareOverride : MonoBehaviour
    {
        public SRPLensFlareData LensFlareData;

        public SRPLensFlareOverride()
        {
        }

        private void OnEnable()
        {
            if (LensFlareData)
                SRPLensFlareCommon.Instance.AddData(this);
        }

        private void OnDisable()
        {
            if (LensFlareData)
                SRPLensFlareCommon.Instance.RemoveData(this);
        }

        public void Start()
        {
            if (LensFlareData)
            {
                LensFlareData.WorldPosition = transform.position;
                SRPLensFlareCommon.Instance.AddData(this);
            }
        }

        public void Update()
        {
            if (LensFlareData)
            {
                LensFlareData.WorldPosition = transform.position;
            }
        }
    }
}
