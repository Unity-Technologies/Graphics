using UnityEngine;

[ExecuteInEditMode]
public class MaterialPlacer : MonoBehaviour
{
    [SerializeField]
    Renderer m_Prefab;

    [SerializeField]
    int m_Rows = 2;

    [SerializeField]
    int m_Cols = 2;

    [SerializeField]
    Vector3 m_Size = Vector3.one;

    [SerializeField]
    string m_FloatName;

    [SerializeField]
    float m_FromValue = 0;

    [SerializeField]
    float m_ToValue = 1;

    int m_LastHash = 0;

    void Update()
    {
        int hash = CalculateParameterHash();
        if (hash != m_LastHash)
        {
            m_LastHash = hash;
            Regenerate();
        }
    }

    [ContextMenu("Regenerate")]
    void Regenerate()
    {
        DestroyAll();
        if (m_LastHash != 0)
            Generate();
    }

    void Generate()
    {
        var tr = transform;
        var count = (float)(m_Cols * m_Rows - 1);
        var _1_count = 1f / count;
        var valueOffset = m_FromValue;
        var valueStep = (m_ToValue - m_FromValue) * _1_count;

        var positionOffset = -m_Size;
        var positionStep = new Vector3(2f * m_Size.x / (m_Rows - 1f), 2f * m_Size.y / (m_Cols - 1f), 0);

        for (var j = 0; j < m_Cols; j++)
        {
            for (var i = 0; i < m_Rows; i++)
            {
                var instance = Instantiate(m_Prefab);
                var itr = instance.transform;
                itr.SetParent(tr, false);
                itr.localPosition = Vector3.Scale(positionStep, new Vector3(i, j, 0)) + positionOffset;
                var mat = Instantiate(instance.sharedMaterial);
                instance.material = mat;
                var value = valueOffset + valueStep * (i + j * m_Rows);
                instance.name = string.Format("{0} {1}", m_Prefab.name, value.ToString("F2"));
                mat.SetFloat(m_FloatName, value);
            }
        }
    }

    void DestroyAll()
    {
        var tr = transform;
        var childCount = tr.childCount;
        for (var i = childCount - 1; i >= 0; --i)
        {
            var child = tr.GetChild(i);
            var renderer = child.GetComponent<Renderer>();

            if (renderer != null)
            {
                var mat = renderer.sharedMaterial;
                DestroyImmediate(mat, false);
            }

            DestroyImmediate(child.gameObject, false);
        }
    }

    int CalculateParameterHash()
    {
        if (m_Prefab == null || string.IsNullOrEmpty(m_FloatName))
            return 0;

        return m_Prefab.GetInstanceID()
            ^ m_Rows.GetHashCode()
            ^ m_Cols.GetHashCode()
            ^ m_Size.GetHashCode()
            ^ m_FloatName.GetHashCode()
            ^ m_FromValue.GetHashCode()
            ^ m_ToValue.GetHashCode();
    }

    void OnValidate()
    {
        m_Rows = Mathf.Max(2, m_Rows);
        m_Cols = Mathf.Max(2, m_Cols);
        m_Size = Vector3.Max(Vector3.zero, m_Size);
    }
}
