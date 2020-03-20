using System;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using UnityEngine.VFX;
using UnityEditor.VFX.UI;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

namespace UnityEditor.VFX.Test
{
    class VFXTestCommon
    {
        static readonly string tempBasePath = "Assets/TmpTests/";
        static readonly string tempFileFormat = tempBasePath + "vfx_{0}.vfx";

        public static VFXGraph MakeTemporaryGraph()
        {
            var guid = System.Guid.NewGuid().ToString();
            string tempFilePath = string.Format(tempFileFormat, guid);
            System.IO.Directory.CreateDirectory(tempBasePath);

            var asset = VisualEffectAssetEditorUtility.CreateNewAsset(tempFilePath);
            VisualEffectResource resource = asset.GetResource(); // force resource creation
            VFXGraph graph = ScriptableObject.CreateInstance<VFXGraph>();
            graph.visualEffectResource = resource;
            return graph;
        }

        public static void DeleteAllTemporaryGraph()
        {
            foreach (string file in System.IO.Directory.GetFiles(tempBasePath))
            {
                try
                {
                    AssetDatabase.DeleteAsset(file);
                }
                catch (System.Exception) // Don't stop if we fail to delete one asset
                {
                }
            }
        }

        public static U GetFieldValue<T, U>(T obj, string fieldName)
            where U : class
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsTrue(field != null, fieldName + ": field not found");
            return field.GetValue(obj) as U;
        }

        public static void SetFieldValue<T, U>(T obj, string fieldName, U value)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsTrue(field != null, fieldName + ": field not found");
            field.SetValue(obj, value);
        }

        public static void CallMethod<T>(T obj, string methodName, object[] parameters)
        {
            var methodInfo = obj.GetType().GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsTrue(methodInfo != null, methodName + ": method not found");
            methodInfo.Invoke(obj, new object[] { null });
        }

        internal static void SetTextFieldValue(VFXSystemBorder sys, string value)
        {
            var systemTextField = GetFieldValue<VFXSystemBorder, TextField>(sys, "m_TitleField");
            systemTextField.value = value;
            SetFieldValue(sys, "m_TitleField", systemTextField);
        }

        internal static void CreateSystems(VFXView view, VFXViewController viewController, int count, int offset, string name = null)
        {
            Func<int, VFXContextController> fnContextController = delegate (int i)
            {
                viewController.ApplyChanges();
                var controller = viewController.allChildren.OfType<VFXContextController>().Cast<VFXContextController>().ToArray();
                return controller[i];
            };

            var contextInitializeDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.name.Contains("Init"));
            var contextOutputDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.name.StartsWith("Output Particle Quad"));
            for (int i = 0; i < count; ++i)
            {
                var output = viewController.AddVFXContext(new Vector2(2 * i, 2 * i), contextOutputDesc);
                var init = viewController.AddVFXContext(new Vector2(i, i), contextInitializeDesc);

                var flowEdge = new VFXFlowEdgeController(fnContextController(2 * i + offset).flowInputAnchors.FirstOrDefault(), fnContextController(2 * i + offset + 1).flowOutputAnchors.FirstOrDefault());
                viewController.AddElement(flowEdge);
            }

            viewController.ApplyChanges();

            if (name != null)
            {
                var systems = GetFieldValue<VFXView, List<VFXSystemBorder>>(view, "m_Systems");
                foreach (var sys in systems)
                {
                    SetTextFieldValue(sys, name);
                    CallMethod(sys, "OnTitleBlur", new object[] { null });
                }
            }
        }

        internal static List<VFXBasicSpawner> CreateSpawners(VFXView view, VFXViewController viewController, int count, string name = null)
        {
            List<VFXBasicSpawner> spawners = new List<VFXBasicSpawner>();
            for (int i = 0; i != count; ++i)
            {
                var spawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();
                spawners.Add(spawner);
                viewController.graph.AddChild(spawner);
            }

            viewController.ApplyChanges();

            if (name != null)
            {
                var elements = view.Query().OfType<GraphElement>().ToList();
                var UIElts = elements.OfType<VFXContextUI>().ToList();
                var contextUITextField = GetFieldValue<VFXContextUI, TextField>(UIElts[0], "m_TextField");
                contextUITextField.value = name;

                foreach (var contextUI in UIElts)
                {
                    SetFieldValue(contextUI, "m_TextField", contextUITextField);
                    CallMethod(contextUI, "OnTitleBlur", new object[] { null });
                }
            }

            return spawners;
        }
    }
}
