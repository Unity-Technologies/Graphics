#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental.VFX;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using System.Linq;
using System.Collections;
using UnityEditor.VFX.Block.Test;
using System.Collections.Generic;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VisualEffectPrefabTest
    {
        GameObject m_mainCamera;
        List<string> m_assetToDelete = new List<string>();
        List<GameObject> m_gameObjectToDelete = new List<GameObject>();

        [OneTimeSetUp]
        public void Init()
        {
            System.IO.Directory.CreateDirectory("Assets/Temp");

            m_mainCamera = new GameObject();
            var camera = m_mainCamera.AddComponent<Camera>();
            camera.transform.localPosition = Vector3.one;
            camera.transform.LookAt(m_mainCamera.transform);
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            foreach (var gameObject in m_gameObjectToDelete)
            {
                try
                {
                    UnityEngine.Object.DestroyImmediate(gameObject);
                }
                catch (System.Exception)
                {
                }
            }

            foreach (var assetPath in m_assetToDelete)
            {
                try
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
                catch (System.Exception)
                {
                }
            }
        }

        static readonly string k_tempFileFormat = "Assets/TmpTests/vfx_prefab_{0}.{1}";
        static int m_TempFileCounter = 0;

        string MakeTempFilePath(string extension)
        {
            m_TempFileCounter++;
            var tempFilePath = string.Format(k_tempFileFormat, m_TempFileCounter, extension);
            m_assetToDelete.Add(tempFilePath);
            return tempFilePath;
        }

        VFXGraph MakeTemporaryGraph()
        {
            var tempFilePath = MakeTempFilePath("vfx");
            var asset = VisualEffectResource.CreateNewAsset(tempFilePath);
            var resource = asset.GetResource(); // force resource creation
            var graph = ScriptableObject.CreateInstance<VFXGraph>();
            graph.visualEffectResource = resource;

            return graph;
        }

        void MakeTemporaryPrebab(GameObject gameObject, out GameObject newGameObject, out GameObject prefabInstanceObject)
        {
            var path = MakeTempFilePath("prefab");
            newGameObject = PrefabUtility.SaveAsPrefabAsset(gameObject, path);
            m_gameObjectToDelete.Add(newGameObject);

            prefabInstanceObject = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
        }

        GameObject MakeTemporaryGameObject()
        {
            var mainObject = new GameObject();
            m_gameObjectToDelete.Add(mainObject);
            return mainObject;
        }

        static readonly bool k_HasFixed_DisabledState = false;
        static readonly bool k_HasFixed_PrefabOverride = false;

        /* Follow "Known issue : " <= This test has been added to cover a future fix in vfx behavior */
        [UnityTest]
        public IEnumerator Create_Prefab_Modify_And_Expect_No_Override()
        {
            var graph = MakeTemporaryGraph();
            var parametersVector3Desc = VFXLibrary.GetParameters().Where(o => o.model.type == typeof(Vector3)).First();

            var exposedName = "ghjkl";
            var parameter = parametersVector3Desc.CreateInstance();
            parameter.SetSettingValue("m_exposedName", exposedName);
            parameter.SetSettingValue("m_exposed", true);
            parameter.value = new Vector3(0, 0, 0);
            graph.AddChild(parameter);
            graph.RecompileIfNeeded();

            var mainObject = MakeTemporaryGameObject();

            var vfx = mainObject.AddComponent<VisualEffect>();
            vfx.visualEffectAsset = graph.visualEffectResource.asset;
            Assert.IsTrue(vfx.HasVector3(exposedName));
            vfx.SetVector3(exposedName, new Vector3(1, 2, 3));

            GameObject newGameObject, prefabInstanceObject;
            MakeTemporaryPrebab(mainObject, out newGameObject, out prefabInstanceObject);
            GameObject.DestroyImmediate(mainObject);

            var currentPrefabInstanceObject = PrefabUtility.InstantiatePrefab(prefabInstanceObject) as GameObject;
            yield return null;

            var vfxInPrefab = prefabInstanceObject.GetComponent<VisualEffect>();
            var expectedNewValue = new Vector3(4, 5, 6);
            if (k_HasFixed_DisabledState)
            {
                Assert.IsTrue(vfxInPrefab.HasVector3(exposedName));
                vfxInPrefab.SetVector3(exposedName, expectedNewValue);
            }
            else
            {
                /* modifying prefab using serialized property */
                var editor = Editor.CreateEditor(vfxInPrefab);
                editor.serializedObject.Update();

                var propertySheet = editor.serializedObject.FindProperty("m_PropertySheet");
                var fieldName = VisualEffectSerializationUtility.GetTypeField(VFXExpression.TypeToType(VFXValueType.Float3)) + ".m_Array";
                var vfxField = propertySheet.FindPropertyRelative(fieldName);

                Assert.AreEqual(1, vfxField.arraySize);

                var property = vfxField.GetArrayElementAtIndex(0);
                property = property.FindPropertyRelative("m_Value");

                property.vector3Value = expectedNewValue;

                editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                GameObject.DestroyImmediate(editor);
                EditorUtility.SetDirty(prefabInstanceObject);
                PrefabUtility.SavePrefabAsset(prefabInstanceObject);
            }
            //AssetDatabase.SaveAssets(); //Helps debug but not necessary

            yield return null;

            var currentVFXInstanciedFromPrefab = currentPrefabInstanceObject.GetComponent<VisualEffect>();
            Assert.IsTrue(currentVFXInstanciedFromPrefab.HasVector3(exposedName));
            var refExposedValue = currentVFXInstanciedFromPrefab.GetVector3(exposedName);

            var newInstanciedFromPrefab = PrefabUtility.InstantiatePrefab(prefabInstanceObject) as GameObject;
            var newVFXInstanciedFromPrefab = newInstanciedFromPrefab.GetComponent<VisualEffect>();
            Assert.IsTrue(newVFXInstanciedFromPrefab.HasVector3(exposedName));
            var newExposedValue = newVFXInstanciedFromPrefab.GetVector3(exposedName);

            var overrides = PrefabUtility.GetObjectOverrides(currentPrefabInstanceObject);

            Assert.AreEqual(newExposedValue.x, expectedNewValue.x); Assert.AreEqual(newExposedValue.y, expectedNewValue.y); Assert.AreEqual(newExposedValue.z, expectedNewValue.z); //< Expected to work
            if (k_HasFixed_PrefabOverride)
            {
                //Known issue : Failing due to fogbugz 1117103
                Assert.AreEqual(refExposedValue.x, expectedNewValue.x); Assert.AreEqual(refExposedValue.y, expectedNewValue.y); Assert.AreEqual(refExposedValue.z, expectedNewValue.z);
                Assert.IsEmpty(overrides);
            }
        }
    }
}
#endif
