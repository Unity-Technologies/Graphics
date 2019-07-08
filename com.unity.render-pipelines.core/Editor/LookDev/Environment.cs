using UnityEngine;
using System;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;

namespace UnityEditor.Rendering.Experimental.LookDev
{
    //[CreateAssetMenu(fileName = "Environment", menuName = "LookDev/Environment", order = 1999)]
    public class Environment : ScriptableObject
    {
        [Serializable]
        public abstract class BaseEnvironmentCubemapHandler
        {
            [SerializeField]
            string m_CubemapGUID;
            Cubemap m_Cubemap;
            
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
        
        //[TODO: check if the shadow/sky split worth the indirection]
        //Note: multi-edition is not supported as we cannot draw multiple HDRI
        [Serializable]
        public class Shadow : BaseEnvironmentCubemapHandler
        {
            // Setup default position to be on the sun in the default HDRI.
            // This is important as the defaultHDRI don't call the set brightest spot function on first call.
            [SerializeField]
            float m_Latitude = 60.0f; // [-90..90]
            [SerializeField]
            float m_Longitude = 299.0f; // [0..360[
            //public float intensity = 1.0f;
            public Color color = Color.white;

            public float sunLatitude
            {
                get => m_Latitude;
                set => m_Latitude = ClampLatitude(value);
            }

            internal static float ClampLatitude(float value) => Mathf.Clamp(value, -90, 90);

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

            public static implicit operator UnityEngine.Rendering.Experimental.LookDev.Shadow(Shadow shadow)
                => shadow == null
                ? default
                : new UnityEngine.Rendering.Experimental.LookDev.Shadow()
                {
                    cubemap = shadow.cubemap,
                    sunPosition = new Vector2(shadow.m_Longitude, shadow.m_Latitude),
                    color = shadow.color
                };
        }

        [Serializable]
        public class Sky : BaseEnvironmentCubemapHandler
        {
            public float rotation = 0.0f;
            public float exposure = 1f;
            
            public static implicit operator UnityEngine.Rendering.Experimental.LookDev.Sky(Sky sky)
                => sky == null
                ? default
                : new UnityEngine.Rendering.Experimental.LookDev.Sky()
                {
                    cubemap = sky.cubemap,
                    longitudeOffset = sky.rotation,
                    exposure = sky.exposure
                };
        }

        public Sky sky = new Sky();
        public Shadow shadow = new Shadow();

        public UnityEngine.Rendering.Experimental.LookDev.Sky shadowSky
            => new UnityEngine.Rendering.Experimental.LookDev.Sky()
            {
                cubemap = shadow.cubemap ?? sky.cubemap,
                longitudeOffset = sky.rotation,
                exposure = sky.exposure
            };

        internal float shadowIntensity
            => shadow.cubemap == null ? 0.3f : 1f;
        
        internal void UpdateSunPosition(Light sun)
            => sun.transform.rotation = Quaternion.Euler(shadow.sunLatitude, sky.rotation + shadow.sunLongitude, 0f);

        public void CopyTo(Environment other)
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

        public void ResetToBrightestSpot()
            => EnvironmentElement.ResetToBrightestSpot(this);
    }

    [CustomEditor(typeof(Environment))]
    class EnvironmentEditor : Editor
    {
        EnvironmentElement m_EnvironmentElement;

        public sealed override VisualElement CreateInspectorGUI()
            => m_EnvironmentElement = new EnvironmentElement(target as Environment);

        // Don't use ImGUI
        public sealed override void OnInspectorGUI() { }

        override public Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
            => EnvironmentElement.GetLatLongThumbnailTexture(target as Environment, width);
    }

    interface IBendable<T>
    {
        void Bind(T data);
    }

    public class EnvironmentElement : VisualElement, IBendable<Environment>
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
        FloatSliderField skyRotationOffset;
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
            temporaryRT.Release();

            float maxLuminance = 0.0f;
            int maxIndex = 0;
            for(int index = height * width - 1; index >= 0; --index)
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
                image = CoreEditorUtils.LoadIcon(@"Packages/com.unity.render-pipelines.core/Editor/LookDev/Icons/", "LookDevSingle1")
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
            skyCubemapField = new ObjectField("Sky with Sun");
            skyCubemapField.allowSceneObjects = false;
            skyCubemapField.objectType = typeof(Cubemap);
            skyCubemapField.RegisterValueChangedCallback(evt =>
            {
                var tmp = environment.sky.cubemap;
                RegisterChange(ref tmp, evt.newValue as Cubemap, updatePreview: true);
                environment.sky.cubemap = tmp;
            });
            foldout.Add(skyCubemapField);

