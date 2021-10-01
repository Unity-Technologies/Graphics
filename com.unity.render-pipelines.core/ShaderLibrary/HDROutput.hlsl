// Important! This file assumes Color.hlsl and ACES.hlsl has been already included.
#ifndef HDROUTPUT_INCLUDED
#define HDROUTPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/HDROutputDefines.cs.hlsl"

#if defined(HDR_OUTPUT_REC2020) || defined(HDR_OUTPUT_SCRGB)
#define HDR_OUTPUT
#endif

// A bit of nomenclature that will be used in the file:
// Gamut: It is the subset of colors that is possible to reproduce by using three specific primary colors.
// Rec709 (ITU-R Recommendation BT709) is a HDTV standard, in our context, we mostly care about its color gamut (https://en.wikipedia.org/wiki/Rec._709). The Rec709 gamut is the same as BT1886 and sRGB.
// Rec2020 (ITU-R Recommendation BT2020) is an UHDTV standard. As above, we mostly reference it w.r.t. the color gamut. (https://en.wikipedia.org/wiki/Rec._2020). Nice property is that all primaries are on the locus.
// DCI-P3 (or just P3): is a gamut used in cinema grading and used by iPhone for example.
// ACEScg: A gamut that is larger than Rec2020.
// ACES2065-1: A gamut that covers the full XYZ space, part of the ACES specs. Mostly used for storage since it is harder to work with than ACEScg.
// WCG: Wide color gamut. This is defined as a color gamut that is wider than the Rec709 one.
// LMS: A color space represented by the response of the three cones of human eye (responsivity peaks Long, Medium, Short)
// OETF (Optical Eelectro Transfer Function): This is a function to goes from optical (linear light) to electro (signal transmitted to the display).
// EOTF (Eelectro Optical  Transfer Function): The inverse of the OETF, used by the TV/Monitor.
// EETF (Eelectro-Electro Transfer Function): This is generally just a remapping function, we use the BT2390 EETF to perform range reduction based on the actual display.
// PQ (Perceptual Quantizer): the EOTF used for HDR10 TVs. It works in the range [0, 10000] nits. Important to keep in mind that this represents an absolute intensity and not relative as for SDR. Sometimes this can be referenced as ST2084. As OETF we'll use the inverse of the PQ curve.
// scRGB: a wide color gamut that uses same color space and white point as sRGB, but with much wider coordinates. Used on windows when 16 bit depth is selected. Most of the color space is imaginary colors. Works differently than with PQ.

// --------------------------------
//  Perceptual Quantizer (PQ) / ST 2084
// --------------------------------

#define MAX_PQ_VALUE 10000 // 10k nits is the maximum supported by the standard.

#define PQ_N (2610.0f / 4096.0f / 4.0f)
#define PQ_M (2523.0f / 4096.0f * 128.0f)
#define PQ_C1 (3424.0f / 4096.0f)
#define PQ_C2 (2413.0f / 4096.0f * 32.0f)
#define PQ_C3 (2392.0f / 4096.0f * 32.0f)

float LinearToPQ(float value, float maxPQValue)
{
    value /= maxPQValue;
    float Ym1 = PositivePow(value, PQ_N);
    float n = (PQ_C1 + PQ_C2 * Ym1);
    float d = (1.0f + PQ_C3 * Ym1);
    return PositivePow(n / d, PQ_M);
}

float LinearToPQ(float value)
{
    return LinearToPQ(value, MAX_PQ_VALUE);
}

float3 LinearToPQ(float3 value, float maxPQValue)
{
    float3 outPQ;
    outPQ.x = LinearToPQ(value.x, maxPQValue);
    outPQ.y = LinearToPQ(value.y, maxPQValue);
    outPQ.z = LinearToPQ(value.z, maxPQValue);
    return outPQ;
}

float3 LinearToPQ(float3 value)
{
    return LinearToPQ(value, MAX_PQ_VALUE);
}

