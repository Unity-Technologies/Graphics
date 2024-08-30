using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class InterlacedCascadesUpdate : MonoBehaviour
{
    [SerializeField]
    private HDAdditionalLightData hdLight;

    private int cascadeCounter;

    private void OnEnable()
    {
        hdLight.shadowUpdateMode = ShadowUpdateMode.OnDemand;
    }

    private void Update()
    {
        if (cascadeCounter < 4)
        {
            hdLight.transform.rotation =
                Quaternion.Euler(45f, 5 * cascadeCounter, 0f);
            hdLight.RequestSubShadowMapRendering(cascadeCounter);
            ++cascadeCounter;
        }
    }
}