            shadowCubemapField = new ObjectField("Sky without Sun");
            shadowCubemapField.allowSceneObjects = false;
            shadowCubemapField.objectType = typeof(Cubemap);
            shadowCubemapField.RegisterValueChangedCallback(evt =>
            {
                var tmp = environment.shadow.cubemap;
                RegisterChange(ref tmp, evt.newValue as Cubemap, updatePreview: true);
                environment.shadow.cubemap = tmp;
            });
            foldout.Add(shadowCubemapField);

            skyRotationOffset = new FloatSliderField("Rotation", 0f, 360f, 5);
            skyRotationOffset.RegisterValueChangedCallback(evt
                => RegisterChange(ref environment.sky.rotation, evt.newValue, updatePreview: true));
            foldout.Add(skyRotationOffset);
            
            skyExposureField = new FloatField("Exposure");
            skyExposureField.RegisterValueChangedCallback(evt
                => RegisterChange(ref environment.sky.exposure, evt.newValue));
            foldout.Add(skyExposureField);
            var style = foldout.Q<Toggle>().style;
            style.marginLeft = 3;
            style.unityFontStyleAndWeight = FontStyle.Bold;
            inspector.Add(foldout);

            sunPosition = new Vector2Field("Sun Position");
            sunPosition.Q("unity-x-input").Q<FloatField>().formatString = "n2";
            sunPosition.Q("unity-y-input").Q<FloatField>().formatString = "n2";
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
                image = CoreEditorUtils.LoadIcon(@"Packages/com.unity.render-pipelines.core/Editor/LookDev/Icons/", "LookDevSingle1")
            });
            sunToBrightess.AddToClassList("sun-to-brightest-button");
            var vector2Input = sunPosition.Q(className: "unity-vector2-field__input");
            vector2Input.Remove(sunPosition.Q(className: "unity-composite-field__field-spacer"));
            vector2Input.Add(sunToBrightess);
            
            shadowColor = new ColorField("Shadow Tint");
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

        void RegisterChange(ref float reflectedVariable, float newValue, FloatSliderField resyncField = null, bool updatePreview = false)
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

        class FloatSliderField : VisualElement, INotifyValueChanged<float>
        {
            Slider slider;
            FloatField endField;
            readonly int maxCharLength;

            public float value
            {
                get => slider.value;
                set
                {
                    if (value == slider.value)
                        return;

                    float trunkedValue = TrunkValue(value);
                    if (trunkedValue == slider.value)
                        return;

                    if (panel != null)
                    {
                        using (ChangeEvent<float> evt = ChangeEvent<float>.GetPooled(slider.value, trunkedValue))
                        {
                            evt.target = this;
                            SetValueWithoutNotify_Internal(trunkedValue);
                            SendEvent(evt);
                        }
                    }
                    else
                    {
                        SetValueWithoutNotify_Internal(trunkedValue);
                    }
                }
            }

            public FloatSliderField(string label, float start, float end, int maxCharLength = -1)
            {
                this.maxCharLength = maxCharLength;
                Add(slider = new Slider(label, start, end));
                slider.Add(endField = new FloatField(maxCharLength));
                slider.RegisterValueChangedCallback(evt
                    => endField.SetValueWithoutNotify(TrunkValue(evt.newValue)));
                endField.RegisterValueChangedCallback(evt
                    => slider.SetValueWithoutNotify(TrunkValue(evt.newValue)));
                endField.style.marginRight = 0;
            }

            public void SetValueWithoutNotify(float newValue)
                => SetValueWithoutNotify_Internal(TrunkValue(newValue));

            void SetValueWithoutNotify_Internal(float newTrunkedValue)
            {
                //Note: SetValueWithoutNotify do not change the cursor position
                // Passing by slider will cause a loop but this loop will be break
                //as new value match the legacy one
                slider.value = newTrunkedValue;
                endField.SetValueWithoutNotify(newTrunkedValue);
            }

            float TrunkValue(float value)
            {
                if (maxCharLength < 0)
                    return value;

                int integerRounded = (int)Math.Round(value);
                int integerLength = integerRounded.ToString().Length;
                if (integerLength + 1 >= maxCharLength)
                    return integerRounded;

                int signChar = value < 0f ? 1 : 0;
                int integerTrunkedLength = (int)Math.Truncate(value).ToString().Length;
                return (float)Math.Round(value, maxCharLength - integerLength - 1 + signChar);
            }
        }
    }
}