float PQToLinear(float value)
{
    float Em2 = PositivePow(value, 1.0f / PQ_M);
    float X = (max(0.0, Em2 - PQ_C1)) / (PQ_C2 - PQ_C3 * Em2);
    return PositivePow(X, 1.0f / PQ_N);
}

float PQToLinear(float value, float maxPQValue)
{
    return PQToLinear(value) * maxPQValue;
}

float3 PQToLinear(float3 value, float maxPQValue)
{
    float3 outLinear;
    outLinear.x = PQToLinear(value.x, maxPQValue);
    outLinear.y = PQToLinear(value.y, maxPQValue);
    outLinear.z = PQToLinear(value.z, maxPQValue);
    return outLinear;
}

float3 PQToLinear(float3 value)
{
    float3 outLinear;
    outLinear.x = PQToLinear(value.x);
    outLinear.y = PQToLinear(value.y);
    outLinear.z = PQToLinear(value.z);
    return outLinear;
}


// --------------------------------------------------------------------------------------------

// --------------------------------
//  Color Space transforms
// --------------------------------
// As any other space transform, changing color space involves a change of basis and therefore a matrix multiplication.
// Note that Rec2020 and Rec2100 share the same color space.

float3 RotateRec709ToRec2020(float3 Rec709Input)
{
    static const float3x3 Rec709ToRec2020Mat = float3x3(

        0.627402, 0.329292, 0.043306,
        0.069095, 0.919544, 0.011360,
        0.016394, 0.088028, 0.895578
    );

    return mul(Rec709ToRec2020Mat, Rec709Input);
}

float3 RotateRec2020ToRec709(float3 Rec2020Input)
{
    static const float3x3 Rec2020ToRec709Mat = float3x3(
         1.660496, -0.587656, -0.072840,
        -0.124547,  1.132895, -0.008348,
        -0.018154, -0.100597,  1.118751
    );
    return mul(Rec2020ToRec709Mat, Rec2020Input);
}

float3 RotateRec709ToOutputSpace(float3 Rec709Input)
{
#ifdef HDR_OUTPUT_SCRGB
    return Rec709Input;
#else
    return RotateRec709ToRec2020(Rec709Input);
#endif
}

float3 RotateRec2020ToOutputSpace(float3 Rec2020Input)
{
#ifdef HDR_OUTPUT_SCRGB
    return RotateRec2020ToRec709(Rec2020Input);
#else
    return Rec2020Input;
#endif
}

float3 RotateRec2020ToLMS(float3 Rec2020Input)
{
    static const float3x3 Rec2020ToLMSMat =
    {
         0.412109375,     0.52392578125, 0.06396484375,
         0.166748046875,  0.720458984375, 0.11279296875,
         0.024169921875,  0.075439453125, 0.900390625
    };

    return mul(Rec2020ToLMSMat, Rec2020Input);
}

float3 Rotate709ToLMS(float3 Rec709Input)
{
    static const float3x3 Rec709ToLMSMat =
    {
         0.412109375,     0.52392578125, 0.06396484375,
         0.166748046875,  0.720458984375, 0.11279296875,
         0.024169921875,  0.075439453125, 0.900390625
    };
    return mul(Rec709ToLMSMat, Rec709Input);
}

// Ref: ICtCp Dolby white paper (https://www.dolby.com/us/en/technologies/dolby-vision/ictcp-white-paper.pdf)
float3 RotatePQLMSToICtCp(float3 LMSInput)
{
    static const float3x3 PQLMSToICtCpMat = float3x3(
        0.5f, 0.5f, 0.0f,
        1.613769f, -3.323486f, 1.709716f,
        4.378174f, -4.245605f, -0.1325683f
        );

    return mul(PQLMSToICtCpMat, LMSInput);
}

float3 RotateLMSToICtCp(float3 lms)
{
    float3 PQLMS = LinearToPQ(max(0.0f, lms));
    return RotatePQLMSToICtCp(PQLMS);
}

