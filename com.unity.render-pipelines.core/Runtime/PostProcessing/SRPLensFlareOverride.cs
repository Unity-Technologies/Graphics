
namespace UnityEngine
{
    [ExecuteInEditMode]
    [AddComponentMenu("Rendering/SRP Lens Flare Source Override")]
    public sealed class SRPLensFlareOverride : MonoBehaviour
    {
        public SRPLensFlareData lensFlareData = null;
        public bool attenuationByLight = true;

        public SRPLensFlareOverride()
        {
        }

        public void OnEnable()
        {
            if (lensFlareData && gameObject.active)
                SRPLensFlareCommon.Instance.AddData(this);
            else
                SRPLensFlareCommon.Instance.RemoveData(this);
        }

        public void OnDisable()
        {
            if (lensFlareData)
                SRPLensFlareCommon.Instance.RemoveData(this);
        }

        public void OnDestroy()
        {
            if (lensFlareData)
                SRPLensFlareCommon.Instance.RemoveData(this);
        }

        public void Start()
        {
            if (lensFlareData != null && gameObject.active)
            {
                SRPLensFlareCommon.Instance.AddData(this);
            }
            else
            {
                SRPLensFlareCommon.Instance.RemoveData(this);
            }
        }

        public void OnValidate()
        {
            if (lensFlareData != null)
            {
                SRPLensFlareCommon.Instance.AddData(this);
            }
            else
            {
                SRPLensFlareCommon.Instance.RemoveData(this);
            }
        }

        public void Update()
        {
            if (lensFlareData == null)
            {
                SRPLensFlareCommon.Instance.RemoveData(this);
            }
        }
    }
}
