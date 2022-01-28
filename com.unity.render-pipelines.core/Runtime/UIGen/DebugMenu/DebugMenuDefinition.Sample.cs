using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering.UIGen
{
    // Sample code
    [DeriveDebugMenu]
    public class SampleDebugMenuData
    {
        //need to have instance (interface instead of attribute? )
        public static SampleDebugMenuData instance { get; } = new SampleDebugMenuData();

        public SampleDebugDataSet1 dataSet1 = new SampleDebugDataSet1();
        public SampleDebugDataSet2 dataSet2 { get; } = new SampleDebugDataSet2();
    }

    public struct Label { }

    public class SampleDebugDataSet1
    {
        public static class UI
        {
            [DebugMenuProperty("Category A")]
            public static Label label;
        }

        public SampleDebugDataSet3 dataSet3 = new();

        [DebugMenuProperty("Category A")]
        public void Button() { }

        [Min(10)]
        [DebugMenuProperty("Category A")]
        public float dataSet1FloatValue;

        int m_DataSet1IntValue;
        [DebugMenuProperty("Category B")]
        public int dataSet1IntValue
        {
            get => m_DataSet1IntValue;
            set
            {
                m_DataSet1IntValue = value;
                Debug.Log(value);
            }
        }
    }

    public class SampleDebugDataSet2
    {
        [DebugMenuProperty("Category A")]
        public float dataSet2FloatValue;
        [DebugMenuProperty("Category C")]
        public int dataSet2IntValue { get; set; }
    }

    public class SampleDebugDataSet3
    {
        [DebugMenuProperty("Category B")]
        public float dataSet3FloatValue;
        [DebugMenuProperty("Category A")]
        public int dataSet3IntValue { get; set; }
    }
}
