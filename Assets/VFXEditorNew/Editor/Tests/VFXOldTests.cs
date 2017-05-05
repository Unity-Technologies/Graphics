using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEditor;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXOldTests
    {
        [Test]
        public void Simplest()
        {
            var computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/VFXShaders/VFXFillIndirectArgs.compute");
            VFXComponent.SetIndirectCompute(computeShader); //HACK : issue with VFXManager Settings...

            var scene = EditorSceneManager.OpenScene("Assets/OldVFXAsset/Test/Simplest.unity", OpenSceneMode.Single);
            var objects = scene.GetRootGameObjects();
            var cameraObject = objects.First(o => o.tag == "MainCamera");
            var camera = cameraObject.GetComponent<Camera>();

            var vfxComponent = objects.FirstOrDefault(o => o.GetComponent<VFXComponent>() != null);
            Assert.IsNotNull(vfxComponent);
        }
    }
}
