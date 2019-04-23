// Important! This file assumes Color.hlsl has been already included. 

// TEMP_REMOVE_NOTES_FCC:
//  - I think at the moment our grading output is in sRGB so we'll need here to rotate back to BT 2020 effectively losing lots of colors, what if we do the opposite? What if we output the grading to BT 2020 and then rotate for the sRGB? For perf?
// TODO_FCC: Decide what to do with the UI (where to put it at least)  


// Attack plan:
//      - Do all the grading and tone mapping in Rec2020 (well, at least do final rotation to Rec2020). Or maybe AP1 since we already have it from the tonemapper? 
//      - We reach the Final pass where we do the display mapping, in here:
//          * If HDR: If we need to output to P3, rotate to Rec2020 to P3 (P3 is a subset), then in any case perform range reduction based min/max brightness given by user or manufacturer (or using decent defaults: 10 to 1000 nits?) with BT2390 EETF (as in CoD) .
//                    this requires moving to ICtCp color space and apply the curve on the I value of the ICtCp value. Then rotate back to Rec2020 + PQ. Probably the whole thing can be LUTed. [https://www.shadertoy.com/view/ldKcz3]
//      -   * If SDR: Do range reduction here (using this https://www.desmos.com/calculator/esjyfpsjvn from frostbite presentation), however keep in mind that we need to account for hue shift. Check slide 93 of frostbite presentation.

// TODO_Perf: When full pipeline is done, try to LUT the most we can. 


// TODOs:
//  - We should tonemap in a rotated color space (i.e. BT2020) to get more out of it. We then rotate to Rec709 on SDR and not viceversa 
//  - Expose the paper white nit  to user, a sensible range is 50 - 300.

// A bit of nomenclature that will be used in the file:
// Gamut: It is the subset of colors that is possible to reproduce by using three specific primary colors. 
// Rec709 (ITU-R Recommendation BT709) is a HDTV standard, in our context, we mostly care about its color gamut (https://en.wikipedia.org/wiki/Rec._709). The Rec709 gamut is the same as BT1886 and sRGB. 
// Rec2020 (ITU-R Recommendation BT2020) is an UHDTV standard. As above, we mostly reference it w.r.t. the color gamut. (https://en.wikipedia.org/wiki/Rec._2020). Nice property is that all primaries are on the locus. 
// DCI-P3 (or just P3): is a gamut used in cinema grading and used by iPhone for example.
// ACEScg: A gamut that is larger than BT2020 and .
// ACES2065-1: A gamut that covers the full XYZ space, part of the ACES specs. Mostly used for storage since it is harder to work with than ACEScg. 
// WCG: Wide color gamut. This is defined as a color gamut that is wider than the Rec709 one. 
// LMS: A color space represented by the response of the three cones of human eye (responsivity peaks Long, Medium, Short) 
// OETF (Optical Eelectro Transfer Function): This is a function to goes from optical (linear light) to electro (signal transmitted to the display). This is what is applied in camera and therefore what we need to use.
// EOTF (Eelectro Optical  Transfer Function): The inverse of the OETF, used by the TV/Monitor.
// EETF (Eelectro-Electro Transfer Function): This is generally just a remapping function, we use the BT2390 EETF to perform range reduction based on the actual display. 
// PQ (Perceptual Quantizer): the EOTF used for HDR TVs. It works in the range [0, 10000] nits. Important to keep in mind that this represents an absolute intensity and not relative as for SDR. Sometimes this can be referenced as ST2084. As OETF we'll use the inverse of the PQ curve.

// Note: Ideally the pipeline should work in WCG, but  this require more fundamental changes both to the rendering pipeline and the content authoring. As such we assume we start from Rec709 color gamut.
// However at some point we'd need to consider the eventuality of WCG aware content and add the option that assumes that the input color space is Rec2020.

