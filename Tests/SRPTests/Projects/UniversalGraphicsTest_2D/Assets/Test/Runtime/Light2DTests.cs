using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[TestFixture]
class Light2DTests 
{
    GameObject m_BaseObj;
    Light2D m_Light;

    [SetUp]
    public void Setup()
    {
        m_BaseObj = new GameObject();
        m_Light = m_BaseObj.AddComponent<Light2D>();
    }

    [TearDown]
    public void Cleanup()
    {
        Object.DestroyImmediate(m_BaseObj);
    }

    [Test]
    public void TargetSortingLayer_Getter()
    {
        Assert.IsNotNull(m_Light.targetSortingLayers);
        Assert.IsNotEmpty(m_Light.targetSortingLayers);
    }

    [Test]
    public void TargetSortingLayer_Setter()
    {
        // Empty
        m_Light.targetSortingLayers = new int[] { };
        Assert.IsEmpty(m_Light.targetSortingLayers);

        // Add default layer
        m_Light.targetSortingLayers = new int[] { SortingLayer.NameToID("Default") };
        Assert.IsTrue(m_Light.targetSortingLayers.Length == 1);
    }

    [Test]
    public void TargetSortingLayer_IsValid()
    {
        foreach (var layer in m_Light.targetSortingLayers)
            Assert.IsTrue(SortingLayer.IsValid(layer));
    }

    [Test]
    public void TargetSortingLayer_AddValidLayer()
    {
        var layers = m_Light.targetSortingLayers;

        m_Light.targetSortingLayers = new int[] { };

        // Add back layers
        foreach (var layer in layers)
            Assert.IsTrue(m_Light.AddTargetSortingLayer(layer));

        Assert.IsTrue(m_Light.targetSortingLayers.Length == layers.Length);
    }

    [Test]
    public void TargetSortingLayer_AddInvalidLayer()
    {
        // Add an invalid layer returns false
        Assert.IsFalse(m_Light.AddTargetSortingLayer("Invalid"));
        // Add random layerID
        Assert.IsFalse(m_Light.AddTargetSortingLayer(234393945));

    }

    [Test]
    public void TargetSortingLayer_RemoveValidLayer()
    {
        var layers = m_Light.targetSortingLayers;

        // Remove layers
        foreach (var layer in layers)
            Assert.IsTrue(m_Light.RemoveTargetSortingLayer(layer));

        Assert.IsEmpty(m_Light.targetSortingLayers);
    }

    [Test]
    public void TargetSortingLayer_RemoveInvalidLayer()
    {
        // Remove an invalid layer returns false
        Assert.IsFalse(m_Light.RemoveTargetSortingLayer("Invalid"));
        // Remove random layerID
        Assert.IsFalse(m_Light.AddTargetSortingLayer(234393945));
    }
}
