
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

        public void OnEnable()
        {
            if (LensFlareData)
                SRPLensFlareCommon.Instance.AddData(this);
            else
                SRPLensFlareCommon.Instance.RemoveData(this);
        }

        public void OnDisable()
        {
            if (LensFlareData)
                SRPLensFlareCommon.Instance.RemoveData(this);
        }

        public void OnDestroy()
        {
            if (LensFlareData)
                SRPLensFlareCommon.Instance.RemoveData(this);
        }

        public void Start()
        {
            if (LensFlareData != null)
            {
                LensFlareData.worldPosition = transform.position;
                SRPLensFlareCommon.Instance.AddData(this);
            }
            else
            {
                SRPLensFlareCommon.Instance.RemoveData(this);
            }
        }

        public void OnValidate()
        {
            if (LensFlareData != null)
            {
                LensFlareData.worldPosition = transform.position;
                SRPLensFlareCommon.Instance.AddData(this);
            }
            else
            {
                SRPLensFlareCommon.Instance.RemoveData(this);
            }
        }

        public void Update()
        {
            if (LensFlareData != null)
            {
                LensFlareData.worldPosition = transform.position;
            }
            else
            {
                SRPLensFlareCommon.Instance.RemoveData(this);
            }
        }
    }
}
