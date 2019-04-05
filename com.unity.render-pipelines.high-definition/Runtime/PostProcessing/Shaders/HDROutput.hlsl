// Important! This file assumes Color.hlsl has been already included. 

// TEMP_REMOVE_NOTES_FCC:
// -  What color space we perform grading in? If any WCG, can we keep that somehow lying around 
// -  GT Tone mapping looks very cool, worth giving it a shot?
// -  A very nice thing to do would be actually rendering in BT2020 or directly in any WCG format. This is a bit of a stretch for now, but worth investigating in the future. 
//  - I think at the moment our grading output is in sRGB so we'll need here to rotate back to BT 2020 effectively losing lots of colors, what if we do the opposite? What if we output the grading to BT 2020 and then rotate for the sRGB? For perf? 



// A bit of nomenclature that will be used in the file:
// Gamut: It is the subset of colors that is possible to reproduce by using three specific primary colors. 
// Rec709 (ITU-R Recommendation BT709) is a HDTV standard, in our context, we mostly care about its color gamut (https://en.wikipedia.org/wiki/Rec._709). The Rec709 gamut is the same as BT1886 and sRGB. 
// Rec2020 (ITU-R Recommendation BT2020) is an UHDTV standard. As above, we mostly reference it w.r.t. the color gamut. (https://en.wikipedia.org/wiki/Rec._2020). Nice property is that all primaries are on the locus. 
// DCI-P3 (or just P3): is a gamut used in cinema grading and used by iPhone for example.
// ACEScg: A gamut that is larger than BT2020 and .
// ACES2065-1: A gamut that covers the full XYZ space, part of the ACES specs. Mostly used for storage since it is harder to work with than ACEScg. 
// WCG: Wide color gamut. This is defined as a color gamut that is wider than the Rec709 one. 
// OETF (Optical Eelectro Transfer Function): This is a function to goes from optical (linear light) to electro (signal transmitted to the display). This is what is applied in camera and therefore what we need to use.
// EOTF (Eelectro Optical  Transfer Function): The inverse of the OETF, used by the TV/Monitor.
// PQ (Perceptual Quantizer): the EOTF used for HDR TVs. It works in the range [0, 10000] nits. Important to keep in mind that this represents an absolute intensity and not relative as for SDR. Sometimes this can be referenced as ST2084. As OETF we'll use the inverse of the PQ curve.




// Note: Ideally the pipeline should work in WCG, but  this require more fundamental changes both to the rendering pipeline and the content authoring. As such we assume we start from Rec709 color gamut.
// However at some point we'd need to consider the eventuality of WCG aware content and add the option that assumes that the input color space is Rec2020.

// --------------------------------
//  COLOR PRIMARIES ROTATION 
// --------------------------------
// We start with a color gamut assumed to be Rec709, to properly output to a display we need to rotate the colour so that the resulting
// image is using the proper color space. As any other space transform, this involves a change of basis and therefore a matrix multiplication.
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

// TODO_FCC: The define should come from a multi compile

float3 RotatePrimaries(float3 Rec709Input)
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

// Infamous Second Son approximation Ref: http://www.glowybits.com/blog/2017/01/04/ifl_iss_hdr_2/
// Fastest option, but also the least accurate. Behaves well for values up to 1400 nits but then quickly starts diverging. 
float3 PatryInvPQ(float3 x)
{
    return (x * (x * (x * (x * (x * 533095.76 + 47438306.2) + 29063622.1) + 575216.76) + 383.09104) + 0.000487781) /
        (x * (x * (x * (x * 66391357.4 + 81884528.2) + 4182885.1) + 10668.404) + 1.0);
}

// Grand Turismo Approx Ref: http://cdn2.gran-turismo.com/data/www/pdi_publications/PracticalHDRandWCGinGTS_note_20181222.pdf
// Slower than Infamous approx, but more precise ( https://www.desmos.com/calculator/0n402k2syc ) in the full [0... 10 000] range, but still faster than reference
float3 GTSInvPQ(float3 inputCol)
{
    float3 k = pow((x * 0.01), 0.1593017578125);
    return (3.61972*(1e-8) + k * (0.00102859 + k * (-0.101284 + 2.05784 * k))) /
        (0.0495245 + k * (0.135214 + k * (0.772669 + k)));
}


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
