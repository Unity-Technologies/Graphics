using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class UpdateReflectionProbe : MonoBehaviour
{
    HDAdditionalReflectionData reflectionProbe;
    [SerializeField] int interval = 1;
    
    // Start is called before the first frame update
    void Start()
    {
        reflectionProbe = GetComponent<HDAdditionalReflectionData>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.frameCount % interval == 0) reflectionProbe.RequestRenderNextUpdate();        
    }
}
