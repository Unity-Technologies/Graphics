using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


namespace UnityEditor.ShaderGraph
{
    //move to core Unity at some point, make sure the hash calculation is identical to GTSBuildInfoGenerator in the meantime

    public class TextureStackStatus
    {
        /// <summary>
        /// Update the keywords on a material to reflect the correct VT state.
        /// </summary>
        /// <param name="material">Material to update</param>
        /// <param name="forceOff">Force VT off even if the material indiates it wants VT and VT assets are correctly build.</param>
        public static void UpdateMaterial(Material material, bool forceOff = false)
        {
            if (material.HasProperty("_VirtualTexturing") == false)
                return;

            bool enable = forceOff ? false : !(material.GetFloat("_VirtualTexturing") == 0.0f || !TextureStackStatus.AllStacksValid(material));
                     
            if (enable)
                material.EnableKeyword("VIRTUAL_TEXTURES_BUILT");
            else
                material.DisableKeyword("VIRTUAL_TEXTURES_BUILT");
        }

        // Scans all materials and updates their VT status
        // Note this may take a long time, don't over use this.
        public static void UpdateAllMaterials(bool forceOff = false)
        {
            // disable VT on all materials
            var matIds = AssetDatabase.FindAssets("t:Material");
            for (int i = 0, length = matIds.Length; i < length; i++)
            {
                EditorUtility.DisplayProgressBar("Updating Materials", "Updating materials for VT changes...", (float)(i / matIds.Length));
                var path = AssetDatabase.GUIDToAssetPath(matIds[i]);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    ShaderGraph.TextureStackStatus.UpdateMaterial(mat, forceOff);
                }
            }
            EditorUtility.ClearProgressBar();
        }

        public static bool AllStacksValid(Material material)
        {
            var shader = material.shader;

            int propCount = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < propCount; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.Stack)
                {
                    string stackPropName = ShaderUtil.GetPropertyName(shader, i);
                    TextureStack vtStack = material.GetTextureStack(stackPropName);

                    if (vtStack != null)
                    {
                        string hash = GetStackHash(stackPropName, material);

                        if (hash != vtStack.atlasName)
                            return false;

                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static List<string> GetInvalidStacksInfo(Material material)
        {
            List<string> result = new List<string>();
            var shader = material.shader;

            int propCount = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < propCount; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.Stack)
                {
                    string stackPropName = ShaderUtil.GetPropertyName(shader, i);
                    VTStack vtStack = material.GetTextureStack(stackPropName);

                    if (vtStack != null)
                    {
                        string hash = GetStackHash(stackPropName, material);

                        if (hash != vtStack.atlasName)
                        {
                            result.Add(string.Format("Mat {0}, vt stack {1} hash does not match texture hash {2}", material.name, vtStack.atlasName, hash));
                        }

                    }
                    else
                    {
                        result.Add(string.Format("Mat {0}, vt stack {1} is null", material.name, stackPropName));
                    }
                }
            }

            return result;
        }

        public static string GetStackHash(string stackPropName, Material material)
        {            
            // fill in a (hashed) name
            string texturesHash = "";

            if (material == null)
                return texturesHash;

            if (material.shader == null)
                return texturesHash;


            string[] textureProperties = ShaderUtil.GetStackTextureProperties(material.shader, stackPropName);

            TextureWrapMode wrapMode = TextureWrapMode.Clamp;
            TextureImporterNPOTScale textureScaling = TextureImporterNPOTScale.None;

            bool firstTextureAdded = false;

            for (int j = 0; j < textureProperties.Length; j++)
            {
                string textureProperty = textureProperties[j];
                string hash = "NO-DATA";  //TODO for empty layers the Granite layer data type is unknown. Therefor, this stack can be part of different tile sets with different layer layouts and still have the same hash

                Debug.Assert(material.HasProperty(textureProperty));    

                Texture2D tex2D = material.GetTexture(textureProperty) as Texture2D;

                if (tex2D != null)
                {
                    string path = AssetDatabase.GetAssetPath(tex2D);

                    if (!string.IsNullOrEmpty(path) && tex2D.imageContentsHash != null )
                    { 
                        Debug.Assert(hash!=null); // todo we checked this before, does this still make sense?

                        TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

                        if (textureImporter != null)// probably a fallback texture if this is false
                        {
                            hash = GetTextureHash(tex2D);

                            if (!firstTextureAdded)
                            {
                                wrapMode = tex2D.wrapMode;
                                textureScaling = textureImporter.npotScale;
                                firstTextureAdded = true;
                            }
                            else
                            {
                                if (wrapMode != tex2D.wrapMode || textureScaling != textureImporter.npotScale)
                                    UnityEngine.Debug.LogError("Texture settings don't match on all layers of StackedTexture");
                            }
                        }
                    }
                }                

                texturesHash += hash;

            }

            String assetHash = "" + wrapMode + textureScaling;

            //todo: ignoring overall settings in hash for now
            //TileSetBuildSettings tileSetBuildSettings = new TileSetBuildSettings(ts);
            String settingsHash = "";


            return ("version_1_" + texturesHash + assetHash + settingsHash).GetHashCode().ToString("X");
        }

        /// <summary>
        /// Get a string that uniquely identifies the texture on the given slot of the given material.
        /// </summary>
        public static string GetTextureHash(Texture2D texture)
        {
            if (texture == null)
            {
                return null;
            }

            // Do the texture resize here if needed. We only resize the texture if it's a normal texture. 
            // This makes sure we get the texture hash after the rescale, which is the hash we'll get when the material is valid
            string assetPath = AssetDatabase.GetAssetPath(texture);
            TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (textureImporter != null)
            {
                /* TODO no resizing for now
                if (textureImporter.maxTextureSize > Constants.TextureResizeSize)
                {
                    textureImporter.maxTextureSize = Constants.TextureResizeSize;
                    AssetDatabase.ImportAsset(assetPath);

                    // Validate setting the size worked.
                    TextureImporter validateImport = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (validateImport.maxTextureSize > Constants.TextureResizeSize)
                    {
                        UnityEngine.Debug.LogError("Could not set maxTextureSize of '" + assetPath + "' to " + Constants.TextureResizeSize + ". Do you have an AssetPostProcessor active that changes texture sizes?");
                    }
                }
                */
            }

            return texture.imageContentsHash.ToString() + GetGraniteLayerDataType(textureImporter);
        }

        //returns null if no valid texture importer is passed
        public static string GetGraniteLayerDataType(TextureImporter textureImporter)
        {
            if (textureImporter == null)
                return null;

            if (textureImporter.textureType == TextureImporterType.NormalMap)
                return "X8Y8Z0_TANGENT";

            if (textureImporter.textureType == TextureImporterType.SingleChannel)
                return "X8";

            //todo is this the only way to detect HDR?
            TextureImporterFormat format = textureImporter.GetAutomaticFormat("Standalone");

            switch (format)
            {
                case TextureImporterFormat.BC6H:
                case TextureImporterFormat.RGB16:
                    return "R16G16B16_FLOAT";
                default:
                    break;
            }

            if (textureImporter.sRGBTexture)
                return "R8G8B8A8_SRGB";
            else
                return "R8G8B8A8_LINEAR";
        }
    }
}
