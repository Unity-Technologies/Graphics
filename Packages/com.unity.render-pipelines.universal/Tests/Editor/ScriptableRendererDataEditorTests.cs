using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class ScriptableRendererDataEditorTests
{
    const string k_TempAssetPath = "Assets/ScriptableRendererDataEditorTests";

    UniversalRendererData m_RendererData;
    string m_AssetPath;
    Editor m_Editor;

    [SetUp]
    public void Setup()
    {
        m_RendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
        CoreUtils.EnsureFolderTreeInAssetFilePath($"{k_TempAssetPath}/test.asset");
        m_AssetPath = $"{k_TempAssetPath}/RendererData_{System.Guid.NewGuid():N}.asset";
        AssetDatabase.CreateAsset(m_RendererData, m_AssetPath);
        AssetDatabase.SaveAssets();

        // Clear undo stack to ensure clean state
        Undo.ClearAll();
    }

    [TearDown]
    public void TearDown()
    {
        // Ensure editor is destroyed before clearing undo or deleting assets
        if (m_Editor != null)
        {
            Object.DestroyImmediate(m_Editor);
            m_Editor = null;
        }

        Undo.ClearAll();

        if (m_RendererData != null)
        {
            AssetDatabase.DeleteAsset(m_AssetPath);
            m_RendererData = null;
        }

        if (AssetDatabase.IsValidFolder(k_TempAssetPath))
            AssetDatabase.DeleteAsset(k_TempAssetPath);
    }

    ScriptableRendererDataEditor CreateEditor()
    {
        m_Editor = Editor.CreateEditor(m_RendererData);
        return (ScriptableRendererDataEditor)m_Editor;
    }

    [Test]
    public void AddComponent_AddsRendererFeature()
    {
        var editor = CreateEditor();

        Assert.AreEqual(0, m_RendererData.rendererFeatures.Count, "Renderer should start with no features");

        editor.AddComponent(typeof(RenderObjects));

        Assert.AreEqual(1, m_RendererData.rendererFeatures.Count, "Renderer should have one feature after adding");
        Assert.IsInstanceOf<RenderObjects>(m_RendererData.rendererFeatures[0], "Added feature should be RenderObjects");
    }

    [Test]
    public void AddComponent_SetsHideFlags()
    {
        var editor = CreateEditor();

        editor.AddComponent(typeof(RenderObjects));

        var feature = m_RendererData.rendererFeatures[0];
        Assert.IsTrue((feature.hideFlags & HideFlags.HideInHierarchy) != 0,
            "Added feature should have HideInHierarchy flag set");
    }

    [Test]
    public void AddComponent_StoresFeatureInSameAsset()
    {
        var editor = CreateEditor();

        // Verify target is persistent (required for sub-asset creation)
        Assert.IsTrue(EditorUtility.IsPersistent(m_RendererData), "RendererData should be persistent");

        editor.AddComponent(typeof(RenderObjects));
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(m_AssetPath);

        // Reload the asset to get fresh references
        m_RendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(m_AssetPath);
        var feature = m_RendererData.rendererFeatures[0];

        // Feature should be stored at the same asset path as the renderer data
        string featurePath = AssetDatabase.GetAssetPath(feature);
        Assert.AreEqual(m_AssetPath, featurePath, "Feature asset path should match renderer data path");

        // Feature should not be the main asset (it's embedded in the renderer data)
        Assert.IsFalse(AssetDatabase.IsMainAsset(feature), "Feature should not be the main asset");
    }

    [Test]
    public void AddComponent_CanBeUndone()
    {
        var editor = CreateEditor();

        Assert.AreEqual(0, m_RendererData.rendererFeatures.Count);

        editor.AddComponent(typeof(RenderObjects));
        Assert.AreEqual(1, m_RendererData.rendererFeatures.Count);

        Undo.PerformUndo();

        // Reload asset to get fresh state
        m_RendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(m_AssetPath);
        Assert.AreEqual(0, m_RendererData.rendererFeatures.Count, "Feature count should be 0 after undo");
    }

    [Test]
    public void AddComponent_CanBeRedone()
    {
        var editor = CreateEditor();

        editor.AddComponent(typeof(RenderObjects));
        Assert.AreEqual(1, m_RendererData.rendererFeatures.Count);

        Undo.PerformUndo();
        m_RendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(m_AssetPath);
        Assert.AreEqual(0, m_RendererData.rendererFeatures.Count);

        Undo.PerformRedo();
        m_RendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(m_AssetPath);
        Assert.AreEqual(1, m_RendererData.rendererFeatures.Count, "Feature count should be 1 after redo");
    }

    [Test]
    public void AddMultipleComponents_MaintainsOrder()
    {
        var editor = CreateEditor();

        editor.AddComponent(typeof(RenderObjects));
        editor.AddComponent(typeof(FullScreenPassRendererFeature));

        Assert.AreEqual(2, m_RendererData.rendererFeatures.Count);
        Assert.IsInstanceOf<RenderObjects>(m_RendererData.rendererFeatures[0]);
        Assert.IsInstanceOf<FullScreenPassRendererFeature>(m_RendererData.rendererFeatures[1]);
    }

    [Test]
    public void NewFeatures_HaveHideInHierarchyFlag()
    {
        var editor = CreateEditor();

        editor.AddComponent(typeof(RenderObjects));
        editor.AddComponent(typeof(FullScreenPassRendererFeature));

        foreach (var feature in m_RendererData.rendererFeatures)
        {
            Assert.IsTrue((feature.hideFlags & HideFlags.HideInHierarchy) != 0,
                $"Feature {feature.name} should have HideInHierarchy flag");
        }
    }

    [Test]
    public void RendererFeatureMapCount_MatchesFeatureCount()
    {
        var editor = CreateEditor();

        editor.AddComponent(typeof(RenderObjects));
        editor.AddComponent(typeof(FullScreenPassRendererFeature));

        Assert.AreEqual(m_RendererData.m_RendererFeatures.Count, m_RendererData.m_RendererFeatureMap.Count,
            "Feature map count should match features count");
    }
}
