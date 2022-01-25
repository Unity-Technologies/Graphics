using System;
using UnityEngine;

public class Datas : MonoBehaviour
{
    enum EnumTest { A, B, C }

    [SerializeField] float m_Progress = 66f;

    [SerializeField] [Range(-6f, 666f)] float m_Slider = 33f;

    [SerializeField] EnumTest m_Enum = EnumTest.C;

    [SerializeField] bool m_Toggle = true;

    [SerializeField] string m_Text = "blabla";

    [SerializeField] int m_Int = 33;

    [SerializeField] float m_Float = 33.33f;

    [SerializeField] long m_Long = 33L;

    [SerializeField] double m_Double = 33.33;

    [SerializeField] Hash128 m_Hash = new Hash128(0ul, 33ul);

    [SerializeField] Vector2 m_Vec2 = new Vector2(3.3f, 3.3f);

    [SerializeField] Vector2Int m_Vec2Int = new Vector2Int(3, 3);

    [SerializeField] Vector3 m_Vec3 = new Vector3(3.3f, 3.3f, 3.3f);

    [SerializeField] Vector3Int m_Vec3Int = new Vector3Int(3, 3, 3);

    [SerializeField] Vector4 m_Vec4 = new Vector4(3.3f, 3.3f, 3.3f, 3.3f);

    [SerializeField] Bounds m_Bounds = new Bounds(new Vector3(3.3f, 3.3f, 3.3f), new Vector3(3.3f, 3.3f, 3.3f));

    [SerializeField] BoundsInt m_BoundsInt = new BoundsInt(new Vector3Int(3, 3, 3), new Vector3Int(3, 3, 3));

    void TestButtonMethodInBindingPath()
    {
        Debug.Log("Clicked on button linked with UXML binding path to this method");
    }


    /* ##########################################################################################
     * Below is how it can be designed for user implementation
     * ########################################################################################## */

    [ButtonUI("ButtonName", "Tooltip")]
    void SecondButtonclick()
    {
        Debug.Log("Clicked on second button linked with attribute!!");
    }

    [SerializeField] int m_Integer = 33;
    [FieldUI("ButtonName", "Tooltip")]
    int integer
    {
        get => m_Integer;
        set
        {
            m_Integer = value;
            /* add any aditional change here */
        }
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class FieldUIAttribute : Attribute
{
    public readonly string name;
    public readonly string tooltip;
    public FieldUIAttribute(string name, string tooltip)
    {
        this.name = name;
        this.tooltip = tooltip;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class ButtonUIAttribute : Attribute
{
    public readonly string name;
    public readonly string tooltip;
    public ButtonUIAttribute(string name, string tooltip)
    {
        this.name = name;
        this.tooltip = tooltip;
    }
}
