using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;

[TestFixture]
public class ScriptableCullingTests
{
    SceneSetup[] m_CurrentLoadedScenes;

    [OneTimeSetUp()]
    public void Setup()
    {
        BackupSceneManagerSetup();
        EditorSceneManager.OpenScene("Assets/ScriptableRenderLoop/Tests/GraphicsTests/Core/Scenes/ScriptableCulling.unity");
    }

    [TearDown()]
    public void TearDown()
    {
        RestoreSceneManagerSetup();
    }

    public void BackupSceneManagerSetup()
    {
        m_CurrentLoadedScenes = EditorSceneManager.GetSceneManagerSetup();
    }

    public void RestoreSceneManagerSetup()
    {
        if ((m_CurrentLoadedScenes == null) || (m_CurrentLoadedScenes.Length == 0))
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }
        else
        {
            EditorSceneManager.RestoreSceneManagerSetup(m_CurrentLoadedScenes);
        }
    }

    [UnityTest()]
    public IEnumerator Toto()
    {
        yield return null;


    }
}
