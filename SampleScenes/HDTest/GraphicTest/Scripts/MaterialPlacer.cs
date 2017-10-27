using UnityEngine;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class MaterialPlacer : MonoBehaviour
{
    public enum TargetType
    {
        Float,
        Color
    }

    [SerializeField]
    Renderer m_Prefab;

    [SerializeField]
    int m_Rows = 2;

    [SerializeField]
    int m_Cols = 2;

    [SerializeField]
    Vector3 m_Size = Vector3.one;

    [SerializeField]
    TargetType m_TargetType;

    [SerializeField]
    [FormerlySerializedAs("m_FloatName")]
    string m_PropertyName;

    [SerializeField]
    [FormerlySerializedAs("m_FromValue")]
    float m_FromValueFloat = 0;

    [SerializeField]
    [FormerlySerializedAs("m_ToValue")]
    float m_ToValueFloat = 1;

    [SerializeField]
    Color m_FromValueColor = Color.white;

    [SerializeField]
    Color m_ToValueColor = Color.white;

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

        var positionOffset = -m_Size;
        var positionStep = new Vector3(2f * m_Size.x / (m_Rows - 1f), 2f * m_Size.y / (m_Cols - 1f), 0);

        switch (m_TargetType)
        {
            case TargetType.Float:
            {
                var valueOffset = m_FromValueFloat;
                var valueStep = (m_ToValueFloat - m_FromValueFloat) * _1_count;

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
                        mat.SetFloat(m_PropertyName, value);
                    }
                }
                break;
            }
            case TargetType.Color:
            {
                var valueOffset = m_FromValueColor;
                var valueStep = (m_ToValueColor - m_FromValueColor) * _1_count;

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
                        mat.SetColor(m_PropertyName, value);
                    }
                }
                break;
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
        if (m_Prefab == null || string.IsNullOrEmpty(m_PropertyName))
            return 0;

        return m_Prefab.GetInstanceID()
            ^ m_Rows.GetHashCode()
            ^ m_Cols.GetHashCode()
            ^ m_Size.GetHashCode()
            ^ m_PropertyName.GetHashCode()
            ^ m_FromValueFloat.GetHashCode()
            ^ m_ToValueFloat.GetHashCode();
    }

    void OnValidate()
    {
        m_Rows = Mathf.Max(2, m_Rows);
        m_Cols = Mathf.Max(2, m_Cols);
        m_Size = Vector3.Max(Vector3.zero, m_Size);
    }
}