float3 RotateRec2020ToICtCp(float3 Rec2020)
{
    float3 lms = RotateRec2020ToLMS(Rec2020);
    float3 PQLMS = LinearToPQ(max(0.0f, lms));
    return RotatePQLMSToICtCp(PQLMS);
}



float3 RotateOutputSpaceToICtCp(float3 inputColor)
{
    // TODO: Do the conversion directly from Rec709 (bake matrix Rec709 -> XYZ -> LMS)
#ifdef HDR_OUTPUT_SCRGB
    inputColor = RotateRec709ToRec2020(inputColor);
#endif
    return RotateRec2020ToICtCp(inputColor);
}

float3 RotateLMSToXYZ(float3 LMSInput)
{
    static const float3x3 LMSToXYZMat = float3x3(
        2.07018005669561320f, -1.32645687610302100f, 0.206616006847855170f,
        0.36498825003265756f, 0.68046736285223520f, -0.045421753075853236f,
        -0.04959554223893212f, -0.04942116118675749f, 1.187995941732803400f
        );
    return mul(LMSToXYZMat, LMSInput);
}

float3 RotateXYZToRec2020(float3 XYZ)
{
    static const float3x3 XYZToRec2020Mat = float3x3(
        1.71235168f, -0.35487896f, -0.25034135f,
        -0.66728621f, 1.61794055f,  0.01495380f,
        0.01763985f, -0.04277060f,  0.94210320f
    );

    return mul(XYZToRec2020Mat, XYZ);
}

float3 RotateXYZToRec709(float3 XYZ)
{
    return mul(XYZ_2_REC709_MAT, XYZ);
}

float3 RotateRec709ToXYZ(float3 rgb)
{
    static const float3x3 Rec709ToXYZMat = float3x3(
        0.412391f, 0.357584f, 0.180481,
        0.212639, 0.715169, 0.0721923,
        0.0193308, 0.119195, 0.950532
        );
    return mul(Rec709ToXYZMat, rgb);
}

float3 RotateRec2020ToXYZ(float3 rgb)
{
    static const float3x3 Rec2020ToXYZMat = float3x3(
        0.638574, 0.144617, 0.167265,
        0.263367, 0.677998, 0.0586353,
        0.0f, 0.0280727, 1.06099
        );

    return mul(Rec2020ToXYZMat, rgb);
}

float3 RotateICtCpToPQLMS(float3 ICtCp)
{
    static const float3x3 ICtCpToPQLMSMat = float3x3(
        1.0f, 0.0086051456939815f, 0.1110356044754732f,
        1.0f, -0.0086051456939815f, -0.1110356044754732f,
        1.0f, 0.5600488595626390f, -0.3206374702321221f
    );

    return mul(ICtCpToPQLMSMat, ICtCp);
}

float3 RotateICtCpToXYZ(float3 ICtCp)
{
    float3 PQLMS = RotateICtCpToPQLMS(ICtCp);
    float3 LMS = PQToLinear(PQLMS, MAX_PQ_VALUE);
    return RotateLMSToXYZ(LMS);
}

float3 RotateICtCpToRec2020(float3 ICtCp)
{
    return RotateXYZToRec2020(RotateICtCpToXYZ(ICtCp));
}

float3 RotateICtCpToRec709(float3 ICtCp)
{
    return RotateXYZToRec709(RotateICtCpToXYZ(ICtCp));
}

float3 RotateICtCpToOutputSpace(float3 ICtCp)
{
#ifdef HDR_OUTPUT_SCRGB
    return RotateICtCpToRec709(ICtCp);
#elif defined(HDR_OUTPUT_REC2020)
    return RotateICtCpToRec2020(ICtCp);
#endif
}

// --------------------------------------------------------------------------------------------

// --------------------------------
//  OETFs
// --------------------------------
// The functions here are OETF, technically for applying the opposite of the PQ curve, we are mapping
// from linear to PQ space as this is what the display expects.
// See this desmos for comparisons https://www.desmos.com/calculator/5jdfc4pgtk
#define PRECISE_PQ 0
#define ISS_APPROX_PQ 1
#define GTS_APPROX_PQ 2

