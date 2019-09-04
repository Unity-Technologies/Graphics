using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

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
        [InitializeOnLoadMethod]
        static void ReimportAllMaterials()
        {
            //This method is called at opening and when HDRP package change (update of manifest.json)
            //Check to see if the upgrader has been run for this project/HDRP version
            PackageManager.PackageInfo hdrpInfo = PackageManager.PackageInfo.FindForAssembly(Assembly.GetAssembly(typeof(HDRenderPipeline)));
            var hdrpVersion = hdrpInfo.version;
            var curUpgradeVersion = HDProjectSettings.packageVersionForMaterialUpgrade;

            bool firstInstallOfHDRP = curUpgradeVersion == HDProjectSettings.k_PackageFirstTimeVersionForMaterials;
            if (curUpgradeVersion != hdrpVersion)
            {
                string[] guids = AssetDatabase.FindAssets("t:material", null);

                foreach (var asset in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(asset);
                    AssetDatabase.ImportAsset(path);
                }

                string commandLineOptions = System.Environment.CommandLine;
                bool inTestSuite = commandLineOptions.Contains("-testResults");
                //prevent popup in test suite as there is no user to interact, no need to save in this case
                if (!inTestSuite && (firstInstallOfHDRP || EditorUtility.DisplayDialog("High Definition Materials Migration",
                    "Your current High Definition Render Pipeline requires a change that will update your Materials. In order to apply this update automatically, you need to save your Project. If you choose not to save your Project, you will need to re-import Materials manually, then save the Project.\n\nPlease note that downgrading from the High Definition Render Pipeline is not supported.",
                    "Save Project", "Not now")))
                {
                    AssetDatabase.SaveAssets();

                    //to prevent data loss, only update the saved version if user applied change
                    HDProjectSettings.packageVersionForMaterialUpgrade = hdrpVersion;
                }
            }
        }
    }

    class MaterialPostprocessor : AssetPostprocessor
    {
        internal static List<string> s_CreatedAssets = new List<string>();

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
                    EditorUtility.SetDirty(assetVersion);
            }
        }

        // Note: It is not possible to separate migration step by kind of shader
        // used. This is due that user can change shader that material reflect.
        // And when user do this, the material is not reimported and we have no
        // hook on this event.
        // So we must have migration step that work on every materials at once.
        // Which also means that if we want to update only one shader, we need
        // to bump all materials version...
        static readonly Action<Material, HDShaderUtils.ShaderID>[] k_Migrations = new Action<Material, HDShaderUtils.ShaderID>[]
        {
            /* EmissiveIntensityToColor,
             * SecondMigrationStep,
             * ... */ 
        };

        #region Migrations

        //exemple migration method, remove it after first real migration
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
