using System;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using SCMCategory = System.ComponentModel.CategoryAttribute;

[TestFixture]
public class RenderPipelineGlobalSettingsEditorTests
{
    #region Test Data Structure
    class TestContainer : ScriptableObject
    {
        public RenderPipelineGraphicsSettingsContainer graphicsSettingsContainer;
    }

    [Serializable]
    public class Base : IRenderPipelineGraphicsSettings
    {
        int IRenderPipelineGraphicsSettings.version => 0;
        public string name;
        public Base() => name = GetType().Name;
        public override string ToString() => name;
    }

    [Serializable]
    class B : Base { }

    [SCMCategory("Cat 1")] [Serializable] class A : Base { }
    [SCMCategory("Cat 1")] [Serializable] class D : Base { }


    [SCMCategory("Cat 2")] [Serializable] class C : Base { }
    [SCMCategory("Cat 2")] [Serializable] class E : Base { }
    [SCMCategory("Cat 2")] [Serializable] class F : Base { }

    [Serializable] class G : Base { }
    [Serializable] class H : Base { }

    #endregion

    SerializedProperty m_SerializedProperty;
    TestContainer m_Container;

    [OneTimeSetUp]
    public void Setup()
    {
        m_Container = ScriptableObject.CreateInstance<TestContainer>();
        m_Container.graphicsSettingsContainer = new();
        var so = new SerializedObject(m_Container);
        m_SerializedProperty = so.FindProperty("graphicsSettingsContainer.m_SettingsList");
    }

    [SetUp]
    public void EachTestSetUp()
    {
        //nuke previous data
        m_Container.graphicsSettingsContainer = new();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        m_SerializedProperty = null;
        UnityEngine.Object.DestroyImmediate(m_Container);
    }

    static TestCaseData[] s_TestCaseDatas =
    {
        new TestCaseData(new List<Base>())
            .SetName("Given an empty list the sort returns nothing.")
            .Returns(new List<(string, string, string)>()),
        new TestCaseData(new List<Base>() { new B() })
            .SetName("Given an list with one class, the sort returns just that class without the category.")
            .Returns(ToList(new Dictionary<string, Dictionary<string, string>>()
            {
                { "B", new() { { "B", "graphicsSettingsContainer.m_SettingsList.Array.data[0]" } } }
            })),
        new TestCaseData(new List<Base>() { new H(), new G(), new B() })
            .SetName("Given an list with classes without category, the sort returns the classes sorted with type.")
            .Returns(ToList(new Dictionary<string, Dictionary<string, string>>()
            {
                { "B", new() {
                    { "B", "graphicsSettingsContainer.m_SettingsList.Array.data[2]" },
                } },
                { "G", new() {
                    { "G", "graphicsSettingsContainer.m_SettingsList.Array.data[1]" },
                } },
                { "H", new() {
                    { "H", "graphicsSettingsContainer.m_SettingsList.Array.data[0]" },
                } },
            })),
        new TestCaseData(new List<Base>() { new E(), new B(), new C(), new A(), new D() })
            .SetName("Given an list with classes without category and classes with, the sort returns the classes sorted with type and inside each category also by type.")
            .Returns(ToList(new Dictionary<string, Dictionary<string, string>>()
            {
                { "B", new() {
                    { "B", "graphicsSettingsContainer.m_SettingsList.Array.data[1]" },
                } },
                { "Cat 1", new() {
                    { "A", "graphicsSettingsContainer.m_SettingsList.Array.data[3]" },
                    { "D", "graphicsSettingsContainer.m_SettingsList.Array.data[4]" },
                } },
                { "Cat 2", new() {
                    { "C", "graphicsSettingsContainer.m_SettingsList.Array.data[2]" },
                    { "E", "graphicsSettingsContainer.m_SettingsList.Array.data[0]" },
                } },
            })),
        new TestCaseData(new List<Base>() { new H(), new E(), new C() })
            .SetName("Given an list with classes without category and classes with, the sort returns the classes sorted with category and then by type.")
            .Returns(ToList(new Dictionary<string, Dictionary<string, string>>()
            {
                { "Cat 2", new() {
                    { "C", "graphicsSettingsContainer.m_SettingsList.Array.data[2]" },
                    { "E", "graphicsSettingsContainer.m_SettingsList.Array.data[1]" },
                } },
                { "H", new() {
                    { "H", "graphicsSettingsContainer.m_SettingsList.Array.data[0]" },
                } },
            })),
        new TestCaseData(new List<Base>() { new B(), new A(), new D(), new C(), new E(), new F(), new G(), new H() })
            .SetName("Given an sorted list, the sort returns the classes sorted correctly")
            .Returns(ToList(new Dictionary<string, Dictionary<string, string>>()
            {
                { "B", new() {
                    { "B", "graphicsSettingsContainer.m_SettingsList.Array.data[0]" },
                } },
                { "Cat 1", new() {
                    { "A", "graphicsSettingsContainer.m_SettingsList.Array.data[1]" },
                    { "D", "graphicsSettingsContainer.m_SettingsList.Array.data[2]" },
                } },
                { "Cat 2", new() {
                    { "C", "graphicsSettingsContainer.m_SettingsList.Array.data[3]" },
                    { "E", "graphicsSettingsContainer.m_SettingsList.Array.data[4]" },
                    { "F", "graphicsSettingsContainer.m_SettingsList.Array.data[5]" },
                } },
                { "G", new() {
                    { "G", "graphicsSettingsContainer.m_SettingsList.Array.data[6]" },
                } },
                { "H", new() {
                    { "H", "graphicsSettingsContainer.m_SettingsList.Array.data[7]" },
                } },
            })),
    };

    public static List<(string, string, string)> ToList(Dictionary<string, Dictionary<string, string>> dictionary)
    {
        var result = new List<(string, string, string)>();
        foreach (var kvp in dictionary)
            foreach (var kvp2 in kvp.Value)
                result.Add((kvp.Key, kvp2.Key, kvp2.Value));
        return result;
    }

    [Test, TestCaseSource(nameof(s_TestCaseDatas))]
    public List<(string, string, string)> CategorizeGivesTheCorrectResult(List<Base> renderPipelineGraphicsSettings)
    {
        // Setup test data
        foreach (var elt in renderPipelineGraphicsSettings)
            m_Container.graphicsSettingsContainer.settingsList.Add(elt);
        m_SerializedProperty.serializedObject.Update();

        // Act
        var res = RenderPipelineGraphicsSettingsContainerPropertyDrawer.Categorize(m_SerializedProperty);

        // Transform it in a comparable collection without Linq because this is forbidden now
        var actualResult = new List<(string, string, string)>();
        foreach (var kvp in res)
            foreach (var kvp2 in kvp.Value)
                actualResult.Add((kvp.Key, kvp2.Key, kvp2.Value.bindingPath));

        return actualResult;
    }
}