#define OETF_CHOICE GTS_APPROX_PQ

// Ref: [Patry 2017] HDR Display Support in Infamous Second Son and Infamous First Light
// Fastest option, but also the least accurate. Behaves well for values up to 1400 nits but then starts diverging.
// IMPORTANT! It requires the input to be scaled from [0 ... 10000] to [0...100]!
float3 PatryApproxLinToPQ(float3 x)
{
    return (x * (x * (x * (x * (x * 533095.76 + 47438306.2) + 29063622.1) + 575216.76) + 383.09104) + 0.000487781) /
        (x * (x * (x * (x * 66391357.4 + 81884528.2) + 4182885.1) + 10668.404) + 1.0);
}

//  Ref: [Uchimura and Suzuki 2018] Practical HDR and Wide Color Techniques in Gran Turismo Sport
// Slower than Infamous approx, but more precise ( https://www.desmos.com/calculator/up4wwozghk ) in the full [0... 10 000] range, but still faster than reference
// IMPORTANT! It requires the input to be scaled from [0 ... 10000] to [0...100]!
float3 GTSApproxLinToPQ(float3 inputCol)
{
    float3 k = PositivePow((inputCol * 0.01), PQ_N);
    return (3.61972*(1e-8) + k * (0.00102859 + k * (-0.101284 + 2.05784 * k))) /
        (0.0495245 + k * (0.135214 + k * (0.772669 + k)));
}

// IMPORTANT! This wants the input in [0...10000] range, if the method requires scaling, it is done inside this function.
float3 OETF(float3 inputCol)
{
#ifdef HDR_OUTPUT_SCRGB
    // IMPORTANT! This assumes that the maximum nits is always higher or same as 80.0f. Seems like a sensible choice, but revisit if we find weird use cases (just min with the the max nits).
    // We need to map the value 1 to 80 nits.
    return inputCol / 80.0f;
#else
    #if OETF_CHOICE == PRECISE_PQ
    return LinearToPQ(inputCol);
    #elif OETF_CHOICE == ISS_APPROX_PQ
    return PatryApproxLinToPQ(inputCol * 0.01f);
    #elif OETF_CHOICE == GTS_APPROX_PQ
    return GTSApproxLinToPQ(inputCol * 0.01f);
    #endif
#endif

}

#define LIN_TO_PQ_FOR_LUT GTS_APPROX_PQ // GTS is close enough https://www.desmos.com/calculator/up4wwozghk
float3 LinearToPQForLUT(float3 inputCol)
{
#if LIN_TO_PQ_FOR_LUT == PRECISE_PQ
    return LinearToPQ(inputCol);
#elif LIN_TO_PQ_FOR_LUT == ISS_APPROX_PQ
    return PatryApproxLinToPQ(inputCol * 0.01f);
#elif LIN_TO_PQ_FOR_LUT == GTS_APPROX_PQ
    return GTSApproxLinToPQ(inputCol * 0.01f);
#endif
}


// --------------------------------------------------------------------------------------------

// --------------------------------
// Range reduction
// --------------------------------
// This section of the file concerns the way we map from full range to whatever range the display supports.
// Also note, we always tonemap luminance component only, so we need to reach this point after we converted
// to a format such as ICtCp or YCbCr
// See https://www.desmos.com/calculator/pqc3raolms for plots
#define RANGE_REDUCTION HDRRANGEREDUCTION_BT2390LUMA_ONLY

// Note this takes x being in [0...10k nits]
float ReinhardTonemap(float x, float peakValue)
{
    float m = MAX_PQ_VALUE * peakValue / (MAX_PQ_VALUE - peakValue);
    return x * m / (x + m);
}

/// BT2390 EETF Helper functions
float T(float A, float Ks)
{
    return (A - Ks) / (1.0f - Ks);
}

