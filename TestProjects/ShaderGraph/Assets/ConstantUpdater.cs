using UnityEngine;
using UnityEditor.ShaderGraph;

[ExecuteInEditMode]
class ConstantUpdater : MonoBehaviour
{
    ConstantComputer m_ConstantComputer;
    Shader m_ConstantComputerShader;

    private void OnPreRender()
    {
        var material = GetComponent<Renderer>().sharedMaterial;
        var shader = material != null ? material.shader : null;
        if (m_ConstantComputerShader != shader)
        {
            m_ConstantComputer = null;
            // TODO: rebuild constant computer...
            m_ConstantComputerShader = shader;
        }

        if (m_ConstantComputer == null)
            return;

        foreach (var (inputName, isFloat) in m_ConstantComputer.InputNames)
        {
            if (material.HasProperty(inputName))
            {
                if (!isFloat)
                {
                    m_ConstantComputer.SetInput(inputName, material.GetVector(inputName));
                }
                else
                {
                    float value;
                    if (inputName == "_SinTime.w")
                        value = Mathf.Sin(Time.time);
                    else if (inputName == "_CosTime.w")
                        value = Mathf.Cos(Time.time);
                    else if (inputName == "unity_DeltaTime.x")
                        value = Time.deltaTime;
                    else if (inputName == "unity_DeltaTime.z")
                        value = 1f / Time.deltaTime;
                    else if (inputName == "_Time.y")
                        value = Time.time;
                    else
                        value = material.GetFloat(inputName);
                    m_ConstantComputer.SetInput(inputName, value);
                }
            }
        }

        m_ConstantComputer.Execute();

        foreach (var (outputName, isFloat) in m_ConstantComputer.OutputNames)
        {
            if (isFloat)
                material.SetFloat(outputName, m_ConstantComputer.GetOutput(outputName).x);
            else
                material.SetVector(outputName, m_ConstantComputer.GetOutput(outputName));
        }
    }
}
