using UnityEngine;
using UnityEditor.ShaderGraph;

[ExecuteInEditMode]
class ConstantUpdater : MonoBehaviour
{
    [System.NonSerialized] ConstantComputer m_ConstantComputer;
    [System.NonSerialized] private Material m_Material;

    public Object ComputerAsset = null;

    private void OnWillRenderObject()
    {
        if (ComputerAsset == null)
            return;

        if (m_Material == null)
        {
            var path = UnityEditor.AssetDatabase.GetAssetPath(ComputerAsset);
            var jsonString = System.IO.File.ReadAllText(path);
            if (string.IsNullOrEmpty(jsonString))
                return;

            m_ConstantComputer = JsonUtility.FromJson<ConstantComputer>(jsonString);
            m_ConstantComputer.ResolveMethodNames();
            m_Material = GetComponent<Renderer>().sharedMaterial;
        }

        if (m_Material == null || m_ConstantComputer == null)
            return;

        foreach (var (inputName, isFloat) in m_ConstantComputer.InputNames)
        {
            if (!isFloat)
            {
                if (m_Material.HasProperty(inputName))
                    m_ConstantComputer.SetInput(inputName, m_Material.GetVector(inputName));
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
                    if (!m_Material.HasProperty(inputName))
                        continue;
                    value = m_Material.GetFloat(inputName);
                }
                m_ConstantComputer.SetInput(inputName, value);
            }
        }

        m_ConstantComputer.Execute();

        foreach (var (outputName, isFloat) in m_ConstantComputer.OutputNames)
        {
            if (isFloat)
                m_Material.SetFloat(outputName, m_ConstantComputer.GetOutput(outputName).x);
            else
                m_Material.SetVector(outputName, m_ConstantComputer.GetOutput(outputName));
        }
    }
}
