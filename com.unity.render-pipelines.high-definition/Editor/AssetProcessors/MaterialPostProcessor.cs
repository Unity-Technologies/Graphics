using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

// Material property names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    class MaterialModificationProcessor : AssetModificationProcessor
    {
        static void OnWillCreateAsset(string asset)
        {
            if (!asset.ToLowerInvariant().EndsWith(".mat"))
                return;

            MaterialPostprocessor.s_CreatedAssets.Add(asset);
        }
    }

    class MaterialReimporter : Editor
    {
        static bool s_NeedToCheckProjSettingExistence = true;

        static internal void ReimportAllMaterials()
        {
            string[] guids = AssetDatabase.FindAssets("t:material", null);
            // There can be several materials subAssets per guid ( ie : FBX files ), remove duplicate guids.
            var distinctGuids = guids.Distinct();

            int materialIdx = 0;
            int totalMaterials = distinctGuids.Count();
            foreach (var asset in distinctGuids)
            {
                materialIdx++;
                var path = AssetDatabase.GUIDToAssetPath(asset);
                EditorUtility.DisplayProgressBar("Material Upgrader re-import", string.Format("({0} of {1}) {2}", materialIdx, totalMaterials, path), (float)materialIdx / (float)totalMaterials);
                AssetDatabase.ImportAsset(path);
            }
            UnityEditor.EditorUtility.ClearProgressBar();

            MaterialPostprocessor.s_NeedsSavingAssets = true;
        }

        [InitializeOnLoadMethod]
        static void RegisterUpgraderReimport()
        {
            EditorApplication.update += () =>
            {
                if (Time.renderedFrameCount > 0)
                {
                    bool fileExist = true;
                    // We check the file existence only once to avoid IO operations every frame.
                    if(s_NeedToCheckProjSettingExistence)
                    {
                        fileExist = System.IO.File.Exists("ProjectSettings/HDRPProjectSettings.asset");
                        s_NeedToCheckProjSettingExistence = false;
                    }

                    //This method is called at opening and when HDRP package change (update of manifest.json)
                    var curUpgradeVersion = HDProjectSettings.materialVersionForUpgrade;

                    if (curUpgradeVersion != MaterialPostprocessor.k_Migrations.Length)
                    {
                        string commandLineOptions = System.Environment.CommandLine;
                        bool inTestSuite = commandLineOptions.Contains("-testResults");
                        if (!inTestSuite && fileExist)
                        {
                            EditorUtility.DisplayDialog("HDRP Material upgrade", "The Materials in your Project were created using an older version of the High Definition Render Pipeline (HDRP)." +
                                                        " Unity must upgrade them to be compatible with your current version of HDRP. \n" +
                                                        " Unity will re-import all of the Materials in your project, save the upgraded Materials to disk, and check them out in source control if needed.\n"+
                                                        " Please see the Material upgrade guide in the HDRP documentation for more information.", "Ok");
                        }

                        ReimportAllMaterials();
                    }

                    if (MaterialPostprocessor.s_NeedsSavingAssets)
                        MaterialPostprocessor.SaveAssetsToDisk();
                }
            };
        }
    }

    class MaterialPostprocessor : AssetPostprocessor
    {
        internal static List<string> s_CreatedAssets = new List<string>();
        internal static List<string> s_ImportedAssetThatNeedSaving = new List<string>();
        internal static bool s_NeedsSavingAssets = false;

        static internal void SaveAssetsToDisk()
        {
            string commandLineOptions = System.Environment.CommandLine;
            bool inTestSuite = commandLineOptions.Contains("-testResults");
            if (inTestSuite)
                return;

            foreach (var asset in s_ImportedAssetThatNeedSaving)
            {
                AssetDatabase.MakeEditable(asset);
            }

            AssetDatabase.SaveAssets();
            //to prevent data loss, only update the saved version if user applied change and assets are written to
            HDProjectSettings.materialVersionForUpgrade = MaterialPostprocessor.k_Migrations.Length;

            s_ImportedAssetThatNeedSaving.Clear();
            s_NeedsSavingAssets = false;
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var asset in importedAssets)
            {
                if (!asset.ToLowerInvariant().EndsWith(".mat"))
                    continue;

                var material = (Material)AssetDatabase.LoadAssetAtPath(asset, typeof(Material));
                if (!HDShaderUtils.IsHDRPShader(material.shader, upgradable: true))
                    continue;

                HDShaderUtils.ShaderID id = HDShaderUtils.GetShaderEnumFromShader(material.shader);
                var latestVersion = k_Migrations.Length;
                var wasUpgraded = false;
                var assetVersions = AssetDatabase.LoadAllAssetsAtPath(asset);
                AssetVersion assetVersion = null;
                foreach (var subAsset in assetVersions)
                {
                    if (subAsset.GetType() == typeof(AssetVersion))
                    {
                        assetVersion = subAsset as AssetVersion;
                        break;
                    }
                }

                //subasset not found
                if (!assetVersion)
                {
                    wasUpgraded = true;
                    assetVersion = ScriptableObject.CreateInstance<AssetVersion>();
                    assetVersion.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable;
                    if (s_CreatedAssets.Contains(asset))
                    {
                        //just created
                        assetVersion.version = latestVersion;
                        s_CreatedAssets.Remove(asset);

                        //[TODO: remove comment once fixed]
                        //due to FB 1175514, this not work. It is being fixed though.
                        //delayed call of the following work in some case and cause infinite loop in other cases.
                        AssetDatabase.AddObjectToAsset(assetVersion, asset);
                    }
                    else
                    {
                        //asset exist prior migration
                        assetVersion.version = 0;
                        AssetDatabase.AddObjectToAsset(assetVersion, asset);
                    }
                }

                //upgrade
                while (assetVersion.version < latestVersion)
                {
                    k_Migrations[assetVersion.version](material, id);
                    assetVersion.version++;
                    wasUpgraded = true;
                }

                if (wasUpgraded)
                {
                    EditorUtility.SetDirty(assetVersion);
                    s_ImportedAssetThatNeedSaving.Add(asset);
                    s_NeedsSavingAssets = true;
                }
            }
        }

        // Note: It is not possible to separate migration step by kind of shader
        // used. This is due that user can change shader that material reflect.
        // And when user do this, the material is not reimported and we have no
        // hook on this event.
        // So we must have migration step that work on every materials at once.
        // Which also means that if we want to update only one shader, we need
        // to bump all materials version...
        static internal Action<Material, HDShaderUtils.ShaderID>[] k_Migrations = new Action<Material, HDShaderUtils.ShaderID>[]
        {
             StencilRefactor,
             ZWriteForTransparent,
        };

        #region Migrations

        // Not used currently:
        // TODO: Script like this must also work with embed material in scene (i.e we need to catch
        // .unity scene and load material and patch in memory. And it must work with perforce
        // i.e automatically checkout all those files).
        static void SpecularOcclusionMode(Material material, HDShaderUtils.ShaderID id)
        {
            switch (id)
            {
                case HDShaderUtils.ShaderID.Lit:
                case HDShaderUtils.ShaderID.LayeredLit:
                case HDShaderUtils.ShaderID.LitTesselation:
                case HDShaderUtils.ShaderID.LayeredLitTesselation:
                    var serializedObject = new SerializedObject(material);
                    var specOcclusionMode = 1;
                    if (FindProperty(serializedObject, "_EnableSpecularOcclusion", SerializedType.Boolean).property != null)
                    {
                        var enableSpecOcclusion = GetSerializedBoolean(serializedObject, "_EnableSpecularOcclusion");
                        if (enableSpecOcclusion)
                        {
                            specOcclusionMode = 2;
                        }
                        RemoveSerializedBoolean(serializedObject, "_EnableSpecularOcclusion");
                        serializedObject.ApplyModifiedProperties();
                    }
                    material.SetInt("_SpecularOcclusionMode", specOcclusionMode);

                    HDShaderUtils.ResetMaterialKeywords(material);
                    break;
            }
        }

        static void StencilRefactor(Material material, HDShaderUtils.ShaderID id)
        {
            HDShaderUtils.ResetMaterialKeywords(material);
        }
        //example migration method, remove it after first real migration
        //static void EmissiveIntensityToColor(Material material, ShaderID id)
        //{
        //    switch(id)
        //    {
        //        case ShaderID.Lit:
        //        case ShaderID.LitTesselation:
        //            var emissiveIntensity = material.GetFloat("_EmissiveIntensity");
        //            var emissiveColor = Color.black;
        //            if (material.HasProperty("_EmissiveColor"))
        //                emissiveColor = material.GetColor("_EmissiveColor");
        //            emissiveColor *= emissiveIntensity;
        //            emissiveColor.a = 1.0f;
        //            material.SetColor("_EmissiveColor", emissiveColor);
        //            material.SetColor("_EmissionColor", Color.white);
        //            break;
        //    }
        //}
        //
        //static void Serialization_API_Usage(Material material, ShaderID id)
        //{
        //    switch(id)
        //    {
        //        case ShaderID.Unlit:
        //            var serializedObject = new SerializedObject(material);
        //            AddSerializedInt(serializedObject, "former", 42);
        //            RenameSerializedScalar(serializedObject, "former", "new");
        //            Debug.Log(GetSerializedInt(serializedObject, "new"));
        //            RemoveSerializedInt(serializedObject, "new");
        //            serializedObject.ApplyModifiedProperties();
        //            break;
        //    }
        //}

        static void ZWriteForTransparent(Material material, HDShaderUtils.ShaderID id)
        {
            // For transparent materials, the ZWrite property that is now used is _TransparentZWrite.
            if (material.GetSurfaceType() == SurfaceType.Transparent)
                material.SetFloat(kTransparentZWrite, material.GetZWrite() ? 1.0f : 0.0f);

            HDShaderUtils.ResetMaterialKeywords(material);
        }

        #endregion

        #region Serialization_API
        //Methods in this region interact on the serialized material
        //without filtering on what used shader knows

        enum SerializedType
        {
            Boolean,
            Integer,
            Float,
            Vector,
            Color,
            Texture
        }

        // do not use directly in migration function
        static SerializedProperty FindBase(SerializedObject material, SerializedType type)
        {
            var propertyBase = material.FindProperty("m_SavedProperties");

            switch (type)
            {
                case SerializedType.Boolean:
                case SerializedType.Integer:
                case SerializedType.Float:
                    propertyBase = propertyBase.FindPropertyRelative("m_Floats");
                    break;
                case SerializedType.Color:
                case SerializedType.Vector:
                    propertyBase = propertyBase.FindPropertyRelative("m_Colors");
                    break;
                case SerializedType.Texture:
                    propertyBase = propertyBase.FindPropertyRelative("m_TexEnvs");
                    break;
                default:
                    throw new ArgumentException($"Unknown SerializedType {type}");
            }

            return propertyBase;
        }

        // do not use directly in migration function
        static (SerializedProperty property, int index, SerializedProperty parent) FindProperty(SerializedObject material, string propertyName, SerializedType type)
        {
            var propertyBase = FindBase(material, type);

            SerializedProperty property = null;
            int maxSearch = propertyBase.arraySize;
            int indexOf = 0;
            for (; indexOf < maxSearch; ++indexOf)
            {
                property = propertyBase.GetArrayElementAtIndex(indexOf);
                if (property.FindPropertyRelative("first").stringValue == propertyName)
                    break;
            }
            if (indexOf == maxSearch)
                throw new ArgumentException($"Unknown property: {propertyName}");

            property = property.FindPropertyRelative("second");
            return (property, indexOf, propertyBase);
        }

        static Color GetSerializedColor(SerializedObject material, string propertyName)
            => FindProperty(material, propertyName, SerializedType.Color)
            .property.colorValue;

        static bool GetSerializedBoolean(SerializedObject material, string propertyName)
            => FindProperty(material, propertyName, SerializedType.Boolean)
            .property.floatValue > 0.5f;

        static int GetSerializedInt(SerializedObject material, string propertyName)
            => (int)FindProperty(material, propertyName, SerializedType.Integer)
            .property.floatValue;

        static Vector2Int GetSerializedVector2Int(SerializedObject material, string propertyName)
        {
            var property = FindProperty(material, propertyName, SerializedType.Vector).property;
            return new Vector2Int(
                (int)property.FindPropertyRelative("r").floatValue,
                (int)property.FindPropertyRelative("g").floatValue);
        }

        static Vector3Int GetSerializedVector3Int(SerializedObject material, string propertyName)
        {
            var property = FindProperty(material, propertyName, SerializedType.Vector).property;
            return new Vector3Int(
                (int)property.FindPropertyRelative("r").floatValue,
                (int)property.FindPropertyRelative("g").floatValue,
                (int)property.FindPropertyRelative("b").floatValue);
        }

        static float GetSerializedFloat(SerializedObject material, string propertyName)
            => FindProperty(material, propertyName, SerializedType.Float)
            .property.floatValue;

        static Vector2 GetSerializedVector2(SerializedObject material, string propertyName)
        {
            var property = FindProperty(material, propertyName, SerializedType.Vector).property;
            return new Vector2(
                property.FindPropertyRelative("r").floatValue,
                property.FindPropertyRelative("g").floatValue);
        }

        static Vector3 GetSerializedVector3(SerializedObject material, string propertyName)
        {
            var property = FindProperty(material, propertyName, SerializedType.Vector).property;
            return new Vector3(
                property.FindPropertyRelative("r").floatValue,
                property.FindPropertyRelative("g").floatValue,
                property.FindPropertyRelative("b").floatValue);
        }

        static Vector4 GetSerializedVector4(SerializedObject material, string propertyName)
        {
            var property = FindProperty(material, propertyName, SerializedType.Vector).property;
            return new Vector4(
                property.FindPropertyRelative("r").floatValue,
                property.FindPropertyRelative("g").floatValue,
                property.FindPropertyRelative("b").floatValue,
                property.FindPropertyRelative("a").floatValue);
        }

        static (Texture texture, Vector2 scale, Vector2 offset) GetSerializedTexture(SerializedObject material, string propertyName)
        {
            var property = FindProperty(material, propertyName, SerializedType.Texture).property;
            return (
                property.FindPropertyRelative("m_Texture").objectReferenceValue as Texture,
                property.FindPropertyRelative("m_Scale").vector2Value,
                property.FindPropertyRelative("m_Offset").vector2Value);
        }

        static void RemoveSerializedColor(SerializedObject material, string propertyName)
        {
            var res = FindProperty(material, propertyName, SerializedType.Color);
            res.parent.DeleteArrayElementAtIndex(res.index);
        }

        static void RemoveSerializedBoolean(SerializedObject material, string propertyName)
        {
            var res = FindProperty(material, propertyName, SerializedType.Boolean);
            res.parent.DeleteArrayElementAtIndex(res.index);
        }

        static void RemoveSerializedInt(SerializedObject material, string propertyName)
        {
            var res = FindProperty(material, propertyName, SerializedType.Integer);
            res.parent.DeleteArrayElementAtIndex(res.index);
        }

        static void RemoveSerializedVector2Int(SerializedObject material, string propertyName)
        {
            var res = FindProperty(material, propertyName, SerializedType.Vector);
            res.parent.DeleteArrayElementAtIndex(res.index);
        }

        static void RemoveSerializedVector3Int(SerializedObject material, string propertyName)
        {
            var res = FindProperty(material, propertyName, SerializedType.Vector);
            res.parent.DeleteArrayElementAtIndex(res.index);
        }

        static void RemoveSerializedFloat(SerializedObject material, string propertyName)
        {
            var res = FindProperty(material, propertyName, SerializedType.Float);
            res.parent.DeleteArrayElementAtIndex(res.index);
        }

        static void RemoveSerializedVector2(SerializedObject material, string propertyName)
        {
            var res = FindProperty(material, propertyName, SerializedType.Vector);
            res.parent.DeleteArrayElementAtIndex(res.index);
        }

        static void RemoveSerializedVector3(SerializedObject material, string propertyName)
        {
            var res = FindProperty(material, propertyName, SerializedType.Vector);
            res.parent.DeleteArrayElementAtIndex(res.index);
        }

        static void RemoveSerializedVector4(SerializedObject material, string propertyName)
        {
            var res = FindProperty(material, propertyName, SerializedType.Vector);
            res.parent.DeleteArrayElementAtIndex(res.index);
        }

        static void RemoveSerializedTexture(SerializedObject material, string propertyName)
        {
            var res = FindProperty(material, propertyName, SerializedType.Texture);
            res.parent.DeleteArrayElementAtIndex(res.index);
        }

        static void AddSerializedColor(SerializedObject material, string name, Color value)
        {
            var propertyBase = FindBase(material, SerializedType.Color);
            int lastPos = propertyBase.arraySize;
            propertyBase.InsertArrayElementAtIndex(lastPos);
            var newProperty = propertyBase.GetArrayElementAtIndex(lastPos);
            newProperty.FindPropertyRelative("first").stringValue = name;
            newProperty.FindPropertyRelative("second").colorValue = value;
        }

        static void AddSerializedBoolean(SerializedObject material, string name, bool value)
        {
            var propertyBase = FindBase(material, SerializedType.Boolean);
            propertyBase.InsertArrayElementAtIndex(0);
            var newProperty = propertyBase.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = name;
            newProperty.FindPropertyRelative("second").floatValue = value ? 1f : 0f;
        }

        static void AddSerializedInt(SerializedObject material, string name, int value)
        {
            var propertyBase = FindBase(material, SerializedType.Integer);
            propertyBase.InsertArrayElementAtIndex(0);
            var newProperty = propertyBase.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = name;
            newProperty.FindPropertyRelative("second").floatValue = value;
        }

        static void AddSerializedVector2Int(SerializedObject material, string name, Vector2Int value)
        {
            var propertyBase = FindBase(material, SerializedType.Vector);
            propertyBase.InsertArrayElementAtIndex(0);
            var newProperty = propertyBase.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = name;
            var container = newProperty.FindPropertyRelative("second");
            container.FindPropertyRelative("r").floatValue = value.x;
            container.FindPropertyRelative("g").floatValue = value.y;
            container.FindPropertyRelative("b").floatValue = 0;
            container.FindPropertyRelative("a").floatValue = 0;
        }

        static void AddSerializedVector3Int(SerializedObject material, string name, Vector3Int value)
        {
            var propertyBase = FindBase(material, SerializedType.Vector);
            propertyBase.InsertArrayElementAtIndex(0);
            var newProperty = propertyBase.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = name;
            var container = newProperty.FindPropertyRelative("second");
            container.FindPropertyRelative("r").floatValue = value.x;
            container.FindPropertyRelative("g").floatValue = value.y;
            container.FindPropertyRelative("b").floatValue = value.z;
            container.FindPropertyRelative("a").floatValue = 0;
        }

        static void AddSerializedFloat(SerializedObject material, string name, float value)
        {
            var propertyBase = FindBase(material, SerializedType.Float);
            propertyBase.InsertArrayElementAtIndex(0);
            var newProperty = propertyBase.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = name;
            newProperty.FindPropertyRelative("second").floatValue = value;
        }

        static void AddSerializedVector2(SerializedObject material, string name, Vector2 value)
        {
            var propertyBase = FindBase(material, SerializedType.Vector);
            propertyBase.InsertArrayElementAtIndex(0);
            var newProperty = propertyBase.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = name;
            var container = newProperty.FindPropertyRelative("second");
            container.FindPropertyRelative("r").floatValue = value.x;
            container.FindPropertyRelative("g").floatValue = value.y;
            container.FindPropertyRelative("b").floatValue = 0;
            container.FindPropertyRelative("a").floatValue = 0;
        }

        static void AddSerializedVector3(SerializedObject material, string name, Vector3 value)
        {
            var propertyBase = FindBase(material, SerializedType.Vector);
            propertyBase.InsertArrayElementAtIndex(0);
            var newProperty = propertyBase.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = name;
            var container = newProperty.FindPropertyRelative("second");
            container.FindPropertyRelative("r").floatValue = value.x;
            container.FindPropertyRelative("g").floatValue = value.y;
            container.FindPropertyRelative("b").floatValue = value.z;
            container.FindPropertyRelative("a").floatValue = 0;
        }

        static void AddSerializedVector4(SerializedObject material, string name, Vector4 value)
        {
            var propertyBase = FindBase(material, SerializedType.Vector);
            propertyBase.InsertArrayElementAtIndex(0);
            var newProperty = propertyBase.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = name;
            var container = newProperty.FindPropertyRelative("second");
            container.FindPropertyRelative("r").floatValue = value.x;
            container.FindPropertyRelative("g").floatValue = value.y;
            container.FindPropertyRelative("b").floatValue = value.z;
            container.FindPropertyRelative("a").floatValue = value.w;
        }

        static void AddSerializedTexture(SerializedObject material, string name, Texture texture, Vector2 scale, Vector2 offset)
        {
            var propertyBase = FindBase(material, SerializedType.Texture);
            propertyBase.InsertArrayElementAtIndex(0);
            var newProperty = propertyBase.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = name;
            var container = newProperty.FindPropertyRelative("second");
            container.FindPropertyRelative("m_Texture").objectReferenceValue = texture;
            container.FindPropertyRelative("m_Scale").vector2Value = scale;
            container.FindPropertyRelative("m_Offset").vector2Value = offset;
        }

        static void RenameSerializedScalar(SerializedObject material, string oldName, string newName)
        {
            var res = FindProperty(material, oldName, SerializedType.Float);
            var value = res.property.floatValue;
            res.parent.InsertArrayElementAtIndex(0);
            var newProperty = res.parent.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = newName;
            newProperty.FindPropertyRelative("second").floatValue = value;
            res.parent.DeleteArrayElementAtIndex(res.index + 1);
        }

        static void RenameSerializedVector(SerializedObject material, string oldName, string newName)
        {
            var res = FindProperty(material, oldName, SerializedType.Vector);
            var valueX = res.property.FindPropertyRelative("r").floatValue;
            var valueY = res.property.FindPropertyRelative("g").floatValue;
            var valueZ = res.property.FindPropertyRelative("b").floatValue;
            var valueW = res.property.FindPropertyRelative("a").floatValue;
            res.parent.InsertArrayElementAtIndex(0);
            var newProperty = res.parent.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = newName;
            var container = newProperty.FindPropertyRelative("second");
            container.FindPropertyRelative("r").floatValue = valueX;
            container.FindPropertyRelative("g").floatValue = valueY;
            container.FindPropertyRelative("b").floatValue = valueZ;
            container.FindPropertyRelative("a").floatValue = valueW;
            res.parent.DeleteArrayElementAtIndex(res.index + 1);
        }

        static void RenameSerializedTexture(SerializedObject material, string oldName, string newName)
        {
            var res = FindProperty(material, oldName, SerializedType.Texture);
            var texture = res.property.FindPropertyRelative("m_Texture").objectReferenceValue;
            var scale = res.property.FindPropertyRelative("m_Scale").vector2Value;
            var offset = res.property.FindPropertyRelative("m_Offset").vector2Value;
            res.parent.InsertArrayElementAtIndex(0);
            var newProperty = res.parent.GetArrayElementAtIndex(0);
            newProperty.FindPropertyRelative("first").stringValue = newName;
            var container = newProperty.FindPropertyRelative("second");
            container.FindPropertyRelative("m_Texture").objectReferenceValue = texture;
            container.FindPropertyRelative("m_Scale").vector2Value = scale;
            container.FindPropertyRelative("m_Offset").vector2Value = offset;
            res.parent.DeleteArrayElementAtIndex(res.index + 1);
        }

        #endregion
    }
}
