#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using System.Linq;
using System.Collections;
using UnityEditor.VFX.Block.Test;
using System.Collections.Generic;
using System.IO;

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
            if (!Directory.Exists("Assets/TmpTests/"))
            {
                Directory.CreateDirectory("Assets/TmpTests/");
            }

            var tempFilePath = MakeTempFilePath("vfx");
            var asset = VisualEffectAssetEditorUtility.CreateNewAsset(tempFilePath);
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

        static readonly bool k_HasFixed_Several_PrefabOverride = false;

        [UnityTest]
        public IEnumerator Create_Prefab_Several_Override()
        {
            var graph = MakeTemporaryGraph();
            var parametersIntDesc = VFXLibrary.GetParameters().Where(o => o.model.type == typeof(int)).First();

            Func<VisualEffect, string> dumpPropertySheetInteger = delegate(VisualEffect target)
            {
                var r = "{";

                var editor = Editor.CreateEditor(target);
                editor.serializedObject.Update();

                var propertySheet = editor.serializedObject.FindProperty("m_PropertySheet");
                var fieldName = VisualEffectSerializationUtility.GetTypeField(VFXExpression.TypeToType(VFXValueType.Int32)) + ".m_Array";
                var vfxField = propertySheet.FindPropertyRelative(fieldName);

                for(int i = 0; i < vfxField.arraySize; ++i)
                {
                    var itField = vfxField.GetArrayElementAtIndex(i);
                    var name = itField.FindPropertyRelative("m_Name").stringValue;
                    var value = itField.FindPropertyRelative("m_Value").intValue;
                    var overridden = itField.FindPropertyRelative("m_Overridden").boolValue;

                    r += string.Format("({0}, {1}, {2})", name, value, overridden);
                    if (i != vfxField.arraySize - 1)
                        r += ", ";
                }

                GameObject.DestroyImmediate(editor);
                r += "}";
                return r;
            };

            var log = string.Empty;
            var exposedProperties = new[] { "a", "b", "c" };
            for (var i = 0; i < exposedProperties.Length; ++i)
            {
                var parameter = parametersIntDesc.CreateInstance();
                parameter.SetSettingValue("m_ExposedName", exposedProperties[i]);
                parameter.SetSettingValue("m_Exposed", true);
                parameter.value = i + 1;
                graph.AddChild(parameter);
            }

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            var mainObject = MakeTemporaryGameObject();
            var vfx = mainObject.AddComponent<VisualEffect>();
            vfx.visualEffectAsset = graph.visualEffectResource.asset;

            GameObject newGameObject, prefabInstanceObject;
            MakeTemporaryPrebab(mainObject, out newGameObject, out prefabInstanceObject);
            GameObject.DestroyImmediate(mainObject);
            yield return null;

            var currentPrefabInstanceObject = PrefabUtility.InstantiatePrefab(prefabInstanceObject) as GameObject;

            var overridenParametersInScene = new[] { new { name = "b", value = 666 }, new { name = "a", value = 444 } };
            var overridenParametersInPrefab = new[] { new { name = "c", value = -123 } };

            log += "Initial Sheet\n";
            log += "Prefab : " + dumpPropertySheetInteger(prefabInstanceObject.GetComponent<VisualEffect>()) + "\n";
            log += "Instance  : " + dumpPropertySheetInteger(currentPrefabInstanceObject.GetComponent<VisualEffect>()) + "\n";
            yield return null;

            foreach (var overridenParameter in overridenParametersInScene)
            {
                currentPrefabInstanceObject.GetComponent<VisualEffect>().SetInt(overridenParameter.name, overridenParameter.value);
            }

            log += "\nIntermediate Sheet\n";
            log += "Prefab : " + dumpPropertySheetInteger(prefabInstanceObject.GetComponent<VisualEffect>()) + "\n";
            log += "Instance  : " + dumpPropertySheetInteger(currentPrefabInstanceObject.GetComponent<VisualEffect>()) + "\n";
            yield return null;

            foreach (var overridenParameter in overridenParametersInPrefab)
            {
                prefabInstanceObject.GetComponent<VisualEffect>().SetInt(overridenParameter.name, overridenParameter.value);
            }

            yield return null;

            log += "\nEnd Sheet\n";
            log += "Prefab : " + dumpPropertySheetInteger(prefabInstanceObject.GetComponent<VisualEffect>()) + "\n";
            log += "Instance  : " + dumpPropertySheetInteger(currentPrefabInstanceObject.GetComponent<VisualEffect>()) + "\n";

            var stringFormat = @"({0} : {1}) ";
            var expectedValues = string.Empty;
            for (var i = 0; i < exposedProperties.Length; ++i)
            {
                var expectedValue = i;
                var expectedName = exposedProperties[i];
                var overrideInPrefab = overridenParametersInPrefab.FirstOrDefault(o => o.name == exposedProperties[i]);
                var overrideInScene = overridenParametersInScene.FirstOrDefault(o => o.name == exposedProperties[i]);

                if (overrideInPrefab != null)
                {
                    expectedValue = overrideInPrefab.value;
                }

                if (overrideInScene != null)
                {
                    expectedValue = overrideInScene.value;
                }

                expectedValues += string.Format(stringFormat, expectedName, expectedValue);
            }

            var actualValues = string.Empty;
            for (var i = 0; i < exposedProperties.Length; ++i)
            {
                var expectedName = exposedProperties[i];
                var actualValue = currentPrefabInstanceObject.GetComponent<VisualEffect>().GetInt(expectedName);
                actualValues += string.Format(stringFormat, expectedName, actualValue);
            }

            if (k_HasFixed_Several_PrefabOverride)
            {
                Assert.AreEqual(expectedValues, actualValues, log);
            }
            else
            {
                Assert.AreNotEqual(expectedValues, actualValues, log); //Did you fixed it ? Should enable this test : k_HasFixed_Several_PrefabOverride
            }
            yield return null;
        }

        private void Add_Valid_System(VFXGraph graph)
        {
            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var blockConstantRate = ScriptableObject.CreateInstance<VFXSpawnerConstantRate>();
            var slotCount = blockConstantRate.GetInputSlot(0);

            var basicInitialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var quadOutput = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();

            quadOutput.SetSettingValue("blendMode", VFXAbstractParticleOutput.BlendMode.Additive);

            var setPosition = ScriptableObject.CreateInstance<Block.SetAttribute>(); //only needed to allocate a minimal attributeBuffer
            setPosition.SetSettingValue("attribute", "position");
            setPosition.inputSlots[0].value = VFX.Position.defaultValue;
            basicInitialize.AddChild(setPosition);

            slotCount.value = 1.0f;

            spawnerContext.AddChild(blockConstantRate);
            graph.AddChild(spawnerContext);
            graph.AddChild(basicInitialize);
            graph.AddChild(quadOutput);

            basicInitialize.LinkFrom(spawnerContext);
            quadOutput.LinkFrom(basicInitialize);
        }

        //Cover regression from 1213773
        [UnityTest]
        public IEnumerator Create_Prefab_Switch_To_Empty_VisualEffectAsset()
        {
            var graph = MakeTemporaryGraph();
            const int systemCount = 3;
            for (int i = 0; i < systemCount; ++i)
            {
                Add_Valid_System(graph);
            }
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            var mainObject = MakeTemporaryGameObject();
            GameObject prefabInstanceObject;
            {
                var tempVFX = mainObject.AddComponent<VisualEffect>();
                tempVFX.visualEffectAsset = graph.visualEffectResource.asset;

                GameObject newGameObject;
                MakeTemporaryPrebab(mainObject, out newGameObject, out prefabInstanceObject);
                GameObject.DestroyImmediate(mainObject);

                mainObject = PrefabUtility.InstantiatePrefab(prefabInstanceObject) as GameObject;
            }
            yield return null;

            var vfx = mainObject.GetComponent<VisualEffect>();

            var vfxInPrefab = prefabInstanceObject.GetComponent<VisualEffect>();
            var systemNames = new List<string>();

            systemNames.Clear();
            vfx.GetSystemNames(systemNames);
            Assert.AreEqual(systemCount * 2, systemNames.Count);

            systemNames.Clear();
            vfxInPrefab.GetSystemNames(systemNames);
            Assert.AreEqual(systemCount * 2, systemNames.Count);

            while (!vfx.isActiveAndEnabled)
                yield return null;

            yield return null;
            {
                //vfxInPrefab.visualEffectAsset = null; //Doesn't cover awake from load beahavior which is the most common

                //modifying prefab using serialized property
                var editor = Editor.CreateEditor(vfxInPrefab);
                editor.serializedObject.Update();

                var assetProperty = editor.serializedObject.FindProperty("m_Asset");
                assetProperty.objectReferenceValue = null;
                editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();

                GameObject.DestroyImmediate(editor);
                EditorUtility.SetDirty(prefabInstanceObject);
            }

            PrefabUtility.SavePrefabAsset(prefabInstanceObject); //It will crash !

            yield return null;

            systemNames.Clear();
            vfx.GetSystemNames(systemNames);
            Assert.AreEqual(0u, systemNames.Count);

            systemNames.Clear();
            vfxInPrefab.GetSystemNames(systemNames);
            Assert.AreEqual(0u, systemNames.Count);

            Assert.IsTrue(true); //Should not have crashed here
        }

        static readonly bool k_HasFixed_DisabledState = true;
        static readonly bool k_HasFixed_PrefabOverride = true;

        /* Follow "Known issue : " <= This test has been added to cover a future fix in vfx behavior */
        [UnityTest]
        public IEnumerator Create_Prefab_Modify_And_Expect_No_Override()
        {
            var graph = MakeTemporaryGraph();
            var parametersVector3Desc = VFXLibrary.GetParameters().Where(o => o.model.type == typeof(Vector3)).First();

            var exposedName = "ghjkl";
            var parameter = parametersVector3Desc.CreateInstance();
            parameter.SetSettingValue("m_ExposedName", exposedName);
            parameter.SetSettingValue("m_Exposed", true);
            parameter.value = new Vector3(0, 0, 0);
            graph.AddChild(parameter);

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

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
            }
            //AssetDatabase.SaveAssets(); //Helps debug but not necessary

            PrefabUtility.SavePrefabAsset(prefabInstanceObject);
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
