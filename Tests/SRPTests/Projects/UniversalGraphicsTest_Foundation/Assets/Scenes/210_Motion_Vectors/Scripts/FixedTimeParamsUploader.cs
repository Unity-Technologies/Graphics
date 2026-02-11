using UnityEngine;

public class FixedTimeParamsUploader : MonoBehaviour
{
    public float desiredTime;

    private MeshRenderer[] m_MeshRenderers;

    private static readonly int s_TimeParametersId = Shader.PropertyToID("_TimeParameters");
    private static readonly int s_LastTimeParametersId = Shader.PropertyToID("_LastTimeParameters");

    void Start()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        m_MeshRenderers = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    void Update()
    {
        if (m_MeshRenderers != null)
        {
            // This overwrites the time parameters for the test materials (this is the only way I found to consistently
            // test time based SG animations as there's no way to guarantee a fixed time and deltaTime in unity)
            //
            // If changing, make sure the below calculations stay consistent with those in ScriptableRenderer.cs
            const float fixedDeltaTime = 1.0f / 30.0f;
            float lastTime = desiredTime - fixedDeltaTime;
            Vector4 timeParametersVector = new Vector4(desiredTime, Mathf.Sin(desiredTime), Mathf.Cos(desiredTime), 0.0f);
            Vector4 lastTimeParametersVector = new Vector4(lastTime, Mathf.Sin(lastTime), Mathf.Cos(lastTime), 0.0f);

            foreach (var meshRenderer in m_MeshRenderers)
            {
                meshRenderer.material.SetVector(s_TimeParametersId, timeParametersVector);
                meshRenderer.material.SetVector(s_LastTimeParametersId, lastTimeParametersVector);
            }
        }
    }
}
