// Important! This file assumes Color.hlsl has been already included.

#define DEBUG_HDR_LUT_WORKFLOW 1

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
// PQ (Perceptual Quantizer): the EOTF used for HDR TVs. It works in the range [0, 10000] nits. Important to keep in mind that this represents an absolute intensity and not relative as for SDR. Sometimes this can be referenced as ST2084. As OETF we'll use the inverse of the PQ curve.


// --------------------------------
//  Perceptual Quantizer (PQ) / ST 2084
// --------------------------------
// This section has a bunch of options, a few of them are accurate a bunch are not.
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
    float Em2 = PositivePow(value, 1 / PQ_M);
    float X = (max(0.0, Em2 - PQ_C1)) / (PQ_C2 - PQ_C3 * Em2);
    return pow(X, 1 / PQ_N);
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

float3 RotateRec2020ToICtCp(float3 Rec2020)
{
    float3 lms = RotateRec2020ToLMS(Rec2020);
    float3 PQLMS = LinearToPQ(max(0.0f, lms));
    return RotatePQLMSToICtCp(PQLMS);
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

float3 RotateICtCpToPQLMS(float3 ICtCp)
{
    static const float3x3 ICtCpToPQLMSMat = float3x3(
        1.0f, 0.0086051456939815f, 0.1110356044754732f,
        1.0f, -0.0086051456939815f, -0.1110356044754732f,
        1.0f, 0.5600488595626390f, -0.3206374702321221f
    );

    return mul(ICtCpToPQLMSMat, ICtCp);
}

float3 RotateICtCpToRec2020(float3 ICtCp)
{
    float3 PQLMS = RotateICtCpToPQLMS(ICtCp);
    float3 LMS = PQToLinear(PQLMS, MAX_PQ_VALUE);
    float3 XYZ = RotateLMSToXYZ(LMS);
    return RotateXYZToRec2020(XYZ);
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
// Slower than Infamous approx, but more precise ( https://www.desmos.com/calculator/0n402k2syc ) in the full [0... 10 000] range, but still faster than reference
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
#if OETF_CHOICE == PRECISE_PQ
    return LinearToPQ(inputCol);
#elif OETF_CHOICE == ISS_APPROX_PQ
    return PatryApproxLinToPQ(inputCol * 0.01f);
#elif OETF_CHOICE == GTS_APPROX_PQ
    return GTSApproxLinToPQ(inputCol * 0.01f);
#endif
}

#define LIN_TO_PQ_FOR_LUT GTS_APPROX_PQ // GTS is close enough https://www.desmos.com/calculator/5jdfc4pgtk
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
#define REINHARD 0
#define BT2390 1
#define RANGE_REDUCTION BT2390

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


float3 PerformRangeReduction(float3 Rec2020Input, float minNits, float maxNits)
{
    float3 ICtCp = RotateRec2020ToICtCp(Rec2020Input); // This is in PQ space.
    float linearLuma = PQToLinear(ICtCp.x, MAX_PQ_VALUE);
#if RANGE_REDUCTION == REINHARD
    linearLuma = ReinhardTonemap(linearLuma, maxNits);
#elif RANGE_REDUCTION == BT2390
    linearLuma = BT2390EETF(linearLuma, minNits, maxNits);
#endif
    ICtCp.x = LinearToPQ(linearLuma);


    return RotateICtCpToRec2020(ICtCp); // This moves back to linear too!
}

float3 PerformRangeReduction(float3 Rec2020Input, float minNits, float maxNits, int mode)
{
    float3 ICtCp = RotateRec2020ToICtCp(Rec2020Input); // This is in PQ space.

    float linearLuma = PQToLinear(ICtCp.x, MAX_PQ_VALUE);
    if (mode == 1)
        linearLuma = ReinhardTonemap(linearLuma, maxNits);
    else if (mode == 2)
        linearLuma = BT2390EETF(linearLuma, minNits, maxNits);

    ICtCp.x = LinearToPQ(linearLuma);

    if (mode == 3)
    {
        float3 outC = 0;
        outC.x = BT2390EETF(Rec2020Input.x, minNits, maxNits);
        outC.y = BT2390EETF(Rec2020Input.y, minNits, maxNits);
        outC.z = BT2390EETF(Rec2020Input.z, minNits, maxNits);
        return outC;
    }
    else if (mode == 4)
    {
        float3 outC = 0;
        outC.x = ReinhardTonemap(Rec2020Input.x, maxNits);
        outC.y = ReinhardTonemap(Rec2020Input.y, maxNits);
        outC.z = ReinhardTonemap(Rec2020Input.z, maxNits);
        return outC;
    }

    return RotateICtCpToRec2020(ICtCp); // This moves back to linear too!
}


// --------------------------------------------------------------------------------------------

// --------------------------------
// Public facing functions
// --------------------------------
// These functions are aggregate of most of what we have above. You can think of this as the public API of the HDR Output library.
// Note that throughout HDRP we are assuming that when it comes to the final pass adjustements, our tonemapper has *NOT*
// performed range reduction and everything is assumed to be displayed on a reference 10k nits display and everything post-tonemapping
// is in the Rec 2020 color space. However we still provide options in case we get a Rec709 input, this will just rotate to Rec2020 and
// procede with the expected pipeline.

float3 HDRMappingFromRec2020(float3 Rec2020Input, float hdrBoost, float minNits, float maxNits)
{
    // The reason to have a boost factor is because the standard for SDR is peaking at 100nits, but televisions are typically 300nits
    // and the colours get boosted. If we want equivalent look in HDR a similar boost needs to happen. It might look washed out otherwise.
    float3 reducedHDR = PerformRangeReduction(Rec2020Input * hdrBoost, minNits, maxNits);
    return OETF(reducedHDR);
}

float3 HDRMappingFromRec709(float3 Rec709Input, float hdrBoost, float minNits, float maxNits, int reductionMode, bool skipOETF = false)
{
    float3 Rec2020Input = RotateRec709ToRec2020(Rec709Input);
    // The reason to have a boost factor is because the standard for SDR is peaking at 100nits, but televisions are typically 300nits
    // and the colours get boosted. If we want equivalent look in HDR a similar boost needs to happen. It might look washed out otherwise.
    float3 reducedHDR = PerformRangeReduction(Rec2020Input * hdrBoost, minNits, maxNits, reductionMode);

    if (skipOETF) return reducedHDR;

    return OETF(reducedHDR);
}


float3 HDRMappingFromRec709_ACES(float3 Rec709Input, float hdrBoost, bool skipOETF = false)
{
    float3 aces = unity_to_ACES(Rec709Input * hdrBoost * 0.01f);
    float3 oces = RRT(aces);
    float3 AP1ODT = ODT_Rec2020_1000nits_ToAP1(oces);

    const float3x3 AP1_2_Rec2020 = mul(XYZ_2_REC2020_MAT, mul(D60_2_D65_CAT, AP1_2_XYZ_MAT));
    float3 linearODT = mul(AP1_2_Rec2020, AP1ODT);

    if (skipOETF) return linearODT;

    return OETF(linearODT);
}

// --------------------------------------------------------------------------------------------

// --------------------------------
// UI Related functions
// --------------------------------

// Assumes UI is linear at this point ? Is it true?
float3 SceneUIComposition(float4 uiSample, float3 pqSceneColor, float paperWhite)
{
    uiSample.rgb = RotateRec709ToRec2020(uiSample.rgb / (uiSample.a == 0.0 ? 1.0 : uiSample.a));
    // TODO: Should we use an approximation here?
    uiSample.rgb = LinearToPQ(uiSample.rgb, (MAX_PQ_VALUE / paperWhite));
    uiSample.rgb *= uiSample.a;
    return uiSample.rgb + pqSceneColor.rgb * (1.0f - uiSample.a);
}

// --------------------------------------------------------------------------------------------
