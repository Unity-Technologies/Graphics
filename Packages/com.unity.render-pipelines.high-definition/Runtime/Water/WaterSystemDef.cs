namespace UnityEngine.Rendering.HighDefinition
{
    internal class WaterConsts
    {
        // The maximal number of bands that the system can hold
        public const int k_WaterHighBandCount = 3;

        // Earth gravitational constant (at the surface)
        public const float k_PhillipsGravityConstant = 9.81f;

        // Maximum choppiness value
        public const float k_WaterMaxChoppinessValue = 2.25f;

        // Constant that converts km/h to m/s
        public const float k_KilometerPerHourToMeterPerSecond = 1.0f / 3.6f;
        // Constant that converts m/s  to km/h
        public const float k_MeterPerSecondToKilometerPerHour = 3.6f;
        // Maximum wind speed that the system allows for the swell (in km/h)
        public const float k_SwellMaximumWindSpeed = 250.0f;
        // Maximum wind speed that the system allows for the swell (in km/h)
        public const float k_SwellMaximumWindSpeedMpS = k_SwellMaximumWindSpeed * k_KilometerPerHourToMeterPerSecond;
        // Maximum wind speed that the system allows for the agitation (in km/h)
        public const float k_AgitationMaximumWindSpeed = 50.0f;

        // Resolution of the mesh used to render the caustics grid
        public const int k_WaterCausticsMeshResolution = 256;
        public const int k_WaterCausticsMeshNumQuads = k_WaterCausticsMeshResolution * k_WaterCausticsMeshResolution;

        // Resolution of the mesh used to render the tessellated water surface
        public const int k_WaterTessellatedMeshResolution = 128;
        public const int k_WaterTessellatedMeshNumQuads = k_WaterTessellatedMeshResolution * k_WaterTessellatedMeshResolution;

        // Swell max patch size
        public const float k_SwellMaxPatchSize = 5000.0f;
        // Swell min patch size
        public const float k_SwellMinPatchSize = 250.0f;
        // Swell minimum ratio between the first and second band
        public const float k_SwellMinRatio = 5.0f;
        // Swell maximum ratio between the first and second band
        public const float k_SwellMaxRatio = 50.0f;

        // Agitation max patch size
        public const float k_AgitationMaxPatchSize = 150.0f;
        // Agitation min patch size
        public const float k_AgitationMinPatchSize = 25.0f;

        // Size of the patch for the first ripple band
        public const float k_RipplesBandSize = 10.0f;
        // Max ripples wind speed
        public const float k_RipplesMaxWindSpeed = 15.0f;

        // Minimum scattering amplitude
        public const float k_MinScatteringAmplitude = 1.0f;

        // Wind value at which the environment roughness transition is done
        public const float k_EnvRoughnessWindSpeed = 30.0f;

        // Scattering range value
        public const float k_ScatteringRange = 0.75f;

        // Resolution of the max amplitude table
        public const int k_TableResolution = 32;

        // Max amplitude table (WindSpeed / PatchSize)
        public static readonly float[] k_MaximumAmplitudeTable = new float[] {0f, 0.07966888f, 0.2717367f, 0.4474014f, 0.5055494f, 0.5237736f, 0.5306075f, 0.5335931f, 0.5350539f, 0.5358352f, 0.5362821f, 0.5365531f, 0.5367244f, 0.5368369f, 0.5369138f, 0.5369676f, 0.5370061f, 0.5370342f, 0.5370553f, 0.5370708f, 0.5370832f, 0.5370926f, 0.5371001f, 0.5371063f, 0.537111f, 0.537115f, 0.5371179f, 0.5371206f, 0.5371227f, 0.5371246f, 0.5371261f, 0.5371273f,
            0f, 0.02377076f, 0.3040674f, 0.7168118f, 1.200752f, 1.75046f, 2.377064f, 2.914901f, 3.291095f, 3.532922f, 3.686541f, 3.785343f, 3.850354f, 3.894217f, 3.924495f, 3.945886f, 3.96131f, 3.972647f, 3.981125f, 3.987558f, 3.992516f, 3.996377f, 3.999427f, 4.001858f, 4.003819f, 4.005409f, 4.006714f, 4.007786f, 4.00868f, 4.00943f, 4.010059f, 4.010591f,
            0f, 0.00167766f, 0.2516024f, 0.693416f, 1.276241f, 1.930328f, 2.593367f, 3.399837f, 4.266063f, 5.056282f, 5.688175f, 6.158275f, 6.499243f, 6.743745f, 6.919492f, 7.047332f, 7.14143f, 7.211648f, 7.264699f, 7.305289f, 7.336744f, 7.361388f, 7.380893f, 7.396493f, 7.409078f, 7.419316f, 7.427711f, 7.43465f, 7.440429f, 7.44526f, 7.44933f, 7.452785f,
            0f, 4.408578E-05f, 0.1833607f, 0.6589456f, 1.250844f, 1.998598f, 2.796385f, 3.598503f, 4.507261f, 5.51288f, 6.540138f, 7.453215f, 8.208902f, 8.800358f, 9.254862f, 9.60058f, 9.862593f, 10.06204f, 10.21528f, 10.33416f, 10.42736f, 10.50104f, 10.55973f, 10.60688f, 10.6451f, 10.67631f, 10.70197f, 10.72326f, 10.74103f, 10.75593f, 10.7685f, 10.77916f,
            0f, 4.006088E-07f, 0.1196065f, 0.6087163f, 1.225746f, 1.966174f, 2.872793f, 3.798886f, 4.726841f, 5.774432f, 6.928916f, 8.126884f, 9.245881f, 10.22859f, 11.04645f, 11.705f, 12.22635f, 12.63568f, 12.95733f, 13.21153f, 13.41329f, 13.57408f, 13.70344f, 13.80836f, 13.89379f, 13.96377f, 14.0215f, 14.06943f, 14.10947f, 14.1431f, 14.1715f, 14.1956f,
            0f, 1.156233E-09f, 0.07127135f, 0.5465209f, 1.196507f, 1.946992f, 2.85665f, 3.883037f, 4.906483f, 5.951984f, 7.085543f, 8.346714f, 9.664865f, 10.92343f, 12.0479f, 13.01002f, 13.80719f, 14.45969f, 14.9908f, 15.41962f, 15.76572f, 16.0449f, 16.27126f, 16.45594f, 16.60739f, 16.73244f, 16.83615f, 16.92262f, 16.99509f, 17.05612f, 17.10778f, 17.15175f,
            0f, 1.039089E-12f, 0.03899262f, 0.4754196f, 1.147466f, 1.932025f, 2.861148f, 3.942429f, 5.047545f, 6.140539f, 7.284455f, 8.558435f, 9.990879f, 11.4719f, 12.9163f, 14.24275f, 15.40471f, 16.39121f, 17.21456f, 17.89591f, 18.45608f, 18.91486f, 19.29179f, 19.60212f, 19.8587f, 20.07159f, 20.24924f, 20.39804f, 20.52321f, 20.6289f, 20.71861f, 20.79503f,
            0f, 2.855458E-16f, 0.02012821f, 0.4056654f, 1.090455f, 1.898387f, 2.816737f, 3.918232f, 5.136121f, 6.358084f, 7.56195f, 8.836155f, 10.25264f, 11.78729f, 13.36593f, 14.89232f, 16.31258f, 17.57889f, 18.67139f, 19.59924f, 20.37938f, 21.03243f, 21.58181f, 22.04157f, 22.42415f, 22.74445f, 23.01298f, 23.23865f, 23.42945f, 23.59139f, 23.72909f, 23.8469f,
            0f, 2.382488E-20f, 0.009666079f, 0.3415855f, 1.035232f, 1.870732f, 2.816113f, 3.903569f, 5.140517f, 6.413571f, 7.643731f, 8.906323f, 10.28578f, 11.8378f, 13.50943f, 15.21574f, 16.88041f, 18.43843f, 19.83261f, 21.05219f, 22.09745f, 22.98746f, 23.74127f, 24.37919f, 24.91602f, 25.36839f, 25.75014f, 26.07271f, 26.34634f, 26.57899f, 26.7777f, 26.94819f,
            0f, 0f, 0.004407257f, 0.2789806f, 0.9757555f, 1.825694f, 2.785372f, 3.864925f, 5.113035f, 6.482924f, 7.845786f, 9.18744f, 10.56828f, 12.08653f, 13.74464f, 15.48311f, 17.28293f, 19.03533f, 20.66464f, 22.15052f, 23.46086f, 24.59644f, 25.57244f, 26.40449f, 27.11182f, 27.71525f, 28.22788f, 28.66462f, 29.03772f, 29.35741f, 29.63186f, 29.86778f,
            0f, 0f, 0.00190067f, 0.2257357f, 0.9006199f, 1.781996f, 2.74833f, 3.824108f, 5.056229f, 6.452427f, 7.901809f, 9.310628f, 10.7078f, 12.20348f, 13.8458f, 15.64249f, 17.53994f, 19.43917f, 21.24734f, 22.94291f, 24.50039f, 25.88452f, 27.09774f, 28.15093f, 29.06003f, 29.83897f, 30.50678f, 31.07995f, 31.57217f, 31.99659f, 32.36229f, 32.67823f,
            0f, 0f, 0.0007572041f, 0.1790478f, 0.8366237f, 1.718776f, 2.70847f, 3.790091f, 5.032242f, 6.44269f, 7.953267f, 9.449829f, 10.91741f, 12.43824f, 14.07615f, 15.86061f, 17.76183f, 19.73103f, 21.68393f, 23.5618f, 25.31435f, 26.90622f, 28.32384f, 29.58112f, 30.68394f, 31.6418f, 32.47251f, 33.19242f, 33.81435f, 34.35196f, 34.81643f, 35.21926f,
            0f, 0f, 0.0002836062f, 0.139262f, 0.7598737f, 1.642733f, 2.650423f, 3.764035f, 5.005919f, 6.415837f, 7.941413f, 9.510447f, 11.04935f, 12.6047f, 14.2302f, 16.00129f, 17.92852f, 19.957f, 22.04005f, 24.09932f, 26.086f, 27.94184f, 29.62995f, 31.14112f, 32.48085f, 33.65757f, 34.68413f, 35.57671f, 36.35223f, 37.02562f, 37.61142f, 38.12167f,
            0f, 0f, 9.952673E-05f, 0.1070949f, 0.6988338f, 1.59597f, 2.621957f, 3.743938f, 4.980056f, 6.39444f, 7.972678f, 9.628988f, 11.26219f, 12.86023f, 14.48867f, 16.24531f, 18.17822f, 20.25199f, 22.39656f, 24.54218f, 26.64509f, 28.64199f, 30.49107f, 32.18918f, 33.71917f, 35.08258f, 36.28627f, 37.34702f, 38.28147f, 39.10056f, 39.81712f, 40.44318f,
            0f, 0f, 3.269755E-05f, 0.08105068f, 0.6346684f, 1.532948f, 2.571001f, 3.705651f, 4.960212f, 6.369124f, 7.969755f, 9.672792f, 11.37281f, 13.0259f, 14.68994f, 16.44201f, 18.3371f, 20.36668f, 22.54611f, 24.78745f, 27.0389f, 29.21974f, 31.27886f, 33.17669f, 34.91597f, 36.48919f, 37.89064f, 39.13328f, 40.2267f, 41.18921f, 42.0363f, 42.77967f,
            0f, 0f, 1.00513E-05f, 0.0603214f, 0.5659673f, 1.463403f, 2.527323f, 3.663546f, 4.929094f, 6.347391f, 7.936816f, 9.644098f, 11.37876f, 13.10778f, 14.83006f, 16.58489f, 18.46096f, 20.50641f, 22.72062f, 25.02729f, 27.35503f, 29.65417f, 31.86657f, 33.93134f, 35.85525f, 37.60601f, 39.19099f, 40.60962f, 41.8757f, 43.00012f, 43.99821f, 44.88412f,
            0f, 0f, 2.872471E-06f, 0.04435309f, 0.5089257f, 1.395387f, 2.476755f, 3.666301f, 4.944954f, 6.338793f, 7.899105f, 9.623148f, 11.42681f, 13.23858f, 15.01177f, 16.78473f, 18.63884f, 20.63537f, 22.79249f, 25.09038f, 27.469f, 29.85722f, 32.20917f, 34.47377f, 36.59269f, 38.56596f, 40.36703f, 41.98612f, 43.43829f, 44.74012f, 45.8972f, 46.92488f,
            0f, 0f, 7.666188E-07f, 0.03220043f, 0.4506788f, 1.314438f, 2.402088f, 3.600613f, 4.901069f, 6.321606f, 7.902833f, 9.673033f, 11.52273f, 13.36683f, 15.16056f, 16.95022f, 18.78957f, 20.76068f, 22.92939f, 25.22888f, 27.66416f, 30.16648f, 32.67064f, 35.10965f, 37.42434f, 39.58344f, 41.57912f, 43.39963f, 45.04712f, 46.52953f, 47.8552f, 49.04368f,
            0f, 0f, 1.901428E-07f, 0.02313177f, 0.3961461f, 1.250772f, 2.340493f, 3.566891f, 4.883543f, 6.297364f, 7.859365f, 9.590779f, 11.45309f, 13.37545f, 15.2634f, 17.10309f, 18.95694f, 20.93247f, 23.08725f, 25.42492f, 27.91365f, 30.47347f, 33.04612f, 35.56208f, 37.9835f, 40.2771f, 42.42115f, 44.41077f, 46.22424f, 47.87185f, 49.35622f, 50.68732f,
            0f, 0f, 4.432507E-08f, 0.01659605f, 0.3482221f, 1.186209f, 2.274328f, 3.495461f, 4.815832f, 6.272548f, 7.850069f, 9.587721f, 11.47687f, 13.41325f, 15.34786f, 17.24554f, 19.12521f, 21.08372f, 23.17903f, 25.4285f, 27.85353f, 30.43495f, 33.0902f, 35.75214f, 38.34772f, 40.80942f, 43.12291f, 45.27406f, 47.2551f, 49.05769f, 50.69623f, 52.17164f,
            0f, 0f, 9.44706E-09f, 0.0115967f, 0.305296f, 1.115087f, 2.212638f, 3.442436f, 4.778667f, 6.234241f, 7.822239f, 9.573297f, 11.45193f, 13.43536f, 15.44884f, 17.44118f, 19.41944f, 21.43157f, 23.54669f, 25.81564f, 28.19874f, 30.69725f, 33.28683f, 35.97184f, 38.63737f, 41.23415f, 43.72229f, 46.05778f, 48.2237f, 50.22312f, 52.03787f, 53.68442f,
            0f, 0f, 1.892961E-09f, 0.008025694f, 0.2649696f, 1.049565f, 2.153724f, 3.416398f, 4.779116f, 6.256924f, 7.830096f, 9.542159f, 11.43512f, 13.46674f, 15.52582f, 17.57616f, 19.60965f, 21.60113f, 23.65216f, 25.86513f, 28.27404f, 30.83788f, 33.53548f, 36.29546f, 39.0573f, 41.77484f, 44.38858f, 46.88597f, 49.23303f, 51.4137f, 53.42418f, 55.26571f,
            0f, 0f, 3.51679E-10f, 0.005525849f, 0.2300672f, 0.9828267f, 2.079638f, 3.349585f, 4.733446f, 6.226134f, 7.824878f, 9.552577f, 11.45662f, 13.52648f, 15.67862f, 17.81293f, 19.88998f, 21.93469f, 24.00569f, 26.21844f, 28.623f, 31.16555f, 33.85472f, 36.63623f, 39.45964f, 42.26899f, 45.00186f, 47.62592f, 50.11964f, 52.45427f, 54.62358f, 56.61493f,
            0f, 0f, 6.082016E-11f, 0.003727285f, 0.1948974f, 0.9103386f, 2.013788f, 3.294757f, 4.671698f, 6.1466f, 7.739853f, 9.473749f, 11.37378f, 13.44792f, 15.60213f, 17.79675f, 19.97143f, 22.10157f, 24.23893f, 26.44145f, 28.79313f, 31.31135f, 33.95137f, 36.68716f, 39.50481f, 42.34691f, 45.16238f, 47.88415f, 50.50232f, 52.98113f, 55.31092f, 57.46139f,
            0f, 0f, 9.792931E-12f, 0.00248521f, 0.1681971f, 0.8546153f, 1.935501f, 3.211515f, 4.604295f, 6.089329f, 7.703278f, 9.450948f, 11.37127f, 13.468f, 15.65271f, 17.85765f, 20.04558f, 22.21788f, 24.41032f, 26.69933f, 29.1078f, 31.65253f, 34.36327f, 37.19258f, 40.09744f, 43.07099f, 46.01152f, 48.8711f, 51.63632f, 54.26872f, 56.7519f, 59.08193f,
            0f, 0f, 1.462557E-12f, 0.001647532f, 0.1438459f, 0.7931088f, 1.867246f, 3.164736f, 4.576475f, 6.078951f, 7.687313f, 9.426596f, 11.33424f, 13.44026f, 15.6712f, 17.95954f, 20.25753f, 22.53791f, 24.78873f, 27.0441f, 29.38384f, 31.88453f, 34.56696f, 37.3826f, 40.32564f, 43.32545f, 46.32125f, 49.27829f, 52.13418f, 54.8728f, 57.45995f, 59.88255f,
            0f, 0f, 2.029447E-13f, 0.001070608f, 0.1224259f, 0.7379785f, 1.787982f, 3.068211f, 4.499474f, 6.040411f, 7.691749f, 9.436932f, 11.32404f, 13.38891f, 15.60447f, 17.90411f, 20.20571f, 22.46699f, 24.74038f, 27.06251f, 29.45521f, 31.9757f, 34.62817f, 37.40799f, 40.32807f, 43.3423f, 46.40731f, 49.46395f, 52.4613f, 55.35933f, 58.14122f, 60.76988f,
            0f, 0f, 2.614718E-14f, 0.0006928894f, 0.1033995f, 0.6810055f, 1.730581f, 3.034133f, 4.486638f, 6.021446f, 7.652265f, 9.396336f, 11.29215f, 13.37414f, 15.6265f, 17.97938f, 20.35659f, 22.70516f, 25.01527f, 27.32869f, 29.67911f, 32.11681f, 34.73093f, 37.50211f, 40.40879f, 43.41506f, 46.51304f, 49.62253f, 52.7036f, 55.72588f, 58.62198f, 61.38444f,
            0f, 0f, 3.112095E-15f, 0.0004388917f, 0.08713411f, 0.6267624f, 1.651286f, 2.970399f, 4.421941f, 5.95403f, 7.60294f, 9.37322f, 11.27541f, 13.33214f, 15.56403f, 17.93124f, 20.35522f, 22.78283f, 25.1741f, 27.53447f, 29.92444f, 32.38216f, 34.99643f, 37.77548f, 40.70869f, 43.79761f, 46.96489f, 50.15966f, 53.32084f, 56.44231f, 59.44736f, 62.31462f,
            0f, 0f, 3.466048E-16f, 0.0002757984f, 0.07301058f, 0.5810083f, 1.590872f, 2.889345f, 4.348913f, 5.903265f, 7.540101f, 9.294528f, 11.19862f, 13.30132f, 15.57237f, 17.95442f, 20.38092f, 22.79476f, 25.19281f, 27.56402f, 29.95196f, 32.41065f, 35.00351f, 37.80022f, 40.75069f, 43.83775f, 47.04081f, 50.30617f, 53.56975f, 56.76492f, 59.88307f, 62.8869f,
            0f, 0f, 3.565781E-17f, 0.0001705384f, 0.06073458f, 0.5260445f, 1.512204f, 2.818909f, 4.289411f, 5.855178f, 7.519925f, 9.289353f, 11.18376f, 13.24182f, 15.48364f, 17.85008f, 20.33566f, 22.85831f, 25.345f, 27.78366f, 30.21792f, 32.66293f, 35.18232f, 37.86638f, 40.71719f, 43.73797f, 46.93105f, 50.21576f, 53.50548f, 56.77405f, 59.9977f, 63.11657f,
            0f, 0f, 3.398541E-18f, 0.0001041713f, 0.05119007f, 0.4937522f, 1.472545f, 2.773492f, 4.258455f, 5.857123f, 7.523153f, 9.283202f, 11.19288f, 13.24743f, 15.47529f, 17.87516f, 20.36746f, 22.87952f, 25.36941f, 27.85065f, 30.33868f, 32.83906f, 35.45756f, 38.22281f, 41.11535f, 44.21394f, 47.44939f, 50.79641f, 54.21289f, 57.59188f, 60.92396f, 64.16597f};
    }

    // This structure holds all the information that can be requested during the deferred water lighting
    [GenerateHLSL(PackingRules.Exact, false)]
    struct WaterSurfaceProfile
    {
        public Vector3 waterAmbientProbe;
        public float tipScatteringHeight;

        public float bodyScatteringHeight;
        public float maxRefractionDistance;
        public uint lightLayers;
        public int cameraUnderWater;

        // Refraction data Data
        public Vector3 transparencyColor;
        public float outScatteringCoefficient;

        // Scattering color
        public Vector3 scatteringColor;
        // Roughness used for environment lighting
        public float envPerceptualRoughness;

        // Smoothness fade transition values
        public float smoothnessFadeStart;
        public float smoothnessFadeDistance;
        public float roughnessEndValue;
        public float padding;
    }

    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesWater
    {
        // Resolution at which the simulation is evaluated
        public uint _BandResolution;
        // Maximal wave height of the current setup
        public float _MaxWaveHeight;
        // Current simulation time
        public float _SimulationTime;
        // Maximal wave height (used for tip scattering)
        public float _ScatteringWaveHeight;

        // Individual sizes of the wave bands
        public Vector4 _PatchSize;

        // Amplitude per band
        public Vector4 _PatchAmplitudeMultiplier;

        // Controls how much the wind affect the current of the waves
        public Vector4 _PatchDirectionDampener;

        // Wind speed per band
        public Vector4 _PatchWindSpeed;

        // Horizontal wind direction
        public Vector4 _PatchWindOrientation;

        // Current speed for each band
        public Vector4 _PatchCurrentSpeed;

        // Current orientation for each band
        public Vector4 _PatchCurrentOrientation;

        // Fade start distance
        public Vector4 _PatchFadeStart;
        // Fade range
        public Vector4 _PatchFadeDistance;
        // Fade value
        public Vector4 _PatchFadeValue;

        // Smoothness of the simulation foam
        public float _SimulationFoamSmoothness;
        // Controls the amount of drag of the simulation foam
        public float _JacobianDrag;
        // Amount of surface foam
        public float _SimulationFoamAmount;
        // TODO WRITE
        public float _SSSMaskCoefficient;

        // Amount of choppiness
        public float _Choppiness;
        // Delta-time since the last simulation step
        public float _DeltaTime;
        // Maximal horizontal displacement
        public float _MaxWaveDisplacement;
        // Maximum refraction distance
        public float _MaxRefractionDistance;

        // Horizontal offsets of the foam texture
        public Vector2 _FoamOffsets;
        // Tiling parameter of the foam texture
        public float _FoamTilling;
        // Attenuation of the foam due to the wind
        public float _WindFoamAttenuation;

        // Color applied to the surfaces that are through the refraction
        public Vector4 _TransparencyColor;

        public Vector4 _ScatteringColorTips;

        public float _DisplacementScattering;
        public int _WaterInitialFrame;
        public int _SurfaceIndex;
        public float _CausticsRegionSize;

        public Vector4 _ScatteringLambertLighting;

        public Vector4 _DeepFoamColor;

        public float _OutScatteringCoefficient;
        public float _FoamSmoothness;
        public float _HeightBasedScattering;
        public float _WaterSmoothness;

        public Vector4 _FoamJacobianLambda;

        // Offsets used to guarantee the coherence between the different simulation resolutions
        public int _WaterRefSimRes;
        public float _WaterSpectrumOffset;
        public int _WaterSampleOffset;
        public int _WaterBandCount;

        public Vector2 _PaddingW0;
        public float _AmbientScattering;
        public int _CausticsBandIndex;
    }

    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesWaterRendering
    {
        // Horizontal size of the grid in the horizontal plane
        public Vector2 _GridSize;
        // Rotation of the water geometry
        public Vector2 _WaterRotation;

        // Offset of the current patch w/r to the origin
        public Vector4 _PatchOffset;

        // Number of LODs used to render infinite water surfaces
        public uint _WaterLODCount;
        // Number of water patches that need to be rendered
        public uint _NumWaterPatches;
        // Intensity of the foam
        public float _FoamIntensity;
        // Intensity of the water caustics
        public float _CausticsIntensity;

        // Scale of the current water mask
        public Vector2 _WaterMaskScale;
        // Offset of the current water mask
        public Vector2 _WaterMaskOffset;

        // Scale of the current foam mask
        public Vector2 _FoamMaskScale;
        // Offset of the current foam mask
        public Vector2 _FoamMaskOffset;

        // Blend distance
        public float _CausticsPlaneBlendDistance;
        // Type of caustics that are rendered
        public int _WaterCausticsEnabled;
        // Which decal layers should affect this surface
        public uint _WaterDecalLayer;
        // Is this surface infinite or finite
        public int _InfiniteSurface;

        // Max tessellation factor
        public float _WaterMaxTessellationFactor;
        // Distance at which the fade of the tessellation starts
        public float _WaterTessellationFadeStart;
        // Size of the range of the tessellation
        public float _WaterTessellationFadeRange;
        // Flag that defines if the camera is in the underwater volume of this surface
        public int _CameraInUnderwaterRegion;
    }

    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesUnderWater
    {
        // Refraction color of the water surface
        public Vector4 _WaterRefractionColor;
        // Scattering color of the water surface
        public Vector4 _WaterScatteringColor;

        // Multiplier of the view distance when under water
        public float _MaxViewDistanceMultiplier;
        // Scattering coefficent for the absorption
        public float _OutScatteringCoeff;
        // Vertical transition size of the water
        public float _WaterTransitionSize;
        // Padding
        public float _PaddingUW;
    }
}
