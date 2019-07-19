using UnityEngine;
using System;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;

namespace UnityEditor.Rendering.LookDev
{
    /// <summary>
    /// Lighting environment used in LookDev
    /// </summary>
    public class Environment : ScriptableObject
    {
        [Serializable]
        public abstract class BaseEnvironmentCubemapHandler
        {
            [SerializeField]
            string m_CubemapGUID;
            Cubemap m_Cubemap;

            /// <summary>
            /// The cubemap used for this part of the lighting environment
            /// </summary>
            public Cubemap cubemap
            {
                get
                {
                    if (m_Cubemap == null || m_Cubemap.Equals(null))
                        LoadCubemap();
                    return m_Cubemap;
                }
                set
                {
                    m_Cubemap = value;
                    m_CubemapGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_Cubemap));
                }
            }

            void LoadCubemap()
            {
                m_Cubemap = null;

                GUID storedGUID;
                GUID.TryParse(m_CubemapGUID, out storedGUID);
                if (!storedGUID.Empty())
                {
                    string path = AssetDatabase.GUIDToAssetPath(m_CubemapGUID);
                    m_Cubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
                }
            }
        }

        /// <summary>
        /// Class containing editor data for shadow part of the lighting environment 
        /// </summary>
        [Serializable]
        public class Shadow : BaseEnvironmentCubemapHandler
        {
            // Setup default position to be on the sun in the default HDRI.
            // This is important as the defaultHDRI don't call the set brightest spot function on first call.
            [SerializeField]
            float m_Latitude = 60.0f; // [-90..90]
            [SerializeField]
            float m_Longitude = 299.0f; // [0..360[

            /// <summary>
            /// The shading tint to used when computing shadow from sun
            /// </summary>
            public Color color = Color.white;

            /// <summary>
            /// The Latitude position of the sun casting shadows
            /// </summary>
            public float sunLatitude
            {
                get => m_Latitude;
                set => m_Latitude = ClampLatitude(value);
            }

            internal static float ClampLatitude(float value) => Mathf.Clamp(value, -90, 90);

            /// <summary>
            /// The Longitude position of the sun casting shadows
            /// </summary>
            public float sunLongitude
            {
                get => m_Longitude;
                set => m_Longitude = ClampLongitude(value);
            }

            internal static float ClampLongitude(float value)
            {
                value = value % 360f;
                if (value < 0.0)
                    value += 360f;
                return value;
            }
        }

        /// <summary>
        /// Class containing editor data for sky part of the lighting environment 
        /// </summary>
        [Serializable]
        public class Sky : BaseEnvironmentCubemapHandler
        {
            /// <summary>
            /// Offset on the longitude. Affect both sky and sun position in Shadow part
            /// </summary>
            public float rotation = 0.0f;
            /// <summary>
            /// Exposure to use with this Sky
            /// </summary>
            public float exposure = 1f;

            /// <summary>
            /// Implicit conversion operator to runtime version of sky datas
            /// </summary>
            /// <param name="sky">Editor version of the datas</param>
            public static implicit operator UnityEngine.Rendering.LookDev.Sky(Sky sky)
                => sky == null
                ? default
                : new UnityEngine.Rendering.LookDev.Sky()
                {
                    cubemap = sky.cubemap,
                    longitudeOffset = sky.rotation,
                    exposure = sky.exposure
                };
        }

        /// <summary>
        /// The sky part of the lighting environment 
        /// </summary>
        public Sky sky = new Sky();

        /// <summary>
        /// The shadow part of the lighting environment 
        /// </summary>
        public Shadow shadow = new Shadow();

        /// <summary>
        /// Compute the shadow runtime data with editor datas
        /// </summary>
        public UnityEngine.Rendering.LookDev.Sky shadowSky
            => new UnityEngine.Rendering.LookDev.Sky()
            {
                cubemap = shadow.cubemap ?? sky.cubemap,
                longitudeOffset = sky.rotation,
                exposure = sky.exposure
            };

        internal float shadowIntensity
            => shadow.cubemap == null ? 0.3f : 1f;

        internal void UpdateSunPosition(Light sun)
            => sun.transform.rotation = Quaternion.Euler(shadow.sunLatitude, sky.rotation + shadow.sunLongitude, 0f);

        internal void CopyTo(Environment other)
        {
            other.sky.cubemap = sky.cubemap;
            other.sky.exposure = sky.exposure;
            other.sky.rotation = sky.rotation;
            other.shadow.cubemap = shadow.cubemap;
            other.shadow.sunLatitude = shadow.sunLatitude;
            other.shadow.sunLongitude = shadow.sunLongitude;
            other.shadow.color = shadow.color;
            other.name = name + " (copy)";
        }

        /// <summary>
        /// Compute sun position to be brightest spot of the sky
        /// </summary>
        public void ResetToBrightestSpot()
            => EnvironmentElement.ResetToBrightestSpot(this);
    }

    [CustomEditor(typeof(Environment))]
    class EnvironmentEditor : Editor
    {
        //display nothing
        public sealed override VisualElement CreateInspectorGUI() => null;

        // Don't use ImGUI
        public sealed override void OnInspectorGUI() { }

        //but make preview in Project window
        override public Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
            => EnvironmentElement.GetLatLongThumbnailTexture(target as Environment, width);
    }

    interface IBendable<T>
    {
        void Bind(T data);
    }

    class EnvironmentElement : VisualElement, IBendable<Environment>
    {
        internal const int k_SkyThumbnailWidth = 200;
        internal const int k_SkyThumbnailHeight = 100;
        const int k_SkadowThumbnailWidth = 60;
        const int k_SkadowThumbnailHeight = 30;
        const int k_SkadowThumbnailXPosition = 130;
        const int k_SkadowThumbnailYPosition = 10;
        static Material s_cubeToLatlongMaterial;
        static Material cubeToLatlongMaterial
        {
            get
            {
                if (s_cubeToLatlongMaterial == null || s_cubeToLatlongMaterial.Equals(null))
                {
                    s_cubeToLatlongMaterial = new Material(Shader.Find("Hidden/LookDev/CubeToLatlong"));
                }
                return s_cubeToLatlongMaterial;
            }
        }

        VisualElement environmentParams;
        Environment environment;

        Image latlong;
        ObjectField skyCubemapField;
        FloatField skyRotationOffset;
        FloatField skyExposureField;
        ObjectField shadowCubemapField;
        Vector2Field sunPosition;
        ColorField shadowColor;
        TextField environmentName;

        Action OnChangeCallback;

        public Environment target => environment;

        public EnvironmentElement() => Create(withPreview: true);
        public EnvironmentElement(bool withPreview, Action OnChangeCallback = null)
        {
            this.OnChangeCallback = OnChangeCallback;
            Create(withPreview);
        }

        public EnvironmentElement(Environment environment)
        {
            Create(withPreview: true);
            Bind(environment);
        }

        void Create(bool withPreview)
        {
            if (withPreview)
            {
                latlong = new Image();
                latlong.style.width = k_SkyThumbnailWidth;
                latlong.style.height = k_SkyThumbnailHeight;
                Add(latlong);
            }

            environmentParams = GetDefaultInspector();
            Add(environmentParams);
        }

        public void Bind(Environment environment)
        {
            this.environment = environment;
            if (environment == null || environment.Equals(null))
                return;

            if (latlong != null && !latlong.Equals(null))
                latlong.image = GetLatLongThumbnailTexture();
            skyCubemapField.SetValueWithoutNotify(environment.sky.cubemap);
            skyRotationOffset.SetValueWithoutNotify(environment.sky.rotation);
            skyExposureField.SetValueWithoutNotify(environment.sky.exposure);
            shadowCubemapField.SetValueWithoutNotify(environment.shadow.cubemap);
            sunPosition.SetValueWithoutNotify(new Vector2(environment.shadow.sunLongitude, environment.shadow.sunLatitude));
            shadowColor.SetValueWithoutNotify(environment.shadow.color);
            environmentName.SetValueWithoutNotify(environment.name);
        }

        public void Bind(Environment environment, Image deportedLatlong)
        {
            latlong = deportedLatlong;
            Bind(environment);
        }

        static public Vector2 PositionToLatLong(Vector2 position)
        {
            Vector2 result = new Vector2();
            result.x = position.y * Mathf.PI * 0.5f * Mathf.Rad2Deg;
            result.y = (position.x * 0.5f + 0.5f) * 2f * Mathf.PI * Mathf.Rad2Deg;

            if (result.x < -90.0f) result.x = -90f;
            if (result.x > 90.0f) result.x = 90f;

            return result;
        }

        public static void ResetToBrightestSpot(Environment environment)
        {
            cubeToLatlongMaterial.SetTexture("_MainTex", environment.sky.cubemap);
            cubeToLatlongMaterial.SetVector("_WindowParams", new Vector4(10000, -1000.0f, 2, 0.0f)); // Neutral value to not clip
            cubeToLatlongMaterial.SetVector("_CubeToLatLongParams", new Vector4(Mathf.Deg2Rad * environment.sky.rotation, 0.5f, 1.0f, 3.0f)); // We use LOD 3 to take a region rather than a single pixel in the map
            cubeToLatlongMaterial.SetPass(0);

            int width = k_SkyThumbnailWidth;
            int height = width >> 1;

            RenderTexture temporaryRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Texture2D brightestPointTexture = new Texture2D(width, height, TextureFormat.RGBAHalf, false);

            // Convert cubemap to a 2D LatLong to read on CPU
            Graphics.Blit(environment.sky.cubemap, temporaryRT, cubeToLatlongMaterial);
            brightestPointTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            brightestPointTexture.Apply();

            // CPU read back
            // From Doc: The returned array is a flattened 2D array, where pixels are laid out left to right, bottom to top (i.e. row after row)
            Color[] color = brightestPointTexture.GetPixels();
            RenderTexture.active = null;
            temporaryRT.Release();

            float maxLuminance = 0.0f;
            int maxIndex = 0;
            for (int index = height * width - 1; index >= 0; --index)
            {
                Color pixel = color[index];
                float luminance = pixel.r * 0.2126729f + pixel.g * 0.7151522f + pixel.b * 0.0721750f;
                if (maxLuminance < luminance)
                {
                    maxLuminance = luminance;
                    maxIndex = index;
                }
            }
            Vector2 sunPosition = PositionToLatLong(new Vector2(((maxIndex % width) / (float)(width - 1)) * 2f - 1f, ((maxIndex / width) / (float)(height - 1)) * 2f - 1f));
            environment.shadow.sunLatitude = sunPosition.x;
            environment.shadow.sunLongitude = sunPosition.y - environment.sky.rotation;
        }

        public Texture2D GetLatLongThumbnailTexture()
            => GetLatLongThumbnailTexture(environment, k_SkyThumbnailWidth);

        public static Texture2D GetLatLongThumbnailTexture(Environment environment, int width)
        {
            int height = width >> 1;
            RenderTexture oldActive = RenderTexture.active;
            RenderTexture temporaryRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture.active = temporaryRT;
            cubeToLatlongMaterial.SetTexture("_MainTex", environment.sky.cubemap);
            cubeToLatlongMaterial.SetVector("_WindowParams",
                new Vector4(
                    height, //height
                    -1000f, //y position, -1000f to be sure to not have clipping issue (we should not clip normally but don't want to create a new shader)
                    2f,     //margin value
                    1f));   //Pixel per Point
            cubeToLatlongMaterial.SetVector("_CubeToLatLongParams",
                new Vector4(
                    Mathf.Deg2Rad * environment.sky.rotation,    //rotation of the environment in radian
                    1f,     //alpha
                    1f,     //intensity
                    0f));   //LOD
            cubeToLatlongMaterial.SetPass(0);
            GL.LoadPixelMatrix(0, width, height, 0);
            GL.Clear(true, true, Color.black);
            Rect skyRect = new Rect(0, 0, width, height);
            Renderer.DrawFullScreenQuad(skyRect);

            if (environment.shadow.cubemap != null)
            {
                cubeToLatlongMaterial.SetTexture("_MainTex", environment.shadow.cubemap);
                cubeToLatlongMaterial.SetVector("_WindowParams",
                    new Vector4(
                        height, //height
                        -1000f, //y position, -1000f to be sure to not have clipping issue (we should not clip normally but don't want to create a new shader)
                        2f,      //margin value
                        1f));   //Pixel per Point
                cubeToLatlongMaterial.SetVector("_CubeToLatLongParams",
                    new Vector4(
                        Mathf.Deg2Rad * environment.sky.rotation,    //rotation of the environment in radian
                        1f,   //alpha
                        0.3f,   //intensity
                        0f));   //LOD
                cubeToLatlongMaterial.SetPass(0);
                int shadowWidth = (int)(width * (k_SkadowThumbnailWidth / (float)k_SkyThumbnailWidth));
                int shadowXPosition = (int)(width * (k_SkadowThumbnailXPosition / (float)k_SkyThumbnailWidth));
                int shadowYPosition = (int)(width * (k_SkadowThumbnailYPosition / (float)k_SkyThumbnailWidth));
                Rect shadowRect = new Rect(
                    shadowXPosition,
                    shadowYPosition,
                    shadowWidth,
                    shadowWidth >> 1);
                Renderer.DrawFullScreenQuad(shadowRect);
            }

            Texture2D result = new Texture2D(width, height, TextureFormat.ARGB32, false);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            result.Apply(false);
            RenderTexture.active = oldActive;
            UnityEngine.Object.DestroyImmediate(temporaryRT);
            return result;
        }

        public VisualElement GetDefaultInspector()
        {
            VisualElement inspector = new VisualElement() { name = "inspector" };

            VisualElement header = new VisualElement() { name = "inspector-header" };
            header.Add(new Image()
            {
                image = CoreEditorUtils.LoadIcon(@"Packages/com.unity.render-pipelines.core/Editor/LookDev/Icons/", "LookDev_EnvironmentHDR", forceLowRes: true)
            });
            environmentName = new TextField();
            environmentName.isDelayed = true;
            environmentName.RegisterValueChangedCallback(evt =>
            {
                string path = AssetDatabase.GetAssetPath(environment);
                environment.name = evt.newValue;
                AssetDatabase.SetLabels(environment, new string[] { evt.newValue });
                EditorUtility.SetDirty(environment);
                AssetDatabase.ImportAsset(path);
                environmentName.name = environment.name;
            });
            header.Add(environmentName);
            inspector.Add(header);

            Foldout foldout = new Foldout()
            {
                text = "Environment Settings"
            };
            skyCubemapField = new ObjectField("Sky with Sun")
            {
                tooltip = "A cubemap that will be used as the sky."
            };
            skyCubemapField.allowSceneObjects = false;
            skyCubemapField.objectType = typeof(Cubemap);
            skyCubemapField.RegisterValueChangedCallback(evt =>
            {
                var tmp = environment.sky.cubemap;
                RegisterChange(ref tmp, evt.newValue as Cubemap);
                environment.sky.cubemap = tmp;
                latlong.image = GetLatLongThumbnailTexture(environment, k_SkyThumbnailWidth);
            });
            foldout.Add(skyCubemapField);

            shadowCubemapField = new ObjectField("Sky without Sun")
            {
                tooltip = "[Optional] A cubemap that will be used to compute self shadowing.\nIt should be the same sky without the sun.\nIf nothing is provided, the sky with sun will be used with lower intensity."
            };
            shadowCubemapField.allowSceneObjects = false;
            shadowCubemapField.objectType = typeof(Cubemap);
            shadowCubemapField.RegisterValueChangedCallback(evt =>
            {
                var tmp = environment.shadow.cubemap;
                RegisterChange(ref tmp, evt.newValue as Cubemap);
                environment.shadow.cubemap = tmp;
                latlong.image = GetLatLongThumbnailTexture(environment, k_SkyThumbnailWidth);
            });
            foldout.Add(shadowCubemapField);

            skyRotationOffset = new FloatField("Rotation")
            {
                tooltip = "Rotation offset on the longitude of the sky."
            };
            skyRotationOffset.RegisterValueChangedCallback(evt
                => RegisterChange(ref environment.sky.rotation, Environment.Shadow.ClampLongitude(evt.newValue), skyRotationOffset, updatePreview: true));
            foldout.Add(skyRotationOffset);

            skyExposureField = new FloatField("Exposure")
            {
                tooltip = "The exposure to apply with this sky."
            };
            skyExposureField.RegisterValueChangedCallback(evt
                => RegisterChange(ref environment.sky.exposure, evt.newValue));
            foldout.Add(skyExposureField);
            var style = foldout.Q<Toggle>().style;
            style.marginLeft = 3;
            style.unityFontStyleAndWeight = FontStyle.Bold;
            inspector.Add(foldout);

            sunPosition = new Vector2Field("Sun Position")
            {
                tooltip = "The sun position as (Longitude, Latitude)\nThe button compute brightest position in the sky with sun."
            };
            sunPosition.Q("unity-x-input").Q<FloatField>().formatString = "n1";
            sunPosition.Q("unity-y-input").Q<FloatField>().formatString = "n1";
            sunPosition.RegisterValueChangedCallback(evt =>
            {
                var tmpContainer = new Vector2(
                    environment.shadow.sunLongitude,
                    environment.shadow.sunLatitude);
                var tmpNewValue = new Vector2(
                    Environment.Shadow.ClampLongitude(evt.newValue.x),
                    Environment.Shadow.ClampLatitude(evt.newValue.y));
                RegisterChange(ref tmpContainer, tmpNewValue, sunPosition);
                environment.shadow.sunLongitude = tmpContainer.x;
                environment.shadow.sunLatitude = tmpContainer.y;
            });
            foldout.Add(sunPosition);

            Button sunToBrightess = new Button(() =>
            {
                ResetToBrightestSpot(environment);
                sunPosition.SetValueWithoutNotify(new Vector2(
                    Environment.Shadow.ClampLongitude(environment.shadow.sunLongitude),
                    Environment.Shadow.ClampLatitude(environment.shadow.sunLatitude)));
            })
            {
                name = "sunToBrightestButton"
            };
            sunToBrightess.Add(new Image()
            {
                image = CoreEditorUtils.LoadIcon(@"Packages/com.unity.render-pipelines.core/Editor/LookDev/Icons/", "LookDev_SunPosition", forceLowRes: true)
            });
            sunToBrightess.AddToClassList("sun-to-brightest-button");
            var vector2Input = sunPosition.Q(className: "unity-vector2-field__input");
            vector2Input.Remove(sunPosition.Q(className: "unity-composite-field__field-spacer"));
            vector2Input.Add(sunToBrightess);

            shadowColor = new ColorField("Shadow Tint")
            {
                tooltip = "The wanted shadow tint to be used when computing shadow."
            };
            shadowColor.RegisterValueChangedCallback(evt
                => RegisterChange(ref environment.shadow.color, evt.newValue));
            foldout.Add(shadowColor);

            style = foldout.Q<Toggle>().style;
            style.marginLeft = 3;
            style.unityFontStyleAndWeight = FontStyle.Bold;
            inspector.Add(foldout);

            return inspector;
        }

        void RegisterChange<TValueType>(ref TValueType reflectedVariable, TValueType newValue, BaseField<TValueType> resyncField = null, bool updatePreview = false)
        {
            if (environment == null || environment.Equals(null))
                return;
            reflectedVariable = newValue;
            resyncField?.SetValueWithoutNotify(newValue);
            if (updatePreview && latlong != null && !latlong.Equals(null))
                latlong.image = GetLatLongThumbnailTexture(environment, k_SkyThumbnailWidth);
            EditorUtility.SetDirty(environment);
            OnChangeCallback?.Invoke();
        }
    }
}