// --------------------------------
//  COLOR PRIMARIES ROTATION 
// --------------------------------
// As any other space transform, changing color space involves a change of basis and therefore a matrix multiplication.
// Note that Rec2020 and Rec2100 share the same color space. 

float3 RotateRec709ToRec2020(float3 Rec709Input)
{
    static const float3x3 Rec709ToRec2020Mat =
    {
        0.627402, 0.329292, 0.043306,
        0.069095, 0.919544, 0.011360,
        0.016394, 0.088028, 0.895578
    };

    return mul(Rec709ToRec2020Mat, Rec709Input);
}

float3 RotateRec709ToP3(float3 Rec709Input)
{
    static const float3x3 Rec709ToP3Mat =
    {
        0.822458, 0.177542, 0.000000,
        0.033193, 0.966807, 0.000000,
        0.017085, 0.072410, 0.910505
    };

    return mul(Rec709ToP3Mat, Rec709Input);
}

float3 RotateRec2020ToRec709(float3 Rec2020Input)
{
    static const float3x3 Rec2020ToRec709Mat =
    {
         1.660496, -0.587656, -0.072840,
        -0.124547,  1.132895, -0.008348,
        -0.018154, -0.100597,  1.118751
    };
    return mul(Rec2020ToRec709Mat, Rec2020Input);
}

// TODO_IMPORTANT: Verify this. 
float3 RotateRec2020ToP3(float3 Rec2020Input)
{
    static const float3x3 Rec2020ToXYZMat =
    {
         1.660496, -0.587656, -0.072840,
        -0.124547,  1.132895, -0.008348,
        -0.018154, -0.100597,  1.118751
    };

    static const float3x3 XYZToP3D65Mat =
    {
         2.4933963, -0.9313459, -0.4026945,
        -0.8294868,  1.7626597,  0.0236246,
         0.0358507, -0.0761827,  0.9570140
    };

    static const float3x3 Rec2020toP3Mat = mul(XYZToP3D65Mat, Rec2020ToXYZMat);

    return mul(Rec2020toP3Mat, Rec2020Input);
}

// Ref: ICtCp Dolby white paper (https://www.dolby.com/us/en/technologies/dolby-vision/ictcp-white-paper.pdf)
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

float3 RotateLMSToRec2020(float3 LMSInput)
{
    static const float3x3 LMSToRec2020Mat =
    {
          3.43660669433308,  -2.50645211865627,  0.0698454243232,
         -0.79132955559892,   1.98360045179229, -0.1922708961934,
         -0.02594989969059,  -0.09891371471173,  1.1248636144023
    };

    return mul(LMSToRec2020Mat, LMSInput);
}
float3 RotateRec709ToWCG(float3 Rec709Input)
{
#ifdef WCG_REC2020
    return RotateRec709ToRec2020(Rec709Input);
#elif defined(WCG_P3)
    return RotateRec709ToP3(Rec709Input);
#endif
    // We should really reach this point.
    return Rec709Input;
}


// --------------------------------
//  OETF   
// --------------------------------
// We need to apply the inverse of the display EOTF which we, for now, we always assume to be PQ.
// Various methods follow.

// This is the accurate inverse of the PQ curve, it involves two pows therefore is leaning to the expensive end. 
float3 AccuratePQInv(float3 inputCol)
{
    return LinearToPQ(inputCol);
}

float AccuratePQInv(float x)
{
    return LinearToPQ(x);
}


// Ref: [Patry 2017] HDR Display Support in Infamous Second Son and Infamous First Light
// Fastest option, but also the least accurate. Behaves well for values up to 1400 nits but then starts diverging. 
float3 PatryInvPQ(float3 x)
{
    return (x * (x * (x * (x * (x * 533095.76 + 47438306.2) + 29063622.1) + 575216.76) + 383.09104) + 0.000487781) /
        (x * (x * (x * (x * 66391357.4 + 81884528.2) + 4182885.1) + 10668.404) + 1.0);
}

