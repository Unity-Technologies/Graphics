using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

class VisualEffectGraphEditorTests
{
    [Test]
    public void DummyTest()
    {
        Assert.IsTrue(true);
    }

    [Test]
    public void Undo_AddVisualEffectComponent_DoesNotCrash()
    {
        var gameObject = new GameObject("UUM_136309");
        try
        {
            for (int i = 0; i < 10; i++)
            {
                Undo.IncrementCurrentGroup();
                Undo.AddComponent<VisualEffect>(gameObject); // Implicitly adds the coupled VFXRenderer
                Undo.PerformUndo();
                Assert.That(gameObject.GetComponent<VisualEffect>(), Is.Null);
                Assert.That(gameObject.GetComponent<VFXRenderer>(), Is.Null);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }
}