float P(float B, float Ks, float L_max)
{
    float TB2 = T(B, Ks) * T(B, Ks);
    float TB3 = TB2 * T(B, Ks);

    return lerp((TB3 - 2 * TB2 + T(B, Ks)), (2.0f * TB3 - 3.0f * TB2 + 1.0f), Ks) + (-2.0f * TB3 + 3.0f*TB2)*L_max;
}


// Ref: https://www.itu.int/dms_pub/itu-r/opb/rep/R-REP-BT.2390-4-2018-PDF-E.pdf page 21
// This takes values in [0...10k nits] and it outputs in the same space. PQ conversion outside.
// If we chose this, it can be optimized (a few identity happen with moving between linear and PQ)
float BT2390EETF(float x, float minLimit, float maxLimit)
{
    float E_0 = LinearToPQ(x);
    // For the following formulas we are assuming L_B = 0 and L_W = 10000 -- see original paper for full formulation
    float E_1 = E_0;
    float L_min = LinearToPQ(minLimit);
    float L_max = LinearToPQ(maxLimit);
    float Ks = 1.5f * L_max - 0.5f; // Knee start
    float b = L_min;

    float E_2 = E_1 < Ks ? E_1 : P(E_1, Ks, L_max);
    float E3Part = (1.0f - E_2);
    float E3Part2 = E3Part * E3Part;
    float E_3 = E_2 + b * (E3Part2 * E3Part2);
    float E_4 = E_3; // Is like this because PQ(L_W)=  1 and PQ(L_B) = 0

    return PQToLinear(E_4, MAX_PQ_VALUE);
}


float3 PerformRangeReduction(float3 input, float minNits, float maxNits)
{
    float3 ICtCp = RotateOutputSpaceToICtCp(input); // This is in PQ space.
    float linearLuma = PQToLinear(ICtCp.x, MAX_PQ_VALUE);
#if RANGE_REDUCTION == HDRRANGEREDUCTION_REINHARD_LUMA_ONLY
    linearLuma = ReinhardTonemap(linearLuma, maxNits);
#elif RANGE_REDUCTION == HDRRANGEREDUCTION_BT2390LUMA_ONLY
    linearLuma = BT2390EETF(linearLuma, minNits, maxNits);
#endif
    ICtCp.x = LinearToPQ(linearLuma);


    return RotateICtCpToOutputSpace(ICtCp); // This moves back to linear too!
}

float3 PerformRangeReduction(float3 input, float minNits, float maxNits, int mode)
{
    float3 outputValue = input;
    if (mode == HDRRANGEREDUCTION_NONE)
    {
        outputValue = input;
    }
    else if (mode == HDRRANGEREDUCTION_REINHARD_LUMA_ONLY || mode == HDRRANGEREDUCTION_BT2390LUMA_ONLY)
    {
        float3 ICtCp = RotateOutputSpaceToICtCp(input);

        float linearLuma = PQToLinear(ICtCp.x, MAX_PQ_VALUE);
        if (mode == HDRRANGEREDUCTION_REINHARD_LUMA_ONLY)
        {
            linearLuma = ReinhardTonemap(linearLuma, maxNits);
        }
        else if (mode == HDRRANGEREDUCTION_BT2390LUMA_ONLY)
        {
            linearLuma = BT2390EETF(linearLuma, minNits, maxNits);
        }

        ICtCp.x = LinearToPQ(linearLuma);

        outputValue = RotateICtCpToOutputSpace(ICtCp);
    }
    else if (mode == HDRRANGEREDUCTION_REINHARD)
    {
        outputValue.x = ReinhardTonemap(input.x, maxNits);
        outputValue.y = ReinhardTonemap(input.y, maxNits);
        outputValue.z = ReinhardTonemap(input.z, maxNits);
    }
    else if (mode == HDRRANGEREDUCTION_BT2390)
    {
        outputValue.x = BT2390EETF(input.x, minNits, maxNits);
        outputValue.y = BT2390EETF(input.y, minNits, maxNits);
        outputValue.z = BT2390EETF(input.z, minNits, maxNits);
    }
    return outputValue;
}