//  Ref: [Uchimura and Suzuki 2018] Practical HDR and Wide Color Techniques in Gran Turismo Sport 
// Slower than Infamous approx, but more precise ( https://www.desmos.com/calculator/0n402k2syc ) in the full [0... 10 000] range, but still faster than reference
float3 GTSInvPQ(float3 inputCol)
{
    float3 k = pow((inputCol * 0.01), 0.1593017578125);
    return (3.61972*(1e-8) + k * (0.00102859 + k * (-0.101284 + 2.05784 * k))) /
        (0.0495245 + k * (0.135214 + k * (0.772669 + k)));
}

// TODO: Fourth option would be implementing the curve as a LUT

#define PRECISE_INV_PQ 0
#define ISS_APPROX_INV_PQ 1
#define GTS_APPROX_INV_PQ 2
#define INV_PQ_CHOICE GTS_APPROX_INV_PQ

float3 ApplyOETF(float3 inputCol)
{
#if INV_PQ_CHOICE == PRECISE_INV_PQ
    return AccuratePQInv(inputCol);
#elif INV_PQ_CHOICE == ISS_APPROX_INV_PQ
    return PatryInvPQ(inputCol);
#elif INV_PQ_CHOICE == GTS_APPROX_INV_PQ
    return GTSInvPQ(inputCol);
#endif
}

// --------------------------------
// Range reduction 
// --------------------------------
// Ref: [Malin 2018] HDR Display in Call of Duty
// Ref: [Fry 2017] High Dynamic Range Color Grading and Display in Frostbite
// We start from a signal that is assumed having 10k nits to the actual Target Display.
// In case of SDR this mean taking as input a linear signal and output a value in the [0...1] range using a piecewise function
// that starts linear and ends up with a exp curve as shoulder to compress highlights ( https://www.desmos.com/calculator/esjyfpsjvn ). 
// In case of HDR we use BT2390 EETF as suggested in Ref: [Malin2018] HDR Display in Call of Duty

// Ref: ICtCp Dolby white paper (https://www.dolby.com/us/en/technologies/dolby-vision/ictcp-white-paper.pdf)
float3 PQLMSToICtCp(float3 LMSInput)
{
    float3x3 PQLMSToICtCpMat = float3x3(
        0.5, 0.5, 0.0,
        1.613769, -3.323486, 1.709716,
        4.378174, -4.245605, -0.1325683
        );

    return mul(PQLMSToICtCpMat, LMSInput);
}

float3 ICtCpToPQLMS(float3 ICtCpInput)
{
    float3x3 ICtCpToPQLMSMat = float3x3(
        1.0,  0.008609,  0.111029,
        1.0, -0.008609, -0.111029,
        1.0,  0.560031, -0.320627
        );

    return mul(ICtCpToPQLMSMat, ICtCpInput);
}

float SDRRangeReduction(float val)
{
    float expShoulder = 1.0f - exp(-val);
    // TODO_FCC:  We should allow this to be set. 
    float linearSectionEnd = 0.33f;

    return val > linearSectionEnd ? expShoulder : val;
}

float3 SDRRangeReduction(float3 val)
{
    return float3(SDRRangeReduction(val.x), SDRRangeReduction(val.y), SDRRangeReduction(val.z));
}


float BT2390EETFHermite(float x, float kneeStart, float maxLum)
{
    float T = (x - kneeStart) / (1.0f - kneeStart);
    float T2 = T * T;
    float T3 = T2 * T;

    return (2 * T3 - 3 * T2 + 1) * kneeStart + (T3 - 2 * T2 + T) * (1 - kneeStart) + (-2 * T3 + 3 * T2) * maxLum;
}

