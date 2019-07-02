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
        //[TODO: check if the shadow/sky split worth the indirection]
        //Note: multi-edition is not supported as we cannot draw multiple HDRI
        [Serializable]
        public class Shadow
        {
            public Cubemap cubemap;
            // Setup default position to be on the sun in the default HDRI.
            // This is important as the defaultHDRI don't call the set brightest spot function on first call.
            [SerializeField]
            internal float m_Latitude = 60.0f; // [-90..90]
            [SerializeField]
            internal float m_Longitude = 299.0f; // [0..360[
            //public float intensity = 1.0f;
            public Color color = Color.white;

            public float latitude
            {
                get => m_Latitude;
                set => m_Latitude = ClampLatitude(value);
            }

            internal static float ClampLatitude(float value) => Mathf.Clamp(value, -90, 90);

            public float longitude
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
        public class Sky
        {
            public Cubemap cubemap;
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
        FloatSliderField shadowSunLatitudeField;
        FloatSliderField shadowSunLongitudeField;
        ColorField shadowColor;

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
            //[TODO: reenable when shadow composition will be finished]
            //shadowCubemapField.SetValueWithoutNotify(environment.shadow.cubemap);
            //shadowSunLatitudeField.SetValueWithoutNotify(environment.shadow.latitude);
            //shadowSunLongitudeField.SetValueWithoutNotify(environment.shadow.longitude);
            //shadowColor.SetValueWithoutNotify(environment.shadow.color);
        }

        public void Bind(Environment environment, Image deportedLatlong)
        {
            latlong = deportedLatlong;
            Bind(environment);
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
            VisualElement inspector = new VisualElement() { name = "Inspector" };
            Foldout skyFoldout = new Foldout()
            {
                text = "Sky"
            };
            skyCubemapField = new ObjectField("Sky with Sun");
            skyCubemapField.allowSceneObjects = false;
            skyCubemapField.objectType = typeof(Cubemap);
            skyCubemapField.RegisterValueChangedCallback(evt
                => RegisterChange(ref environment.sky.cubemap, evt.newValue as Cubemap, updatePreview: true));
            skyFoldout.Add(skyCubemapField);

            skyRotationOffset = new FloatSliderField("Rotation", 0f, 360f, 5);
            skyRotationOffset.RegisterValueChangedCallback(evt
                => RegisterChange(ref environment.sky.rotation, evt.newValue, updatePreview: true));
            skyFoldout.Add(skyRotationOffset);
            
            skyExposureField = new FloatField("Exposure");
            skyExposureField.RegisterValueChangedCallback(evt
                => RegisterChange(ref environment.sky.exposure, evt.newValue));
            skyFoldout.Add(skyExposureField);
            var style = skyFoldout.Q<Toggle>().style;
            style.marginLeft = 3;
            style.unityFontStyleAndWeight = FontStyle.Bold;
            inspector.Add(skyFoldout);

            //[TODO: reenable when shadow composition will be finished]
            //Foldout shadowFoldout = new Foldout()
            //{
            //    text = "Shadow"
            //};
            //shadowCubemapField = new ObjectField("Sky w/o Sun");
            //shadowCubemapField.allowSceneObjects = false;
            //shadowCubemapField.objectType = typeof(Cubemap);
            //shadowCubemapField.RegisterValueChangedCallback(evt
            //    => RegisterChange(ref environment.shadow.cubemap, evt.newValue as Cubemap, updatePreview: true));
            //shadowFoldout.Add(shadowCubemapField);

            //shadowColor = new ColorField("Color");
            //shadowColor.RegisterValueChangedCallback(evt
            //    => RegisterChange(ref environment.shadow.color, evt.newValue));
            //shadowFoldout.Add(shadowColor);

            //shadowSunLatitudeField = new FloatSliderField("Sun Latitude", -90f, 90f, 5);
            //shadowSunLatitudeField.RegisterValueChangedCallback(evt
            //    => RegisterChange(ref environment.shadow.m_Latitude, Environment.Shadow.ClampLatitude(evt.newValue), shadowSunLatitudeField));
            //shadowFoldout.Add(shadowSunLatitudeField);
            
            //shadowSunLongitudeField = new FloatSliderField("Sun Longitude", 0f, 359.999f, 5);
            //shadowSunLongitudeField.RegisterValueChangedCallback(evt
            //    => RegisterChange(ref environment.shadow.m_Longitude, Environment.Shadow.ClampLongitude(evt.newValue), shadowSunLongitudeField));
            //shadowFoldout.Add(shadowSunLongitudeField);

            //Button sunToBrightess = new Button(()
            //    => { /* [TODO] */ })
            //{
            //    text = "Sun position to brightest"
            //};
            //shadowFoldout.Add(sunToBrightess);

            //style = shadowFoldout.Q<Toggle>().style;
            //style.marginLeft = 3;
            //style.unityFontStyleAndWeight = FontStyle.Bold;
            //inspector.Add(shadowFoldout);

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
                //slider.SetValueWithoutNotify(newTrunkedValue);
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
