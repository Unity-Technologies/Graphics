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

    static int CountSceneVFXRenderers()
    {
        int count = 0;
        foreach (var renderer in Resources.FindObjectsOfTypeAll<VFXRenderer>())
        {
            if (!EditorUtility.IsPersistent(renderer))
                count++;
        }
        return count;
    }

    public enum RemovedVisualEffectOverrideAction
    {
        ApplyRemovedComponent,
        ApplyAll,
        RevertRemovedComponent,
        RevertAll,
    }

    static void PerformOverrideAction(RemovedVisualEffectOverrideAction action, GameObject instance, Component assetVisualEffect)
    {
        switch (action)
        {
            case RemovedVisualEffectOverrideAction.ApplyRemovedComponent:
                PrefabUtility.ApplyRemovedComponent(instance, assetVisualEffect, InteractionMode.UserAction);
                break;
            case RemovedVisualEffectOverrideAction.ApplyAll:
                PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.UserAction);
                break;
            case RemovedVisualEffectOverrideAction.RevertRemovedComponent:
                PrefabUtility.RevertRemovedComponent(instance, assetVisualEffect, InteractionMode.UserAction);
                break;
            case RemovedVisualEffectOverrideAction.RevertAll:
                PrefabUtility.RevertPrefabInstance(instance, InteractionMode.UserAction);
                break;
        }
    }

    static void AssertVFXRendererMatchesVisualEffect(GameObject instance, int initialRendererCount)
    {
        int expectedRendererCount = instance.GetComponent<VisualEffect>() != null ? 1 : 0;
        Assert.That(instance.GetComponents<VFXRenderer>().Length, Is.EqualTo(expectedRendererCount));
        Assert.That(CountSceneVFXRenderers(), Is.EqualTo(initialRendererCount + expectedRendererCount));
    }

    [Test]
    public void Undo_RemovedVisualEffectOverrideOnPrefab_DoesNotLeakVFXRenderer([Values] RemovedVisualEffectOverrideAction action)
    {
        const string prefabPath = "Assets/UUM_155270.prefab";
        var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Packages/com.unity.visualeffectgraph/Editor/Templates/Simple_Loop.vfx");
        Assert.That(asset, Is.Not.Null);

        int initialRendererCount = CountSceneVFXRenderers();
        var instance = new GameObject("UUM_155270");
        try
        {
            instance.AddComponent<VisualEffect>().visualEffectAsset = asset;
            PrefabUtility.SaveAsPrefabAssetAndConnect(instance, prefabPath, InteractionMode.AutomatedAction);
            var assetVisualEffect = PrefabUtility.GetCorrespondingObjectFromSource(instance.GetComponent<VisualEffect>());

            Undo.IncrementCurrentGroup();
            Undo.DestroyObjectImmediate(instance.GetComponent<VisualEffect>()); // Implicitly removes the coupled VFXRenderer
            Undo.IncrementCurrentGroup();
            PerformOverrideAction(action, instance, assetVisualEffect);
            AssertVFXRendererMatchesVisualEffect(instance, initialRendererCount);

            Undo.PerformUndo();
            Undo.PerformUndo();

            Assert.That(instance.GetComponent<VisualEffect>(), Is.Not.Null);
            AssertVFXRendererMatchesVisualEffect(instance, initialRendererCount);

            UnityEngine.Object.DestroyImmediate(instance);
            Assert.That(CountSceneVFXRenderers(), Is.EqualTo(initialRendererCount));
        }
        finally
        {
            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance);
            AssetDatabase.DeleteAsset(prefabPath);
            Undo.ClearAll();
        }
    }

    static int CountRemovedComponentOverrides(GameObject instanceRoot, GameObject containingInstanceGameObject)
    {
        int count = 0;
        foreach (var removedComponent in PrefabUtility.GetRemovedComponents(instanceRoot))
        {
            if (removedComponent.containingInstanceGameObject == containingInstanceGameObject)
                count++;
        }
        return count;
    }

    [Test]
    public void RevertRemovedVisualEffect_UsingNestedPrefabSource_OnlyRevertsTargetInstance()
    {
        const string childPrefabPath = "Assets/UUM_155270_Child.prefab";
        const string parentPrefabPath = "Assets/UUM_155270_Parent.prefab";
        var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Packages/com.unity.visualeffectgraph/Editor/Templates/Simple_Loop.vfx");
        Assert.That(asset, Is.Not.Null);

        int initialRendererCount = CountSceneVFXRenderers();
        GameObject instance = null;
        try
        {
            var childContents = new GameObject("Child");
            childContents.AddComponent<VisualEffect>().visualEffectAsset = asset;
            var childPrefab = PrefabUtility.SaveAsPrefabAsset(childContents, childPrefabPath);
            UnityEngine.Object.DestroyImmediate(childContents);

            var parentContents = new GameObject("Parent");
            for (int i = 1; i <= 2; i++)
            {
                var child = (GameObject)PrefabUtility.InstantiatePrefab(childPrefab);
                child.name = "Child" + i;
                child.transform.SetParent(parentContents.transform);
            }
            var parentPrefab = PrefabUtility.SaveAsPrefabAsset(parentContents, parentPrefabPath);
            UnityEngine.Object.DestroyImmediate(parentContents);

            instance = (GameObject)PrefabUtility.InstantiatePrefab(parentPrefab);
            var child1 = instance.transform.Find("Child1").gameObject;
            var child2 = instance.transform.Find("Child2").gameObject;
            Undo.IncrementCurrentGroup();
            Undo.DestroyObjectImmediate(child1.GetComponent<VisualEffect>()); // Implicitly removes the coupled VFXRenderer
            Undo.DestroyObjectImmediate(child2.GetComponent<VisualEffect>());
            Assert.That(CountRemovedComponentOverrides(instance, child1), Is.EqualTo(2));
            Assert.That(CountRemovedComponentOverrides(instance, child2), Is.EqualTo(2));

            // The asset component comes from the child Prefab, which both Child1 and Child2 are instances of.
            Undo.IncrementCurrentGroup();
            PrefabUtility.RevertRemovedComponent(child1, childPrefab.GetComponent<VisualEffect>(), InteractionMode.UserAction);

            Assert.That(child1.GetComponent<VisualEffect>(), Is.Not.Null);
            Assert.That(child1.GetComponents<VFXRenderer>().Length, Is.EqualTo(1));
            Assert.That(CountRemovedComponentOverrides(instance, child1), Is.EqualTo(0));
            Assert.That(child2.GetComponent<VisualEffect>(), Is.Null);
            Assert.That(child2.GetComponent<VFXRenderer>(), Is.Null);
            Assert.That(CountRemovedComponentOverrides(instance, child2), Is.EqualTo(2));
            Assert.That(CountSceneVFXRenderers(), Is.EqualTo(initialRendererCount + 1));

            Undo.PerformUndo();

            Assert.That(child1.GetComponent<VisualEffect>(), Is.Null);
            Assert.That(child1.GetComponent<VFXRenderer>(), Is.Null);
            Assert.That(child2.GetComponent<VisualEffect>(), Is.Null);
            Assert.That(CountSceneVFXRenderers(), Is.EqualTo(initialRendererCount));
            Assert.That(CountRemovedComponentOverrides(instance, child1), Is.EqualTo(2));
            Assert.That(CountRemovedComponentOverrides(instance, child2), Is.EqualTo(2));
        }
        finally
        {
            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance);
            AssetDatabase.DeleteAsset(parentPrefabPath);
            AssetDatabase.DeleteAsset(childPrefabPath);
            Undo.ClearAll();
        }
    }
}