// TODO: Can we squash this all thing into a LUT? Probably so.
// Ref: BT2390 standard doc https://www.itu.int/dms_pub/itu-r/opb/rep/R-REP-BT.2390-3-2017-PDF-E.pdf
float3 HDRRangeReduction(float3 Rec2020Input, float minNit, float maxNit)
{
    // 1) Rec 2020 to LMS -> LMS to PQ -> PQ to ICtCp
    float3 LMSVal = RotateRec2020ToLMS(Rec2020Input);
    // For now accurate, TODO: Check if we can use the approx version. 
    float3 PQLMS = AccuratePQInv(LMSVal);
    float3 ICtCp = PQLMSToICtCp(PQLMS);

    // 2) Apply EETF [ https://www.desmos.com/calculator/vgc7s0juzp ]
    // TODO_FCC: Compute this two values on CPU
    float minLumPQ = AccuratePQInv(minNit);
    float maxLumPQ = AccuratePQInv(maxNit);

    float kneeStart = 1.5f * maxLumPQ - 0.5f;
    float e2 = ICtCp.x < kneeStart ? ICtCp.x : BT2390EETFHermite(ICtCp.x, kneeStart, maxLumPQ);

    float intensityReduced = e2 + minLumPQ * PositivePow((1.0f - e2), 4);
    // As mentioned by BT2390, we need to adjust saturation
    float saturationScale = min(intensityReduced / ICtCp.x, ICtCp.x / intensityReduced);

    float3 newICtCp = float3(intensityReduced, ICtCp.xy * saturationScale);

    // 3) ICtCp to PQ -> PQ to LMS -> LMS to Rec 2020
    PQLMS = ICtCpToPQLMS(newICtCp);
    LMSVal = PQToLinear(PQLMS);   // TODO_FCC: Approx?
    return RotateLMSToRec2020(LMSVal);
}

// --------------------------------
// Display mapping functions 
// --------------------------------
// These functions are aggregate of most of what we have above. You can this as the public API of the HDR Output library.
// Note that throughout HDRP we are assuming that when it comes to the final pass adjustements, our tonemapper has *NOT*
// performed range reduction and everything is assumed to be displayed on a reference 10k nits display and everything post-tonemapping
// is in the Rec 2020 color space. However we still provide options in case we get a Rec709 input, this will just rotate to Rec2020 and
// procede with the expected pipeline. 

float3 SDRMapping_NoRotation(float3 input, float huePreservingFraction)
{
    // As in [Fry 2017], we mix hue-preservation reduction and hue shifted reduction. 
    float maxChannel = Max3(input.x, input.y, input.z);
    float reducedMax = SDRRangeReduction(maxChannel);
    float3 reducedColHuePreserving = input * (reducedMax / maxChannel);

    // This is not hue preserving. 
    float3 rangePerChannelReduced = SDRRangeReduction(input);

    // NOTE: No OETF here since Unity handles it separately later on. 
    return lerp(rangePerChannelReduced, reducedColHuePreserving, huePreservingFraction);
}

float3 SDRMapping(float3 Rec2020Input, float huePreservingFraction)
{
    // Move to the correct color space
    float3 Rec709Val = RotateRec2020ToRec709(SDRMapping_NoRotation(Rec2020Input, huePreservingFraction));
    return Rec709Val;
}

float3 HDRMapping(float3 Rec2020Input, float minNits, float maxNits)
{
    // First up we do range reduction
    float3 reducedHDR = HDRRangeReduction(Rec2020Input, minNits, maxNits);

#ifdef WCG_P3
    // The whole pipeline operates in Rec2020, if we need P3, we'll need to rotate now before going through the OETF
    reducedHDR = RotateRec2020ToP3(reducedHDR);
#endif

    return ApplyOETF(reducedHDR);
}

// Warning! This is never a good idea vs. the above, ideally the pipeline should always be working in the wider gamut
float3 HDRMappingFromRec709(float3 Rec709Input, float minNits, float maxNits)
{
    float3 rec2020Input = RotateRec709ToRec2020(Rec709Input);
    return HDRMapping(Rec709Input, minNits, maxNits);
}