// --------------------------------------------------------------------------------------------

// --------------------------------
// Public facing functions
// --------------------------------
// These functions are aggregate of most of what we have above. You can think of this as the public API of the HDR Output library.
// Note that throughout HDRP we are assuming that when it comes to the final pass adjustements, our tonemapper has *NOT*
// performed range reduction and everything is assumed to be displayed on a reference 10k nits display and everything post-tonemapping
// is in either the Rec 2020 or Rec709 color space. The Rec709 version just rotate to Rec2020 before going forward if required by the output device.

float3 HDRMappingFromRec2020(float3 Rec2020Input, float paperWhite, float minNits, float maxNits, int reductionMode, bool skipOETF = false)
{
    float3 outputSpaceInput = RotateRec2020ToOutputSpace(Rec2020Input);
    float3 reducedHDR = PerformRangeReduction(outputSpaceInput * paperWhite, minNits, maxNits, reductionMode);

    if (skipOETF) return reducedHDR;

    return OETF(reducedHDR);
}

float3 HDRMappingFromRec709(float3 Rec709Input, float paperWhite, float minNits, float maxNits, int reductionMode, bool skipOETF = false)
{
    float3 outputSpaceInput = RotateRec709ToOutputSpace(Rec709Input);
    float3 reducedHDR = PerformRangeReduction(outputSpaceInput * paperWhite, minNits, maxNits, reductionMode);

    if (skipOETF) return reducedHDR;

    return OETF(reducedHDR);
}


float3 HDRMappingACES(float3 aces, float hdrBoost, int reductionMode, bool skipOETF = false)
{
    aces = (aces * hdrBoost * 0.01f);
    float3 oces = RRT(aces);

    float3 AP1ODT = 0;

    // This is a static branch.
    if (reductionMode == HDRRANGEREDUCTION_ACES1000NITS)
    {
        AP1ODT = ODT_1000nits_ToAP1(oces);
    }
    else if (reductionMode == HDRRANGEREDUCTION_ACES2000NITS)
    {
        AP1ODT = ODT_2000nits_ToAP1(oces);
    }
    else if (reductionMode == HDRRANGEREDUCTION_ACES4000NITS)
    {
        AP1ODT = ODT_4000nits_ToAP1(oces);
    }

    float3 linearODT = 0;
#if defined(HDR_OUTPUT_SCRGB)
    const float3x3 AP1_2_Rec709 = mul(XYZ_2_REC709_MAT, mul(D60_2_D65_CAT, AP1_2_XYZ_MAT));
    linearODT = mul(AP1_2_Rec709, AP1ODT);
#else
    const float3x3 AP1_2_Rec2020 = mul(XYZ_2_REC2020_MAT, mul(D60_2_D65_CAT, AP1_2_XYZ_MAT));
    linearODT = mul(AP1_2_Rec2020, AP1ODT);
#endif
    if (skipOETF) return linearODT;

    return OETF(linearODT);
}

// --------------------------------------------------------------------------------------------

// --------------------------------
// UI Related functions
// --------------------------------

float3 ProcessUIForHDR(float3 uiSample, float paperWhite, float maxNits)
{
#ifdef HDR_OUTPUT_SCRGB
    uiSample.rgb = (uiSample.rgb * paperWhite);
#else
    uiSample.rgb = RotateRec709ToRec2020(uiSample.rgb);
    uiSample.rgb *= paperWhite;
#endif
    return uiSample.rgb;
}

float3 SceneUIComposition(float4 uiSample, float3 sceneColor, float paperWhite, float maxNits)
{
    // Undo the pre multiply.
    uiSample.rgb = uiSample.rgb / (uiSample.a == 0.0f ? 1.0 : uiSample.a);
    uiSample.rgb = ProcessUIForHDR(uiSample.rgb, paperWhite, maxNits);
    return uiSample.rgb * uiSample.a + sceneColor.rgb * (1.0f - uiSample.a);
}

// --------------------------------------------------------------------------------------------

#endif
