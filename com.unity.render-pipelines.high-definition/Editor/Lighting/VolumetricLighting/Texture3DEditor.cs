using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class Texture3DEditor : EditorWindow
    {
        [MenuItem("Window/Render Pipeline/Create 3D Texture from 2D Texture Slices")]
        public static void ShowWindow()
        {
            Texture3DEditor window = EditorWindow.GetWindow<Texture3DEditor>();
            window.Show();
        }

        private static readonly string SAVE_BUTTON_NAME = "Generate and Save 3D Texture";
        private static readonly string SAVE_DIALOGUE = "Save Texture3D";
        private static readonly string DEFAULT_SAVE_DIRECTORY = "Assets";
        private static readonly string DEFAULT_FILENAME = "untitled_texture3D.asset";
        private static readonly string FILE_EXTENSION = "asset";
        private enum Texture3DEditorError
        {
            None = 0,
            NoSavePathSpecified,
            SliceUnreadable,
            NoSlicesSpecified,
            NoTextureAssigned,
            Count
        };

        private static readonly string[] texture3DEditorErrorDescriptions =
        {
            "No Error",
            "Error: No save path specified.",
            "Error: Texture slice not readable.\nPlease set Read/Write Enabled checkbox in Texture2D import settings to allow this tool to use Texture2D as slice source.",
            "Error: No texture slices specified.\nTo create a texture slice, click Add Slice and supply a Texture2D.",
            "Error: One or more texture slices has not texture assigned.",
        };

        public enum ResampleMode
        {
            Nearest,
            Trilinear,
            Count
        };

        private static readonly string[] RESAMPLE_MODE_NAMES =
        {
            "Nearest",
            "Trilinear"
        };

        private static readonly string[] RESAMPLE_MODE_DESCRIPTIONS =
        {
            "Nearest: Lowest quality, but fastest.\nAssigns the geometrically closest source to destination pixel\nUseful for textures where interpolation is not meaninful, such as noise look up tables.",
            "Trilinear: Medium quality, medium performance.\nNot ideal for upsampling.",
        };

        public enum TextureFormatValid
        {
            Alpha8 = 0,
            RGB24,
            RGBA32,
            ARGB32,
            RHalf,
            RGHalf,
            RGBAHalf,
            RFloat,
            RGFloat,
            RGBAFloat,
            Count
        };

        private static readonly string[] TEXTURE_FORMAT_VALID_NAMES =
        {
            "Alpha8",
            "RGB24",
            "RGBA32",
            "ARGB32",
            "RHalf",
            "RGHalf",
            "RGBAHalf",
            "RFloat",
            "RGFloat",
            "RGBAFloat"
        };

        // https://docs.unity3d.com/ScriptReference/TextureFormat.html
        private static readonly string[] TEXTURE_FORMAT_VALID_DESCRIPTIONS =
        {
            "Alpha-only texture format.",
            "Color texture format, 8-bits per channel.",
            "Color with alpha texture format, 8-bits per channel.",
            "Color with alpha texture format, 8-bits per channel.",
            "Scalar (R) texture format, 16 bit floating point.",
            "Two color (RG) texture format, 16 bit floating point per channel.",
            "RGB color and alpha texture format, 16 bit floating point per channel.",
            "Scalar (R) texture format, 32 bit floating point.",
            "Two color (RG) texture format, 32 bit floating point per channel.",
            "RGB color and alpha texture format, 32-bit floats per channel."
        };

        private static TextureFormat GetTextureFormat(TextureFormatValid textureFormatValid)
        {
            switch (textureFormatValid)
            {
                case TextureFormatValid.Alpha8: return TextureFormat.Alpha8;
                case TextureFormatValid.RGB24: return TextureFormat.RGB24;
                case TextureFormatValid.RGBA32: return TextureFormat.RGBA32;
                case TextureFormatValid.ARGB32: return TextureFormat.ARGB32;
                case TextureFormatValid.RHalf: return TextureFormat.RHalf;
                case TextureFormatValid.RGHalf: return TextureFormat.RGHalf;
                case TextureFormatValid.RGBAHalf: return TextureFormat.RGBAHalf;
                case TextureFormatValid.RFloat: return TextureFormat.RFloat;
                case TextureFormatValid.RGFloat: return TextureFormat.RGFloat;
                case TextureFormatValid.RGBAFloat: return TextureFormat.RGBAFloat;
                default: return TextureFormat.RGBA32;
            }
        }

        public enum FilterModeValid
        {
            Point = 0,
            Bilinear,
            Trilinear,
            Count
        };

        private static readonly string[] FILTER_MODE_VALID_NAMES =
        {
            "Point",
            "Bilinear",
            "Trilinear"
        };

        private static FilterMode GetTextureFilterMode(FilterModeValid filterModeValid)
        {
            switch (filterModeValid)
            {
                case FilterModeValid.Point: return FilterMode.Point;
                case FilterModeValid.Bilinear: return FilterMode.Bilinear;
                case FilterModeValid.Trilinear: return FilterMode.Trilinear;
                default: return FilterMode.Point;
            }
        }

        public enum TextureWrapModeValid
        {
            Clamp = 0,
            Repeat,
            Count
        };

        private static readonly string[] TEXTURE_WRAP_MODE_VALID_NAMES =
        {
            "Clamp",
            "Repeat"
        };

        private static TextureWrapMode GetTextureWrapMode(TextureWrapModeValid textureWrapModeValid)
        {
            switch (textureWrapModeValid)
            {
                case TextureWrapModeValid.Clamp: return TextureWrapMode.Clamp;
                case TextureWrapModeValid.Repeat: return TextureWrapMode.Repeat;
                default: return TextureWrapMode.Clamp;
            }
        }

        public struct Texture3DSettings
        {
            public string name;
            public ResampleMode resampleMode;
            public TextureFormatValid textureFormatValid;
            public FilterModeValid filterModeValid;
            public TextureWrapModeValid textureWrapModeValid;
            public bool isMipmapEnabled;
            public int anisoLevel;
            public int width;
            public int height;
            public int depth;
        };

        private static readonly string GUI_NAME_IS_TEXTURE_3D_SETTINGS_DISPLAYED = "Output Settings";
        private static readonly string GUI_NAME_NAME = "Texture 3D Name";
        private static readonly string GUI_NAME_RESAMPLE_MODE = "Resample Mode";
        private static readonly string GUI_NAME_TEXTURE_FORMAT_VALID = "Texture Format";
        private static readonly string GUI_NAME_FILTER_MODE_VALID = "Filter Mode";
        private static readonly string GUI_NAME_WRAP_MODE_VALID = "Wrap Mode";
        private static readonly string GUI_NAME_IS_MIPMAP_ENABLED = "Mipmap Enabled";
        private static readonly string GUI_NAME_ANISO_LEVEL = "Aniso Level";
        private static readonly string GUI_NAME_WIDTH = "Width";
        private static readonly string GUI_NAME_HEIGHT = "Height";
        private static readonly string GUI_NAME_DEPTH = "Depth";
        private static readonly string GUI_NAME_IS_TEXTURE_SLICES_DISPLAYED = "Texture Slices";
        private static readonly string GUI_NAME_TEXTURE_SLICE_ADD = "Add Slice";
        private static readonly string GUI_NAME_TEXTURE_SLICE_REMOVE = "Remove Slice";
        private static readonly string GUI_NAME_TEXTURE_SLICE_TEXTURE = "Unassigned Slice";

        [System.NonSerialized] private bool isTexture3DSettingsDisplayed = true;
        [System.NonSerialized] public Texture3DSettings texture3DSettings;

        [System.NonSerialized] private bool isTextureSlicesDisplayed = true;
        [System.NonSerialized] private Vector2 textureSliceScrollPosition;
        [System.NonSerialized] public List<Texture2D> textureSlices;
        [System.NonSerialized] public List<Color[]> textureSlicesData;
        [System.NonSerialized] private Texture3DEditorError error;

        private void OnGUI()
        {
            // Display any errors at the top of our GUI.
            // Error will have one frame of latency.
            ErrorDisplay();

            if (ErrorCapture(ComputeAndDisplayTexture3DSettings(ref texture3DSettings)))
            {
                return;
            }

            if (ErrorCapture(ComputeAndDisplayTextureSlices(ref textureSlices, ref textureSlicesData)))
            {
                return;
            }

            if (GUILayout.Button(SAVE_BUTTON_NAME))
            {
                Texture3D texture3D = null;
                if (ErrorCapture(Texture3DEditor.ComputeTexture3DFromTextureSlices(ref texture3D, textureSlices, textureSlicesData, texture3DSettings)))
                {
                    return;
                }

                if (ErrorCapture(Texture3DEditor.SaveAssetFromTexture3D(texture3DSettings, texture3D)))
                {
                    return;
                }
            }
        }

        private void ErrorDisplay()
        {
            if (error != Texture3DEditorError.None)
            {
                EditorGUILayout.HelpBox(texture3DEditorErrorDescriptions[(uint)error], MessageType.Info);
            }

            // Error has been displayed to user.
            // Clear error to allow user an opportunity to make changes.
            error = Texture3DEditorError.None;
        }

        private bool ErrorCapture(Texture3DEditorError errorNext)
        {
            error = (error == Texture3DEditorError.None) ? errorNext : error;
            return !(error == Texture3DEditorError.None);
        }

        private static int SanitizeResolution(int x)
        {
            // Enforce power of two resolutions in range [2, 64]
            int res = Mathf.Max(x, 2);
            res = Mathf.NextPowerOfTwo(res);
            res = Mathf.Min(res, 64);
            return res;
        }

        private Texture3DEditorError ComputeAndDisplayTexture3DSettings(ref Texture3DSettings res)
        {
            isTexture3DSettingsDisplayed = EditorGUILayout.Foldout(isTexture3DSettingsDisplayed, GUI_NAME_IS_TEXTURE_3D_SETTINGS_DISPLAYED);
            if (isTexture3DSettingsDisplayed)
            {
                EditorGUILayout.BeginVertical();
                ++EditorGUI.indentLevel;

                res.name = EditorGUILayout.TextField(GUI_NAME_NAME, res.name);
                res.resampleMode = (ResampleMode)EditorGUILayout.Popup(GUI_NAME_RESAMPLE_MODE, (int)res.resampleMode, RESAMPLE_MODE_NAMES);
                res.textureFormatValid = (TextureFormatValid)EditorGUILayout.Popup(GUI_NAME_TEXTURE_FORMAT_VALID, (int)res.textureFormatValid, TEXTURE_FORMAT_VALID_NAMES);
                res.filterModeValid = (FilterModeValid)EditorGUILayout.Popup(GUI_NAME_FILTER_MODE_VALID, (int)res.filterModeValid, FILTER_MODE_VALID_NAMES);
                res.textureWrapModeValid = (TextureWrapModeValid)EditorGUILayout.Popup(GUI_NAME_WRAP_MODE_VALID, (int)res.textureWrapModeValid, TEXTURE_WRAP_MODE_VALID_NAMES);

                res.isMipmapEnabled = EditorGUILayout.Toggle(GUI_NAME_IS_MIPMAP_ENABLED, res.isMipmapEnabled);
                res.anisoLevel = EditorGUILayout.IntSlider(GUI_NAME_ANISO_LEVEL, res.anisoLevel, 1, 9);
                res.width = Texture3DEditor.SanitizeResolution(EditorGUILayout.IntField(GUI_NAME_WIDTH, res.width));
                res.height = Texture3DEditor.SanitizeResolution(EditorGUILayout.IntField(GUI_NAME_HEIGHT, res.height));
                res.depth = Texture3DEditor.SanitizeResolution(EditorGUILayout.IntField(GUI_NAME_DEPTH, res.depth));

                --EditorGUI.indentLevel;
                EditorGUILayout.EndVertical();
            }

            return Texture3DEditorError.None;
        }

        private Texture3DEditorError ComputeAndDisplayTextureSlices(ref List<Texture2D> slices, ref List<Color[]> slicesData)
        {
            if (slices == null) { slices = new List<Texture2D>(); }
            if (slicesData == null) { slicesData = new List<Color[]>(); }

            Texture3DEditorError error = Texture3DEditorError.None;

            isTextureSlicesDisplayed = EditorGUILayout.Foldout(isTextureSlicesDisplayed, GUI_NAME_IS_TEXTURE_SLICES_DISPLAYED);
            if (isTextureSlicesDisplayed)
            {
                EditorGUILayout.BeginVertical();
                ++EditorGUI.indentLevel;

                // Place slice add button outside of scroll view to make it always visible regardless of scroll position.
                if (GUILayout.Button(GUI_NAME_TEXTURE_SLICE_ADD))
                {
                    slices.Add(null);
                    slicesData.Add(null);
                }

                textureSliceScrollPosition = EditorGUILayout.BeginScrollView(textureSliceScrollPosition);
                {
                    int indexRemoval = -1;
                    for (int i = 0, iLen = slices.Count; i < iLen; ++i)
                    {
                        ++EditorGUI.indentLevel;
                        EditorGUILayout.BeginHorizontal();

                        // TODO: Creating garbage with this int to string conversion.
                        // Could create a static readonly string lut given that iLen is <= 64.
                        EditorGUILayout.SelectableLabel(i.ToString());

                        if (GUILayout.Button(GUI_NAME_TEXTURE_SLICE_REMOVE)) { indexRemoval = i; }

                        Texture2D slicePrevious = slices[i];
                        slices[i] = (Texture2D)EditorGUILayout.ObjectField((slices[i] != null) ? slices[i].name : GUI_NAME_TEXTURE_SLICE_TEXTURE, slices[i], typeof(Texture2D), true);
                        if (slices[i] != null && slices[i] != slicePrevious)
                        {
                            // Different texture was assigned. Grab the pixel data for use in resampling.
                            slicesData[i] = slices[i].GetPixels(0, 0, slices[i].width, slices[i].height);
                        }

                        // Texture2D slice source must be readable in order to access data via GetPixel functions.
                        // TODO: 18.3 adds Texture2D.isReadable flag which will allow us to report error to user.
                        // if (slices[i] != null && !(slices[i].isReadable))
                        // {
                        //     error = (error != Texture3DEditorError.None) ? Texture3DEditorError.SliceUnreadable : error;
                        // }

                        EditorGUILayout.EndHorizontal();
                        --EditorGUI.indentLevel;
                    }
                    if (indexRemoval >= 0)
                    {
                        slices.RemoveAt(indexRemoval);
                        slicesData.RemoveAt(indexRemoval);
                    }
                }

                EditorGUILayout.EndScrollView();
                --EditorGUI.indentLevel;
                EditorGUILayout.EndVertical();
            }

            return error;
        }

        private static Texture3DEditorError ComputeTexture3DFromTextureSlices(ref Texture3D res, List<Texture2D> textureSlices, List<Color[]> textureSlicesData, Texture3DSettings settings)
        {
            if (textureSlices.Count == 0) { return Texture3DEditorError.NoSlicesSpecified; }
            for (int i = 0, iLen = textureSlices.Count; i < iLen; ++i)
            {
                if (textureSlices[i] == null) { return Texture3DEditorError.NoTextureAssigned; }
            }

            Color[] data = new Color[settings.width * settings.height * settings.depth];
            res = new Texture3D(
                settings.width,
                settings.height,
                settings.depth,
                Texture3DEditor.GetTextureFormat(settings.textureFormatValid),
                settings.isMipmapEnabled
            );
            res.wrapMode = Texture3DEditor.GetTextureWrapMode(settings.textureWrapModeValid);
            res.filterMode = Texture3DEditor.GetTextureFilterMode(settings.filterModeValid);
            res.anisoLevel = settings.anisoLevel;

            Debug.Assert((int)settings.resampleMode >= 0 && settings.resampleMode < ResampleMode.Count);
            Texture3DEditorError error = Texture3DEditorError.None;
            switch (settings.resampleMode)
            {
                case ResampleMode.Nearest:
                    error = ResampleNearest(ref data, textureSlices, textureSlicesData, settings);
                    break;

                case ResampleMode.Trilinear:
                    error = ResampleTrilinear(ref data, textureSlices, textureSlicesData, settings);
                    break;

                default:
                    break;
            }
            if (error != Texture3DEditorError.None) { return error; }
            res.SetPixels(data, 0);
            res.Apply(settings.isMipmapEnabled);
            return Texture3DEditorError.None;
        }

        private static Texture3DEditorError ResampleNearest(ref Color[] data, List<Texture2D> textureSlices, List<Color[]> textureSlicesData, Texture3DSettings settings)
        {
            float wScale = 1.0f / (float)settings.depth;
            float wBias = 0.5f / (float)settings.depth;

            // When computing the slice indices that straddle our interpolation interval we need to subtract off 0.5 pixels,
            // as interpolation happens between pixel centers, i.e: 0.5 and 1.5 pixels.
            float uScale = 1.0f / (float)settings.width;
            float uBias = 0.5f / (float)settings.width;
            float vScale = 1.0f / (float)settings.height;
            float vBias = 0.5f / (float)settings.height;
            float ziScale = (float)textureSlices.Count;
            float ziBias = -0.5f;
            for (int z = 0; z < settings.depth; ++z)
            {
                float w = (float)z * wScale + wBias;
                float wSamplePositonSlices = w * ziScale + ziBias;
                float wSampleInterpolation = Frac(wSamplePositonSlices);
                int iz0 = Mathf.FloorToInt(wSamplePositonSlices);
                int iz1 = iz0 + 1;

                switch (settings.textureWrapModeValid)
                {
                    case TextureWrapModeValid.Repeat:
                        iz0 = PixelWrap(iz0, textureSlices.Count);
                        iz1 = PixelWrap(iz1, textureSlices.Count);
                        break;
                    case TextureWrapModeValid.Clamp:
                    default:
                        iz0 = PixelClamp(iz0, textureSlices.Count);
                        iz1 = PixelClamp(iz1, textureSlices.Count);
                        break;
                }

                Texture2D textureSlice0 = textureSlices[iz0];
                Texture2D textureSlice1 = textureSlices[iz1];

                Color[] textureSliceData0 = textureSlicesData[iz0];
                Color[] textureSliceData1 = textureSlicesData[iz1];

                for (int y = 0; y < settings.height; ++y)
                {
                    for (int x = 0; x < settings.width; ++x)
                    {
                        int i = z * settings.height * settings.width + y * settings.width + x;
                        float u = (float)x * uScale + uBias;
                        float v = (float)y * vScale + vBias;

                        float px0 = u * (float)textureSlice0.width;
                        float py0 = v * (float)textureSlice0.height;
                        float px1 = u * (float)textureSlice1.width;
                        float py1 = v * (float)textureSlice1.height;

                        int pxFloor0 = Mathf.FloorToInt(px0 - 0.5f);
                        int pyFloor0 = Mathf.FloorToInt(py0 - 0.5f);
                        int pxFloor1 = Mathf.FloorToInt(px1 - 0.5f);
                        int pyFloor1 = Mathf.FloorToInt(py1 - 0.5f);

                        switch (settings.textureWrapModeValid)
                        {
                            case TextureWrapModeValid.Repeat:
                                pxFloor0 = PixelWrap(pxFloor0, textureSlice0.width);
                                pyFloor0 = PixelWrap(pyFloor0, textureSlice0.height);
                                pxFloor1 = PixelWrap(pxFloor1, textureSlice1.width);
                                pyFloor1 = PixelWrap(pyFloor1, textureSlice1.height);
                                break;
                            case TextureWrapModeValid.Clamp:
                                pxFloor0 = PixelClamp(pxFloor0, textureSlice0.width);
                                pyFloor0 = PixelClamp(pyFloor0, textureSlice0.height);
                                pxFloor1 = PixelClamp(pxFloor1, textureSlice1.width);
                                pyFloor1 = PixelClamp(pyFloor1, textureSlice1.height);
                                break;
                        }

                        int i0 = pyFloor0 * textureSlice0.width + pxFloor0;
                        int i1 = pyFloor1 * textureSlice1.width + pyFloor1;

                        data[i] = Color.Lerp(textureSliceData0[i0], textureSliceData0[i1], wSampleInterpolation);
                    }
                }
            }

            return Texture3DEditorError.None;
        }

        private static Texture3DEditorError ResampleTrilinear(ref Color[] data, List<Texture2D> textureSlices, List<Color[]> textureSlicesData, Texture3DSettings settings)
        {
            float wScale = 1.0f / (float)settings.depth;
            float wBias = 0.5f / (float)settings.depth;

            // When computing the slice indices that straddle our interpolation interval we need to subtract off 0.5 pixels,
            // as interpolation happens between pixel centers, i.e: 0.5 and 1.5 pixels.
            float uScale = 1.0f / (float)settings.width;
            float uBias = 0.5f / (float)settings.width;
            float vScale = 1.0f / (float)settings.height;
            float vBias = 0.5f / (float)settings.height;
            float ziScale = (float)textureSlices.Count;
            float ziBias = -0.5f;
            for (int z = 0; z < settings.depth; ++z)
            {
                float w = (float)z * wScale + wBias;
                float wSamplePositonSlices = w * ziScale + ziBias;
                float wSampleInterpolation = Frac(wSamplePositonSlices);
                int iz0 = Mathf.FloorToInt(wSamplePositonSlices);
                int iz1 = iz0 + 1;

                switch (settings.textureWrapModeValid)
                {
                    case TextureWrapModeValid.Repeat:
                        iz0 = PixelWrap(iz0, textureSlices.Count);
                        iz1 = PixelWrap(iz1, textureSlices.Count);
                        break;
                    case TextureWrapModeValid.Clamp:
                    default:
                        iz0 = PixelClamp(iz0, textureSlices.Count);
                        iz1 = PixelClamp(iz1, textureSlices.Count);
                        break;
                }

                Texture2D textureSlice0 = textureSlices[iz0];
                Texture2D textureSlice1 = textureSlices[iz1];

                Color[] textureSliceData0 = textureSlicesData[iz0];
                Color[] textureSliceData1 = textureSlicesData[iz1];

                for (int y = 0; y < settings.height; ++y)
                {
                    for (int x = 0; x < settings.width; ++x)
                    {
                        int i = z * settings.height * settings.width + y * settings.width + x;
                        float u = (float)x * uScale + uBias;
                        float v = (float)y * vScale + vBias;

                        float px0 = u * (float)textureSlice0.width;
                        float py0 = v * (float)textureSlice0.height;
                        float px1 = u * (float)textureSlice1.width;
                        float py1 = v * (float)textureSlice1.height;

                        int pxFloor0 = Mathf.FloorToInt(px0 - 0.5f);
                        int pyFloor0 = Mathf.FloorToInt(py0 - 0.5f);
                        int pxFloor1 = Mathf.FloorToInt(px1 - 0.5f);
                        int pyFloor1 = Mathf.FloorToInt(py1 - 0.5f);

                        float ax0 = px0 - ((float)pxFloor0 + 0.5f);
                        float ay0 = py0 - ((float)pyFloor0 + 0.5f);
                        float ax1 = px1 - ((float)pxFloor1 + 0.5f);
                        float ay1 = py1 - ((float)pyFloor1 + 0.5f);

                        int pxCeil0 = pxFloor0 + 1;
                        int pyCeil0 = pyFloor0 + 1;
                        int pxCeil1 = pxFloor1 + 1;
                        int pyCeil1 = pyFloor1 + 1;

                        switch (settings.textureWrapModeValid)
                        {
                            case TextureWrapModeValid.Repeat:
                                pxFloor0 = PixelWrap(pxFloor0, textureSlice0.width);
                                pyFloor0 = PixelWrap(pyFloor0, textureSlice0.height);
                                pxFloor1 = PixelWrap(pxFloor1, textureSlice1.width);
                                pyFloor1 = PixelWrap(pyFloor1, textureSlice1.height);

                                pxCeil0 = PixelWrap(pxCeil0, textureSlice0.width);
                                pyCeil0 = PixelWrap(pyCeil0, textureSlice0.height);
                                pxCeil1 = PixelWrap(pxCeil1, textureSlice1.width);
                                pyCeil1 = PixelWrap(pyCeil1, textureSlice1.height);
                                break;
                            case TextureWrapModeValid.Clamp:
                                pxFloor0 = PixelClamp(pxFloor0, textureSlice0.width);
                                pyFloor0 = PixelClamp(pyFloor0, textureSlice0.height);
                                pxFloor1 = PixelClamp(pxFloor1, textureSlice1.width);
                                pyFloor1 = PixelClamp(pyFloor1, textureSlice1.height);

                                pxCeil0 = PixelClamp(pxCeil0, textureSlice0.width);
                                pyCeil0 = PixelClamp(pyCeil0, textureSlice0.height);
                                pxCeil1 = PixelClamp(pxCeil1, textureSlice1.width);
                                pyCeil1 = PixelClamp(pyCeil1, textureSlice1.height);
                                break;
                        }

                        int inw0 = pyCeil0 * textureSlice0.width + pxFloor0;
                        int ine0 = pyCeil0 * textureSlice0.width + pxCeil0;
                        int isw0 = pyFloor0 * textureSlice0.width + pxFloor0;
                        int ise0 = pyFloor0 * textureSlice0.width + pxCeil0;
                        Color cn0 = Color.Lerp(textureSliceData0[inw0], textureSliceData0[ine0], ax0);
                        Color cs0 = Color.Lerp(textureSliceData0[isw0], textureSliceData0[ise0], ax0);
                        Color c0 = Color.Lerp(cs0, cn0, ay0);

                        int inw1 = pyCeil1 * textureSlice1.width + pxFloor1;
                        int ine1 = pyCeil1 * textureSlice1.width + pxCeil1;
                        int isw1 = pyFloor1 * textureSlice1.width + pxFloor1;
                        int ise1 = pyFloor1 * textureSlice1.width + pxCeil1;
                        Color cn1 = Color.Lerp(textureSliceData1[inw1], textureSliceData1[ine1], ax1);
                        Color cs1 = Color.Lerp(textureSliceData1[isw1], textureSliceData1[ise1], ax1);
                        Color c1 = Color.Lerp(cs1, cn1, ay1);

                        data[i] = Color.Lerp(c0, c1, wSampleInterpolation);
                    }
                }
            }

            return Texture3DEditorError.None;
        }

        private static Texture3DEditorError SaveAssetFromTexture3D(Texture3DSettings settings, Texture3D texture3D)
        {
            string filename = (settings.name.Length > 0) ? (settings.name + "." + FILE_EXTENSION) : DEFAULT_FILENAME;

            // Returns path relative to project base directory.
            // Required for AssetDatabase.CreateAsset() which expects paths relative to project base.
            string path = EditorUtility.SaveFilePanelInProject(SAVE_DIALOGUE, filename, FILE_EXTENSION, "");
            if (path.Length == 0) { return Texture3DEditorError.NoSavePathSpecified; }

            AssetDatabase.CreateAsset(texture3D, path);

            return Texture3DEditorError.None;
        }

        private static float Frac(float x)
        {
            return x - Mathf.Floor(x);
        }

        private static int PixelWrap(int pixelPosition, int resolution)
        {
            Debug.Assert(pixelPosition > -resolution && pixelPosition < (resolution * 2));

            return (pixelPosition < 0)
                ? (pixelPosition + resolution)
                : ((pixelPosition >= resolution)
                    ? (pixelPosition - resolution)
                    : pixelPosition
                );
        }

        private static int PixelClamp(int pixelPosition, int resolution)
        {
            return Mathf.Clamp(pixelPosition, 0, resolution - 1);
        }
    }
}