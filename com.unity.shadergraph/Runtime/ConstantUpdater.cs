using UnityEngine;
using UnityEngine.ShaderGraph;

[ExecuteInEditMode]
public class ConstantUpdater : MonoBehaviour
{
    public ConstantComputer ConstantComputer = null;

    private void OnWillRenderObject()
    {
        if (ConstantComputer == null)
            return;

        var renderer = GetComponent<Renderer>();
        if (renderer == null)
            return;

        var material = renderer.sharedMaterial;
        if (material == null)
            return;

        foreach (var (inputName, isFloat) in ConstantComputer.InputNames)
        {
            if (!isFloat)
            {
                if (material.HasProperty(inputName))
                    ConstantComputer.SetInput(inputName, material.GetVector(inputName));
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
                {
                    if (!material.HasProperty(inputName))
                        continue;
                    value = material.GetFloat(inputName);
                }
                ConstantComputer.SetInput(inputName, value);
            }
        }

        ConstantComputer.Execute();

        foreach (var (outputName, isFloat) in ConstantComputer.OutputNames)
        {
            if (isFloat)
                material.SetFloat(outputName, ConstantComputer.GetOutput(outputName).x);
            else
                material.SetVector(outputName, ConstantComputer.GetOutput(outputName));
        }
    }
}
