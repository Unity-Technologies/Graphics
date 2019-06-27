using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.LWRP
{
    // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
    static class ShaderConstants
    {
        public static readonly int _TRANSMITTANCE_TEXTURE_WIDTH        = Shader.PropertyToID("TRANSMITTANCE_TEXTURE_WIDTH");
        public static readonly int _TRANSMITTANCE_TEXTURE_HEIGHT       = Shader.PropertyToID("TRANSMITTANCE_TEXTURE_HEIGHT");
        public static readonly int _SCATTERING_TEXTURE_R_SIZE          = Shader.PropertyToID("SCATTERING_TEXTURE_R_SIZE");
        public static readonly int _SCATTERING_TEXTURE_MU_SIZE         = Shader.PropertyToID("SCATTERING_TEXTURE_MU_SIZE");
        public static readonly int _SCATTERING_TEXTURE_MU_S_SIZE       = Shader.PropertyToID("SCATTERING_TEXTURE_MU_S_SIZE");
        public static readonly int _SCATTERING_TEXTURE_NU_SIZE         = Shader.PropertyToID("SCATTERING_TEXTURE_NU_SIZE");
        public static readonly int _SCATTERING_TEXTURE_WIDTH           = Shader.PropertyToID("SCATTERING_TEXTURE_WIDTH");
        public static readonly int _SCATTERING_TEXTURE_HEIGHT          = Shader.PropertyToID("SCATTERING_TEXTURE_HEIGHT");
        public static readonly int _SCATTERING_TEXTURE_DEPTH           = Shader.PropertyToID("SCATTERING_TEXTURE_DEPTH");
        public static readonly int _IRRADIANCE_TEXTURE_WIDTH           = Shader.PropertyToID("IRRADIANCE_TEXTURE_WIDTH");
        public static readonly int _IRRADIANCE_TEXTURE_HEIGHT          = Shader.PropertyToID("IRRADIANCE_TEXTURE_HEIGHT");
        public static readonly int _SKY_SPECTRAL_RADIANCE_TO_LUMINANCE = Shader.PropertyToID("SKY_SPECTRAL_RADIANCE_TO_LUMINANCE");
        public static readonly int _SUN_SPECTRAL_RADIANCE_TO_LUMINANCE = Shader.PropertyToID("SUN_SPECTRAL_RADIANCE_TO_LUMINANCE");
        public static readonly int _solar_irradiance                   = Shader.PropertyToID("solar_irradiance");
        public static readonly int _rayleigh_scattering                = Shader.PropertyToID("rayleigh_scattering");
        public static readonly int _mie_scattering                     = Shader.PropertyToID("mie_scattering");
        public static readonly int _mie_extinction                     = Shader.PropertyToID("mie_extinction");
        public static readonly int _absorption_extinction              = Shader.PropertyToID("absorption_extinction");
        public static readonly int _ground_albedo                      = Shader.PropertyToID("ground_albedo");
        public static readonly int _luminanceFromRadiance              = Shader.PropertyToID("luminanceFromRadiance");
        public static readonly int _sun_angular_radius                 = Shader.PropertyToID("sun_angular_radius");
        public static readonly int _bottom_radius                      = Shader.PropertyToID("bottom_radius");
        public static readonly int _top_radius                         = Shader.PropertyToID("top_radius");
        public static readonly int _mie_phase_function_g               = Shader.PropertyToID("mie_phase_function_g");
        public static readonly int _mu_s_min                           = Shader.PropertyToID("mu_s_min");
        public static readonly int _sky_exposure                       = Shader.PropertyToID("sky_exposure");
        public static readonly int _earth_center                       = Shader.PropertyToID("earth_center");
        public static readonly int _sun_size                           = Shader.PropertyToID("sun_size");
        public static readonly int _sun_direction                      = Shader.PropertyToID("sun_direction");
        public static readonly int _white_point                        = Shader.PropertyToID("white_point");
        public static readonly int _fog_amount                         = Shader.PropertyToID("fog_amount");
        public static readonly int _sun_edge                           = Shader.PropertyToID("sun_edge");
        public static readonly int _transmittance_texture              = Shader.PropertyToID("transmittance_texture");
        public static readonly int _scattering_texture                 = Shader.PropertyToID("scattering_texture");
        public static readonly int _irradiance_texture                 = Shader.PropertyToID("irradiance_texture");
        public static readonly int _single_mie_scattering_texture      = Shader.PropertyToID("single_mie_scattering_texture");
        public static readonly int _frustumCorners                     = Shader.PropertyToID("frustumCorners");
    }

    public class BrunetonModel
    {
        private const int READ = 0;
        private const int WRITE = 1;

        private const double kLambdaR = 680.0;
        private const double kLambdaG = 550.0;
        private const double kLambdaB = 440.0;

        private const int kLambdaMin = 360;
        private const int kLambdaMax = 830;

        /// <summary>
        /// The wavelength values, in nanometers, and sorted in increasing order, for
        /// which the solar_irradiance, rayleigh_scattering, mie_scattering,
        /// mie_extinction and ground_albedo samples are provided. If your shaders
        /// use luminance values (as opposed to radiance values, see above), use a
        /// large number of wavelengths (e.g. between 15 and 50) to get accurate
        /// results (this number of wavelengths has absolutely no impact on the
        /// shader performance).
        /// </summary>
        public IList<double> Wavelengths { get; set; }

        /// <summary>
        /// The solar irradiance at the top of the atmosphere, in W/m^2/nm. This
        /// vector must have the same size as the wavelengths parameter.
        /// </summary>
        public IList<double> SolarIrradiance { get; set; }

        /// <summary>
        /// The sun's angular radius, in radians. Warning: the implementation uses
        /// approximations that are valid only if this value is smaller than 0.1.
        /// </summary>
        public double SunAngularRadius { get; set; }

        /// <summary>
        /// The distance between the planet center and the bottom of the atmosphere in m.
        /// </summary>
        public double BottomRadius { get; set; }

        /// <summary>
        /// The distance between the planet center and the top of the atmosphere in m.
        /// </summary>
        public double TopRadius { get; set; }

        /// <summary>
        /// The density profile of air molecules, i.e. a function from altitude to
        /// dimensionless values between 0 (null density) and 1 (maximum density).
        /// Layers must be sorted from bottom to top. The width of the last layer is
        /// ignored, i.e. it always extend to the top atmosphere boundary. At most 2
        /// layers can be specified.
        /// </summary>
        public DensityProfileLayer RayleighDensity { get; set; }

        /// <summary>
        /// The scattering coefficient of air molecules at the altitude where their
        /// density is maximum (usually the bottom of the atmosphere), as a function
        /// of wavelength, in m^-1. The scattering coefficient at altitude h is equal
        /// to 'rayleigh_scattering' times 'rayleigh_density' at this altitude. This
        /// vector must have the same size as the wavelengths parameter.
        /// </summary>
        public IList<double> RayleighScattering { get; set; }

        /// <summary>
        /// The density profile of aerosols, i.e. a function from altitude to
        /// dimensionless values between 0 (null density) and 1 (maximum density).
        /// Layers must be sorted from bottom to top. The width of the last layer is
        /// ignored, i.e. it always extend to the top atmosphere boundary. At most 2
        /// layers can be specified.
        /// </summary>
        public DensityProfileLayer MieDensity { get; set; }

        /// <summary>
        /// The scattering coefficient of aerosols at the altitude where their
        /// density is maximum (usually the bottom of the atmosphere), as a function
        /// of wavelength, in m^-1. The scattering coefficient at altitude h is equal
        /// to 'mie_scattering' times 'mie_density' at this altitude. This vector
        /// must have the same size as the wavelengths parameter.
        /// </summary>
        public IList<double> MieScattering { get; set; }

        /// <summary>
        /// The extinction coefficient of aerosols at the altitude where their
        /// density is maximum (usually the bottom of the atmosphere), as a function
        /// of wavelength, in m^-1. The extinction coefficient at altitude h is equal
        /// to 'mie_extinction' times 'mie_density' at this altitude. This vector
        /// must have the same size as the wavelengths parameter.
        /// </summary>
        public IList<double> MieExtinction { get; set; }

        /// <summary>
        /// The asymetry parameter for the Cornette-Shanks phase function for the aerosols.
        /// </summary>
        public double MiePhaseFunctionG { get; set; }

        /// <summary>
        /// The density profile of air molecules that absorb light (e.g. ozone), i.e.
        /// a function from altitude to dimensionless values between 0 (null density)
        /// and 1 (maximum density). Layers must be sorted from bottom to top. The
        /// width of the last layer is ignored, i.e. it always extend to the top
        /// atmosphere boundary. At most 2 layers can be specified.
        /// </summary>
        public IList<DensityProfileLayer> AbsorptionDensity { get; set; }

        /// <summary>
        /// The extinction coefficient of molecules that absorb light (e.g. ozone) at
        /// the altitude where their density is maximum, as a function of wavelength,
        /// in m^-1. The extinction coefficient at altitude h is equal to
        /// 'absorption_extinction' times 'absorption_density' at this altitude. This
        /// vector must have the same size as the wavelengths parameter.
        /// </summary>
        public IList<double> AbsorptionExtinction { get; set; }

        /// <summary>
        /// The average albedo of the ground, as a function of wavelength. This
        /// vector must have the same size as the wavelengths parameter.
        /// </summary>
        public IList<double> GroundAlbedo { get; set; }

        /// <summary>
        /// The maximum Sun zenith angle for which atmospheric scattering must be
        /// precomputed, in radians (for maximum precision, use the smallest Sun
        /// zenith angle yielding negligible sky light radiance values. For instance,
        /// for the Earth case, 102 degrees is a good choice for most cases (120
        /// degrees is necessary for very high exposure values).
        /// </summary>
        public double MaxSunZenithAngle { get; set; }

        /// <summary>
        /// The length unit used in your shaders and meshes. This is the length unit
        /// which must be used when calling the atmosphere model shader functions.
        /// </summary>
        public double LengthUnitInMeters { get; set; }

        /// <summary>
        /// The number of wavelengths for which atmospheric scattering must be
        /// precomputed (the temporary GPU memory used during precomputations, and
        /// the GPU memory used by the precomputed results, is independent of this
        /// number, but the precomputation time is directly proportional to this number):
        /// - if this number is less than or equal to 3, scattering is precomputed
        /// for 3 wavelengths, and stored as irradiance values. Then both the
        /// radiance-based and the luminance-based API functions are provided (see
        /// the above note).
        /// - otherwise, scattering is precomputed for this number of wavelengths
        /// (rounded up to a multiple of 3), integrated with the CIE color matching
        /// functions, and stored as illuminance values. Then only the
        /// luminance-based API functions are provided (see the above note).
        /// </summary>
        public int NumPrecomputedWavelengths { get { return UseLuminance == LUMINANCE.PRECOMPUTED ? 15 : 3; } }

        /// <summary>
        /// Whether to pack the (red component of the) single Mie scattering with the
        /// Rayleigh and multiple scattering in a single texture, or to store the
        /// (3 components of the) single Mie scattering in a separate texture.
        /// </summary>
        public bool CombineScatteringTextures { get; set; }

        /// <summary>
        /// Use radiance or luminance mode.
        /// </summary>
        public LUMINANCE UseLuminance { get; set; }

        /// <summary>
        /// Whether to use half precision floats (16 bits) or single precision floats
        /// (32 bits) for the precomputed textures. Half precision is sufficient for
        /// most cases, except for very high exposure values.
        /// </summary>
        public bool HalfPrecision { get; set; }

        /// <summary>
        /// Sky Exposure
        /// </summary>
        public double Exposure { get; set; }

        /// <summary>
        /// Perform white balance
        /// </summary>
        public bool DoWhiteBalance { get; set; }

        /// <summary>
        /// Scalar to tone control the amount of fog
        /// </summary>
        public double FogAmount { get; set; }

        /// <summary>
        /// Size of the sun
        /// </summary>
        public double SunSize { get; set; }

        /// <summary>
        /// Edge size of the sun
        /// </summary>
        public double SunEdge { get; set; }

        public Vector3 SunDir {get; set; }

        public RenderTexture TransmittanceTexture { get; private set; }

        public RenderTexture ScatteringTexture { get; private set; }

        public RenderTexture IrradianceTexture { get; private set; }

        public RenderTexture OptionalSingleMieScatteringTexture { get; private set; }

        public static BrunetonModel Create(BrunetonParameters brunetonParams)
        {
            int kLambdaMin = 360;
            int kLambdaMax = 830;
            bool kUseConstantSolarSpectrum = false;
            bool kUseOzone = true;
            bool kUseCombinedTextures = true;
            bool kUseHalfPrecision = false;
            bool kDoWhiteBalance = false;
            float kSunAngularRadius = 0.00935f / 2.0f;
            float kBottomRadius = 6360000.0f;
            float kLengthUnitInMeters = 1000.0f;
            LUMINANCE kUseLuminance = LUMINANCE.NONE;

            double[] kSolarIrradiance = new double[]
                {
                    1.11776, 1.14259, 1.01249, 1.14716, 1.72765, 1.73054, 1.6887, 1.61253,
                    1.91198, 2.03474, 2.02042, 2.02212, 1.93377, 1.95809, 1.91686, 1.8298,
                    1.8685, 1.8931, 1.85149, 1.8504, 1.8341, 1.8345, 1.8147, 1.78158, 1.7533,
                    1.6965, 1.68194, 1.64654, 1.6048, 1.52143, 1.55622, 1.5113, 1.474, 1.4482,
                    1.41018, 1.36775, 1.34188, 1.31429, 1.28303, 1.26758, 1.2367, 1.2082,
                    1.18737, 1.14683, 1.12362, 1.1058, 1.07124, 1.04992
                };

            double[] kOzoneCrossSection = new double[]
            {
                    1.18e-27, 2.182e-28, 2.818e-28, 6.636e-28, 1.527e-27, 2.763e-27, 5.52e-27,
                    8.451e-27, 1.582e-26, 2.316e-26, 3.669e-26, 4.924e-26, 7.752e-26, 9.016e-26,
                    1.48e-25, 1.602e-25, 2.139e-25, 2.755e-25, 3.091e-25, 3.5e-25, 4.266e-25,
                    4.672e-25, 4.398e-25, 4.701e-25, 5.019e-25, 4.305e-25, 3.74e-25, 3.215e-25,
                    2.662e-25, 2.238e-25, 1.852e-25, 1.473e-25, 1.209e-25, 9.423e-26, 7.455e-26,
                    6.566e-26, 5.105e-26, 4.15e-26, 4.228e-26, 3.237e-26, 2.451e-26, 2.801e-26,
                    2.534e-26, 1.624e-26, 1.465e-26, 2.078e-26, 1.383e-26, 7.105e-27
            };

            double kDobsonUnit = 2.687e20;
            double kMaxOzoneNumberDensity = 300.0 * kDobsonUnit / 15000.0;
            double kConstantSolarIrradiance = 1.5;
            double kTopRadius = 6420000.0;
            double kRayleigh = 1.24062e-6;
            double kRayleighScaleHeight = 8000.0;
            double kMieScaleHeight = 1200.0;
            double kMieAngstromAlpha = 0.0;
            double kMieAngstromBeta = 5.328e-3;
            double kMieSingleScatteringAlbedo = 0.9;
            double kGroundAlbedo = 0.1;
            double max_sun_zenith_angle = (kUseHalfPrecision ? 102.0 : 120.0) / 180.0 * Mathf.PI;

            DensityProfileLayer rayleigh_layer = new DensityProfileLayer("rayleigh", 0.0, 1.0, -1.0 / kRayleighScaleHeight, 0.0, 0.0);
            DensityProfileLayer mie_layer = new DensityProfileLayer("mie", 0.0, 1.0, -1.0 / kMieScaleHeight, 0.0, 0.0);

            List<DensityProfileLayer> ozone_density = new List<DensityProfileLayer>();
            ozone_density.Add(new DensityProfileLayer("absorption0", 25000.0, 0.0, 0.0, 1.0 / 15000.0, -2.0 / 3.0));
            ozone_density.Add(new DensityProfileLayer("absorption1", 0.0, 0.0, 0.0, -1.0 / 15000.0, 8.0 / 3.0));

            List<double> wavelengths = new List<double>();
            List<double> solar_irradiance = new List<double>();
            List<double> rayleigh_scattering = new List<double>();
            List<double> mie_scattering = new List<double>();
            List<double> mie_extinction = new List<double>();
            List<double> absorption_extinction = new List<double>();
            List<double> ground_albedo = new List<double>();

            for (int l = kLambdaMin; l <= kLambdaMax; l += 10)
            {
                double lambda = l * 1e-3;  // micro-meters
                double mie = kMieAngstromBeta / kMieScaleHeight * System.Math.Pow(lambda, -kMieAngstromAlpha);

                wavelengths.Add(l);

                if (kUseConstantSolarSpectrum)
                    solar_irradiance.Add(kConstantSolarIrradiance);
                else
                    solar_irradiance.Add(kSolarIrradiance[(l - kLambdaMin) / 10]);

                rayleigh_scattering.Add(kRayleigh * System.Math.Pow(lambda, -4) * brunetonParams.m_raleightScattering);
                mie_scattering.Add(mie * kMieSingleScatteringAlbedo * brunetonParams.m_mieScattering);
                mie_extinction.Add(mie);
                absorption_extinction.Add(kUseOzone ? brunetonParams.m_ozoneDensity * kMaxOzoneNumberDensity * kOzoneCrossSection[(l - kLambdaMin) / 10] : 0.0);
                ground_albedo.Add(kGroundAlbedo);
            }

            BrunetonModel model = new BrunetonModel();
            model.HalfPrecision = kUseHalfPrecision;
            model.CombineScatteringTextures = kUseCombinedTextures;
            model.UseLuminance = kUseLuminance;
            model.Wavelengths = wavelengths;
            model.SolarIrradiance = solar_irradiance;
            model.SunAngularRadius = brunetonParams.m_sunSize * kSunAngularRadius;
            model.BottomRadius = kBottomRadius;
            model.Exposure = brunetonParams.m_exposure;
            model.DoWhiteBalance = kDoWhiteBalance;
            model.TopRadius = kTopRadius;
            model.RayleighDensity = rayleigh_layer;
            model.RayleighScattering = rayleigh_scattering;
            model.MieDensity = mie_layer;
            model.MieScattering = mie_scattering;
            model.MieExtinction = mie_extinction;
            model.MiePhaseFunctionG = brunetonParams.m_phase;
            model.AbsorptionDensity = ozone_density;
            model.AbsorptionExtinction = absorption_extinction;
            model.GroundAlbedo = ground_albedo;
            model.MaxSunZenithAngle = max_sun_zenith_angle;
            model.LengthUnitInMeters = kLengthUnitInMeters;
            model.SunSize = (double)brunetonParams.m_sunSize;
            model.SunEdge = (double)brunetonParams.m_sunEdge;
            model.SunDir = Vector3.up;

            return model;
        }

        /// <summary>
        /// Bind to a pixel shader for rendering.
        /// </summary>
        public void BindToMaterial(Material mat)
        {
            if (UseLuminance == LUMINANCE.NONE)
                mat.EnableKeyword("RADIANCE_API_ENABLED");
            else
                mat.DisableKeyword("RADIANCE_API_ENABLED");

            if (CombineScatteringTextures)
                mat.EnableKeyword("COMBINED_SCATTERING_TEXTURES");
            else
                mat.DisableKeyword("COMBINED_SCATTERING_TEXTURES");

            mat.SetTexture("transmittance_texture", TransmittanceTexture);
            mat.SetTexture("scattering_texture", ScatteringTexture);
            mat.SetTexture("irradiance_texture", IrradianceTexture);

            if(CombineScatteringTextures)
                mat.SetTexture("single_mie_scattering_texture", Texture2D.blackTexture);
            else
                mat.SetTexture("single_mie_scattering_texture", OptionalSingleMieScatteringTexture);

            mat.SetInt("TRANSMITTANCE_TEXTURE_WIDTH", CONSTANTS.TRANSMITTANCE_WIDTH);
            mat.SetInt("TRANSMITTANCE_TEXTURE_HEIGHT", CONSTANTS.TRANSMITTANCE_HEIGHT);
            mat.SetInt("SCATTERING_TEXTURE_R_SIZE", CONSTANTS.SCATTERING_R);
            mat.SetInt("SCATTERING_TEXTURE_MU_SIZE", CONSTANTS.SCATTERING_MU);
            mat.SetInt("SCATTERING_TEXTURE_MU_S_SIZE", CONSTANTS.SCATTERING_MU_S);
            mat.SetInt("SCATTERING_TEXTURE_NU_SIZE", CONSTANTS.SCATTERING_NU);
            mat.SetInt("SCATTERING_TEXTURE_WIDTH", CONSTANTS.SCATTERING_WIDTH);
            mat.SetInt("SCATTERING_TEXTURE_HEIGHT", CONSTANTS.SCATTERING_HEIGHT);
            mat.SetInt("SCATTERING_TEXTURE_DEPTH", CONSTANTS.SCATTERING_DEPTH);
            mat.SetInt("IRRADIANCE_TEXTURE_WIDTH", CONSTANTS.IRRADIANCE_WIDTH);
            mat.SetInt("IRRADIANCE_TEXTURE_HEIGHT", CONSTANTS.IRRADIANCE_HEIGHT);

            mat.SetFloat("sun_angular_radius", (float)SunAngularRadius);
            mat.SetFloat("bottom_radius", (float)(BottomRadius / LengthUnitInMeters));
            mat.SetFloat("top_radius", (float)(TopRadius / LengthUnitInMeters));
            mat.SetFloat("mie_phase_function_g", (float)MiePhaseFunctionG);
            mat.SetFloat("mu_s_min", (float)Math.Cos(MaxSunZenithAngle));

            Vector3 skySpectralRadianceToLuminance, sunSpectralRadianceToLuminance;
            SkySunRadianceToLuminance(out skySpectralRadianceToLuminance, out sunSpectralRadianceToLuminance);

            mat.SetVector("SKY_SPECTRAL_RADIANCE_TO_LUMINANCE", skySpectralRadianceToLuminance);
            mat.SetVector("SUN_SPECTRAL_RADIANCE_TO_LUMINANCE", sunSpectralRadianceToLuminance);

            double[] lambdas = new double[] { kLambdaR, kLambdaG, kLambdaB };

            Vector3 solarIrradiance = ToVector(Wavelengths, SolarIrradiance, lambdas, 1.0);
            mat.SetVector("solar_irradiance", solarIrradiance);

            Vector3 rayleighScattering = ToVector(Wavelengths, RayleighScattering, lambdas, LengthUnitInMeters);
            mat.SetVector("rayleigh_scattering", rayleighScattering);

            Vector3 mieScattering = ToVector(Wavelengths, MieScattering, lambdas, LengthUnitInMeters);
            mat.SetVector("mie_scattering", mieScattering);

            mat.SetFloat("fog_amount", (float)FogAmount);
            mat.SetFloat("sun_edge", (float)SunEdge);
        }

        /// <summary>
        /// Bind to a pixel shader for rendering.
        /// </summary>
        public void BindToPipeline(CommandBuffer cmd, double[] lambdas, double[] luminance_from_radiance)
        {
            if (lambdas == null)
                lambdas = new double[] { kLambdaR, kLambdaG, kLambdaB };

            if (luminance_from_radiance == null)
                luminance_from_radiance = new double[] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 };

            float angularRadius = (float)SunAngularRadius;
            cmd.EnableShaderKeyword("_ADDITIONAL_LIGHTS");
            cmd.SetGlobalTexture("transmittance_texture", TransmittanceTexture);
            cmd.SetGlobalTexture("scattering_texture", ScatteringTexture);
            cmd.SetGlobalTexture("irradiance_texture", IrradianceTexture);
            cmd.SetGlobalTexture("single_mie_scattering_texture", Texture2D.blackTexture);

            cmd.SetGlobalInt("TRANSMITTANCE_TEXTURE_WIDTH", CONSTANTS.TRANSMITTANCE_WIDTH);
            cmd.SetGlobalInt("TRANSMITTANCE_TEXTURE_HEIGHT", CONSTANTS.TRANSMITTANCE_HEIGHT);
            cmd.SetGlobalInt("SCATTERING_TEXTURE_R_SIZE", CONSTANTS.SCATTERING_R);
            cmd.SetGlobalInt("SCATTERING_TEXTURE_MU_SIZE", CONSTANTS.SCATTERING_MU);
            cmd.SetGlobalInt("SCATTERING_TEXTURE_MU_S_SIZE", CONSTANTS.SCATTERING_MU_S);
            cmd.SetGlobalInt("SCATTERING_TEXTURE_NU_SIZE", CONSTANTS.SCATTERING_NU);
            cmd.SetGlobalInt("SCATTERING_TEXTURE_WIDTH", CONSTANTS.SCATTERING_WIDTH);
            cmd.SetGlobalInt("SCATTERING_TEXTURE_HEIGHT", CONSTANTS.SCATTERING_HEIGHT);
            cmd.SetGlobalInt("SCATTERING_TEXTURE_DEPTH", CONSTANTS.SCATTERING_DEPTH);
            cmd.SetGlobalInt("IRRADIANCE_TEXTURE_WIDTH", CONSTANTS.IRRADIANCE_WIDTH);
            cmd.SetGlobalInt("IRRADIANCE_TEXTURE_HEIGHT", CONSTANTS.IRRADIANCE_HEIGHT);

            Vector3 skySpectralRadianceToLuminance, sunSpectralRadianceToLuminance;
            SkySunRadianceToLuminance(out skySpectralRadianceToLuminance, out sunSpectralRadianceToLuminance);

            cmd.SetGlobalVector("SKY_SPECTRAL_RADIANCE_TO_LUMINANCE", skySpectralRadianceToLuminance);
            cmd.SetGlobalVector("SUN_SPECTRAL_RADIANCE_TO_LUMINANCE", sunSpectralRadianceToLuminance);

            Vector3 solarIrradiance = ToVector(Wavelengths, SolarIrradiance, lambdas, 1.0);
            cmd.SetGlobalVector("solar_irradiance", solarIrradiance);

            Vector3 rayleighScattering = ToVector(Wavelengths, RayleighScattering, lambdas, LengthUnitInMeters);
            cmd.SetGlobalVector("rayleigh_scattering", rayleighScattering);

            Vector3 mieScattering = ToVector(Wavelengths, MieScattering, lambdas, LengthUnitInMeters);
            Vector3 mieExtinction = ToVector(Wavelengths, MieExtinction, lambdas, LengthUnitInMeters);

            BindDensityLayer(cmd, MieDensity);
            cmd.SetGlobalVector("mie_scattering", mieScattering);
            cmd.SetGlobalVector("mie_extinction", mieExtinction);

            Vector3 absorptionExtinction = ToVector(Wavelengths, AbsorptionExtinction, lambdas, LengthUnitInMeters);
            BindDensityLayer(cmd, AbsorptionDensity[0]);
            BindDensityLayer(cmd, AbsorptionDensity[1]);
            cmd.SetGlobalVector("absorption_extinction", absorptionExtinction);

            Vector3 groundAlbedo = ToVector(Wavelengths, GroundAlbedo, lambdas, 1.0);
            cmd.SetGlobalVector("ground_albedo", groundAlbedo);

            cmd.SetGlobalMatrix("luminanceFromRadiance", Matrix4x4.identity);// ToMatrix(luminance_from_radiance));
            cmd.SetGlobalFloat("sun_angular_radius", angularRadius);
            cmd.SetGlobalFloat("bottom_radius", (float)(BottomRadius / LengthUnitInMeters));
            cmd.SetGlobalFloat("top_radius", (float)(TopRadius / LengthUnitInMeters));
            cmd.SetGlobalFloat("mie_phase_function_g", (float)MiePhaseFunctionG);
            cmd.SetGlobalFloat("mu_s_min", (float)Math.Cos(MaxSunZenithAngle));

            cmd.SetGlobalFloat("sky_exposure", (float)Exposure);
            cmd.SetGlobalVector("earth_center", new Vector3(0.0f, -(float)BottomRadius / (float)LengthUnitInMeters, 0.0f));
            cmd.SetGlobalVector("sun_size", new Vector2((float)Math.Tan(angularRadius), (float)Math.Cos(angularRadius)));
            cmd.SetGlobalVector("sun_direction", -RenderSettings.sun.transform.forward);

            double white_point_r = 1.0;
            double white_point_g = 1.0;
            double white_point_b = 1.0;
            if (DoWhiteBalance)
            {
                ConvertSpectrumToLinearSrgb(out white_point_r, out white_point_g, out white_point_b);

                double white_point = (white_point_r + white_point_g + white_point_b) / 3.0;
                white_point_r /= white_point;
                white_point_g /= white_point;
                white_point_b /= white_point;
            }

            cmd.SetGlobalVector("white_point", new Vector3((float)white_point_r, (float)white_point_g, (float)white_point_b));

            cmd.SetGlobalFloat("fog_amount", (float)FogAmount);
            cmd.SetGlobalFloat("sun_edge", (float)SunEdge);

        }

        /// <summary>
        /// Bind to a compute shader for precomutation of textures.
        /// </summary>
        public void BindToCompute(ComputeShader compute, double[] lambdas, double[] luminance_from_radiance)
        {
            if (compute == null)
                return;

            if (lambdas == null)
                lambdas = new double[] { kLambdaR, kLambdaG, kLambdaB };

            if (luminance_from_radiance == null)
                luminance_from_radiance = new double[] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 };

            float angularRadius = (float)SunAngularRadius;

            compute.SetInt("TRANSMITTANCE_TEXTURE_WIDTH", CONSTANTS.TRANSMITTANCE_WIDTH);
            compute.SetInt("TRANSMITTANCE_TEXTURE_HEIGHT", CONSTANTS.TRANSMITTANCE_HEIGHT);
            compute.SetInt("SCATTERING_TEXTURE_R_SIZE", CONSTANTS.SCATTERING_R);
            compute.SetInt("SCATTERING_TEXTURE_MU_SIZE", CONSTANTS.SCATTERING_MU);
            compute.SetInt("SCATTERING_TEXTURE_MU_S_SIZE", CONSTANTS.SCATTERING_MU_S);
            compute.SetInt("SCATTERING_TEXTURE_NU_SIZE", CONSTANTS.SCATTERING_NU);
            compute.SetInt("SCATTERING_TEXTURE_WIDTH", CONSTANTS.SCATTERING_WIDTH);
            compute.SetInt("SCATTERING_TEXTURE_HEIGHT", CONSTANTS.SCATTERING_HEIGHT);
            compute.SetInt("SCATTERING_TEXTURE_DEPTH", CONSTANTS.SCATTERING_DEPTH);
            compute.SetInt("IRRADIANCE_TEXTURE_WIDTH", CONSTANTS.IRRADIANCE_WIDTH);
            compute.SetInt("IRRADIANCE_TEXTURE_HEIGHT", CONSTANTS.IRRADIANCE_HEIGHT);

            Vector3 skySpectralRadianceToLuminance, sunSpectralRadianceToLuminance;
            SkySunRadianceToLuminance(out skySpectralRadianceToLuminance, out sunSpectralRadianceToLuminance);

            compute.SetVector("SKY_SPECTRAL_RADIANCE_TO_LUMINANCE", skySpectralRadianceToLuminance);
            compute.SetVector("SUN_SPECTRAL_RADIANCE_TO_LUMINANCE", sunSpectralRadianceToLuminance);

            Vector3 solarIrradiance = ToVector(Wavelengths, SolarIrradiance, lambdas, 1.0);
            compute.SetVector("solar_irradiance", solarIrradiance);

            Vector3 rayleighScattering = ToVector(Wavelengths, RayleighScattering, lambdas, LengthUnitInMeters);
            BindDensityLayer(compute, RayleighDensity);
            compute.SetVector("rayleigh_scattering", rayleighScattering);

            Vector3 mieScattering = ToVector(Wavelengths, MieScattering, lambdas, LengthUnitInMeters);
            Vector3 mieExtinction = ToVector(Wavelengths, MieExtinction, lambdas, LengthUnitInMeters);
            BindDensityLayer(compute, MieDensity);
            compute.SetVector("mie_scattering", mieScattering);
            compute.SetVector("mie_extinction", mieExtinction);

            Vector3 absorptionExtinction = ToVector(Wavelengths, AbsorptionExtinction, lambdas, LengthUnitInMeters);
            BindDensityLayer(compute, AbsorptionDensity[0]);
            BindDensityLayer(compute, AbsorptionDensity[1]);
            compute.SetVector("absorption_extinction", absorptionExtinction);

            Vector3 groundAlbedo = ToVector(Wavelengths, GroundAlbedo, lambdas, 1.0);
            compute.SetVector("ground_albedo", groundAlbedo);

            compute.SetFloats("luminanceFromRadiance", ToMatrix(luminance_from_radiance));
            compute.SetFloat("sun_angular_radius", angularRadius);
            compute.SetFloat("bottom_radius", (float)(BottomRadius / LengthUnitInMeters));
            compute.SetFloat("top_radius", (float)(TopRadius / LengthUnitInMeters));
            compute.SetFloat("mie_phase_function_g", (float)MiePhaseFunctionG);
            compute.SetFloat("mu_s_min", (float)Math.Cos(MaxSunZenithAngle));

            compute.SetFloat("sky_exposure", (float)Exposure);
            compute.SetVector("earth_center", new Vector3(0.0f, -(float)BottomRadius / (float)LengthUnitInMeters, 0.0f));
            compute.SetVector("sun_size", new Vector2((float)Math.Tan(angularRadius), (float)Math.Cos(angularRadius)));
            compute.SetVector("sun_direction", -RenderSettings.sun.transform.forward);

            double white_point_r = 1.0;
            double white_point_g = 1.0;
            double white_point_b = 1.0;
            if (DoWhiteBalance)
            {
                ConvertSpectrumToLinearSrgb(out white_point_r, out white_point_g, out white_point_b);

                double white_point = (white_point_r + white_point_g + white_point_b) / 3.0;
                white_point_r /= white_point;
                white_point_g /= white_point;
                white_point_b /= white_point;
            }

            compute.SetVector("white_point", new Vector3((float)white_point_r, (float)white_point_g, (float)white_point_b));

            compute.SetFloat("fog_amount", (float)FogAmount);
            compute.SetFloat("sun_edge", (float)SunEdge);
        }

        /// <summary>
        /// Bind to global shader properties..
        /// </summary>
        public void BindGlobal(Camera camera, double[] lambdas, double[] luminance_from_radiance)
        {
            if (lambdas == null)
                lambdas = new double[] { kLambdaR, kLambdaG, kLambdaB };

            if (luminance_from_radiance == null)
                luminance_from_radiance = new double[] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 };

            float angularRadius = (float)SunAngularRadius;

            Shader.SetGlobalInt(ShaderConstants._TRANSMITTANCE_TEXTURE_WIDTH, CONSTANTS.TRANSMITTANCE_WIDTH);
            Shader.SetGlobalInt(ShaderConstants._TRANSMITTANCE_TEXTURE_HEIGHT, CONSTANTS.TRANSMITTANCE_HEIGHT);
            Shader.SetGlobalInt(ShaderConstants._SCATTERING_TEXTURE_R_SIZE, CONSTANTS.SCATTERING_R);
            Shader.SetGlobalInt(ShaderConstants._SCATTERING_TEXTURE_MU_SIZE, CONSTANTS.SCATTERING_MU);
            Shader.SetGlobalInt(ShaderConstants._SCATTERING_TEXTURE_MU_S_SIZE, CONSTANTS.SCATTERING_MU_S);
            Shader.SetGlobalInt(ShaderConstants._SCATTERING_TEXTURE_NU_SIZE, CONSTANTS.SCATTERING_NU);
            Shader.SetGlobalInt(ShaderConstants._SCATTERING_TEXTURE_WIDTH, CONSTANTS.SCATTERING_WIDTH);
            Shader.SetGlobalInt(ShaderConstants._SCATTERING_TEXTURE_HEIGHT, CONSTANTS.SCATTERING_HEIGHT);
            Shader.SetGlobalInt(ShaderConstants._SCATTERING_TEXTURE_DEPTH, CONSTANTS.SCATTERING_DEPTH);
            Shader.SetGlobalInt(ShaderConstants._IRRADIANCE_TEXTURE_WIDTH, CONSTANTS.IRRADIANCE_WIDTH);
            Shader.SetGlobalInt(ShaderConstants._IRRADIANCE_TEXTURE_HEIGHT, CONSTANTS.IRRADIANCE_HEIGHT);

            Vector3 skySpectralRadianceToLuminance, sunSpectralRadianceToLuminance;
            SkySunRadianceToLuminance(out skySpectralRadianceToLuminance, out sunSpectralRadianceToLuminance);

            Shader.SetGlobalVector(ShaderConstants._SKY_SPECTRAL_RADIANCE_TO_LUMINANCE, skySpectralRadianceToLuminance);
            Shader.SetGlobalVector(ShaderConstants._SUN_SPECTRAL_RADIANCE_TO_LUMINANCE, sunSpectralRadianceToLuminance);

            Vector3 solarIrradiance = ToVector(Wavelengths, SolarIrradiance, lambdas, 1.0);
            Shader.SetGlobalVector(ShaderConstants._solar_irradiance, solarIrradiance);

            Vector3 rayleighScattering = ToVector(Wavelengths, RayleighScattering, lambdas, LengthUnitInMeters);
            BindDensityLayer(RayleighDensity);
            Shader.SetGlobalVector(ShaderConstants._rayleigh_scattering, rayleighScattering);

            Vector3 mieScattering = ToVector(Wavelengths, MieScattering, lambdas, LengthUnitInMeters);
            Vector3 mieExtinction = ToVector(Wavelengths, MieExtinction, lambdas, LengthUnitInMeters);
            BindDensityLayer(MieDensity);
            Shader.SetGlobalVector(ShaderConstants._mie_scattering, mieScattering);
            Shader.SetGlobalVector(ShaderConstants._mie_extinction, mieExtinction);

            Vector3 absorptionExtinction = ToVector(Wavelengths, AbsorptionExtinction, lambdas, LengthUnitInMeters);
            BindDensityLayer(AbsorptionDensity[0]);
            BindDensityLayer(AbsorptionDensity[1]);
            Shader.SetGlobalVector(ShaderConstants._absorption_extinction, absorptionExtinction);

            Vector3 groundAlbedo = ToVector(Wavelengths, GroundAlbedo, lambdas, 1.0);
            Shader.SetGlobalVector(ShaderConstants._ground_albedo, groundAlbedo);

            Shader.SetGlobalFloatArray(ShaderConstants._luminanceFromRadiance, ToMatrix(luminance_from_radiance));
            Shader.SetGlobalFloat(ShaderConstants._sun_angular_radius, angularRadius);
            Shader.SetGlobalFloat(ShaderConstants._bottom_radius, (float)(BottomRadius / LengthUnitInMeters));
            Shader.SetGlobalFloat(ShaderConstants._top_radius, (float)(TopRadius / LengthUnitInMeters));
            Shader.SetGlobalFloat(ShaderConstants._mie_phase_function_g, (float)MiePhaseFunctionG);
            Shader.SetGlobalFloat(ShaderConstants._mu_s_min, (float)Math.Cos(MaxSunZenithAngle));

            Shader.SetGlobalFloat(ShaderConstants._sky_exposure, (float)Exposure);
            Shader.SetGlobalVector(ShaderConstants._earth_center, new Vector3(0.0f, -(float)BottomRadius / (float)LengthUnitInMeters, 0.0f));
            Shader.SetGlobalVector(ShaderConstants._sun_size, new Vector2((float)Math.Tan(angularRadius), (float)Math.Cos(angularRadius)));
            Shader.SetGlobalVector(ShaderConstants._sun_direction, -RenderSettings.sun.transform.forward);

            double white_point_r = 1.0;
            double white_point_g = 1.0;
            double white_point_b = 1.0;
            if (DoWhiteBalance)
            {
                ConvertSpectrumToLinearSrgb(out white_point_r, out white_point_g, out white_point_b);

                double white_point = (white_point_r + white_point_g + white_point_b) / 3.0;
                white_point_r /= white_point;
                white_point_g /= white_point;
                white_point_b /= white_point;
            }

            Shader.SetGlobalVector(ShaderConstants._white_point, new Vector3((float)white_point_r, (float)white_point_g, (float)white_point_b));

            Shader.SetGlobalFloat(ShaderConstants._fog_amount, (float)FogAmount);
            Shader.SetGlobalFloat(ShaderConstants._sun_edge, (float)SunEdge);


            // Render sky specific.
            if (UseLuminance == LUMINANCE.NONE)
                Shader.EnableKeyword("RADIANCE_API_ENABLED");
            else
                Shader.DisableKeyword("RADIANCE_API_ENABLED");

            if (CombineScatteringTextures)
                Shader.EnableKeyword("COMBINED_SCATTERING_TEXTURES");
            else
                Shader.DisableKeyword("COMBINED_SCATTERING_TEXTURES");

            Shader.SetGlobalTexture(ShaderConstants._transmittance_texture, TransmittanceTexture);
            Shader.SetGlobalTexture(ShaderConstants._scattering_texture, ScatteringTexture);
            Shader.SetGlobalTexture(ShaderConstants._irradiance_texture, IrradianceTexture);

            if(CombineScatteringTextures)
                Shader.SetGlobalTexture(ShaderConstants._single_mie_scattering_texture, Texture2D.blackTexture);
            else
                Shader.SetGlobalTexture(ShaderConstants._single_mie_scattering_texture, OptionalSingleMieScatteringTexture);

            float CAMERA_FOV = camera.fieldOfView;
            float CAMERA_ASPECT_RATIO = camera.aspect;
            float CAMERA_NEAR = camera.nearClipPlane;
            float CAMERA_FAR = camera.farClipPlane;

            Matrix4x4 frustumCorners = Matrix4x4.identity;

            float fovWHalf = CAMERA_FOV * 0.5f;

            Vector3 toRight = camera.transform.right * CAMERA_NEAR * Mathf.Tan(fovWHalf * Mathf.Deg2Rad) * CAMERA_ASPECT_RATIO;
            Vector3 toTop = camera.transform.up * CAMERA_NEAR * Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

            Vector3 topLeft = (camera.transform.forward * CAMERA_NEAR - toRight + toTop);
            float CAMERA_SCALE = topLeft.magnitude * CAMERA_FAR / CAMERA_NEAR;

            topLeft.Normalize();
            topLeft *= CAMERA_SCALE;

            Vector3 topRight = (camera.transform.forward * CAMERA_NEAR + toRight + toTop);
            topRight.Normalize();
            topRight *= CAMERA_SCALE;

            Vector3 bottomRight = (camera.transform.forward * CAMERA_NEAR + toRight - toTop);
            bottomRight.Normalize();
            bottomRight *= CAMERA_SCALE;

            Vector3 bottomLeft = (camera.transform.forward * CAMERA_NEAR - toRight - toTop);
            bottomLeft.Normalize();
            bottomLeft *= CAMERA_SCALE;

            frustumCorners.SetRow(0, topLeft);
            frustumCorners.SetRow(1, topRight);
            frustumCorners.SetRow(2, bottomRight);
            frustumCorners.SetRow(3, bottomLeft);

            Shader.SetGlobalMatrix(ShaderConstants._frustumCorners, frustumCorners);
        }

        public void Release()
        {
            ReleaseTexture(TransmittanceTexture);
            ReleaseTexture(ScatteringTexture);
            ReleaseTexture(IrradianceTexture);
            ReleaseTexture(OptionalSingleMieScatteringTexture);
        }

        public void ExecutePrecomputation(ScriptableRenderContext context, ComputeShader precomputationCS)
        {
            const string kBrenetonComputeLookupsTag = "Bruneton Compute Lookups";
            const int kNumScatteringOrders = 4;

            CommandBuffer cmd = CommandBufferPool.Get(kBrenetonComputeLookupsTag);

            this.DoPrecomputation(precomputationCS, kNumScatteringOrders);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void ExecuteSkyBox(ScriptableRenderContext context, Camera camera, Material renderSkyMat)
        {
            const string kBrenetonSkyRenderTag = "Bruneton Sky Render";

            CommandBuffer cmd = CommandBufferPool.Get(kBrenetonSkyRenderTag);

            // Bind common shader uniforms for both precomputation and skybox-render stages.

            /*
            this.BindToMaterial(renderSkyMat);

            // Bind shader uniform specific to the skybox-render.

            renderSkyMat.SetFloat("sky_exposure", UseLuminance != LUMINANCE.NONE ? (float)Exposure * 1e-5f : (float)Exposure);
            renderSkyMat.SetVector("earth_center", new Vector3(0.0f, -(float)this.BottomRadius / (float)this.LengthUnitInMeters, 0.0f));
            renderSkyMat.SetVector("sun_size", new Vector2(Mathf.Tan((float)this.SunAngularRadius), Mathf.Cos((float)this.SunAngularRadius)));
            renderSkyMat.SetVector("sun_direction", -RenderSettings.sun.transform.forward);

            double white_point_r = 1.0;
            double white_point_g = 1.0;
            double white_point_b = 1.0;
            if (DoWhiteBalance)
            {
                this.ConvertSpectrumToLinearSrgb(out white_point_r, out white_point_g, out white_point_b);

                double white_point = (white_point_r + white_point_g + white_point_b) / 3.0;
                white_point_r /= white_point;
                white_point_g /= white_point;
                white_point_b /= white_point;
            }

            renderSkyMat.SetVector("white_point", new Vector3((float)white_point_r, (float)white_point_g, (float)white_point_b));

            float CAMERA_FOV = camera.fieldOfView;
            float CAMERA_ASPECT_RATIO = camera.aspect;
            float CAMERA_NEAR = camera.nearClipPlane;
            float CAMERA_FAR = camera.farClipPlane;

            Matrix4x4 frustumCorners = Matrix4x4.identity;

            float fovWHalf = CAMERA_FOV * 0.5f;

            Vector3 toRight = camera.transform.right * CAMERA_NEAR * Mathf.Tan(fovWHalf * Mathf.Deg2Rad) * CAMERA_ASPECT_RATIO;
            Vector3 toTop = camera.transform.up * CAMERA_NEAR * Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

            Vector3 topLeft = (camera.transform.forward * CAMERA_NEAR - toRight + toTop);
            float CAMERA_SCALE = topLeft.magnitude * CAMERA_FAR / CAMERA_NEAR;

            topLeft.Normalize();
            topLeft *= CAMERA_SCALE;

            Vector3 topRight = (camera.transform.forward * CAMERA_NEAR + toRight + toTop);
            topRight.Normalize();
            topRight *= CAMERA_SCALE;

            Vector3 bottomRight = (camera.transform.forward * CAMERA_NEAR + toRight - toTop);
            bottomRight.Normalize();
            bottomRight *= CAMERA_SCALE;

            Vector3 bottomLeft = (camera.transform.forward * CAMERA_NEAR - toRight - toTop);
            bottomLeft.Normalize();
            bottomLeft *= CAMERA_SCALE;

            frustumCorners.SetRow(0, topLeft);
            frustumCorners.SetRow(1, topRight);
            frustumCorners.SetRow(2, bottomRight);
            frustumCorners.SetRow(3, bottomLeft);

            renderSkyMat.SetMatrix("frustumCorners", frustumCorners);
            */

            cmd.DrawProcedural(Matrix4x4.identity, renderSkyMat, 0, MeshTopology.Triangles, 6);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private BrunetonModel()
        {}

        /// <summary>
        /// The Init method precomputes the atmosphere textures. It first allocates the
        /// temporary resources it needs, then calls Precompute to do the
        /// actual precomputations, and finally destroys the temporary resources.
        ///
        /// Note that there are two precomputation modes here, depending on whether we
        /// want to store precomputed irradiance or illuminance values:
        ///
        /// In precomputed irradiance mode, we simply need to call
        /// Precompute with the 3 wavelengths for which we want to precompute
        /// irradiance, namely kLambdaR, kLambdaG, kLambdaB(with the identity matrix for
        /// luminance_from_radiance, since we don't want any conversion from radiance to luminance).
        /// 
        /// In precomputed illuminance mode, we need to precompute irradiance for
        /// num_precomputed_wavelengths, and then integrate the results,
        /// multiplied with the 3 CIE xyz color matching functions and the XYZ to sRGB
        /// matrix to get sRGB illuminance values.
        /// A naive solution would be to allocate temporary textures for the
        /// intermediate irradiance results, then perform the integration from irradiance
        /// to illuminance and store the result in the final precomputed texture. In
        /// pseudo-code (and assuming one wavelength per texture instead of 3):
        ///  
        ///  create n temporary irradiance textures
        ///  for each wavelength lambda in the n wavelengths:
        ///     precompute irradiance at lambda into one of the temporary textures
        ///  initializes the final illuminance texture with zeros
        ///  for each wavelength lambda in the n wavelengths:
        ///     accumulate in the final illuminance texture the product of the
        ///     precomputed irradiance at lambda (read from the temporary textures)
        ///     with the value of the 3 sRGB color matching functions at lambda 
        ///     (i.e. the product of the XYZ to sRGB matrix with the CIE xyz color matching functions).
        ///  
        /// However, this be would waste GPU memory. Instead, we can avoid allocating
        /// temporary irradiance textures, by merging the two above loops:
        ///  
        ///   for each wavelength lambda in the n wavelengths:
        ///     accumulate in the final illuminance texture (or, for the first
        ///     iteration, set this texture to) the product of the precomputed
        ///     irradiance at lambda (computed on the fly) with the value of the 3
        ///     sRGB color matching functions at lambda.
        ///  
        /// This is the method we use below, with 3 wavelengths per iteration instead
        /// of 1, using Precompute to compute 3 irradiances values per
        /// iteration, and luminance_from_radiance to multiply 3 irradiances
        /// with the values of the 3 sRGB color matching functions at 3 different
        /// wavelengths (yielding a 3x3 matrix).
        ///
        /// This yields the following implementation:
        /// </summary>
        private void DoPrecomputation(ComputeShader compute, int num_scattering_orders)
        {
            // The precomputations require temporary textures, in particular to store the
            // contribution of one scattering order, which is needed to compute the next
            // order of scattering (the final precomputed textures store the sum of all
            // the scattering orders). We allocate them here, and destroy them at the end
            // of this method.
            TextureBuffer buffer = new TextureBuffer(HalfPrecision);
            buffer.Clear(compute);

            // The actual precomputations depend on whether we want to store precomputed
            // irradiance or illuminance values.
            if (NumPrecomputedWavelengths <= 3)
            {
                Precompute(compute, buffer, null, null, false, num_scattering_orders);
            }
            else
            {
                int num_iterations = (NumPrecomputedWavelengths + 2) / 3;
                double dlambda = (kLambdaMax - kLambdaMin) / (3.0 * num_iterations);

                for (int i = 0; i < num_iterations; ++i)
                {
                    double[] lambdas = new double[]
                    {
                        kLambdaMin + (3 * i + 0.5) * dlambda,
                        kLambdaMin + (3 * i + 1.5) * dlambda,
                        kLambdaMin + (3 * i + 2.5) * dlambda
                    };

                    double[] luminance_from_radiance = new double[]
                    {
                        Coeff(lambdas[0], 0) * dlambda, Coeff(lambdas[1], 0) * dlambda, Coeff(lambdas[2], 0) * dlambda,
                        Coeff(lambdas[0], 1) * dlambda, Coeff(lambdas[1], 1) * dlambda, Coeff(lambdas[2], 1) * dlambda,
                        Coeff(lambdas[0], 2) * dlambda, Coeff(lambdas[1], 2) * dlambda, Coeff(lambdas[2], 2) * dlambda
                    };

                    bool blend = i > 0;
                    Precompute(compute, buffer, lambdas, luminance_from_radiance, blend, num_scattering_orders);
                }

                // After the above iterations, the transmittance texture contains the
                // transmittance for the 3 wavelengths used at the last iteration. But we
                // want the transmittance at kLambdaR, kLambdaG, kLambdaB instead, so we
                // must recompute it here for these 3 wavelengths:
                int compute_transmittance = compute.FindKernel("ComputeTransmittance");
                BindToCompute(compute, null, null);
                compute.SetTexture(compute_transmittance, "transmittanceWrite", buffer.TransmittanceArray[WRITE]);
                compute.SetVector("blend", new Vector4(0, 0, 0, 0));

                int NUM = CONSTANTS.NUM_THREADS;
                compute.Dispatch(compute_transmittance, CONSTANTS.TRANSMITTANCE_WIDTH / NUM, CONSTANTS.TRANSMITTANCE_HEIGHT / NUM, 1);
                Swap(buffer.TransmittanceArray);
            }

            //Grab ref to textures and mark as null in buffer so they are not released.
            TransmittanceTexture = buffer.TransmittanceArray[READ];
            buffer.TransmittanceArray[READ] = null;

            ScatteringTexture = buffer.ScatteringArray[READ];
            buffer.ScatteringArray[READ] = null;

            IrradianceTexture = buffer.IrradianceArray[READ];
            buffer.IrradianceArray[READ] = null;

            if(CombineScatteringTextures)
            {
                OptionalSingleMieScatteringTexture = null;
            }
            else
            {
                OptionalSingleMieScatteringTexture = buffer.OptionalSingleMieScatteringArray[READ];
                buffer.OptionalSingleMieScatteringArray[READ] = null;
            }

            // Delete the temporary resources allocated at the begining of this method.
            buffer.Release();
        }

        private double Coeff(double lambda, int component)
        {
            // Note that we don't include MAX_LUMINOUS_EFFICACY here, to avoid
            // artefacts due to too large values when using half precision on GPU.
            // We add this term back in kAtmosphereShader, via
            // SKY_SPECTRAL_RADIANCE_TO_LUMINANCE (see also the comments in the
            // Model constructor).
            double x = CieColorMatchingFunctionTableValue(lambda, 1);
            double y = CieColorMatchingFunctionTableValue(lambda, 2);
            double z = CieColorMatchingFunctionTableValue(lambda, 3);
            double sRGB = CONSTANTS.XYZ_TO_SRGB[component * 3 + 0] * x +
                          CONSTANTS.XYZ_TO_SRGB[component * 3 + 1] * y +
                          CONSTANTS.XYZ_TO_SRGB[component * 3 + 2] * z;

            return sRGB;
        }

        private void BindDensityLayer(ComputeShader compute, DensityProfileLayer layer)
        {
            compute.SetFloat(layer.Name + "_width", (float)(layer.Width / LengthUnitInMeters));
            compute.SetFloat(layer.Name + "_exp_term", (float)layer.ExpTerm);
            compute.SetFloat(layer.Name + "_exp_scale", (float)(layer.ExpScale * LengthUnitInMeters));
            compute.SetFloat(layer.Name + "_linear_term", (float)(layer.LinearTerm * LengthUnitInMeters));
            compute.SetFloat(layer.Name + "_constant_term", (float)layer.ConstantTerm);
        }

        private void BindDensityLayer(CommandBuffer cmd, DensityProfileLayer layer)
        {
            cmd.SetGlobalFloat(layer.Name + "_width", (float)(layer.Width / LengthUnitInMeters));
            cmd.SetGlobalFloat(layer.Name + "_exp_term", (float)layer.ExpTerm);
            cmd.SetGlobalFloat(layer.Name + "_exp_scale", (float)(layer.ExpScale * LengthUnitInMeters));
            cmd.SetGlobalFloat(layer.Name + "_linear_term", (float)(layer.LinearTerm * LengthUnitInMeters));
            cmd.SetGlobalFloat(layer.Name + "_constant_term", (float)layer.ConstantTerm);
        }

        private void BindDensityLayer(DensityProfileLayer layer)
        {
            Shader.SetGlobalFloat(layer.Name + "_width", (float)(layer.Width / LengthUnitInMeters));
            Shader.SetGlobalFloat(layer.Name + "_exp_term", (float)layer.ExpTerm);
            Shader.SetGlobalFloat(layer.Name + "_exp_scale", (float)(layer.ExpScale * LengthUnitInMeters));
            Shader.SetGlobalFloat(layer.Name + "_linear_term", (float)(layer.LinearTerm * LengthUnitInMeters));
            Shader.SetGlobalFloat(layer.Name + "_constant_term", (float)layer.ConstantTerm);
        }

        private Vector3 ToVector(IList<double> wavelengths, IList<double> v, IList<double> lambdas, double scale)
        {
            double r = Interpolate(wavelengths, v, lambdas[0]) * scale;
            double g = Interpolate(wavelengths, v, lambdas[1]) * scale;
            double b = Interpolate(wavelengths, v, lambdas[2]) * scale;

            return new Vector3((float)r, (float)g, (float)b);
        }

        /// <summary>
        /// Finally, we need a utility function to compute the value of the conversion
        /// constants *_RADIANCE_TO_LUMINANCE, used above to convert the
        /// spectral results into luminance values. These are the constants k_r, k_g, k_b
        /// described in Section 14.3 of <a href="https://arxiv.org/pdf/1612.04336.pdf">A
        /// Qualitative and Quantitative Evaluation of 8 Clear Sky Models</a>.
        ///
        /// Computing their value requires an integral of a function times a CIE color
        /// matching function. Thus, we first need functions to interpolate an arbitrary
        /// function (specified by some samples), and a CIE color matching function
        /// (specified by tabulated values), at an arbitrary wavelength. This is the purpose
        /// of the following two functions:
        /// </summary>
        private static double CieColorMatchingFunctionTableValue(double wavelength, int column)
        {
            if (wavelength <= kLambdaMin || wavelength >= kLambdaMax) return 0.0;

            double u = (wavelength - kLambdaMin) / 5.0;
            int row = (int)Math.Floor(u);

            u -= row;
            return CONSTANTS.CIE_2_DEG_COLOR_MATCHING_FUNCTIONS[4 * row + column] * (1.0 - u) + CONSTANTS.CIE_2_DEG_COLOR_MATCHING_FUNCTIONS[4 * (row + 1) + column] * u;
        }

        private static double Interpolate(IList<double> wavelengths, IList<double> wavelength_function, double wavelength)
        {
            if (wavelength < wavelengths[0]) return wavelength_function[0];

            for (int i = 0; i < wavelengths.Count - 1; ++i)
            {
                if (wavelength < wavelengths[i + 1])
                {
                    double u = (wavelength - wavelengths[i]) / (wavelengths[i + 1] - wavelengths[i]);
                    return wavelength_function[i] * (1.0 - u) + wavelength_function[i + 1] * u;
                }
            }

            return wavelength_function[wavelength_function.Count - 1];
        }

        /// <summary>
        ///  Compute the values for the SKY_RADIANCE_TO_LUMINANCE constant. In theory
        /// this should be 1 in precomputed illuminance mode (because the precomputed
        /// textures already contain illuminance values). In practice, however, storing
        /// true illuminance values in half precision textures yields artefacts
        /// (because the values are too large), so we store illuminance values divided
        /// by MAX_LUMINOUS_EFFICACY instead. This is why, in precomputed illuminance
        /// mode, we set SKY_RADIANCE_TO_LUMINANCE to MAX_LUMINOUS_EFFICACY.
        /// </summary>
        private void SkySunRadianceToLuminance(out Vector3 skySpectralRadianceToLuminance, out Vector3 sunSpectralRadianceToLuminance)
        {
            bool precompute_illuminance = NumPrecomputedWavelengths > 3;
            double sky_k_r, sky_k_g, sky_k_b;

            if (precompute_illuminance)
                sky_k_r = sky_k_g = sky_k_b = CONSTANTS.MAX_LUMINOUS_EFFICACY;
            else
                ComputeSpectralRadianceToLuminanceFactors(Wavelengths, SolarIrradiance, -3, out sky_k_r, out sky_k_g, out sky_k_b);

            // Compute the values for the SUN_RADIANCE_TO_LUMINANCE constant.
            double sun_k_r, sun_k_g, sun_k_b;
            ComputeSpectralRadianceToLuminanceFactors(Wavelengths, SolarIrradiance, 0, out sun_k_r, out sun_k_g, out sun_k_b);

            skySpectralRadianceToLuminance = new Vector3((float)sky_k_r, (float)sky_k_g, (float)sky_k_b);
            sunSpectralRadianceToLuminance = new Vector3((float)sun_k_r, (float)sun_k_g, (float)sun_k_b);
        }

        /// <summary>
        /// We can then implement a utility function to compute the "spectral radiance to
        /// luminance" conversion constants (see Section 14.3 in <a
        /// href="https://arxiv.org/pdf/1612.04336.pdf">A Qualitative and Quantitative
        /// Evaluation of 8 Clear Sky Models</a> for their definitions):
        /// The returned constants are in lumen.nm / watt.
        /// </summary>
        private static void ComputeSpectralRadianceToLuminanceFactors(IList<double> wavelengths, IList<double> solar_irradiance, double lambda_power, out double k_r, out double k_g, out double k_b)
        {
            k_r = 0.0;
            k_g = 0.0;
            k_b = 0.0;
            double solar_r = Interpolate(wavelengths, solar_irradiance, kLambdaR);
            double solar_g = Interpolate(wavelengths, solar_irradiance, kLambdaG);
            double solar_b = Interpolate(wavelengths, solar_irradiance, kLambdaB);
            int dlambda = 1;

            for (int lambda = kLambdaMin; lambda < kLambdaMax; lambda += dlambda)
            {
                double x_bar = CieColorMatchingFunctionTableValue(lambda, 1);
                double y_bar = CieColorMatchingFunctionTableValue(lambda, 2);
                double z_bar = CieColorMatchingFunctionTableValue(lambda, 3);

                double[] xyz2srgb = CONSTANTS.XYZ_TO_SRGB;
                double r_bar = xyz2srgb[0] * x_bar + xyz2srgb[1] * y_bar + xyz2srgb[2] * z_bar;
                double g_bar = xyz2srgb[3] * x_bar + xyz2srgb[4] * y_bar + xyz2srgb[5] * z_bar;
                double b_bar = xyz2srgb[6] * x_bar + xyz2srgb[7] * y_bar + xyz2srgb[8] * z_bar;
                double irradiance = Interpolate(wavelengths, solar_irradiance, lambda);

                k_r += r_bar * irradiance / solar_r * Math.Pow(lambda / kLambdaR, lambda_power);
                k_g += g_bar * irradiance / solar_g * Math.Pow(lambda / kLambdaG, lambda_power);
                k_b += b_bar * irradiance / solar_b * Math.Pow(lambda / kLambdaB, lambda_power);
            }

            k_r *= CONSTANTS.MAX_LUMINOUS_EFFICACY * dlambda;
            k_g *= CONSTANTS.MAX_LUMINOUS_EFFICACY * dlambda;
            k_b *= CONSTANTS.MAX_LUMINOUS_EFFICACY * dlambda;
        }

        /// <summary>
        /// The utility method ConvertSpectrumToLinearSrgb is implemented
        /// with a simple numerical integration of the given function, times the CIE color
        /// matching funtions(with an integration step of 1nm), followed by a matrix
        /// multiplication:
        /// </summary>
        private void ConvertSpectrumToLinearSrgb(out double r, out double g, out double b)
        {
            double x = 0.0;
            double y = 0.0;
            double z = 0.0;
            const int dlambda = 1;
            for (int lambda = kLambdaMin; lambda < kLambdaMax; lambda += dlambda)
            {
                double value = Interpolate(Wavelengths, SolarIrradiance, lambda);
                x += CieColorMatchingFunctionTableValue(lambda, 1) * value;
                y += CieColorMatchingFunctionTableValue(lambda, 2) * value;
                z += CieColorMatchingFunctionTableValue(lambda, 3) * value;
            }

            double[] XYZ_TO_SRGB = CONSTANTS.XYZ_TO_SRGB;
            r = CONSTANTS.MAX_LUMINOUS_EFFICACY * (XYZ_TO_SRGB[0] * x + XYZ_TO_SRGB[1] * y + XYZ_TO_SRGB[2] * z) * dlambda;
            g = CONSTANTS.MAX_LUMINOUS_EFFICACY * (XYZ_TO_SRGB[3] * x + XYZ_TO_SRGB[4] * y + XYZ_TO_SRGB[5] * z) * dlambda;
            b = CONSTANTS.MAX_LUMINOUS_EFFICACY * (XYZ_TO_SRGB[6] * x + XYZ_TO_SRGB[7] * y + XYZ_TO_SRGB[8] * z) * dlambda;
        }

        /// <summary>
        /// Finally, we provide the actual implementation of the precomputation algorithm
        /// described in Algorithm 4.1 of
        /// <a href="https://hal.inria.fr/inria-00288758/en">our paper</a>. Each step is
        /// explained by the inline comments below.
        /// </summary>
        private void Precompute(
            ComputeShader compute,
            TextureBuffer buffer,
            double[] lambdas,
            double[] luminance_from_radiance,
            bool blend,
            int num_scattering_orders)
        {

            int BLEND = blend ? 1 : 0;
            int NUM_THREADS = CONSTANTS.NUM_THREADS;

            BindToCompute(compute, lambdas, luminance_from_radiance);

            int compute_transmittance = compute.FindKernel("ComputeTransmittance");
            int compute_direct_irradiance = compute.FindKernel("ComputeDirectIrradiance");
            int compute_single_scattering = compute.FindKernel("ComputeSingleScattering");
            int compute_scattering_density = compute.FindKernel("ComputeScatteringDensity");
            int compute_indirect_irradiance = compute.FindKernel("ComputeIndirectIrradiance");
            int compute_multiple_scattering = compute.FindKernel("ComputeMultipleScattering");

            // Compute the transmittance, and store it in transmittance_texture
            compute.SetTexture(compute_transmittance, "transmittanceWrite", buffer.TransmittanceArray[WRITE]);
            compute.SetVector("blend", new Vector4(0, 0, 0, 0));

            compute.Dispatch(compute_transmittance, CONSTANTS.TRANSMITTANCE_WIDTH / NUM_THREADS, CONSTANTS.TRANSMITTANCE_HEIGHT / NUM_THREADS, 1);
            Swap(buffer.TransmittanceArray);

            // Compute the direct irradiance, store it in delta_irradiance_texture and,
            // depending on 'blend', either initialize irradiance_texture_ with zeros or
            // leave it unchanged (we don't want the direct irradiance in
            // irradiance_texture_, but only the irradiance from the sky).
            compute.SetTexture(compute_direct_irradiance, "deltaIrradianceWrite", buffer.DeltaIrradianceTexture); //0
            compute.SetTexture(compute_direct_irradiance, "irradianceWrite", buffer.IrradianceArray[WRITE]); //1
            compute.SetTexture(compute_direct_irradiance, "irradianceRead", buffer.IrradianceArray[READ]);
            compute.SetTexture(compute_direct_irradiance, "transmittanceRead", buffer.TransmittanceArray[READ]);
            compute.SetVector("blend", new Vector4(0, BLEND, 0, 0));
            compute.Dispatch(compute_direct_irradiance, CONSTANTS.IRRADIANCE_WIDTH / NUM_THREADS, CONSTANTS.IRRADIANCE_HEIGHT / NUM_THREADS, 1);
            Swap(buffer.IrradianceArray);

            // Compute the rayleigh and mie single scattering, store them in
            // delta_rayleigh_scattering_texture and delta_mie_scattering_texture, and
            // either store them or accumulate them in scattering_texture_ and
            // optional_single_mie_scattering_texture_.
            compute.SetTexture(compute_single_scattering, "deltaRayleighScatteringWrite", buffer.DeltaRayleighScatteringTexture); //0
            compute.SetTexture(compute_single_scattering, "deltaMieScatteringWrite", buffer.DeltaMieScatteringTexture); //1
            compute.SetTexture(compute_single_scattering, "scatteringWrite", buffer.ScatteringArray[WRITE]); //2
            compute.SetTexture(compute_single_scattering, "scatteringRead", buffer.ScatteringArray[READ]);
            compute.SetTexture(compute_single_scattering, "singleMieScatteringWrite", buffer.OptionalSingleMieScatteringArray[WRITE]); //3
            compute.SetTexture(compute_single_scattering, "singleMieScatteringRead", buffer.OptionalSingleMieScatteringArray[READ]);
            compute.SetTexture(compute_single_scattering, "transmittanceRead", buffer.TransmittanceArray[READ]);
            compute.SetVector("blend", new Vector4(0, 0, BLEND, BLEND));

            for (int layer = 0; layer < CONSTANTS.SCATTERING_DEPTH; ++layer)
            {
                compute.SetInt("layer", layer);
                compute.Dispatch(compute_single_scattering, CONSTANTS.SCATTERING_WIDTH / NUM_THREADS, CONSTANTS.SCATTERING_HEIGHT / NUM_THREADS, 1);
            }
            Swap(buffer.ScatteringArray);
            Swap(buffer.OptionalSingleMieScatteringArray);

            // Compute the 2nd, 3rd and 4th order of scattering, in sequence.
            for (int scattering_order = 2; scattering_order <= num_scattering_orders; ++scattering_order)
            {
                // Compute the scattering density, and store it in
                // delta_scattering_density_texture.
                compute.SetTexture(compute_scattering_density, "deltaScatteringDensityWrite", buffer.DeltaScatteringDensityTexture); //0
                compute.SetTexture(compute_scattering_density, "transmittanceRead", buffer.TransmittanceArray[READ]);
                compute.SetTexture(compute_scattering_density, "singleRayleighScatteringRead", buffer.DeltaRayleighScatteringTexture);
                compute.SetTexture(compute_scattering_density, "singleMieScatteringRead", buffer.DeltaMieScatteringTexture);
                compute.SetTexture(compute_scattering_density, "multipleScatteringRead", buffer.DeltaMultipleScatteringTexture);
                compute.SetTexture(compute_scattering_density, "irradianceRead", buffer.DeltaIrradianceTexture);
                compute.SetInt("scatteringOrder", scattering_order);
                compute.SetVector("blend", new Vector4(0, 0, 0, 0));

                for (int layer = 0; layer < CONSTANTS.SCATTERING_DEPTH; ++layer)
                {
                    compute.SetInt("layer", layer);
                    compute.Dispatch(compute_scattering_density, CONSTANTS.SCATTERING_WIDTH / NUM_THREADS, CONSTANTS.SCATTERING_HEIGHT / NUM_THREADS, 1);
                }

                // Compute the indirect irradiance, store it in delta_irradiance_texture and
                // accumulate it in irradiance_texture_.
                compute.SetTexture(compute_indirect_irradiance, "deltaIrradianceWrite", buffer.DeltaIrradianceTexture); //0
                compute.SetTexture(compute_indirect_irradiance, "irradianceWrite", buffer.IrradianceArray[WRITE]); //1
                compute.SetTexture(compute_indirect_irradiance, "irradianceRead", buffer.IrradianceArray[READ]);
                compute.SetTexture(compute_indirect_irradiance, "singleRayleighScatteringRead", buffer.DeltaRayleighScatteringTexture);
                compute.SetTexture(compute_indirect_irradiance, "singleMieScatteringRead", buffer.DeltaMieScatteringTexture);
                compute.SetTexture(compute_indirect_irradiance, "multipleScatteringRead", buffer.DeltaMultipleScatteringTexture);
                compute.SetInt("scatteringOrder", scattering_order - 1);
                compute.SetVector("blend", new Vector4(0, 1, 0, 0));

                compute.Dispatch(compute_indirect_irradiance, CONSTANTS.IRRADIANCE_WIDTH / NUM_THREADS, CONSTANTS.IRRADIANCE_HEIGHT / NUM_THREADS, 1);
                Swap(buffer.IrradianceArray);

                // Compute the multiple scattering, store it in
                // delta_multiple_scattering_texture, and accumulate it in
                // scattering_texture_.
                compute.SetTexture(compute_multiple_scattering, "deltaMultipleScatteringWrite", buffer.DeltaMultipleScatteringTexture); //0
                compute.SetTexture(compute_multiple_scattering, "scatteringWrite", buffer.ScatteringArray[WRITE]); //1
                compute.SetTexture(compute_multiple_scattering, "scatteringRead", buffer.ScatteringArray[READ]);
                compute.SetTexture(compute_multiple_scattering, "transmittanceRead", buffer.TransmittanceArray[READ]);
                compute.SetTexture(compute_multiple_scattering, "deltaScatteringDensityRead", buffer.DeltaScatteringDensityTexture);
                compute.SetVector("blend", new Vector4(0, 1, 0, 0));

                for (int layer = 0; layer < CONSTANTS.SCATTERING_DEPTH; ++layer)
                {
                    compute.SetInt("layer", layer);
                    compute.Dispatch(compute_multiple_scattering, CONSTANTS.SCATTERING_WIDTH / NUM_THREADS, CONSTANTS.SCATTERING_HEIGHT / NUM_THREADS, 1);
                }
                Swap(buffer.ScatteringArray);
            }

            return;
        }

        private void Swap(RenderTexture[] arr)
        {
            RenderTexture tmp = arr[READ];
            arr[READ] = arr[WRITE];
            arr[WRITE] = tmp;
        }

        private void ReleaseTexture(RenderTexture tex)
        {
            if (tex == null) return;
            GameObject.DestroyImmediate(tex);
        }

        /// <summary>
        /// Convert a double array to a float matrix to bind to compute shader.
        /// Array is transposed.
        /// </summary>
        private float[] ToMatrix(double[] arr)
        {
            return new float[]
            {
                    (float)arr[0], (float)arr[3], (float)arr[6], 0,
                    (float)arr[1], (float)arr[4], (float)arr[7], 0,
                    (float)arr[2], (float)arr[5], (float)arr[8], 0,
                    0, 0, 0, 1
            };
        }

    }

}