#ifndef UNITY_PHYSICAL_CAMERA_INCLUDED
#define UNITY_PHYSICAL_CAMERA_INCLUDED

// Has to be kept in sync with ColorUtils.cs

// References:
// "Moving Frostbite to PBR" (Sebastien Lagarde & Charles de Rousiers)
//   https://seblagarde.files.wordpress.com/2015/07/course_notes_moving_frostbite_to_pbr_v32.pdf
// "Implementing a Physically Based Camera" (Padraic Hennessy)
//   https://placeholderart.wordpress.com/2014/11/16/implementing-a-physically-based-camera-understanding-exposure/

float ComputeEV100(float aperture, float shutterSpeed, float ISO)
{
    // EV number is defined as:
    //   2^ EV_s = N^2 / t and EV_s = EV_100 + log2 (S /100)
    // This gives
    //   EV_s = log2 (N^2 / t)
    //   EV_100 + log2 (S /100) = log2 (N^2 / t)
    //   EV_100 = log2 (N^2 / t) - log2 (S /100)
    //   EV_100 = log2 (N^2 / t . 100 / S)
    return log2((aperture * aperture) / shutterSpeed * 100.0 / ISO);
}

float ComputeEV100FromAvgLuminance(float avgLuminance, float calibrationConstant)
{
    const float K = calibrationConstant;
    return log2(avgLuminance * 100.0 / K);
}

float ComputeEV100FromAvgLuminance(float avgLuminance)
{
    // We later use the middle gray at 12.7% in order to have
    // a middle gray at 18% with a sqrt(2) room for specular highlights
    // But here we deal with the spot meter measuring the middle gray
    // which is fixed at 12.5 for matching standard camera
    // constructor settings (i.e. calibration constant K = 12.5)
    // Reference: http://en.wikipedia.org/wiki/Film_speed
    const float K = 12.5; // Reflected-light meter calibration constant
    return ComputeEV100FromAvgLuminance(avgLuminance, K);
}

float ConvertEV100ToExposure(float EV100, float exposureScale)
{
    // Compute the maximum luminance possible with H_sbs sensitivity
    // maxLum = 78 / ( S * q ) * N^2 / t
    //        = 78 / ( S * q ) * 2^ EV_100
    //        = 78 / (100 * s_LensAttenuation) * 2^ EV_100
    //        = exposureScale * 2^ EV
    // Reference: http://en.wikipedia.org/wiki/Film_speed
    float maxLuminance = exposureScale * pow(2.0, EV100);
    return 1.0 / maxLuminance;
}

float ConvertEV100ToExposure(float EV100)
{
    const float exposureScale = 1.2;
    return ConvertEV100ToExposure(EV100, exposureScale);
}

float ComputeISO(float aperture, float shutterSpeed, float targetEV100)
{
    // Compute the required ISO to reach the target EV100
    return ((aperture * aperture) * 100.0) / (shutterSpeed * pow(2.0, targetEV100));
}

float ComputeLuminanceAdaptation(float previousLuminance, float currentLuminance, float speedDarkToLight, float speedLightToDark, float deltaTime)
{
    float delta = currentLuminance - previousLuminance;
    float speed = delta > 0.0 ? speedDarkToLight : speedLightToDark;

    // Exponential decay
    return previousLuminance + delta * (1.0 - exp2(-deltaTime * speed));
}

#endif // UNITY_PHYSICAL_CAMERA_INCLUDED
