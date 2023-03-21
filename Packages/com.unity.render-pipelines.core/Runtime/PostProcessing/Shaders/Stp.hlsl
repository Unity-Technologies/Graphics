////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//
//
//                                              SPATIAL TEMPORAL POST [STP] v1.0
//
//
//==============================================================================================================================
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
// C/C++/GLSL/HLSL PORTABILITY BASED ON AMD's 'ffx_a.h', and STP "cleaner" partly based on a modified RCAS 'ffx_fsr1.h'.
// INCLUDING ASSOCIATED LICENSE BELOW
//------------------------------------------------------------------------------------------------------------------------------
// Copyright (c) 2021 Advanced Micro Devices, Inc. All rights reserved.
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//==============================================================================================================================
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                           NOTES
//------------------------------------------------------------------------------------------------------------------------------
// AUTHOR'S NOTE
// =============
// - This code was authored by Timothy Paul Lottes at Unity, feel free to contact directly for any questions.
// - The code style and conventions are a variation of the author's personal conventions.
//------------------------------------------------------------------------------------------------------------------------------
// PLATFORM SPECIFIC WORKAROUNDS
// =============================
// - These all default to not enabled {0}, define to {1} to enable.
// - define STP_BUG_ALIAS16 1 .... Define to enable workaround for asuint16()/asfloat16().
// - define STP_BUG_PRX 1 ........ Define to disable approximate transendentals.
// - define STP_BUG_SAT_INF 1 .... Define to workaround platforms with broken 16-bit saturate +/- INF.
// - define STP_BUG_SAT 1 ........ Define to workaround compiler incorrectly factoring out inner saturate in 16-bit code.
//------------------------------------------------------------------------------------------------------------------------------
// PROCESS TO FIND AND RESOLVE PLATFORM SPECIFIC BUGS
// ==================================================
// - Typically an issue would be in the 'define STP_MEDIUM 1' usage for 32-bit, and possibly the 16-bit explicit code paths.
// - First use all the defines in the "platform specific workarounds above", see if the issue goes away.
//   If so, then binary search to find the minimum number of defines which toggles on/off the issue.
// - If that does not work continue.
// - Use 'define STP_CONFIG_0 1' to get the low-end configuration.
//   If the problem goes away the issue is specific to the differences with the high-end configuration.
//   Configurion 0 turns off directional filtering and the cleaner pass, and runs one instead of 4 motion vectors.
// - Use 'define STP_AKS 0' to turn off adaptive kernel sharping, see if that removes the problem.
// - Try toggling on/off (via 1/0) the following defines separately,
//    - 'define STP_BUG_KILL_ANTI 1' - Disable anti-flicker (output will flicker a lot).
//    - 'define STP_BUG_KILL_DSP 1' - Disable displacement sharpening (this is in the inline function, output will get blurry).
//    - 'define STP_BUG_KILL_FEED 1' - Disable feedback.
// - Try using 'define STP_BUG' {1 through 24} and note which debug views correlate with the specific bug in final output.
//    - Note need to run 'define STP_CONFIG_0 1' to run without the cleaner (as debug output only works without the cleaner).
//------------------------------------------------------------------------------------------------------------------------------
// CONFIGURATIONS
// ==============
// - INDEPENDENT OPTIONS
//    - define STP_GRAIN {0 := off, 1 := monochrome, 3 := colored}
//    - define STP_32BIT {0 := disable, 1 := compile the 32-bit version or implicit precision version}
//    - deifne STP_MEDIUM {0 := disable (for PC), 1 := enable the implicit precision version for 32-bit for mobile}
//    - define STP_16BIT {0 := disable, 1 := compile the explicit 16-bit version}
//    - define STP_GPU {to include shader code}
//    - define STP_GLSL {to include the GLSL version of the code}
//    - define STP_HLSL {to include the HLSL version of the code}
//    - define STP_TAA {to include the StpTaa<H,F>() entry points}
//    - define STP_CLN {to include the StpCln<H,F>() entry points}
//    - define STP_IN {to include the StpIn<H,F>() entry points}
//    - define STP_UBE {to include the StpUbe<H,F>() entry points}
//    - define STP_POSTMAP {running STP, 0 := before, 1 := after, application tonemapping}
// - HIGH END
//    - The path for PC (high quality)
//    - This needs use to StpCln<F,H>()!
//    - define STP_CONFIG_1 1
// - LOW END
//    - This is the suggested path for mobile (it is the lowest quality, highest performance option)
//    - This should NOT use StpCln<F,H>()!
//    - define STP_CONFIG_0 1
//------------------------------------------------------------------------------------------------------------------------------
// PLATFORM SPECIFIC ISSUES (UPDATED LATE 2022)
// ============================================
// - AMD
//    - For STP_16BIT use 'define STP_BUG_SAT 1'
// - Qualcomm
//    - STP_16BIT as pixel shader running slower than STP_32BIT with STP_MEDIUM
//------------------------------------------------------------------------------------------------------------------------------
// TYPICAL PC CONFIG FOR 32-BIT
// ============================
// define STP_CONFIG_1 1 ...... Use the high quality version
// define STP_GRAIN 1
// define STP_32BIT 1
// define STP_MEDIUM 1 ........ Speculatively use the medium precision if the platform wants it
// define STP_GPU 1
// define STP_HLSL 1
// define STP_TAA 1
// define STP_CLN 1
// define STP_IN 1
// define STP_UBE 1
// define STP_POSTMAP 0 ....... Unity HDRP is pretonemap
//------------------------------------------------------------------------------------------------------------------------------
// TYPICAL PC CONFIG FOR 16-BIT
// ============================
// define STP_CONFIG_1 1
// define STP_GRAIN 1
// define STP_16BIT 1 ......... Use the explicit packed 16-bit path (without STP_MEDIUM)
// define STP_GPU 1
// define STP_HLSL 1
// define STP_TAA 1
// define STP_CLN 1
// define STP_IN 1
// define STP_UBE 1
// define STP_POSTMAP 0
//------------------------------------------------------------------------------------------------------------------------------
// TYPICAL MOBILE CONFIG (GLES)
// ============================
// define STP_CONFIG_0 1 ...... Use the faster version
// define STP_GRAIN 1
// define STP_32BIT 1
// define STP_MEDIUM 1
// define STP_GPU 1
// define STP_HLSL 1
// define STP_TAA 1
// define STP_CLN 1
// define STP_IN 1
// define STP_UBE 1
// define STP_POSTMAP 0
//------------------------------------------------------------------------------------------------------------------------------
// TYPICAL MOBILE CONFIG (EXPLICIT 16-BIT IN VULKAN)
// =================================================
// define STP_CONFIG_0 1
// define STP_GRAIN 1
// define STP_16BIT 1 .............................. Switch to explicit 16-bit
// define STP_GPU 1
// define STP_HLSL 1
// define STP_TAA 1
// define STP_CLN 1
// define STP_IN 1
// define STP_UBE 1
// define STP_POSTMAP 0
//------------------------------------------------------------------------------------------------------------------------------
// {MIN,MAX} SAMPLING
// ==================
// - Ideally have this enabled for highest quality.
// - It is used for both UINT and interpolatible formats.
// - If STP_MAX_MIN is enabled, the inline function will call StpInPriFedMinA<H,F>() to better dilate convergence.
//    - So in theory that might introduce cost if texture fetch bound.
//------------------------------------------------------------------------------------------------------------------------------
// IMPORTANT NOTES
// ===============
// - All callbacks should explicitly sample from MIP level 0.
//    - Meaning if used in a pixel shader do not allow implicit LOD calculation.
// - The algorithm is tuned for pre-tonemap operation, post tonemap will have lower quality.
//==============================================================================================================================
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                       CONFIGURATIONS
//==============================================================================================================================
// Highest quality.
#ifdef STP_CONFIG_1
    #ifndef STP_USE_CLN
        #define STP_USE_CLN 1
    #endif
#endif
//------------------------------------------------------------------------------------------------------------------------------
// Fastest.
#ifdef STP_CONFIG_0
    #ifndef STP_USE_CLN
        #define STP_USE_CLN 0
    #endif
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                       EXTERNAL OPTIONS
//==============================================================================================================================
// DEBUG TOOLS
// ===========
// Note some of these have slightly pixel shifted output.
// Unless marked, these need to have the cleaner to be off.
// 0 ... Do nothing
// 1 ... Visualize input color
//       Note this by default has displacement 'sharpening' already applied, use STP_BUG_KILL_DSP define to see actual input
// 2 ... Visualize input depth {RG} with a little input color in {B}
//       Note responsive/off-screen input will be very dark, this represents depth=near
//       Also if this view is a good way to find non-water-tight geometry (holes in depth will be visible)
// 3 ... Visualize shaped absolute input motion {R=horz,G=vert} with a little input color in {B}
//       Note responsive/off-screen will look yellow, and this doesn't show sign of motion
// 4 ... Visualize responsive/off-screen {R} with a little input color in {B}
//       Note at this point responsive is non-dilated (can be single pixel)
// 5 ... Visualize input A, temporal neighborhood expansion {G}, and feedback kill {R}, a little input color in {B}
//       Responsive pixels will dilate into red tinted boxes
//       Disocclusions (non-motion match) and off-screen will show up as red tinted
//       Responsive and off-screen will also present with {B}=1 (full blue tinted)
// 6 ... Visualize spatial {G} and temporal {R} contrast
//       The spatial contrast {G} view is the average of the four nearest inputs (so appears dilated, but aligned with {R})
//       The temporal contrast {R} shows up often in disocclusions
//       The responsive/off-screen should have no temporal contrast {R}
// 7 ... Visualize neighborhood max, each {RGB} is different neighborhood value
// 8 ... Visualize neighborhood min, each {RGB} is different neighborhood value
// 9 ... Visualize shaped angular nearest input motion {R=horz,G=vert} with a little input color in {B}
//       Note responsive/off-screen will look yellow, and this doesn't show sign of motion
//       Edges will be both axis aligned and diagonal when scaling
// 10 .. Visualize cleaner masks {R=low pass, B=sharpen}
//       This requires the cleaner to be on, and this doesn't respect STP_BUG_SPLIT (it's always full screen)
//       The {R} should be eroded of single input pixel features, and then smoothly dilated (expanded) with a smooth edge
//       The {B} shows nothing if STP_SHARP=0
//       The {B} with STP_SHARP=1 output will typically look fully blue with dilated stippled dots
//       The {B} with STP_SHARP=2 output gets darker where sharpening was reduced
//       Note with STP_SHARP=2, some amount of edges get dark
//       It is designed to reduce sharpening on pure horizontal and vertical edges to avoid visable ringing
//       Thus this STP_SHARP=2 option mostly sharpens on the diagonals
// 11 .. Visualize clamped feedback, each {RGB} is different neighborhood value
//       This will show colors on edges of disocclusions
// 12 .. Visualize initial blend ratio, each {RGB} is different neighborhood value
//       Will be black where responsive/off-screen
// 13 .. Visualize kernel shape (dark := sharp to light := smooth)
//       Responsive/off-screen/disoccluded/edge area will be light, motion will increase lightness
// 14 .. Visualize filtering direction {RB}
//       Unlike option 16, this will not show banding
// 15 .. Visualize final blend ratio (amount of feedback), each {RGB} is different neighborhood value in the 2x2
//       Image will go black in areas feedback is not used
// 16 .. Visualize directional filtering position values {R=x, G=y, B=interpolation between {x,y}}
//       Seeing banding here is ok, it happens due to the sub-pixel offset during scaling or de-jitter
// 17 .. Visualize anti-flicker weights, each {RGB} is different neighborhood value
//       Edges colored based on gradient
//       Disocclusions and responsive should be mostly black
// 18 .. Visualize final weights, each {RGB} is different neighborhood value
//       This will have flicker banding due to sub-pixel jitter and scaling
// 19 .. Visualize feedback
//       Responsive/off-screen areas will show the color of the upper left pixel
//       Disocclusions will show a trailing ghost separated by an aliased edge
// 20 .. Visualize feedback of blend ratio (aka convergence) adjusted by the convergence modifier (motion)
//       This will show one frame of ghosting in disocclusions
//       Blend ratio is a 2-bit value in alpha of feedback
//       Blend ratio is dithered so it can have more than 4 values depending on bilinear fetch of feedback
//       Blend ratio gets decoded into 0.5 to something approaching black
//       The closeness to black depends on the motion velocity, the closer to still the closer to black
//       Blend ratio is 1.0 (ligher) in areas of responsive and off-screen
// 21 .. Visualize bilinear blur amount {RG} with a little input color in {B}
//       The screen will get a yellow tint the more feedback moves off the texel center
//       This disables split screen
// 22 .. Visualize initial displacement sharpening amount {RG} with a little input color in {B}
//       This starts with bilinear blur amount, scaled and biased by a factor based on the amount of scaling
//       After a little bit of scaling, the initial displacement is nearly always on
//       At no scaling, initial displacement is based mostly on bilinear blur amount
//       Displacement sharpening is removed in areas that lack motion match or responsive or off-screen
//       Final sharpening amount is modulated by other feedback factors, but they cannot be visualized
//       This disables split screen
// 23 .. Visualize feedback, but just convergence
#ifndef STP_BUG
    #define STP_BUG 0
#endif
//------------------------------------------------------------------------------------------------------------------------------
// Default {0} to use full-screen debug view, use {1} to enable split-screen.
// Debug views {9,10,23} don't work with split screen (as they are from the inline function).
#ifndef STP_BUG_SPLIT
    #define STP_BUG_SPLIT 1
#endif
// These need to turn off split screen (because they are fed from the inline prepass).
#if ((STP_BUG == 21) || (STP_BUG == 22))
    #undef STP_BUG_SPLIT
    #define STP_BUG_SPLIT 0
#endif
//==============================================================================================================================
// Kill anti-flicker weighting {1}, or not {0}.
#ifndef STP_BUG_KILL_ANTI
    #define STP_BUG_KILL_ANTI 0
#endif
//------------------------------------------------------------------------------------------------------------------------------
// Kill displacement sharpening {1}, or not {0}.
#ifndef STP_BUG_KILL_DSP
    #define STP_BUG_KILL_DSP 0
#endif
//------------------------------------------------------------------------------------------------------------------------------
// Kill feedback {1}, or not {0}.
#ifndef STP_BUG_KILL_FEED
    #define STP_BUG_KILL_FEED 0
#endif
//==============================================================================================================================
// PLATFORM SPECIFIC BUG WORKAROUNDS
// =================================
// Define to {1} to disable usage of transendental approximations using float/int aliasing.
#ifndef STP_BUG_PRX
    #define STP_BUG_PRX 1
#endif
//------------------------------------------------------------------------------------------------------------------------------
// Define to {1} for workaround if platform cannot use saturate of +/- INF correctly.
#ifndef STP_BUG_SAT_INF
    #define STP_BUG_SAT_INF 0
#endif
//------------------------------------------------------------------------------------------------------------------------------
// Define to {1} for workaround for compilier incorrectly factoring out inner saturate in 16-bit code.
#ifndef STP_BUG_SAT
    #define STP_BUG_SAT 0
#endif
//------------------------------------------------------------------------------------------------------------------------------
// Define to {1} for workarounds for broken asuint16()/asfloat16().
#ifndef STP_BUG_ALIAS16
    #define STP_BUG_ALIAS16 0
    #undef STP_BUG_PRX
    #define STP_BUG_PRX 1
#endif
//==============================================================================================================================
// Default to using {64,1,1} workgroups, define to {0} to use {128,1,1} workgroups (for Qualcomm only).
#ifndef STP_64
    #define STP_64 1
#endif
//------------------------------------------------------------------------------------------------------------------------------
// Adaptive kernel sharpness {1} on, {0} off.
// If disabled, STP is slightly less sharp (when scaling by 4x area), but has less temporal aliasing.
#ifndef STP_AKS
    #define STP_AKS 1
#endif
//------------------------------------------------------------------------------------------------------------------------------
// Use film grain (with or without the cleaner).
//  {0} ... off
//  {1} ... on for monochromatic (default)
//  {3} ... on for 3 channel
#ifndef STP_GRAIN
    #define STP_GRAIN 1
#endif
//------------------------------------------------------------------------------------------------------------------------------
// Define to {1} to use the max/min sampling permutation.
#ifndef STP_MAX_MIN
    #define STP_MAX_MIN 0
#endif
//------------------------------------------------------------------------------------------------------------------------------
// STP's TAA is designed to run post-tonemap.
// Run 0 := pre-tonemap, 1 := post-tonemap.
#ifndef STP_POSTMAP
    #define STP_POSTMAP 0
#endif
//------------------------------------------------------------------------------------------------------------------------------
// Define {1} to use semi-persistent workgroups, else {0} to try with non-semi-persistent.
#ifndef STP_SEMI
    #define STP_SEMI 1
#endif
//------------------------------------------------------------------------------------------------------------------------------
// Sharpener (RCAS) in the cleaner.
//  {0} ... Off
//  {1} ... On
//  {2} ... On, attempts to avoid sharpening noise and halos on V+H edges, but results in some reduction of peak sharpness.
#ifndef STP_SHARP
    #define STP_SHARP 2
#endif
//------------------------------------------------------------------------------------------------------------------------------
// For UBER shader, this is the temporal spacing in output pixels in height.
#ifndef STP_SPACER
    #define STP_SPACER 32
#endif
//------------------------------------------------------------------------------------------------------------------------------
// This STP_SAFE_DILATE=0 moves to a tighter 2x2 dilation for nearest {z,motion} match check.
// This is mostly safe with up to 4x area scaling and angular dilation.
#ifndef STP_SAFE_DILATE 
    #define STP_SAFE_DILATE 1
#endif
//------------------------------------------------------------------------------------------------------------------------------
// This improves the anti-flicker by improving anti-aliasing.
#ifndef STP_SHAPE_ANTI
    #define STP_SHAPE_ANTI 1
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                     EXPERIMENTAL FEATURES
//------------------------------------------------------------------------------------------------------------------------------
// Anything here is disabled by default.
// These features may go away, and have no official support.
//==============================================================================================================================
// Eroded convergence (to try to remove thin flicker) default {0} off, or {1} on.
#ifndef STP_ERODE
    #define STP_ERODE 0
#endif
// Disable STP_ERODE if {min,max} sampling is not supported.
#if (STP_MAX_MIN == 0)
    #undef STP_ERODE
    #define STP_ERODE 0
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                  C/C++/GLSL/HLSL PORTABILITY
//==============================================================================================================================
#if defined(STP_CPU)
    #ifndef STP_RESTRICT
        #define STP_RESTRICT __restrict
    #endif
//------------------------------------------------------------------------------------------------------------------------------
    #ifndef STP_STATIC
        #define STP_STATIC static
    #endif
//------------------------------------------------------------------------------------------------------------------------------
    typedef unsigned char StpB1;
    typedef unsigned short StpW1;
    typedef float StpF1;
    typedef uint32_t StpU1;
    #define StpF1_(a) ((StpF1)(a))
    #define StpU1_(a) ((StpU1)(a))
    STP_STATIC StpU1 StpU1_F1(StpF1 a) { union { StpF1 f; StpU1 u; } bits; bits.f = a; return bits.u; }
    #define StpOutF2 StpF1 *STP_RESTRICT
    #define StpExp2F1(x) exp2f(x)
    STP_STATIC StpF1 StpMaxF1(StpF1 a, StpF1 b) { return a > b ? a : b; }
//------------------------------------------------------------------------------------------------------------------------------
    // Convert float to half (in lower 16-bits of output).
    // Same fast technique as documented here: ftp://ftp.fox-toolkit.org/pub/fasthalffloatconversion.pdf
    // Supports denormals.
    // Conversion rules are to make computations possibly "safer" on the GPU,
    //  -INF & -NaN -> -65504
    //  +INF & +NaN -> +65504
    STP_STATIC StpU1 StpU1_H1_F1(StpF1 f) {
        static StpW1 base[512] = {
            0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,
            0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,
            0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,
            0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,
            0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,
            0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,
            0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0001,0x0002,0x0004,0x0008,0x0010,0x0020,0x0040,0x0080,0x0100,
            0x0200,0x0400,0x0800,0x0c00,0x1000,0x1400,0x1800,0x1c00,0x2000,0x2400,0x2800,0x2c00,0x3000,0x3400,0x3800,0x3c00,
            0x4000,0x4400,0x4800,0x4c00,0x5000,0x5400,0x5800,0x5c00,0x6000,0x6400,0x6800,0x6c00,0x7000,0x7400,0x7800,0x7bff,
            0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,
            0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,
            0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,
            0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,
            0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,
            0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,
            0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,0x7bff,
            0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,
            0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,
            0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,
            0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,
            0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,
            0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,
            0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8001,0x8002,0x8004,0x8008,0x8010,0x8020,0x8040,0x8080,0x8100,
            0x8200,0x8400,0x8800,0x8c00,0x9000,0x9400,0x9800,0x9c00,0xa000,0xa400,0xa800,0xac00,0xb000,0xb400,0xb800,0xbc00,
            0xc000,0xc400,0xc800,0xcc00,0xd000,0xd400,0xd800,0xdc00,0xe000,0xe400,0xe800,0xec00,0xf000,0xf400,0xf800,0xfbff,
            0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,
            0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,
            0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,
            0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,
            0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,
            0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,
            0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff,0xfbff };
        static StpB1 shift[512] = {
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x17,0x16,0x15,0x14,0x13,0x12,0x11,0x10,0x0f,
            0x0e,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,
            0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x17,0x16,0x15,0x14,0x13,0x12,0x11,0x10,0x0f,
            0x0e,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,
            0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x0d,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,
            0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18,0x18 };
        union { StpF1 f; StpU1 u; } bits;
        bits.f = f; StpU1 u = bits.u; StpU1 i = u >> 23;
        return (StpU1)(base[i]) + ((u & 0x7fffff) >> shift[i]); }
//------------------------------------------------------------------------------------------------------------------------------
    STP_STATIC StpU1 StpU1_H2_F2(StpInF2 a) { return StpU1_H1_F1(a[0]) + (StpU1_H1_F1(a[1]) << 16); }
#endif
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_GLSL)
    #define StpP1 bool
//------------------------------------------------------------------------------------------------------------------------------
    #define StpF1 float
    #define StpF2 vec2
    #define StpF3 vec3
    #define StpF4 vec4
//------------------------------------------------------------------------------------------------------------------------------
    #define StpU1 uint
    #define StpU2 uvec2
    #define StpU3 uvec3
    #define StpU4 uvec4
//------------------------------------------------------------------------------------------------------------------------------
    #define StpF1_U1(x) uintBitsToFloat(StpU1(x))
    #define StpF2_U2(x) uintBitsToFloat(StpU2(x))
    #define StpF3_U3(x) uintBitsToFloat(StpU3(x))
    #define StpF4_U4(x) uintBitsToFloat(StpU4(x))
    #define StpU1_F1(x) floatBitsToUint(StpF1(x))
    #define StpU2_F2(x) floatBitsToUint(StpF2(x))
    #define StpU3_F3(x) floatBitsToUint(StpF3(x))
    #define StpU4_F4(x) floatBitsToUint(StpF4(x))
//------------------------------------------------------------------------------------------------------------------------------
    #define StpU1_H2_F2 packHalf2x16
    #define StpF2_H2_U1 unpackHalf2x16
//------------------------------------------------------------------------------------------------------------------------------
    StpU1 StpBfeU1(StpU1 src, StpU1 off, StpU1 bits) { return bitfieldExtract(src, int(off), int(bits)); }
    // Proxy for V_BFI_B32 where the 'mask' is set as 'bits', 'mask=(1<<bits)-1', and 'bits' needs to be an immediate.
    StpU1 StpBfiMU1(StpU1 src, StpU1 ins, StpU1 bits) { return bitfieldInsert(src, ins, 0, int(bits)); }
#endif
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_GLSL) && defined(STP_16BIT)
    #define StpH1 float16_t
    #define StpH2 f16vec2
    #define StpH3 f16vec3
    #define StpH4 f16vec4
//------------------------------------------------------------------------------------------------------------------------------
    #define StpW1 uint16_t
    #define StpW2 u16vec2
    #define StpW3 u16vec3
    #define StpW4 u16vec4
//------------------------------------------------------------------------------------------------------------------------------
    #define StpW2_U1(x) unpackUint2x16(StpU1(x))
    #define StpH2_U1(x) unpackFloat2x16(StpU1(x))
//------------------------------------------------------------------------------------------------------------------------------
    #define StpW1_H1(x) halfBitsToUint16(StpH1(x))
    #define StpW2_H2(x) halfBitsToUint16(StpH2(x))
    #define StpW3_H3(x) halfBitsToUint16(StpH3(x))
    #define StpW4_H4(x) halfBitsToUint16(StpH4(x))
//------------------------------------------------------------------------------------------------------------------------------
    #define StpH1_W1(x) uint16BitsToHalf(StpW1(x))
    #define StpH2_W2(x) uint16BitsToHalf(StpW2(x))
    #define StpH3_W3(x) uint16BitsToHalf(StpW3(x))
    #define StpH4_W4(x) uint16BitsToHalf(StpW4(x))
#endif
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_HLSL)
    #define StpP1 bool
//------------------------------------------------------------------------------------------------------------------------------
    #define StpF1 float
    #define StpF2 float2
    #define StpF3 float3
    #define StpF4 float4
//------------------------------------------------------------------------------------------------------------------------------
    #define StpU1 uint
    #define StpU2 uint2
    #define StpU3 uint3
    #define StpU4 uint4
//------------------------------------------------------------------------------------------------------------------------------
    #define StpF1_U1(x) asfloat(StpU1(x))
    #define StpF2_U2(x) asfloat(StpU2(x))
    #define StpF3_U3(x) asfloat(StpU3(x))
    #define StpF4_U4(x) asfloat(StpU4(x))
    #define StpU1_F1(x) asuint(StpF1(x))
    #define StpU2_F2(x) asuint(StpF2(x))
    #define StpU3_F3(x) asuint(StpF3(x))
    #define StpU4_F4(x) asuint(StpF4(x))
//------------------------------------------------------------------------------------------------------------------------------
    StpU1 StpU1_H2_F2_x(StpF2 a) { return f32tof16(a.x) | (f32tof16(a.y) << 16); }
    #define StpU1_H2_F2(a) StpU1_H2_F2_x(StpF2(a))
//------------------------------------------------------------------------------------------------------------------------------
    StpF2 StpF2_H2_U1_x(StpU1 x) { return StpF2(f16tof32(x & 0xFFFF), f16tof32(x >> 16)); }
    #define StpF2_H2_U1(x) StpF2_H2_U1_x(StpU1(x))
//------------------------------------------------------------------------------------------------------------------------------
    StpU1 StpBfeU1(StpU1 src, StpU1 off, StpU1 bits) { StpU1 mask = (1u << bits) - 1; return (src >> off) & mask; }
    StpU1 StpBfiMU1(StpU1 src, StpU1 ins, StpU1 bits) { StpU1 mask = (1u << bits) - 1; return (ins & mask) | (src & (~mask)); }
#endif
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_HLSL) && defined(STP_MEDIUM)
    #define StpMF1 min16float
    #define StpMF2 min16float2
    #define StpMF3 min16float3
    #define StpMF4 min16float4
#endif
//==============================================================================================================================
#if defined(STP_GPU) && (!defined(STP_MEDIUM))
    #define StpMF1 StpF1
    #define StpMF2 StpF2
    #define StpMF3 StpF3
    #define StpMF4 StpF4
#endif
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_HLSL) && defined(STP_16BIT)
    #define StpH1 float16_t
    #define StpH2 float16_t2
    #define StpH3 float16_t3
    #define StpH4 float16_t4
//------------------------------------------------------------------------------------------------------------------------------
    #define StpW1 uint16_t
    #define StpW2 uint16_t2
    #define StpW3 uint16_t3
    #define StpW4 uint16_t4
//------------------------------------------------------------------------------------------------------------------------------
    StpW2 StpW2_U1_x(StpU1 x) { StpU2 t = StpU2(x & 0xFFFF, x >> 16); return StpW2(t); }
    #define StpW2_U1(x) StpW2_U1_x(StpU1(x))
    StpH2 StpH2_U1_x(StpU1 x) { StpF2 t = f16tof32(StpU2(x & 0xFFFF,x >> 16)); return StpH2(t); }
    #define StpH2_U1(x) StpH2_U1_x(StpU1(x))
//------------------------------------------------------------------------------------------------------------------------------
    #define StpW1_H1(x) asuint16(StpH1(x))
    #define StpW2_H2(x) asuint16(StpH2(x))
    #define StpW3_H3(x) asuint16(StpH3(x))
    #define StpW4_H4(x) asuint16(StpH4(x))
//------------------------------------------------------------------------------------------------------------------------------
    #define StpH1_W1(x) asfloat16(StpW1(x))
    #define StpH2_W2(x) asfloat16(StpW2(x))
    #define StpH3_W3(x) asfloat16(StpW3(x))
    #define StpH4_W4(x) asfloat16(StpW4(x))
#endif
//==============================================================================================================================
#if defined(STP_GPU) && (defined(STP_GLSL) || defined(STP_HLSL))
    StpF1 StpMaxF1(StpF1 a, StpF1 b) { return max(a, b); }
//------------------------------------------------------------------------------------------------------------------------------
    StpF1 StpF1_x(StpF1 x) { return StpF1(x); }
    StpF2 StpF2_x(StpF1 x) { return StpF2(x, x); }
    StpF3 StpF3_x(StpF1 x) { return StpF3(x, x, x); }
    StpF4 StpF4_x(StpF1 x) { return StpF4(x, x, x, x); }
    #define StpF1_(x) StpF1_x(StpF1(x))
    #define StpF2_(x) StpF2_x(StpF1(x))
    #define StpF3_(x) StpF3_x(StpF1(x))
    #define StpF4_(x) StpF4_x(StpF1(x))
//------------------------------------------------------------------------------------------------------------------------------
    StpMF1 StpMF1_x(StpMF1 x) { return StpMF1(x); }
    StpMF2 StpMF2_x(StpMF1 x) { return StpMF2(x, x); }
    StpMF3 StpMF3_x(StpMF1 x) { return StpMF3(x, x, x); }
    StpMF4 StpMF4_x(StpMF1 x) { return StpMF4(x, x, x, x); }
    #define StpMF1_(x) StpMF1_x(StpMF1(x))
    #define StpMF2_(x) StpMF2_x(StpMF1(x))
    #define StpMF3_(x) StpMF3_x(StpMF1(x))
    #define StpMF4_(x) StpMF4_x(StpMF1(x))
//------------------------------------------------------------------------------------------------------------------------------
    StpU1 StpU1_x(StpU1 x) { return StpU1(x); }
    StpU2 StpU2_x(StpU1 x) { return StpU2(x, x); }
    StpU3 StpU3_x(StpU1 x) { return StpU3(x, x, x); }
    StpU4 StpU4_x(StpU1 x) { return StpU4(x, x, x, x); }
    #define StpU1_(x) StpU1_x(StpU1(x))
    #define StpU2_(x) StpU2_x(StpU1(x))
    #define StpU3_(x) StpU3_x(StpU1(x))
    #define StpU4_(x) StpU4_x(StpU1(x))
//------------------------------------------------------------------------------------------------------------------------------
    #if 0
        // Slow implementation (if not pattern matched by a compiler).
        StpF1 StpCpySgnF1(StpF1 d, StpF1 s) { return StpF1_U1(StpU1_F1(d) | (StpU1_F1(s) & StpU1_(0x80000000u))); }
        StpF2 StpCpySgnF2(StpF2 d, StpF2 s) { return StpF2_U2(StpU2_F2(d) | (StpU2_F2(s) & StpU2_(0x80000000u))); }
        StpF3 StpCpySgnF3(StpF3 d, StpF3 s) { return StpF3_U3(StpU3_F3(d) | (StpU3_F3(s) & StpU3_(0x80000000u))); }
        StpF4 StpCpySgnF4(StpF4 d, StpF4 s) { return StpF4_U4(StpU4_F4(d) | (StpU4_F4(s) & StpU4_(0x80000000u))); }
    #else
        // Faster implementation (one portable BFI).
        StpF1 StpCpySgnF1(StpF1 d, StpF1 s) { return StpF1_U1(StpBfiMU1(StpU1_F1(s), StpU1_F1(d), StpU1_(31))); }
        StpF2 StpCpySgnF2(StpF2 d, StpF2 s) { return StpF2(StpCpySgnF1(d.x, s.x), StpCpySgnF1(d.y, s.y)); }
        StpF3 StpCpySgnF3(StpF3 d, StpF3 s) {
            return StpF3(StpCpySgnF1(d.x, s.x), StpCpySgnF1(d.y, s.y), StpCpySgnF1(d.z, s.z)); }
        StpF4 StpCpySgnF4(StpF4 d, StpF4 s) {
            return StpF4(StpCpySgnF1(d.x, s.x), StpCpySgnF1(d.y, s.y), StpCpySgnF1(d.z, s.z), StpCpySgnF1(d.w, s.w)); }
    #endif
    StpF1 StpMax3F1(StpF1 x, StpF1 y, StpF1 z) { return max(x, max(y, z)); }
    StpF2 StpMax3F2(StpF2 x, StpF2 y, StpF2 z) { return max(x, max(y, z)); }
    StpF3 StpMax3F3(StpF3 x, StpF3 y, StpF3 z) { return max(x, max(y, z)); }
    StpF4 StpMax3F4(StpF4 x, StpF4 y, StpF4 z) { return max(x, max(y, z)); }
    StpF1 StpMin3F1(StpF1 x, StpF1 y, StpF1 z) { return min(x, min(y, z)); }
    StpF2 StpMin3F2(StpF2 x, StpF2 y, StpF2 z) { return min(x, min(y, z)); }
    StpF3 StpMin3F3(StpF3 x, StpF3 y, StpF3 z) { return min(x, min(y, z)); }
    StpF4 StpMin3F4(StpF4 x, StpF4 y, StpF4 z) { return min(x, min(y, z)); }
    StpU1 StpMax3U1(StpU1 x, StpU1 y, StpU1 z) { return max(x, max(y, z)); }
    StpU1 StpMin3U1(StpU1 x, StpU1 y, StpU1 z) { return min(x, min(y, z)); }
//------------------------------------------------------------------------------------------------------------------------------
    StpMF1 StpMax3MF1(StpMF1 x, StpMF1 y, StpMF1 z) { return max(x, max(y, z)); }
    StpMF2 StpMax3MF2(StpMF2 x, StpMF2 y, StpMF2 z) { return max(x, max(y, z)); }
    StpMF3 StpMax3MF3(StpMF3 x, StpMF3 y, StpMF3 z) { return max(x, max(y, z)); }
    StpMF4 StpMax3MF4(StpMF4 x, StpMF4 y, StpMF4 z) { return max(x, max(y, z)); }
    StpMF1 StpMin3MF1(StpMF1 x, StpMF1 y, StpMF1 z) { return min(x, min(y, z)); }
    StpMF2 StpMin3MF2(StpMF2 x, StpMF2 y, StpMF2 z) { return min(x, min(y, z)); }
    StpMF3 StpMin3MF3(StpMF3 x, StpMF3 y, StpMF3 z) { return min(x, min(y, z)); }
    StpMF4 StpMin3MF4(StpMF4 x, StpMF4 y, StpMF4 z) { return min(x, min(y, z)); }
//------------------------------------------------------------------------------------------------------------------------------
    // Make {<+0 := -1.0, >=+0 := 1.0}.
    StpF1 StpSgnOneF1(StpF1 x) { return StpF1_U1(StpBfiMU1(StpU1_F1(x), StpU1_(0x3f800000), StpU1_(31))); }
#endif
//==============================================================================================================================
#if defined(STP_GPU) && (defined(STP_GLSL) || defined(STP_HLSL)) && defined(STP_16BIT)
    StpH1 StpH1_x(StpH1 x) { return StpH1(x); }
    StpH2 StpH2_x(StpH1 x) { return StpH2(x, x); }
    StpH3 StpH3_x(StpH1 x) { return StpH3(x, x, x); }
    StpH4 StpH4_x(StpH1 x) { return StpH4(x, x, x, x); }
    #define StpH1_(x) StpH1_x(StpH1(x))
    #define StpH2_(x) StpH2_x(StpH1(x))
    #define StpH3_(x) StpH3_x(StpH1(x))
    #define StpH4_(x) StpH4_x(StpH1(x))
//------------------------------------------------------------------------------------------------------------------------------
    StpW1 StpW1_x(StpW1 x) { return StpW1(x); }
    StpW2 StpW2_x(StpW1 x) { return StpW2(x, x); }
    StpW3 StpW3_x(StpW1 x) { return StpW3(x, x, x); }
    StpW4 StpW4_x(StpW1 x) { return StpW4(x, x, x, x); }
    #define StpW1_(x) StpW1_x(StpW1(x))
    #define StpW2_(x) StpW2_x(StpW1(x))
    #define StpW3_(x) StpW3_x(StpW1(x))
    #define StpW4_(x) StpW4_x(StpW1(x))
//------------------------------------------------------------------------------------------------------------------------------
    StpH1 StpMax3H1(StpH1 x, StpH1 y, StpH1 z) { return max(x, max(y, z)); }
    StpH2 StpMax3H2(StpH2 x, StpH2 y, StpH2 z) { return max(x, max(y, z)); }
    StpH3 StpMax3H3(StpH3 x, StpH3 y, StpH3 z) { return max(x, max(y, z)); }
    StpH4 StpMax3H4(StpH4 x, StpH4 y, StpH4 z) { return max(x, max(y, z)); }
    StpH1 StpMin3H1(StpH1 x, StpH1 y, StpH1 z) { return min(x, min(y, z)); }
    StpH2 StpMin3H2(StpH2 x, StpH2 y, StpH2 z) { return min(x, min(y, z)); }
    StpH3 StpMin3H3(StpH3 x, StpH3 y, StpH3 z) { return min(x, min(y, z)); }
    StpH4 StpMin3H4(StpH4 x, StpH4 y, StpH4 z) { return min(x, min(y, z)); }
    StpW1 StpMax3W1(StpW1 x, StpW1 y, StpW1 z) { return max(x, max(y, z)); }
    StpW1 StpMin3W1(StpW1 x, StpW1 y, StpW1 z) { return min(x, min(y, z)); }
#endif
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_GLSL)
    StpF1 StpFractF1(StpF1 x) { return fract(x); }
    StpF2 StpFractF2(StpF2 x) { return fract(x); }
    StpF3 StpFractF3(StpF3 x) { return fract(x); }
    StpF4 StpFractF4(StpF4 x) { return fract(x); }
    StpF1 StpLerpF1(StpF1 x, StpF1 y, StpF1 z) { return mix(x, y, z); }
    StpF2 StpLerpF2(StpF2 x, StpF2 y, StpF2 z) { return mix(x, y, z); }
    StpF3 StpLerpF3(StpF3 x, StpF3 y, StpF3 z) { return mix(x, y, z); }
    StpF4 StpLerpF4(StpF4 x, StpF4 y, StpF4 z) { return mix(x, y, z); }
    StpF1 StpRcpF1(StpF1 x) { return StpF1_(1.0) / x; }
    StpF2 StpRcpF2(StpF2 x) { return StpF2_(1.0) / x; }
    StpF3 StpRcpF3(StpF3 x) { return StpF3_(1.0) / x; }
    StpF4 StpRcpF4(StpF4 x) { return StpF4_(1.0) / x; }
    StpF1 StpSatF1(StpF1 x) { return clamp(x, StpF1_(0.0), StpF1_(1.0)); }
    StpF2 StpSatF2(StpF2 x) { return clamp(x, StpF2_(0.0), StpF2_(1.0)); }
    StpF3 StpSatF3(StpF3 x) { return clamp(x, StpF3_(0.0), StpF3_(1.0)); }
    StpF4 StpSatF4(StpF4 x) { return clamp(x, StpF4_(0.0), StpF4_(1.0)); }
//------------------------------------------------------------------------------------------------------------------------------
    StpMF1 StpFractMF1(StpMF1 x) { return fract(x); }
    StpMF2 StpFractMF2(StpMF2 x) { return fract(x); }
    StpMF3 StpFractMF3(StpMF3 x) { return fract(x); }
    StpMF4 StpFractMF4(StpMF4 x) { return fract(x); }
    StpMF1 StpLerpMF1(StpMF1 x, StpMF1 y, StpMF1 z) { return mix(x, y, z); }
    StpMF2 StpLerpMF2(StpMF2 x, StpMF2 y, StpMF2 z) { return mix(x, y, z); }
    StpMF3 StpLerpMF3(StpMF3 x, StpMF3 y, StpMF3 z) { return mix(x, y, z); }
    StpMF4 StpLerpMF4(StpMF4 x, StpMF4 y, StpMF4 z) { return mix(x, y, z); }
    StpMF1 StpRcpMF1(StpMF1 x) { return StpMF1_(1.0) / x; }
    StpMF2 StpRcpMF2(StpMF2 x) { return StpMF2_(1.0) / x; }
    StpMF3 StpRcpMF3(StpMF3 x) { return StpMF3_(1.0) / x; }
    StpMF4 StpRcpMF4(StpMF4 x) { return StpMF4_(1.0) / x; }
    StpMF1 StpSatMF1(StpMF1 x) { return clamp(x, StpMF1_(0.0), StpMF1_(1.0)); }
    StpMF2 StpSatMF2(StpMF2 x) { return clamp(x, StpMF2_(0.0), StpMF2_(1.0)); }
    StpMF3 StpSatMF3(StpMF3 x) { return clamp(x, StpMF3_(0.0), StpMF3_(1.0)); }
    StpMF4 StpSatMF4(StpMF4 x) { return clamp(x, StpMF4_(0.0), StpMF4_(1.0)); }
#endif
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_GLSL) && defined(STP_16BIT)
    StpH1 StpFractH1(StpH1 x) { return fract(x); }
    StpH2 StpFractH2(StpH2 x) { return fract(x); }
    StpH3 StpFractH3(StpH3 x) { return fract(x); }
    StpH4 StpFractH4(StpH4 x) { return fract(x); }
    StpH1 StpLerpH1(StpH1 x, StpH1 y, StpH1 z) { return mix(x, y, z); }
    StpH2 StpLerpH2(StpH2 x, StpH2 y, StpH2 z) { return mix(x, y, z); }
    StpH3 StpLerpH3(StpH3 x, StpH3 y, StpH3 z) { return mix(x, y, z); }
    StpH4 StpLerpH4(StpH4 x, StpH4 y, StpH4 z) { return mix(x, y, z); }
    StpH1 StpRcpH1(StpH1 x) { return StpH1_(1.0) / x; }
    StpH2 StpRcpH2(StpH2 x) { return StpH2_(1.0) / x; }
    StpH3 StpRcpH3(StpH3 x) { return StpH3_(1.0) / x; }
    StpH4 StpRcpH4(StpH4 x) { return StpH4_(1.0) / x; }
    StpH1 StpSatH1(StpH1 x) { return clamp(x, StpH1_(0.0), StpH1_(1.0)); }
    StpH2 StpSatH2(StpH2 x) { return clamp(x, StpH2_(0.0), StpH2_(1.0)); }
    StpH3 StpSatH3(StpH3 x) { return clamp(x, StpH3_(0.0), StpH3_(1.0)); }
    StpH4 StpSatH4(StpH4 x) { return clamp(x, StpH4_(0.0), StpH4_(1.0)); }
#endif
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_HLSL)
    StpF1 StpFractF1(StpF1 x) { return x - floor(x); }
    StpF2 StpFractF2(StpF2 x) { return x - floor(x); }
    StpF3 StpFractF3(StpF3 x) { return x - floor(x); }
    StpF4 StpFractF4(StpF4 x) { return x - floor(x); }
    StpF1 StpLerpF1(StpF1 x, StpF1 y, StpF1 z) { return lerp(x, y, z); }
    StpF2 StpLerpF2(StpF2 x, StpF2 y, StpF2 z) { return lerp(x, y, z); }
    StpF3 StpLerpF3(StpF3 x, StpF3 y, StpF3 z) { return lerp(x, y, z); }
    StpF4 StpLerpF4(StpF4 x, StpF4 y, StpF4 z) { return lerp(x, y, z); }
    StpF1 StpRcpF1(StpF1 x) { return rcp(x); }
    StpF2 StpRcpF2(StpF2 x) { return rcp(x); }
    StpF3 StpRcpF3(StpF3 x) { return rcp(x); }
    StpF4 StpRcpF4(StpF4 x) { return rcp(x); }
    StpF1 StpSatF1(StpF1 x) { return saturate(x); }
    StpF2 StpSatF2(StpF2 x) { return saturate(x); }
    StpF3 StpSatF3(StpF3 x) { return saturate(x); }
    StpF4 StpSatF4(StpF4 x) { return saturate(x); }
//------------------------------------------------------------------------------------------------------------------------------
    StpMF1 StpFractMF1(StpMF1 x) { return x - floor(x); }
    StpMF2 StpFractMF2(StpMF2 x) { return x - floor(x); }
    StpMF3 StpFractMF3(StpMF3 x) { return x - floor(x); }
    StpMF4 StpFractMF4(StpMF4 x) { return x - floor(x); }
    StpMF1 StpLerpMF1(StpMF1 x, StpMF1 y, StpMF1 z) { return lerp(x, y, z); }
    StpMF2 StpLerpMF2(StpMF2 x, StpMF2 y, StpMF2 z) { return lerp(x, y, z); }
    StpMF3 StpLerpMF3(StpMF3 x, StpMF3 y, StpMF3 z) { return lerp(x, y, z); }
    StpMF4 StpLerpMF4(StpMF4 x, StpMF4 y, StpMF4 z) { return lerp(x, y, z); }
    StpMF1 StpRcpMF1(StpMF1 x) { return rcp(x); }
    StpMF2 StpRcpMF2(StpMF2 x) { return rcp(x); }
    StpMF3 StpRcpMF3(StpMF3 x) { return rcp(x); }
    StpMF4 StpRcpMF4(StpMF4 x) { return rcp(x); }
    StpMF1 StpSatMF1(StpMF1 x) { return saturate(x); }
    StpMF2 StpSatMF2(StpMF2 x) { return saturate(x); }
    StpMF3 StpSatMF3(StpMF3 x) { return saturate(x); }
    StpMF4 StpSatMF4(StpMF4 x) { return saturate(x); }
#endif
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_HLSL) && defined(STP_16BIT)
    StpH1 StpFractH1(StpH1 x) { return x - floor(x); }
    StpH2 StpFractH2(StpH2 x) { return x - floor(x); }
    StpH3 StpFractH3(StpH3 x) { return x - floor(x); }
    StpH4 StpFractH4(StpH4 x) { return x - floor(x); }
    StpH1 StpLerpH1(StpH1 x, StpH1 y, StpH1 z) { return lerp(x, y, z); }
    StpH2 StpLerpH2(StpH2 x, StpH2 y, StpH2 z) { return lerp(x, y, z); }
    StpH3 StpLerpH3(StpH3 x, StpH3 y, StpH3 z) { return lerp(x, y, z); }
    StpH4 StpLerpH4(StpH4 x, StpH4 y, StpH4 z) { return lerp(x, y, z); }
    StpH1 StpRcpH1(StpH1 x) { return rcp(x); }
    StpH2 StpRcpH2(StpH2 x) { return rcp(x); }
    StpH3 StpRcpH3(StpH3 x) { return rcp(x); }
    StpH4 StpRcpH4(StpH4 x) { return rcp(x); }
    StpH1 StpSatH1(StpH1 x) { return saturate(x); }
    StpH2 StpSatH2(StpH2 x) { return saturate(x); }
    StpH3 StpSatH3(StpH3 x) { return saturate(x); }
    StpH4 StpSatH4(StpH4 x) { return saturate(x); }
#endif
//==============================================================================================================================
#if defined(STP_GPU) && (defined(STP_GLSL) || defined(STP_HLSL))
    StpF1 StpExp2F1(StpF1 x) { return exp2(x); }
    StpF1 StpLog2F1(StpF1 x) { return log2(x); }
//------------------------------------------------------------------------------------------------------------------------------
    StpMF1 StpExp2MF1(StpMF1 x) { return exp2(x); }
    StpMF1 StpLog2MF1(StpMF1 x) { return log2(x); }
//------------------------------------------------------------------------------------------------------------------------------
    #define STP_INFN_F StpF1_U1(0xff800000u)
    #define STP_INFP_F StpF1_U1(0x7f800000u)
    StpF1 StpGtZeroF1(StpF1 x) { return StpSatF1(x * StpF1_(STP_INFP_F)); }
    StpF3 StpGtZeroF3(StpF3 x) { return StpSatF3(x * StpF3_(STP_INFP_F)); }
    StpF4 StpGtZeroF4(StpF4 x) { return StpSatF4(x * StpF4_(STP_INFP_F)); }
    StpF1 StpSignedF1(StpF1 x) { return StpSatF1(x * StpF1_(STP_INFN_F)); }
    StpF2 StpSignedF2(StpF2 x) { return StpSatF2(x * StpF2_(STP_INFN_F)); }
    StpF3 StpSignedF3(StpF3 x) { return StpSatF3(x * StpF3_(STP_INFN_F)); }
    StpF4 StpSignedF4(StpF4 x) { return StpSatF4(x * StpF4_(STP_INFN_F)); }
//------------------------------------------------------------------------------------------------------------------------------
    #if STP_BUG_PRX
        StpF1 StpPrxLoSqrtF1(StpF1 a) { return sqrt(a); }
        StpF3 StpPrxLoSqrtF3(StpF3 a) { return sqrt(a); }
        StpF4 StpPrxLoSqrtF4(StpF4 a) { return sqrt(a); }
    #else
        StpF1 StpPrxLoSqrtF1(StpF1 a) { return StpF1_U1((StpU1_F1(a) >> StpU1_(1)) + StpU1_(0x1fbc4639)); }
        StpF3 StpPrxLoSqrtF3(StpF3 a) { return StpF3_U3((StpU3_F3(a) >> StpU3_(1)) + StpU3_(0x1fbc4639)); }
        StpF4 StpPrxLoSqrtF4(StpF4 a) { return StpF4_U4((StpU4_F4(a) >> StpU4_(1)) + StpU4_(0x1fbc4639)); }
    #endif
//------------------------------------------------------------------------------------------------------------------------------
    #if STP_BUG_PRX
        StpF1 StpPrxLoRcpF1(StpF1 a) { return StpRcpF1(a); }
        StpF2 StpPrxLoRcpF2(StpF2 a) { return StpRcpF2(a); }
        StpF3 StpPrxLoRcpF3(StpF3 a) { return StpRcpF3(a); }
        StpF4 StpPrxLoRcpF4(StpF4 a) { return StpRcpF4(a); }
        StpF1 StpPrxMedRcpF1(StpF1 a) { return StpRcpF1(a); }
        StpF3 StpPrxMedRcpF3(StpF3 a) { return StpRcpF3(a); }
    #else
        StpF1 StpPrxLoRcpF1(StpF1 a) { return StpF1_U1(StpU1_(0x7ef07ebb) - StpU1_F1(a)); }
        StpF2 StpPrxLoRcpF2(StpF2 a) { return StpF2_U2(StpU2_(0x7ef07ebb) - StpU2_F2(a)); }
        StpF3 StpPrxLoRcpF3(StpF3 a) { return StpF3_U3(StpU3_(0x7ef07ebb) - StpU3_F3(a)); }
        StpF4 StpPrxLoRcpF4(StpF4 a) { return StpF4_U4(StpU4_(0x7ef07ebb) - StpU4_F4(a)); }
        StpF1 StpPrxMedRcpF1(StpF1 a) { StpF1 b = StpF1_U1(StpU1_(0x7ef19fff) - StpU1_F1(a));
            return b * (-b * a + StpF1_(2.0)); }
        StpF3 StpPrxMedRcpF3(StpF3 a) { StpF3 b = StpF3_U3(StpU3_(0x7ef19fff) - StpU3_F3(a));
            return b * (-b * a + StpF3_(2.0)); }
    #endif
//------------------------------------------------------------------------------------------------------------------------------
    #define STP_STATIC /* */
    #define StpInF2 in StpF2
    #define StpInF4 in StpF4
    #define StpInOutU4 inout StpU4
    #define StpOutF2 out StpF2
    #define StpVarF2 StpF2
#endif
//==============================================================================================================================
#if defined(STP_GPU) && (defined(STP_GLSL) || defined(STP_HLSL)) && defined(STP_MEDIUM)
    #if STP_BUG_SAT_INF
        // Defined if unable to use the fast path because of problem related to saturating +/- INF.
        StpMF1 StpGtZeroMF1(StpMF1 x) { return (x > StpMF1_(0.0)) ? StpMF1_(1.0) : StpMF1_(0.0); }
        StpMF3 StpGtZeroMF3(StpMF3 x) { return StpMF3(StpGtZeroMF1(x.r), StpGtZeroMF1(x.g), StpGtZeroMF1(x.b)); }
        StpMF4 StpGtZeroMF4(StpMF4 x) { return StpMF4(StpGtZeroMF1(x.r), StpGtZeroMF1(x.g),
            StpGtZeroMF1(x.b), StpGtZeroMF1(x.a)); }
        StpMF1 StpSignedMF1(StpMF1 x) { return (x < StpMF1_(0.0)) ? StpMF1_(1.0) : StpMF1_(0.0); }
        StpMF2 StpSignedMF2(StpMF2 x) { return StpMF2(StpSignedMF1(x.r), StpSignedMF1(x.g)); }
        StpMF3 StpSignedMF3(StpMF3 x) { return StpMF3(StpSignedMF1(x.r), StpSignedMF1(x.g), StpSignedMF1(x.b)); }
        StpMF4 StpSignedMF4(StpMF4 x) { return StpMF4(StpSignedMF1(x.r), StpSignedMF1(x.g),
            StpSignedMF1(x.b), StpSignedMF1(x.a)); }
    #elif STP_BUG_SAT
        // Defined if compiler factors out saturation incorrectly.
        #define STP_INFN_MF StpMF1(StpF1_U1(0xff800000u))
        #define STP_INFP_MF StpMF1(StpF1_U1(0x7f800000u))
        StpMF1 StpGtZeroMF1(StpMF1 x) { return max(min(x * StpMF1_(STP_INFP_MF), StpMF1_(1.0)), StpMF1_(0.0)); }
        StpMF3 StpGtZeroMF3(StpMF3 x) { return max(min(x * StpMF3_(STP_INFP_MF), StpMF3_(1.0)), StpMF3_(0.0)); }
        StpMF4 StpGtZeroMF4(StpMF4 x) { return max(min(x * StpMF4_(STP_INFP_MF), StpMF4_(1.0)), StpMF4_(0.0)); }
        StpMF1 StpSignedMF1(StpMF1 x) { return max(min(x * StpMF1_(STP_INFN_MF), StpMF1_(1.0)), StpMF1_(0.0)); }
        StpMF2 StpSignedMF2(StpMF2 x) { return max(min(x * StpMF2_(STP_INFN_MF), StpMF2_(1.0)), StpMF2_(0.0)); }
        StpMF3 StpSignedMF3(StpMF3 x) { return max(min(x * StpMF3_(STP_INFN_MF), StpMF3_(1.0)), StpMF3_(0.0)); }
        StpMF4 StpSignedMF4(StpMF4 x) { return max(min(x * StpMF4_(STP_INFN_MF), StpMF4_(1.0)), StpMF4_(0.0)); }
    #else
        // Using +/- INF typecast down to medium precision.
        #define STP_INFN_MF StpMF1(StpF1_U1(0xff800000u))
        #define STP_INFP_MF StpMF1(StpF1_U1(0x7f800000u))
        StpMF1 StpGtZeroMF1(StpMF1 x) { return StpSatMF1(x * StpMF1_(STP_INFP_MF)); }
        StpMF3 StpGtZeroMF3(StpMF3 x) { return StpSatMF3(x * StpMF3_(STP_INFP_MF)); }
        StpMF4 StpGtZeroMF4(StpMF4 x) { return StpSatMF4(x * StpMF4_(STP_INFP_MF)); }
        StpMF1 StpSignedMF1(StpMF1 x) { return StpSatMF1(x * StpMF1_(STP_INFN_MF)); }
        StpMF2 StpSignedMF2(StpMF2 x) { return StpSatMF2(x * StpMF2_(STP_INFN_MF)); }
        StpMF3 StpSignedMF3(StpMF3 x) { return StpSatMF3(x * StpMF3_(STP_INFN_MF)); }
        StpMF4 StpSignedMF4(StpMF4 x) { return StpSatMF4(x * StpMF4_(STP_INFN_MF)); }
    #endif
//------------------------------------------------------------------------------------------------------------------------------
    // Unable to use the approximations due to not knowing what the type actually is.
    StpMF1 StpPrxLoSqrtMF1(StpMF1 a) { return sqrt(a); }
    StpMF3 StpPrxLoSqrtMF3(StpMF3 a) { return sqrt(a); }
    StpMF4 StpPrxLoSqrtMF4(StpMF4 a) { return sqrt(a); }
//------------------------------------------------------------------------------------------------------------------------------
    StpMF1 StpPrxLoRcpMF1(StpMF1 a) { return StpRcpMF1(a); }
    StpMF2 StpPrxLoRcpMF2(StpMF2 a) { return StpRcpMF2(a); }
    StpMF3 StpPrxLoRcpMF3(StpMF3 a) { return StpRcpMF3(a); }
    StpMF4 StpPrxLoRcpMF4(StpMF4 a) { return StpRcpMF4(a); }
    StpMF1 StpPrxMedRcpMF1(StpMF1 a) { return StpRcpMF1(a); }
    StpMF3 StpPrxMedRcpMF3(StpMF3 a) { return StpRcpMF3(a); }
#endif
//==============================================================================================================================
#if defined(STP_GPU) && (defined(STP_GLSL) || defined(STP_HLSL)) && (!defined(STP_MEDIUM))
    // Same types so just use the full precision version.
    #define StpGtZeroMF1(a) StpGtZeroF1(a)
    #define StpGtZeroMF2(a) StpGtZeroF2(a)
    #define StpGtZeroMF3(a) StpGtZeroF3(a)
    #define StpGtZeroMF4(a) StpGtZeroF4(a)
    #define StpSignedMF1(a) StpSignedF1(a)
    #define StpSignedMF2(a) StpSignedF2(a)
    #define StpSignedMF3(a) StpSignedF3(a)
    #define StpSignedMF4(a) StpSignedF4(a)
//------------------------------------------------------------------------------------------------------------------------------
    // The medium precision types are the same as the full precision so use the full precision approximations.
    #define StpPrxLoSqrtMF1(a) StpPrxLoSqrtF1(a)
    #define StpPrxLoSqrtMF3(a) StpPrxLoSqrtF3(a)
    #define StpPrxLoSqrtMF4(a) StpPrxLoSqrtF4(a)
//------------------------------------------------------------------------------------------------------------------------------
    #define StpPrxLoRcpMF1(a) StpPrxLoRcpF1(a)
    #define StpPrxLoRcpMF2(a) StpPrxLoRcpF2(a)
    #define StpPrxLoRcpMF3(a) StpPrxLoRcpF3(a)
    #define StpPrxLoRcpMF4(a) StpPrxLoRcpF4(a)
    #define StpPrxMedRcpMF1(a) StpPrxMedRcpF1(a)
    #define StpPrxMedRcpMF3(a) StpPrxMedRcpF3(a)
#endif
//==============================================================================================================================
#if defined(STP_GPU) && (defined(STP_GLSL) || defined(STP_HLSL)) && defined(STP_16BIT)
    StpH1 StpExp2H1(StpH1 x) { return exp2(x); }
    StpH1 StpLog2H1(StpH1 x) { return log2(x); }
//------------------------------------------------------------------------------------------------------------------------------
    #if STP_BUG_ALIAS16
        // Use 32-bit aliasing to build the +/-INF, then typecast to 16-bit.
        #define STP_INFN_H StpH1(StpF1_U1(0xff800000u))
        #define STP_INFP_H StpH1(StpF1_U1(0x7f800000u))
    #else
        #define STP_INFN_H StpH1_W1(StpW1_(0xfc00))
        #define STP_INFP_H StpH1_W1(StpW1_(0x7c00))
    #endif
    #if STP_BUG_SAT_INF
        StpH1 StpGtZeroH1(StpH1 x) { return (x > StpH1_(0.0)) ? StpH1_(1.0) : StpH1_(0.0); }
        StpH3 StpGtZeroH3(StpH3 x) { return StpH3(StpGtZeroH1(x.r), StpGtZeroH1(x.g), StpGtZeroH1(x.b)); }
        StpH4 StpGtZeroH4(StpH4 x) { return StpH4(StpGtZeroH1(x.r), StpGtZeroH1(x.g),
            StpGtZeroH1(x.b), StpGtZeroH1(x.a)); }
        StpH1 StpSignedH1(StpH1 x) { return (x < StpH1_(0.0)) ? StpH1_(1.0) : StpH1_(0.0); }
        StpH2 StpSignedH2(StpH2 x) { return StpH2(StpSignedH1(x.r), StpSignedH1(x.g)); }
        StpH3 StpSignedH3(StpH3 x) { return StpH3(StpSignedH1(x.r), StpSignedH1(x.g), StpSignedH1(x.b)); }
        StpH4 StpSignedH4(StpH4 x) { return StpH4(StpSignedH1(x.r), StpSignedH1(x.g),
            StpSignedH1(x.b), StpSignedH1(x.a)); }
    #elif STP_BUG_SAT
        StpH1 StpGtZeroH1(StpH1 x) { return max(min(x * StpH1_(STP_INFP_H), StpH1_(1.0)), StpH1_(0.0)); }
        StpH3 StpGtZeroH3(StpH3 x) { return max(min(x * StpH3_(STP_INFP_H), StpH3_(1.0)), StpH3_(0.0)); }
        StpH4 StpGtZeroH4(StpH4 x) { return max(min(x * StpH4_(STP_INFP_H), StpH4_(1.0)), StpH4_(0.0)); }
        StpH1 StpSignedH1(StpH1 x) { return max(min(x * StpH1_(STP_INFN_H), StpH1_(1.0)), StpH1_(0.0)); }
        StpH2 StpSignedH2(StpH2 x) { return max(min(x * StpH2_(STP_INFN_H), StpH2_(1.0)), StpH2_(0.0)); }
        StpH3 StpSignedH3(StpH3 x) { return max(min(x * StpH3_(STP_INFN_H), StpH3_(1.0)), StpH3_(0.0)); }
        StpH4 StpSignedH4(StpH4 x) { return max(min(x * StpH4_(STP_INFN_H), StpH4_(1.0)), StpH4_(0.0)); }
    #else
        StpH1 StpGtZeroH1(StpH1 x) { return StpSatH1(x * StpH1_(STP_INFP_H)); }
        StpH3 StpGtZeroH3(StpH3 x) { return StpSatH3(x * StpH3_(STP_INFP_H)); }
        StpH4 StpGtZeroH4(StpH4 x) { return StpSatH4(x * StpH4_(STP_INFP_H)); }
        StpH1 StpSignedH1(StpH1 x) { return StpSatH1(x * StpH1_(STP_INFN_H)); }
        StpH2 StpSignedH2(StpH2 x) { return StpSatH2(x * StpH2_(STP_INFN_H)); }
        StpH3 StpSignedH3(StpH3 x) { return StpSatH3(x * StpH3_(STP_INFN_H)); }
        StpH4 StpSignedH4(StpH4 x) { return StpSatH4(x * StpH4_(STP_INFN_H)); }
    #endif
//------------------------------------------------------------------------------------------------------------------------------
    #if STP_BUG_PRX
        StpH1 StpPrxLoSqrtH1(StpH1 a) { return sqrt(a); }
        StpH3 StpPrxLoSqrtH3(StpH3 a) { return sqrt(a); }
        StpH4 StpPrxLoSqrtH4(StpH4 a) { return sqrt(a); }
    #else
        StpH1 StpPrxLoSqrtH1(StpH1 a) { return StpH1_W1((StpW1_H1(a) >> StpW1_(1)) + StpW1_(0x1de2)); }
        StpH3 StpPrxLoSqrtH3(StpH3 a) { return StpH3_W3((StpW3_H3(a) >> StpW3_(1)) + StpW3_(0x1de2)); }
        StpH4 StpPrxLoSqrtH4(StpH4 a) { return StpH4_W4((StpW4_H4(a) >> StpW4_(1)) + StpW4_(0x1de2)); }
    #endif
//------------------------------------------------------------------------------------------------------------------------------
    #if STP_BUG_PRX
        StpH1 StpPrxLoRcpH1(StpH1 a) { return StpRcpH1(a); }
        StpH2 StpPrxLoRcpH2(StpH2 a) { return StpRcpH2(a); }
        StpH3 StpPrxLoRcpH3(StpH3 a) { return StpRcpH3(a); }
        StpH4 StpPrxLoRcpH4(StpH4 a) { return StpRcpH4(a); }
        StpH1 StpPrxMedRcpH1(StpH1 a) { return StpRcpH1(a); }
        StpH3 StpPrxMedRcpH3(StpH3 a) { return StpRcpH3(a); }
    #else
        StpH1 StpPrxLoRcpH1(StpH1 a) { return StpH1_W1(StpW1_(0x7784) - StpW1_H1(a)); }
        StpH2 StpPrxLoRcpH2(StpH2 a) { return StpH2_W2(StpW2_(0x7784) - StpW2_H2(a)); }
        StpH3 StpPrxLoRcpH3(StpH3 a) { return StpH3_W3(StpW3_(0x7784) - StpW3_H3(a)); }
        StpH4 StpPrxLoRcpH4(StpH4 a) { return StpH4_W4(StpW4_(0x7784) - StpW4_H4(a)); }
        StpH1 StpPrxMedRcpH1(StpH1 a) { StpH1 b = StpH1_W1(StpW1_(0x778d) - StpW1_H1(a));
            return b * (-b * a + StpH1_(2.0)); }
        StpH3 StpPrxMedRcpH3(StpH3 a) { StpH3 b = StpH3_W3(StpW3_(0x778d) - StpW3_H3(a));
            return b * (-b * a + StpH3_(2.0)); }
    #endif
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                        LANE REMAPPING
//==============================================================================================================================
#if defined(STP_GPU)
    // More complex remap which is safe for both portability (different wave sizes up to 128) and for 2D wave reductions.
    //  6543210
    //  =======
    //  ..xx..x
    //  yy..yy.
    // Details,
    //  LANE TO 8x16 MAPPING
    //  ====================
    //  00 01 08 09 10 11 18 19
    //  02 03 0a 0b 12 13 1a 1b
    //  04 05 0c 0d 14 15 1c 1d
    //  06 07 0e 0f 16 17 1e 1f
    //  20 21 28 29 30 31 38 39
    //  22 23 2a 2b 32 33 3a 3b
    //  24 25 2c 2d 34 35 3c 3d
    //  26 27 2e 2f 36 37 3e 3f
    //  .......................
    //  ... repeat the 8x8 ....
    //  .... pattern, but .....
    //  .... for 40 to 7f .....
    //  .......................
    StpU2 StpRmp8x16U2(StpU1 a) {
        // Note the BFIs used for MSBs have "strange offsets" due to leaving space for the LSB bits replaced in the BFI.
        return StpU2(StpBfiMU1(StpBfeU1(a, 2u, 3u), a, 1u),
            StpBfiMU1(StpBfeU1(a, 3u, 4u), StpBfeU1(a, 1u, 2u), 2u)); }
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                               INTERNAL TUNING (DON'T CHANGE)
//==============================================================================================================================
// This sets the amount of anti-aliasing and is proportional to output pixel size.
//                         xxxxxxxxxx ... Actual radius.
//                  xxx ................. PSinCos() returns {-1/4 to 1/4}.
#define STP_KERNEL (4.0 * (1.0 / 12.0))
//------------------------------------------------------------------------------------------------------------------------------
// Control the min (motion match), and max (no motion match), in units of pixels.
// Settings of {min=0.5,max=1.0} won't work for 9x area scaling (trailing edge smears).
// Setting too tight won't have enough slop for motion matching (motion match easily fails, leading to loss of detail).
// If STP_DIF_MAX is too big, it will look like edges expand (or float) during change of motion.
#define STP_DIF_MIN (0.5 / 8.0)
#define STP_DIF_MAX (1.0 / 8.0)
// Computed constants.
#define STP_DIF_ADD (STP_DIF_MIN * STP_DIF_MIN)
#define STP_DIF_AMP (1.0 / (STP_DIF_MAX * STP_DIF_MAX - STP_DIF_ADD))
//------------------------------------------------------------------------------------------------------------------------------
// Maximum amount of bilinear feedback deblur.
// Maximum should be {8 := smoother or 16 := sharper}, beyond that gets unnatural (too much gradient loss).
#define STP_BI_MAX 16.0
// Fine tune the flicker vs blur of the bilinear deblur.
// Move in powers of two.
// 32768.0 is too blurry, 1.0 is too flickery.
// 8192.0 is the threshold where enemies demo area isn't too flickery.
#define STP_BI_TUNE 8192.0
//------------------------------------------------------------------------------------------------------------------------------
// The range of anti-flicker.
#define STP_ANTI_LIM 8192.0
// Amount to push anti-flicker towards average to avoid loss of anti-aliasing.
// This has a side effect of setting the minimum amount of temporal flicker as well.
// So the divisor is the adjustment.
// Using larger divisors will decrease the effect (leaving less anti-aliasing, but more detail).
#define STP_PUSH (STP_ANTI_LIM / 16.0)
//------------------------------------------------------------------------------------------------------------------------------
// This value controls the limit of neighborhood expansion for sharpening {0.0 := none, 1.0 := maximum}.
// Setting this to the maximum will result in more flicker on fine detail.
// Setting this to the minimum will result in too much loss of sharpening.
#define STP_NE_LIM (1.0)
//------------------------------------------------------------------------------------------------------------------------------
// Interal RCAS tuning.
#define STP_RCAS_LIMIT (0.25 - (1.0 / 16.0))
//------------------------------------------------------------------------------------------------------------------------------
// Maximum feedback (must be larger than 4/5 and smaller than 1).
#define STP_FEED_MAX (31.0/32.0)
//------------------------------------------------------------------------------------------------------------------------------
// Shaped displacement sharpening {1} on, {0} off.
// This only effects the shaping by convergence.
// Faster with this disabled, but this is on by default now, as it helps for peak sharpness.
#ifndef STP_SDS
    #define STP_SDS 1
#endif
//------------------------------------------------------------------------------------------------------------------------------
// De-weight pixel contribution if in front of angular interpolated near depth.
// This removes artifacts that look like floating dithering or edges.
#define STP_AVOID_NEAR (1.0 / 16.0)
//------------------------------------------------------------------------------------------------------------------------------
// Control the balance between sharpness and temporal aliasing.
// Less is sharper and will flicker too much.
// More is not sharp enough and flickers less.
//  1024.0 - Flickers a little more on stills, is sharper.
//  2048.0 - Flickers less, less sharp.
//  4096.0 - Flickers a lot less, and is a lot less sharp (in motion).
// This was set to bias towards output with a non-fixed camera (where the extra sharpness is desired).
// This is a good idea regardless because in general you want a non-fixed camera to hide temporal flicker.
#define STP_SHARP_FLICKER (1024.0)
//------------------------------------------------------------------------------------------------------------------------------
// Z encoding non-linearly correct dithering, on to improve motion match mask.
#define STP_Z_DIT 1
//------------------------------------------------------------------------------------------------------------------------------
// Motion encoding, non-linearly correct dithering, on to improve motion match mask.
#define STP_M_DIT 1
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                      JITTER LOCATIONS
//------------------------------------------------------------------------------------------------------------------------------
// STP requires a 4xMSAA pattern distributed across 4 frames repeated for the sub-pixel jitter offset.
// The whole 4xMSAA pattern offset changes every 4 frames for a 16 frame pattern.
//------------------------------------------------------------------------------------------------------------------------------
//  . 0 . .  {-1/8,-3/8} | {01,00}
//  . . . 1  { 3/8,-1/8} | {11,01}
//  3 . . .  {-3/8, 1/8} | {00,10}
//  . . 2 .  { 1/8, 3/8} | {10,11}
//------------------------------------------------------------------------------------------------------------------------------
//  00=-3/8, 01=-1/8, 10=1/8, 11=3/8
//------------------------------------------------------------------------------------------------------------------------------
//    33221100
//    ========
//  x 00101101 = 2D
//  y 10110100 = B4
//==============================================================================================================================
//  . 0 .
//  3 . 1
//  . 2 .
//------------------------------------------------------------------------------------------------------------------------------
//  0 { 0/8,-1/8} | {01,00}
//  1 { 1/8, 0/8} | {10,01}
//  2 { 0/8, 1/8} | {01,10}
//  3 {-1/8, 0/8} | {00,01}
//------------------------------------------------------------------------------------------------------------------------------
//    33221100
//    ========
//  x 00011001 = 19
//  y 01100100 = 64
//==============================================================================================================================
// Generate jitter amount given frame index.
STP_STATIC void StpJit16(StpOutF2 p, StpU1 frame) {
    // 4xMSAA.
    StpU1 frame0 = (frame & StpU1_(3)) << StpU1_(1);
    StpU1 ix = (StpU1_(0x2D) >> frame0) & StpU1_(3);
    StpU1 iy = (StpU1_(0xB4) >> frame0) & StpU1_(3);
    p[0] = StpF1_(ix) * StpF1_(1.0 / 4.0) + StpF1_(-3.0 / 8.0);
    p[1] = StpF1_(iy) * StpF1_(1.0 / 4.0) + StpF1_(-3.0 / 8.0);
    // Modified by the '+' offset in groups of 4 frames.
    frame0 = ((frame >> StpU1_(2)) & StpU1_(3)) << StpU1_(1);
    ix = (StpU1_(0x19) >> frame0) & StpU1_(3);
    iy = (StpU1_(0x64) >> frame0) & StpU1_(3);
    p[0] += StpF1_(ix) * StpF1_(1.0 / 8.0) + StpF1_(-1.0 / 8.0);
    p[1] += StpF1_(iy) * StpF1_(1.0 / 8.0) + StpF1_(-1.0 / 8.0); }
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                     PARABOLIC {SIN,COS}
//------------------------------------------------------------------------------------------------------------------------------
// Now longer used, but in for future updates.
//==============================================================================================================================
#if defined(STP_GPU)
    // Input is {-1 to 1} representing {0 to 2 pi}, output is {-1/4 to 1/4} representing {-1 to 1}.
    void StpPSinF2(inout StpF2 p) { p = p * abs(p) - p; }
    // This is used to dither position of gather4 fetch for nearest motion vector to remove nearest artifacts when scaling.
    // Input 'p.x' is {0 to 1} representing {0 to 2 pi}, output is {-1/4 to 1/4} representing {-1 to 1}.
    void StpPSinCosF(inout StpF2 p) { p.y = StpFractF1(p.x + StpF1_(0.25)); p = p * StpF2_(2.0) - StpF2_(1.0); StpPSinF2(p); }
//------------------------------------------------------------------------------------------------------------------------------
    void StpPSinMF2(inout StpMF2 p) { p = p * abs(p) - p; }
    void StpPSinCosMF(inout StpMF2 p) {
        p.y = StpFractMF1(p.x + StpMF1_(0.25));
        p = p * StpMF2_(2.0) - StpMF2_(1.0); StpPSinMF2(p); }
#endif
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_16BIT)
    void StpPSinH2(inout StpH2 p) { p = p * abs(p) - p; }
    void StpPSinCosH(inout StpH2 p) { p.y = StpFractH1(p.x + StpH1_(0.25)); p = p * StpH2_(2.0) - StpH2_(1.0); StpPSinH2(p); }
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                        DEPTH ENCODING
//------------------------------------------------------------------------------------------------------------------------------
// Using a log2() based encoding, takes {0 to inf} to {0 to 1}.
//  log2(k.x*z)*k.y
// Where
//  k.x = 1/near ............ (so that k0*z is 1 when z=near)
//  k.y = 1/log2(k.x*far) ... (so that output is {0 to 1} ranged)
//------------------------------------------------------------------------------------------------------------------------------
// And the inverse
//  exp2(x*k.x)*k.y
// Where
//  k.x = log2(far/near)
//  k.y = near
//==============================================================================================================================
#if defined(STP_GPU)
    // Build the constants, based on near and far planes.
    // The 'far' is where anything more distant clamps to 1.0.
    StpF2 StpZCon(StpF1 near, StpF1 far) {
        StpF2 k;
        k.x = StpRcpF1(near);
        k.y = StpRcpF1(log2(k.x * far));
        return k; }
//------------------------------------------------------------------------------------------------------------------------------
    // Where 'k' is generated by StpZCon().
    StpF1 StpZPack(StpF1 z, StpF2 k, StpF1 dit) {
        #if (STP_Z_DIT == 0)
            // No dither.
            return StpSatF1(log2(k.x * z) * k.y);
        #endif
        #if (STP_Z_DIT == 1)
            // Fast linearly incorrect dither for 10-bit.
            return StpSatF1(log2(k.x * z) * k.y + dit * StpF1_(1.0 / 1024.0) - StpF1_(0.5 / 1024.0));
        #endif
    }
//==============================================================================================================================
    // Build the constants, based on near and far planes.
    // The 'far' is where anything more distant clamps to 1.0.
    StpF2 StpZUnCon(StpF1 near, StpF1 far) {
        StpF2 k;
        k.x = log2(far * StpRcpF1(near));
        k.y = near;
        return k; }
//------------------------------------------------------------------------------------------------------------------------------
    // Where 'k' is generated by StpZUnCon().
    StpF1 StpZUnpack(StpF1 x, StpF2 k) { return exp2(x * k.x) * k.y; }
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                            STATIC GEOMETRY MOTION FORWARD PROJECTION
//==============================================================================================================================
// This is a separate section simply for documentation.
// This logic must be computed in 32-bit precision (in theory).
//------------------------------------------------------------------------------------------------------------------------------
// MOTION MATCH NOTES
// ==================
// - The 'position - motion' is the reprojected position.
// - Where {0 to 1} is no motion to a screen in motion.
// - Motion check works with a differential vector '((motionPrior - motionCurrent) * kC)'.
// - For static forward projection it will be '((motionPrior*0.5 - motionCurrent) * kC)'.
//    - Due to motionPrior being in {-1 to 1} NDC instead of {0 to 1} for screen.
// - Working with motion vector differences to avoid complexity with jitter.
//------------------------------------------------------------------------------------------------------------------------------
// MOTION VECTOR NOTES
// ===================
// - 'reprojection = position - motion'
// - 'reprojection + motion = position'
// - 'motion = position - reprojection'
// - So motion points forward.
//------------------------------------------------------------------------------------------------------------------------------
// FORWARD PROJECTION LOGIC
// ========================
// HAVE INPUT {0 TO 1} SCREEN POSITION
//  xy
// GET XY INTO {-1 TO 1} NDC [2 FMA, CANNOT FACTOR, NEED AT END]
//  x=x*2-1
//  y=y*2-1
// HAVE INPUT {0 TO INF} DEPTH
//  z
// GET FROM {XY NDC, DEPTH} TO 3D VIEW POSITION [4 FMA]
//  xx=x*((z*g+h)/a) ... xx=x*(z*(g/a)+(h/a)) ... xx=x*(z*k0+k1)
//  yy=y*((z*g+h)/b) ... yy=y*(z*(g/b)+(h/b)) ... yy=y*(z*k2+k3)
// TRANSFORM TO NEW VIEW
//  xxx=xx*i+yy*j+z*k+l
//  yyy=xx*m+yy*n+z*o+p
//  zzz=xx*q+yy*r+z*s+t
// PROJECTION [9 FMA]
//  xxxx=xxx*a ..... xxxx=xx*(i*a)+yy*(j*a)+z*(k*a)+(l*a) ..... xxxx=xx*k4+yy*k5+z*k6+k7
//  yyyy=yyy*b ..... yyyy=xx*(m*b)+yy*(n*b)+z*(o*b)+(p*b) ..... yyyy=xx*k8+yy*k9+z*kA+kB
//  wwww=zzz*g+h ... wwww=xx*(q*g)+yy*(r*g)+z*(s*g)+(t*g+h) ... wwww=xx*kC+yy*kD+z*kE+kF
// PERSPECTIVE DIVIDE [1 RCP]
//  xxxxx=xxxx/wwww
//  yyyyy=yyyy/wwww
// SUBTRACT TO GET 2X MOTION [2 FMA]
//  u=xxxxx-x ... u=xxxx*(1/wwww)-x
//  v=yyyyy-y ... v=yyyy*(1/wwww)-y
// CONSTANTS (SEE BELOW FOR MEANING OF VARIABLES)
//  k0=g/a ... Constants {a,b,c,d,g,h} for prior projection
//  k1=h/a
//  k2=g/b
//  k3=h/b
//  k4=i*a ... Constants {a,b,c,d,g,h} for next projection
//  k5=j*a
//  k6=k*a
//  k7=l*a
//  k8=m*b
//  k9=n*b
//  kA=o*b
//  kB=p*b
//  kC=q*g
//  kD=r*g
//  kE=s*g
//  kF=t*g+h
//------------------------------------------------------------------------------------------------------------------------------
// BACKWARD PROJECTION LOGIC
// =========================
//  This starts from '3D VIEW POSITION' of 'FORWARD PROJECTION LOGIC', but with different constants.
// TRANSFORM TO NEW VIEW
//  xxx=xx*i+yy*j+z*k+l
//  yyy=xx*m+yy*n+z*o+p
//  zzz=xx*q+yy*r+z*s+t
// PROJECTION [9 FMA]
//  xxxx=xxx*a ..... xxxx=xx*(i*a)+yy*(j*a)+z*(k*a)+(l*a) ..... xxxx=xx*kG+yy*kH+z*kI+kJ
//  yyyy=yyy*b ..... yyyy=xx*(m*b)+yy*(n*b)+z*(o*b)+(p*b) ..... yyyy=xx*kK+yy*kL+z*kM+kN
//  wwww=zzz*g+h ... wwww=xx*(q*g)+yy*(r*g)+z*(s*g)+(t*g+h) ... wwww=xx*kO+yy*kP+z*kQ+kR
// PERSPECTIVE DIVIDE [1 RCP]
//  xxxxx=xxxx/wwww
//  yyyyy=yyyy/wwww
// SUBTRACT TO GET 2X MOTION [2 FMA]
//  u=xxxxx-x ... u=xxxx*(1/wwww)-x
//  v=yyyyy-y ... v=yyyy*(1/wwww)-y
// CONSTANTS (SEE BELOW FOR MEANING OF VARIABLES)
//  kG=i*a ... Constants {a,b,c,d,g,h} for previous prior projection, and {i,j,k,l,m,n,o,p,q,r,s,t} for prior back projection
//  kH=j*a
//  kI=k*a
//  kJ=l*a
//  kK=m*b
//  kL=n*b
//  kM=o*b
//  kN=p*b
//  kO=q*g
//  kP=r*g
//  kQ=s*g
//  kR=t*g+h
//==============================================================================================================================
// GET FROM {0 TO 1} TO {-1 TO 1}
// ==============================
// - Get to NDC for {x,y}
//   X:=x*2-1
//   Y:=y*2-1
//------------------------------------------------------------------------------------------------------------------------------
// FORWARD VIEW
// ============
// - Using 12 values
//    X:=x*i+y*j+z*k+l
//    Y:=x*m+y*n+z*o+p
//    Z:=x*q+y*r+z*s+t
//    W:=1
//     i j k l
//     m n o p
//     q r s t
//     0 0 0 1
//------------------------------------------------------------------------------------------------------------------------------
// PROJECTIONS
// ===========
// - INPUTS
//    n ... near plane z
//    f ... far plane z
// - DX ORTHO PROJECTION
//    c:=1/(f-n)
//    d:=-n/(f-n)
//    X:=x*a
//    Y:=y*b
//    Z:=z*c+d ... (w=1 on input)
//    W:=1
//     a 0 0 0
//     0 b 0 0
//     0 0 c d
//     0 0 0 1
// - DX PERSPECTIVE PROJECTION (LEFT HANDED)
//    c:=f/(f-n)
//    d:=-(f*n)/(f-n)
//    X:=x*a
//    Y:=y*b
//    Z:=z*c+d ... (w=1 on input)
//    W:=z
//     a 0 0 0
//     0 b 0 0
//     0 0 c d
//     0 0 1 0 ... (note DX allows the 1 to be non-one)
// - DX PERSPECTIVE PROJECTION REVERSED FOR BETTER PRECISION (LEFT HANDED)
//    c:=-n/(f-n)
//    d:=(f*n)/(f-n)
//    X:=x*a
//    Y:=y*b
//    Z:=z*c+d ... (w=1 on input)
//    W:=z
//     a 0 0 0
//     0 b 0 0
//     0 0 c d
//     0 0 1 0
// - DX PERSPECTIVE PROJECTION REVERSED WITH INF FAR (LEFT HANDED)
//    X:=x*a
//    Y:=y*b
//    Z:=n ... (w=1 on input)
//    W:=z
//    a 0 0 0
//    0 b 0 0
//    0 0 0 n
//    0 0 1 0
// - GL PERSPECTIVE PROJECTION
//    c:=-(f+n)/(f-n)
//    d:=-(2fn)/(f-n)
//    X:=x*a
//    Y:=y*b
//    Z:=z*c+d ... (w=1 on input)
//    W:=z
//     a 0  0 0
//     0 b  0 0
//     0 0  c d
//     0 0 -1 0
// - GENERALIZED (WILL DO ANYTHING)
//    X:=x*a
//    Y:=y*b
//    Z:=z*c+d ... (w=1 on input)
//    W:=z*g+h
//     a 0 0 0
//     0 b 0 0
//     0 0 c d
//     0 0 g h
//------------------------------------------------------------------------------------------------------------------------------
// PROJECTED TO NDC
// ================
// - Ignoring viewport transform
//    X:=x/w
//    Y:=y/w
//    Z:=z/w
//    W:=1/w
// - Inverse
//    x=X*w
//    y=Y*w
//==============================================================================================================================
#if defined(STP_GPU)
    // Generates forward {-1 to 1} NDC forward projection vectors given (from prior frame),
    //  p .... {0 to 1} screen position
    //  z .... {0 to INF} depth
    //  m .... {0 to 1} prior motion vector
    // The results are approximately corrected for dynamic motion.
    // This takes 'dynamicMotion = priorMotionVector - priorStaticGeometryBackprojection'
    // Then adds that estimate of dynamic motion to the static geometry forward projection vector.
    StpF2 StpFor(StpF2 p, StpF1 z, StpF2 m, StpF1 kMotionMatch,
    StpF4 k0123, StpF4 k4567, StpF4 k89AB, StpF4 kCDEF, StpF4 kGHIJ, StpF4 kKLMN, StpF4 kOPQR){
        // Implements the logic described above in the comments.
        p = p * StpF2_(2.0) - StpF2_(1.0);
        StpF2 q;
        q.x = p.x * (z * k0123.x + k0123.y);
        q.y = p.y * (z * k0123.z + k0123.w);
        StpF3 v;
        v.x = q.x * k4567.x + q.y * k4567.y + z * k4567.z + k4567.w;
        v.y = q.x * k89AB.x + q.y * k89AB.y + z * k89AB.z + k89AB.w;
        v.z = q.x * kCDEF.x + q.y * kCDEF.y + z * kCDEF.z + kCDEF.w;
        v.z = StpRcpF1(v.z);
        StpF3 v2;
        v2.x = q.x * kGHIJ.x + q.y * kGHIJ.y + z * kGHIJ.z + kGHIJ.w;
        v2.y = q.x * kKLMN.x + q.y * kKLMN.y + z * kKLMN.z + kKLMN.w;
        v2.z = q.x * kOPQR.x + q.y * kOPQR.y + z * kOPQR.z + kOPQR.w;
        v2.z = StpRcpF1(v2.z);
        // Motion vector points forward (to estimated position in next frame).
        // Negative motion vector points back to where the pixel was in the prior frame.
        // Motion vector is {0 to 1} for one screen, but this logic is {-1 to 1} based (hence a 2x scaling).
        //      __STATIC_FORWARD________   __DYNAMIC_ESTIMATE_____________________________________________________
        return (v.xy * StpF2_(v.z) - p) + ((StpF2_(2.0) * m) - (p - v2.xy * StpF2_(v2.z))) * StpF2_(kMotionMatch); }
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                    MOTION VECTOR ENCODING
//------------------------------------------------------------------------------------------------------------------------------
// {MSB 10-bit depth, LSB {11,11}-bit motion with sqrt() encoding}
// Motion is encoding in sqrt() space.
//------------------------------------------------------------------------------------------------------------------------------
// 11111111111111110000000000000000
// fedcba9876543210fedcba9876543210
// ================================
// zzzzzzzzzz...................... 10-bit encoded z
// ..........yyyyyyyyyyy........... 11-bit {-1 to <1} y encoded in gamma 2.0 (sqrt)
// .....................xxxxxxxxxxx 11-bit {-1 to <1} x encoded in gamma 2.0 (sqrt)
//------------------------------------------------------------------------------------------------------------------------------
// The 32-bit path is 8 ops to decode {x,y}.
//------------------------------------------------------------------------------------------------------------------------------
// There once was a 16-bit path which takes 6 ops to decode (bit extra because ABS isn't free).
//     hhhhhhhhhhhhhhhhllllllllllllllll
//     ================================
//     zzzzzzzzzzyyyyyyyyyyyxxxxxxxxxxx  input
//     zzzzzyyyyyyyyyyyxxxxxxxxxxx00000  << 5
//     00000yyyyyyyyyyyxxxxxxxxxxx00000  & 0x7FFFFFF
//     00000yyyyyyyyyyy00000xxxxxxxxxxx  >> 5 (for 16-bit LSB only)
// This gets 11-bit integers which perfectly alias lowest non-denormal and denormals of FP16.
// Can scale by '16384' and subtract 1 to decompress without a CVT.
//==============================================================================================================================
#if defined(STP_GPU)
    // The 'z' comes in {0 to 1}.
    // This depends on 'v' ranging inside and including {-1 to 1}.
    StpU1 StpMvPack(StpF1 z, StpF2 v, StpF1 dit) {
        // {-1 to 1} linear to gamma 2.0 {-1 to 1}
        #if STP_M_DIT
           v = StpCpySgnF2(StpSatF2(sqrt(abs(v)) + StpF2_(dit * StpF1_(1.0 / 1024.0) - StpF1_(0.5 / 1024.0))), v);
        #else
           v = StpCpySgnF2(sqrt(abs(v)), v);
        #endif
        // Limit to {-1024/1024 to 1023/1024}.
        v = min(v, StpF2_(1023.0/1024.0));
        // Encode to 11-bit with zero at center of one step.
        v = v * StpF2_(1024.0) + StpF2_(1024.0);
        // Pack.
        return (StpU1(z * StpF1(1023.0)) << StpU1(22)) + (StpU1(v.y) << StpU1(11)) + StpU1(v.x); }
//------------------------------------------------------------------------------------------------------------------------------
    // Unpacks velocity, and also computes an error estimate in 'e'.
    void StpMvUnpackE(out StpF1 z, out StpF2 v, out StpF2 e, StpU1 i) {
        StpU1 iz = StpBfeU1(i, 22u, 10u);
        StpU1 iy = StpBfeU1(i, 11u, 11u);
        StpU1 ix = StpBfeU1(i, 0, 11u);
        z = StpF1(iz) * StpF1_(1.0 / 1023.0);
        v.y = StpF1(iy) * StpF1_(1.0 / 1024.0) + StpF1_(-1.0);
        v.x = StpF1(ix) * StpF1_(1.0 / 1024.0) + StpF1_(-1.0);
        // Conservative error, is difference between next step and self.
        e = abs(v) + StpF2_(1.0 / 1024.0);
        e *= e;
        v *= abs(v);
        e = e - abs(v); }
//------------------------------------------------------------------------------------------------------------------------------
    void StpMvUnpackVF(out StpF2 v, StpU1 i) {
        StpU1 iy = StpBfeU1(i, 11u, 11u);
        StpU1 ix = StpBfeU1(i, 0, 11u);
        v.y = StpF1(iy) * StpF1_(1.0 / 1024.0) + StpF1_(-1.0);
        v.x = StpF1(ix) * StpF1_(1.0 / 1024.0) + StpF1_(-1.0);
        v *= abs(v); }
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                       COLOR CONVERSION
//==============================================================================================================================
#if defined(STP_GPU)
    // Scaling in the reversible tonemapper (should be >= 1).
    // Getting too close to 1.0 will result in luma inversions in highly saturated content.
    // Using 4.0 or ideally 8.0 is recommended.
    #define STP_SAT 8.0
#endif
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_32BIT)
    void StpToneF1(inout StpF1 x) { StpF1 y = StpRcpF1(StpF1_(STP_SAT) + x); x = StpSatF1(x * StpF1_(y)); }
//------------------------------------------------------------------------------------------------------------------------------
    // Reversible tonemapper.
    void StpToneF3(inout StpF3 x) {
        StpF1 y = StpRcpF1(StpF1_(STP_SAT) + StpMax3F1(x.r, x.g, x.b));
        x = StpSatF3(x * StpF3_(y)); }
//------------------------------------------------------------------------------------------------------------------------------
    void StpToneInvF3(inout StpF3 x) {
        StpF1 y = StpRcpF1(
            //               |-----| <- Using 32768.0 causes problems in Unity with bloom on at least some platforms.
            //               |     |    So output maximum is 16384 for StpToneInvF3().
            max(StpF1_(1.0 / 16384.0), StpSatF1(StpF1_(1.0 / STP_SAT) - StpMax3F1(x.r, x.g, x.b) * StpF1_(1.0 / STP_SAT))));
        x *= StpF3_(y); }
//------------------------------------------------------------------------------------------------------------------------------
    // Convert LDR RGB to Gamma 2.0 RGB {0 to 1}.
    // This is for storage to 8-bit.
    // This is temporal dithered.
    // Unoptimized logic (for reference).
    //     StpF3 n = sqrt(c);
    //     n = floor(n * StpF3_(255.0)) * StpF3_(1.0 / 255.0);
    //     StpF3 a = n * n;
    //     StpF3 b = n + StpF3_(1.0 / 255.0); b = b * b;
    //     // Ratio of 'a' to 'b' required to produce 'c'.
    //     StpF3 r = (c - b) * StpRcpF3(a - b);
    //     // Use the ratio as a cutoff to choose 'a' or 'b'.
    //     c = StpSatF3(n + StpGtZeroF3(StpF3_(dit) - r) * StpF3_(1.0 / 255.0));
    // Optimized from 57 to 42 clks on GCN.
    StpF3 StpRgbGamDit8F3(StpF3 c, StpF1 dit) {
        StpF3 n = sqrt(c);
        n = floor(n * StpF3_(255.0)) * StpF3_(1.0 / 255.0);
        StpF3 a = n * n;
        StpF3 b = n + StpF3_(1.0 / 255.0);
        c = StpSatF3(n + StpGtZeroF3(StpF3_(dit) * (b * b - a) - (b * b - c)) * StpF3_(1.0 / 255.0)); return c; }
//------------------------------------------------------------------------------------------------------------------------------
    // Version for 10-bit for feedback.
    StpF3 StpRgbGamDit10F3(StpF3 c, StpF1 dit) {
        StpF3 n = sqrt(c);
        n = floor(n * StpF3_(1023.0)) * StpF3_(1.0 / 1023.0);
        StpF3 a = n * n;
        StpF3 b = n + StpF3_(1.0 / 1023.0);
        c = StpSatF3(n + StpGtZeroF3(StpF3_(dit) * (b * b - a) - (b * b - c)) * StpF3_(1.0 / 1023.0)); return c; }
//------------------------------------------------------------------------------------------------------------------------------
    // In cases where TAA outputs without extra processing, only store out feedback.
    // Then in the next pass, use this function to convert feedback back to color.
    void StpFeed2ClrF(inout StpF3 c) {
        c *= c;
        #if (STP_POSTMAP == 0)
            StpToneInvF3(c.rgb);
        #endif
    }
#endif
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_32BIT)
    void StpToneMF1(inout StpMF1 x) { StpMF1 y = StpRcpMF1(StpMF1_(STP_SAT) + x); x = StpSatMF1(x * StpMF1_(y)); }
//------------------------------------------------------------------------------------------------------------------------------
    void StpToneMF3(inout StpMF3 x) {
        StpMF1 y = StpRcpMF1(StpMF1_(STP_SAT) + StpMax3MF1(x.r, x.g, x.b));
        x = StpSatMF3(x * StpMF3_(y)); }
//------------------------------------------------------------------------------------------------------------------------------
    void StpToneInvMF3(inout StpMF3 x) {
        StpMF1 y = StpRcpMF1(
            max(StpMF1_(1.0 / 16384.0), StpSatMF1(StpMF1_(1.0 / STP_SAT) -
                StpMax3MF1(x.r, x.g, x.b) * StpMF1_(1.0 / STP_SAT))));
        x *= StpMF3_(y); }
//------------------------------------------------------------------------------------------------------------------------------
    StpMF3 StpRgbGamDit8MF3(StpMF3 c, StpMF1 dit) {
        StpMF3 n = sqrt(c);
        n = floor(n * StpMF3_(255.0)) * StpMF3_(1.0 / 255.0);
        StpMF3 a = n * n;
        StpMF3 b = n + StpMF3_(1.0 / 255.0);
        c = StpSatMF3(n + StpGtZeroMF3(StpMF3_(dit) * (b * b - a) - (b * b - c)) * StpMF3_(1.0 / 255.0)); return c; }
//------------------------------------------------------------------------------------------------------------------------------
    StpMF3 StpRgbGamDit10MF3(StpMF3 c, StpMF1 dit) {
        StpMF3 n = sqrt(c);
        n = floor(n * StpMF3_(1023.0)) * StpMF3_(1.0 / 1023.0);
        StpMF3 a = n * n;
        StpMF3 b = n + StpMF3_(1.0 / 1023.0);
        c = StpSatMF3(n + StpGtZeroMF3(StpMF3_(dit) * (b * b - a) - (b * b - c)) * StpMF3_(1.0 / 1023.0)); return c; }
//------------------------------------------------------------------------------------------------------------------------------
    void StpFeed2ClrMF(inout StpMF3 c) {
        c *= c;
        #if (STP_POSTMAP == 0)
            StpToneInvMF3(c.rgb);
        #endif
    }
#endif
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_16BIT)
    void StpToneH1(inout StpH1 x) { StpH1 y = StpRcpH1(StpH1_(STP_SAT) + x); x = StpSatH1(x * StpH1_(y)); }
//------------------------------------------------------------------------------------------------------------------------------
    void StpToneH3(inout StpH3 x) {
        StpH1 y = StpRcpH1(StpH1_(STP_SAT) + StpMax3H1(x.r, x.g, x.b));
        x = StpSatH3(x * StpH3_(y)); }
//------------------------------------------------------------------------------------------------------------------------------
    void StpToneInvH3(inout StpH3 x) {
        StpH1 y = StpRcpH1(
            max(StpH1_(1.0 / 16384.0), StpSatH1(StpH1_(1.0 / STP_SAT) - StpMax3H1(x.r, x.g, x.b) * StpH1_(1.0 / STP_SAT))));
        x *= StpH3_(y); }
//------------------------------------------------------------------------------------------------------------------------------
    StpH3 StpRgbGamDit8H3(StpH3 c, StpH1 dit) {
        StpH3 n = sqrt(c);
        n = floor(n * StpH3_(255.0)) * StpH3_(1.0 / 255.0);
        StpH3 a = n * n;
        StpH3 b = n + StpH3_(1.0 / 255.0);
        c = StpSatH3(n + StpGtZeroH3(StpH3_(dit) * (b * b - a) - (b * b - c)) * StpH3_(1.0 / 255.0)); return c; }
//------------------------------------------------------------------------------------------------------------------------------
    StpH3 StpRgbGamDit10H3(StpH3 c, StpH1 dit) {
        StpH3 n = sqrt(c);
        n = floor(n * StpH3_(1023.0)) * StpH3_(1.0 / 1023.0);
        StpH3 a = n * n;
        StpH3 b = n + StpH3_(1.0 / 1023.0);
        c = StpSatH3(n + StpGtZeroH3(StpH3_(dit) * (b * b - a) - (b * b - c)) * StpH3_(1.0 / 1023.0)); return c; }
//------------------------------------------------------------------------------------------------------------------------------
    void StpFeed2ClrH(inout StpH3 c) {
        c *= c;
        #if (STP_POSTMAP == 0)
            StpToneInvH3(c.rgb);
        #endif
    }
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                   COLOR CONVERSION TOOLS
//------------------------------------------------------------------------------------------------------------------------------
// Some platforms do not have a hardware sRGB image store (requires manual conversion).
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_32BIT)
    StpF3 StpLinearToSrgbF3(StpF3 c) {
        StpF3 j = StpF3(0.0031308 * 12.92, 12.92, 1.0 / 2.4); StpF2 k = StpF2(1.055, -0.055);
        return clamp(j.xxx, c * j.yyy, pow(c, j.zzz) * k.xxx + k.yyy); }
//------------------------------------------------------------------------------------------------------------------------------
    StpMF3 StpLinearToSrgbMF3(StpMF3 c) {
        StpMF3 j = StpMF3(0.0031308 * 12.92, 12.92, 1.0 / 2.4); StpMF2 k = StpMF2(1.055, -0.055);
        return clamp(j.xxx, c * j.yyy, pow(c, j.zzz) * k.xxx + k.yyy); }
#endif
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_16BIT)
    StpH3 StpLinearToSrgbH3(StpH3 c) {
        StpH3 j = StpH3(0.0031308 * 12.92, 12.92, 1.0 / 2.4); StpH2 k = StpH2(1.055, -0.055);
        return clamp(j.xxx, c * j.yyy, pow(c, j.zzz) * k.xxx + k.yyy); }
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                     CONSTANT GENERATION
//==============================================================================================================================
STP_STATIC void StpUbeCon(
// Generated constants.
StpInOutU4 con0,
// Current image resolution in pixels.
StpInF2 imgC,
// Feedback (aka output) resolution in pixels.
StpInF2 imgF) {
//------------------------------------------------------------------------------------------------------------------------------
    // StpF1 kCRcpFY;
    con0[0] = StpU1_F1(imgC[1] / imgF[1]);
    // StpU1 kCX;
    con0[1] = StpU1_(imgC[0]);
    // StpU1 spacer.
    con0[2] = StpU1_(STP_SPACER);
    // StpU1 kCY;
    con0[3] = StpU1_(imgF[1]); }
//==============================================================================================================================
STP_STATIC void StpClnCon(
// Generated constants.
StpInOutU4 con0,
StpInOutU4 con1,
// Amount of sharpening {0 = maximum, >0 is amount of stops less of sharpening}.
StpF1 sharp,
// Amount of grain {0 = maximum, >0 is amount of stops less of grain}.
StpF1 grain) {
//------------------------------------------------------------------------------------------------------------------------------
    // Baseline sharpening set to avoid something too unnatural since pre-cleaner STP is already sharp.
    sharp += StpF1_(0.33333);
//------------------------------------------------------------------------------------------------------------------------------
    StpVarF2 kSharp;
    kSharp[0] = StpExp2F1(-sharp);
    kSharp[1] = StpF1_(0.0);
    StpVarF2 kGrain;
    kGrain[0] = StpExp2F1(-grain);
    kGrain[1] = StpF1_(-0.5) * kGrain[0];
//------------------------------------------------------------------------------------------------------------------------------
    con0[0] = StpU1_F1(kGrain[0]);
    con0[1] = StpU1_F1(kGrain[1]);
    con0[2] = StpU1_F1(kSharp[0]);
    con0[3] = StpU1_(0);
//------------------------------------------------------------------------------------------------------------------------------
    con1[0] = StpU1_H2_F2(kGrain);
    con1[1] = StpU1_H2_F2(kSharp);
    con1[2] = StpU1_(0);
    con1[3] = StpU1_(0); }
//==============================================================================================================================
STP_STATIC void StpInCon(
// Generated constants.
StpInOutU4 con0,
StpInOutU4 con1,
StpInOutU4 con2,
StpInOutU4 con3,
StpInOutU4 con4,
StpInOutU4 con5,
StpInOutU4 con6,
StpInOutU4 con7,
StpInOutU4 con8,
StpInOutU4 con9,
StpInOutU4 conA,
StpInOutU4 conB,
StpInOutU4 conC,
StpInOutU4 conD,
// Linear depth near plane for log2 depth encoding.
StpF1 near,
// Linear depth far plane for log2 depth encoding.
StpF1 far,
// Frame count for current frame (sets jitter).
StpU1 frame,
// Current image resolution in pixels.
StpInF2 imgC,
// Prior image resolution in pixels.
StpInF2 imgP,
// Feedback (aka output) resolution in pixels.
StpInF2 imgF,
// Ratio of 'currentFrameTime/priorFrameTime'.
StpF1 motionMatch,
// Projection matrix data {a,b,c,d,g,h}.
// This is used to do static geometry forward projection.
//  a 0 0 0
//  0 b 0 0
//  0 0 c d
//  0 0 g h
// For reference, an DX ortho projection would be,
//  a 0 0 0
//  0 b 0 0
//  0 0 c d
//  0 0 0 1
// And a DX, left handed perspective projection would be,
//  a 0 0 0
//  0 b 0 0
//  0 0 c d ... c := f/(f-n), d := -(f*n)/(f-n)
//  0 0 1 0
// Previous prior projection.
StpInF2 prjPrvAB,
StpInF4 prjPrvCDGH,
// Prior projection.
StpInF2 prjPriAB,
StpInF4 prjPriCDGH,
// Current projection (the difference enables changing zoom).
StpInF2 prjCurAB,
StpInF4 prjCurCDGH,
// Forward viewspace transform.
// Transform prior 3D view position into current 3D view position.
// This is used to do static geometry forward projection.
//  X := x*i + y*j +z*k +l
//  Y := x*m + y*n +z*o +p
//  Z := x*q + y*r +z*s +t
//  W := 1
//   i j k l
//   m n o p
//   q r s t
//   0 0 0 1
StpInF4 forIJKL,
StpInF4 forMNOP,
StpInF4 forQRST,
// Prior frame backward viewspace transform.
// Transform prior 3D view position into previous-prior 3D view position.
// This is used to 'fix' static geometry forward projection for dynamic motion.
//  X := x*i + y*j +z*k +l
//  Y := x*m + y*n +z*o +p
//  Z := x*q + y*r +z*s +t
//  W := 1
//   i j k l
//   m n o p
//   q r s t
//   0 0 0 1
StpInF4 bckIJKL,
StpInF4 bckMNOP,
StpInF4 bckQRST) {
//------------------------------------------------------------------------------------------------------------------------------
    // StpF2 kRcpC := 1.0 / size of current input image in pixels.
    con0[0] = StpU1_F1(StpF1_(1.0) / imgC[0]);
    con0[1] = StpU1_F1(StpF1_(1.0) / imgC[1]);
    // StpF2 kHalfRcpC := 0.5 / size of current input image in pixels.
    con0[2] = StpU1_F1(StpF1_(0.5) / imgC[0]);
    con0[3] = StpU1_F1(StpF1_(0.5) / imgC[1]);
//------------------------------------------------------------------------------------------------------------------------------
    // StpF2 kC := Size of current input image in pixels.
    con1[0] = StpU1_F1(imgC[0]);
    con1[1] = StpU1_F1(imgC[1]);
//------------------------------------------------------------------------------------------------------------------------------
    // Grab jitter for current and prior frames.
    StpVarF2 jitP;
    StpVarF2 jitC;
    StpJit16(jitP, frame - StpU1_(1));
    StpJit16(jitC, frame);
    // StpF2 kJitCRcpCUnjitPRcpP := Map current into prior frame.
    con1[2] = StpU1_F1(jitC[0] / imgC[0] - jitP[0] / imgP[0]);
    con1[3] = StpU1_F1(jitC[1] / imgC[1] - jitP[1] / imgP[1]);
//------------------------------------------------------------------------------------------------------------------------------
    // StpF2 kHalfRcpP := Half size of a pixel in the prior frame.
    con2[0] = StpU1_F1(StpF1_(0.5) / imgP[0]);
    con2[1] = StpU1_F1(StpF1_(0.5) / imgP[1]);
//------------------------------------------------------------------------------------------------------------------------------
    // StpF2 kDepth := Copied logic from StpZCon().
    StpF1 k0 = StpRcpF1(near);
    StpF1 k1 = StpRcpF1(StpLog2F1(k0 * far));
    con2[2] = StpU1_F1(k0);
    con2[3] = StpU1_F1(k1);
//------------------------------------------------------------------------------------------------------------------------------
    // StpF2 kJitCRcpC := Take {0 to 1} position in current image, and map back to {0 to 1} position in feedback (removes jitter).
    con3[0] = StpU1_F1(jitC[0] / imgC[0]);
    con3[1] = StpU1_F1(jitC[1] / imgC[1]);
//------------------------------------------------------------------------------------------------------------------------------
    // StpF2 kF := size of feedback (aka output) in pixels.
    con3[2] = StpU1_F1(imgF[0]);
    con3[3] = StpU1_F1(imgF[1]);
//------------------------------------------------------------------------------------------------------------------------------
    // StpF4 kOS := Scale and bias to check for out of bounds (and kill feedback).
    // Scaled and biased output needs to {-1 out of bounds, >-1 in bounds, <1 in bounds, 1 out of bounds}.
    StpVarF2 s;
    // Undo 'pM' scaling, and multiply by 2 (as this needs to be -1 to 1 at edge of acceptable reprojection).
    s[0] = StpF1_(2.0);
    s[1] = StpF1_(2.0);
    // Scaling to push outside safe reprojection over 1.
    s[0] *= imgP[0] / (imgP[0] + StpF1_(4.0));
    s[1] *= imgP[1] / (imgP[1] + StpF1_(4.0));
    con4[0] = StpU1_F1(s[0]);
    con4[1] = StpU1_F1(s[1]);
    // Factor out subtracting off the mid point scaled by the multiply term.
    con4[2] = StpU1_F1(StpF1_(-0.5) * s[0]);
    con4[3] = StpU1_F1(StpF1_(-0.5) * s[1]);
//------------------------------------------------------------------------------------------------------------------------------
    // kSharp
    //  .x = mul term
    //  .y = add term
    // Add term amounts, (input/output for x)
    //  none ...... 1         -> 0.0
    //  2x area ... sqrt(1/2) -> 0.59
    //  4x area ... 1/2       -> 1.0
    StpVarF2 kSharp;
    kSharp[1] = StpSatF1(StpF1_(2.0) - StpF1_(2.0) * (imgC[0] / imgF[0]));
    kSharp[0] = StpF1_(1.0) - kSharp[1];
    con5[0] = StpU1_F1(kSharp[0]);
    con5[1] = StpU1_F1(kSharp[1]);
    con5[2] = StpU1_H2_F2(kSharp);
    // kMotionMatch
    con5[3] = StpU1_F1(motionMatch);
//------------------------------------------------------------------------------------------------------------------------------
    // StpF2 kUnDepth := Copied logic from StpZUnCon().
    con6[0] = StpU1_F1(StpLog2F1(far * StpRcpF1(near)));
    con6[1] = StpU1_F1(near);
    // Unused for now.
    con6[2] = StpU1_(0);
    con6[3] = StpU1_(0);
//------------------------------------------------------------------------------------------------------------------------------
    // See header docs in "STATIC GEOMETRY MOTION FORWARD PROJECTION".
    // k0123
    con7[0] = StpU1_F1(prjPriCDGH.z / prjPriAB.x);
    con7[1] = StpU1_F1(prjPriCDGH.w / prjPriAB.x);
    con7[2] = StpU1_F1(prjPriCDGH.z / prjPriAB.y);
    con7[3] = StpU1_F1(prjPriCDGH.w / prjPriAB.y);
    // k4567
    con8[0] = StpU1_F1(forIJKL.x * prjCurAB.x);
    con8[1] = StpU1_F1(forIJKL.y * prjCurAB.x);
    con8[2] = StpU1_F1(forIJKL.z * prjCurAB.x);
    con8[3] = StpU1_F1(forIJKL.w * prjCurAB.x);
    // k89AB
    con9[0] = StpU1_F1(forMNOP.x * prjCurAB.y);
    con9[1] = StpU1_F1(forMNOP.y * prjCurAB.y);
    con9[2] = StpU1_F1(forMNOP.z * prjCurAB.y);
    con9[3] = StpU1_F1(forMNOP.w * prjCurAB.y);
    // kCDEF
    conA[0] = StpU1_F1(forQRST.x * prjCurCDGH.z);
    conA[1] = StpU1_F1(forQRST.y * prjCurCDGH.z);
    conA[2] = StpU1_F1(forQRST.z * prjCurCDGH.z);
    conA[3] = StpU1_F1(forQRST.w * prjCurCDGH.z + prjCurCDGH.w);
    // kGHIJ
    conB[0] = StpU1_F1(bckIJKL.x * prjPrvAB.x);
    conB[1] = StpU1_F1(bckIJKL.y * prjPrvAB.x);
    conB[2] = StpU1_F1(bckIJKL.z * prjPrvAB.x);
    conB[3] = StpU1_F1(bckIJKL.w * prjPrvAB.x);
    // kKLMN
    conC[0] = StpU1_F1(bckMNOP.x * prjPrvAB.y);
    conC[1] = StpU1_F1(bckMNOP.y * prjPrvAB.y);
    conC[2] = StpU1_F1(bckMNOP.z * prjPrvAB.y);
    conC[3] = StpU1_F1(bckMNOP.w * prjPrvAB.y);
    // kOPQR
    conD[0] = StpU1_F1(bckQRST.x * prjPrvCDGH.z);
    conD[1] = StpU1_F1(bckQRST.y * prjPrvCDGH.z);
    conD[2] = StpU1_F1(bckQRST.z * prjPrvCDGH.z);
    conD[3] = StpU1_F1(bckQRST.w * prjPrvCDGH.z + prjPrvCDGH.w); }
//==============================================================================================================================
STP_STATIC void StpTaaCon(
// Generated constants.
StpInOutU4 con0,
StpInOutU4 con1,
StpInOutU4 con2,
StpInOutU4 con3,
StpInOutU4 con4,
// Amount of grain {0 = maximum, >0 is amount of stops less of grain}.
StpF1 grain,
// Frame count for current frame (sets jitter).
StpU1 frame,
// Current image resolution in pixels.
StpInF2 imgC,
// Feedback (aka output) resolution in pixels.
StpInF2 imgF) {
//------------------------------------------------------------------------------------------------------------------------------
    // Grab jitter for current frame.
    StpVarF2 jitC;
    StpJit16(jitC, frame);
//------------------------------------------------------------------------------------------------------------------------------
    // Conversion from integer pix position to center pix float pixel position in image for current input.
    //  xy := multiply term (M) --- Scale by 1/imgF to get to {0 to 1}.
    //  zw := addition term (A) --- Add 0.5*M to get to center of pixel, then subtract jitC to undo jitter.
    // StpF2 kCRcpF.
    con0[0] = StpU1_F1(imgC[0] / imgF[0]);
    con0[1] = StpU1_F1(imgC[1] / imgF[1]);
    // StpF2 kHalfCRcpFUnjitC.
    con0[2] = StpU1_F1(StpF1_(0.5) * imgC[0] / imgF[0] - jitC[0]);
    con0[3] = StpU1_F1(StpF1_(0.5) * imgC[1] / imgF[1] - jitC[1]);
//------------------------------------------------------------------------------------------------------------------------------
    // StpF2 kRcpC := 1/size of current input image in pixels.
    con1[0] = StpU1_F1(StpF1_(1.0) / imgC[0]);
    con1[1] = StpU1_F1(StpF1_(1.0) / imgC[1]);
//------------------------------------------------------------------------------------------------------------------------------
    // StpF1 kDubRcpCX := 2 times kRcpC.x.
    con1[2] = StpU1_F1(StpF1_(2.0) / imgC[0]);
//------------------------------------------------------------------------------------------------------------------------------
    // StpH2 kKRcpF := STP_KERNEL/size of current output image in pixels.
    // Kernel is adaptive based on the amount of scaling ('c/f' term).
    StpVarF2 kKRcpF;
    kKRcpF[0] = StpF1_(STP_KERNEL) * imgC[0] / (imgF[0] * imgF[0]);
    kKRcpF[1] = StpF1_(STP_KERNEL) * imgC[1] / (imgF[1] * imgF[1]);
    con1[3] = StpU1_H2_F2(kKRcpF);
//------------------------------------------------------------------------------------------------------------------------------
    // StpF2 kRcpF := 1/size of feedback image (aka output) in pixels.
    con2[0] = StpU1_F1(StpF1_(1.0) / imgF[0]);
    con2[1] = StpU1_F1(StpF1_(1.0) / imgF[1]);
//------------------------------------------------------------------------------------------------------------------------------
    // StpF2 kHalfRcpF := 0.5/size of feedback image (aka output) in pixels.
    con2[2] = StpU1_F1(StpF1_(0.5) / imgF[0]);
    con2[3] = StpU1_F1(StpF1_(0.5) / imgF[1]);
//------------------------------------------------------------------------------------------------------------------------------
    StpVarF2 kGrain;
    kGrain[0] = StpExp2F1(-grain);
    kGrain[1] = StpF1_(-0.5) * kGrain[0];
    con3[0] = StpU1_F1(kKRcpF[0]);
    con3[1] = StpU1_F1(kKRcpF[1]);
    con3[2] = StpU1_H2_F2(kGrain);
    con3[3] = StpU1_(0);
//------------------------------------------------------------------------------------------------------------------------------
    con4[0] = StpU1_F1(kGrain[0]);
    con4[1] = StpU1_F1(kGrain[1]);
    // StpF2 kF := size of feedback image in pixels.
    con4[2] = StpU1_F1(imgF[0]);
    con4[3] = StpU1_F1(imgF[1]); }
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//
//                                                      INLINE ENTRY POINT
//
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_32BIT) && defined(STP_IN)
    // Callback prototypes.
    // All the data used for input.
    void StpInDatF(
    inout StpF1 r,  // Responsive input pixel (this kills input and output feedback) {0.0 := responsive, 1.0 := normal}.
    inout StpMF3 c, // Input color, this is linear HDR or post-tonemap-linear depending on STP_POSTMAP.
    inout StpF1 z,  // Input depth, this is linear {0:near to INF:far} ranged.
    inout StpF2 m,  // Input motion, 'position - motion' is the reprojected position, where {0 to 1} is range of the screen.
    StpU2 o); // For coordinate o.
//------------------------------------------------------------------------------------------------------------------------------
    // Dither value {0 to 1} this should be output pixel frequency spatial temporal blue noise.
    StpMF1 StpInDitF(StpU2 o);
//------------------------------------------------------------------------------------------------------------------------------
    // Gather4 on prior luma.
    StpMF4 StpInPriLumF(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    // Prior frame motion {z,x,y} packed input.
    #if STP_MAX_MIN
        // Sample via minimum.
        StpU1 StpInPriMotMinF(StpF2 p);
    #else
        // Fallback to gather4 on red channel.
        StpU4 StpInPriMot4F(StpF2 p);
    #endif
//------------------------------------------------------------------------------------------------------------------------------
    // Feedback {color,convergence}.
    // Bilinear fetch with clamp to edge.
    StpMF4 StpInPriFedF(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    #if STP_MAX_MIN
        // Minimum alpha for feedback.
        // This is only used if STP_SDS is enabled.
        StpMF1 StpInPriFedMinAF(StpF2 p);
    #endif
//==============================================================================================================================
    // Last pass that stores color, inline logic to setup for STP.
    void StpInF(
    out StpMF4 oC, // Output color (to be stored).
    out StpU1 oM,  // Output motion (to be stored).
    out StpMF1 oL, // Output luma (to be stored).
    StpU2 pp,   // Input position {0 to size-1} across the input frame.
    StpU4 con0, // Constants generated by StpInCon().
    StpU4 con1,
    StpU4 con2,
    StpU4 con3,
    StpU4 con4,
    StpU4 con5,
    StpU4 con6,
    StpU4 con7,
    StpU4 con8,
    StpU4 con9,
    StpU4 conA,
    StpU4 conB,
    StpU4 conC,
    StpU4 conD) {
//------------------------------------------------------------------------------------------------------------------------------
        // Grab input parameters.
        StpF1 r;
        StpMF3 c;
        StpF1 z;
        StpF2 m;
        StpInDatF(r,c,z,m,pp);
//------------------------------------------------------------------------------------------------------------------------------
        // Used for debug only, will get dead code removal otherwise.
        StpMF4 bug = StpMF4_(0.0);
//------------------------------------------------------------------------------------------------------------------------------
        #if defined(STP_HLSL)
            // Avoid compiler warning as error.
            oL = StpMF1_(0.0);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // Rename constants.
        StpF2 kRcpC = StpF2_U2(con0.xy);
        StpF2 kHalfRcpC = StpF2_U2(con0.zw);
        StpF2 kC = StpF2_U2(con1.xy);
        StpF2 kJitCRcpCUnjitPRcpP = StpF2_U2(con1.zw);
        StpF2 kHalfRcpP = StpF2_U2(con2.xy);
        StpF2 kDepth = StpF2_U2(con2.zw);
        StpF2 kJitCRcpC = StpF2_U2(con3.xy);
        StpF2 kF = StpF2_U2(con3.zw);
        StpF4 kOS = StpF4_U4(con4);
        StpF2 kSharp = StpF2_U2(con5.xy);
        StpF1 kMotionMatch = StpF1_U1(con5.w);
        StpF2 kUnDepth = StpF2_U2(con6.xy);
        StpF4 k0123 = StpF4_U4(con7);
        StpF4 k4567 = StpF4_U4(con8);
        StpF4 k89AB = StpF4_U4(con9);
        StpF4 kCDEF = StpF4_U4(conA);
        StpF4 kGHIJ = StpF4_U4(conB);
        StpF4 kKLMN = StpF4_U4(conC);
        StpF4 kOPQR = StpF4_U4(conD);
//------------------------------------------------------------------------------------------------------------------------------
        StpMF1 d = StpInDitF(pp);
//------------------------------------------------------------------------------------------------------------------------------
        // Compute float position {0 to 1} across screen.
        StpF2 p = StpF2(pp) * kRcpC + kHalfRcpC;
//------------------------------------------------------------------------------------------------------------------------------
        // Motion reprojected position in prior frame.
        StpF2 pM = (p - m) + kJitCRcpCUnjitPRcpP;
//------------------------------------------------------------------------------------------------------------------------------
        // Grab mixed 2x2 and 4-tap 3x3 ring neighborhood for prior reprojected nearest {z,motion}.
        // Slightly smaller than a full 3x3, appears to be safe enough at least up to 9x area with angular dilation.
        // This nearest dilates {z, motion} reprojection to avoid pulling in anti-aliased edges and leaving temporal ringing.
        StpU1 mZVPN;
        StpU4 mZVP2a = StpInPriMot4F(pM - kHalfRcpP);
        StpU4 mZVP2b = StpInPriMot4F(pM + kHalfRcpP);
        #if STP_MAX_MIN
            mZVPN = StpInPriMotMinF(pM);
        #else
            StpU4 mZVP4 = StpInPriMot4F(pM);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // Grab reprojected luma (2x2 neighborhood).
        StpMF4 rL4 = StpInPriLumF(pM);
//------------------------------------------------------------------------------------------------------------------------------
        // Fetch reprojected feedback at points matching 2x2 color input.
        StpF2 pF = (p - m) + kJitCRcpC;
        StpMF4 cF = StpInPriFedF(pF);
        #if STP_MAX_MIN
            // Using minimum sampling to dilate convergence.
            cF.a = StpInPriFedMinAF(pF);
        #endif
//==============================================================================================================================
//      Independent logic.
//==============================================================================================================================
        // Quick estimation of how much blur is introduced by bilinear filtering of feedback.
        // This is used to adjust the amount of 'displacement' (sharpening).
        // {0 := on texel, 1/2 := half texel, 1 := on texel}.
        StpMF2 biXY = StpMF2(StpFractF2(m * kF));
        // {0 := on, 1/2 := half, 0 := on}.
        biXY = min(biXY, StpMF2_(1.0) - biXY);
        // {0 := on (no blur), 1/2 := half, 1 := between all four texels (max blur)}.
        StpMF1 bi = biXY.x + biXY.y;
        #if (STP_BUG == 21)
            bug.rgb = StpMF3_(bi);
        #endif
        // Force a minimum amount of displacement sharpening when still.
        bi = bi * kSharp.x + kSharp.y;
//------------------------------------------------------------------------------------------------------------------------------
        // Estimate if reprojection is on-screen.
        StpF2 onXY = StpF2(pM.xy);
        // {-1 to 1} is on screen.
        onXY = onXY * kOS.xy + kOS.zw;
        // If responsive then mark as 'off-screen' and allow offscreen handling to do the rest.
        // Remember 'r' is {0.0 := responsive, or 1.0 = normal}.
        // {0 := offscreen, 1 := onscreen}.
        StpF1 onS = StpSignedF1(max(abs(onXY.x), abs(onXY.y)) - r);
//------------------------------------------------------------------------------------------------------------------------------
        StpF1 dd = StpF1_(d);
        // Convert depth {0 to inf} to {0 to 1} safe for 10-bit value.
        z = StpZPack(z, kDepth, dd);
//------------------------------------------------------------------------------------------------------------------------------
        // Pack {MSB depth, LSB 11-bit XY motion}
        oM = StpMvPack(z, m, dd);
        // Kill new motion vector if {offscreen or responsive}.
        if(onS == StpF1_(0.0)) oM = StpU1_(0);
//------------------------------------------------------------------------------------------------------------------------------
        // Pre-process color.
        // If running pre-tonemap, then do a fast reversible tonemapper (convert from {0 to inf} to {0 to 1}).
        #if (STP_POSTMAP == 0)
            StpToneMF3(c);
        #endif
        // Get luma approximation in linear.
        StpMF1 cL = StpMax3MF1(c.r, c.g, c.b);
        // Initialize output luma to the non-dithered value (in gamma 2.0 space).
        oL = sqrt(cL);
//==============================================================================================================================
//      Dependent logic.
//==============================================================================================================================
        #if (STP_MAX_MIN == 0)
            mZVPN = min(StpMin3U1(mZVP4.x, mZVP4.y, mZVP4.z), mZVP4.w);
        #endif
        // Get nearest prior {z, motion}. This needs the 2x2 and the 4-tap ring.
        // . Z      . Z
        // X . (a)  X . (b)
        #if STP_SAFE_DILATE
            mZVPN = StpMin3U1(StpMin3U1(mZVPN, mZVP2a.x, mZVP2a.z), mZVP2b.x, mZVP2b.z);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // The {motion} matching logic.
        StpF2 mPN;
        StpF1 mZPN;
        StpF2 mE;
        // Motion 'm' units are {1 := move by one screen}.
        StpMvUnpackE(mZPN, mPN, mE, mZVPN);
//------------------------------------------------------------------------------------------------------------------------------
        // Replace with a smoother error estimate.
        // This '1/256' instead of '1/1024' is to be more accepting of a motion match. 
        // The 'sqrt()' cannot be the low precision approximation without visually seeing differences in the mask.
        mE = sqrt(abs(m)) + StpF2_(1.0 / 256.0);
        mE = mE * mE - abs(m);
//------------------------------------------------------------------------------------------------------------------------------
        // Static geometry motion + estimated dynamic motion matching logic.
        // Take unpacked low precision {0 to 1} Z and decode to {0 to INF}.
        StpF1 sgZ = StpZUnpack(mZPN, kUnDepth);
        StpF2 sgM = StpFor(pM, sgZ, mPN, kMotionMatch, k0123, k4567, k89AB, kCDEF, kGHIJ, kKLMN, kOPQR);
        // Note 'sgM' is in NDC {-1 to 1} space and 'm' is in {0 to 1} space, thus the 0.5 scaling factor.
        // The difference gets conservative possible motion encoding error subtracted out via 'saturate(abs(..)-mE)'.
        sgM = StpSatF2(abs(sgM * StpF2_(0.5) - m) - mE) * kC;
        StpF1 sgD = dot(sgM, sgM);
//------------------------------------------------------------------------------------------------------------------------------
        // Finish check if motion matches.
        sgD = StpSatF1(sgD * StpF1_(STP_DIF_AMP) - StpF1_(STP_DIF_ADD * STP_DIF_AMP));
        // {0 := no match, 1 := match}
        StpF1 mMatch = StpF1_(1.0) - sgD;
//------------------------------------------------------------------------------------------------------------------------------
        // Anti-flicker neighborhood expansion.
        // All of this logic runs in gamma 2.0.
        // Grab neighborhood of self and reprojected prior 2x2.
        // This is in paired {max, -min} form.
        StpMF2 xnyN = StpMax3MF2(StpMax3MF2(
            StpMF2(oL, -oL), StpMF2(rL4.x, -rL4.x), StpMF2(rL4.y, -rL4.y)), StpMF2(rL4.z, -rL4.z), StpMF2(rL4.w, -rL4.w));
//------------------------------------------------------------------------------------------------------------------------------
        // Distance from neighborhood to self (remember xnyN.y is negated).
        StpMF2 ns = StpMF2(xnyN.x, xnyN.y) + StpMF2(-oL, oL);
        StpMF1 ne = max(ns.x, ns.y);
//------------------------------------------------------------------------------------------------------------------------------
        // Clean is {0 to 1} ranged.
        // onS {0 := off, 1 := on-screen}
        // r {0 := responsive, 1 := normal}
        // mMatch {0 := no match, 1 := match}
        StpMF1 clean = StpMF1(onS * r * mMatch);
        // Split into 2 values.
        // {0 to 0.5} representing clean {0 to 1}
        // {0.5 to 1} representing temporal neighborhood scaling {0 to 1}
        StpMF1 cleanN = StpSatMF1(clean * StpMF1_(2.0) - StpMF1_(1.0));
        clean = StpSatMF1(clean * StpMF1_(2.0));
//------------------------------------------------------------------------------------------------------------------------------
        // Shape alpha (convergence) to modulate the amount of displacement sharpening.
        // This is getting extensive notes in case it ever needs modification.
        // --
        // If 10-bit feedback, have 2-bit alpha, must decode.
        //  B  VAL  NEED
        //  =  ===  ====
        //  0  0    1/2
        //  1  1/3  2/3
        //  2  2/3  3/4
        //  3  1    7/8
        // Going to use a linear decode 'x=f(a)=a*(3/8)+(1/2)' approximation for 10:10:10:2.
        // --
        // Want to map convergence 'x' from {1/2 to 7/8} to amount of sharpening.
        // Sharpening would be '1/(1-x)' so ranging {2 to 8}.
        // But want maximum to be 1 instead of 8, so '(1/8)/(1-x)'.
        // Also want to feather off sharpening around x={1/2}.
        // So modulate sharpening by a line through {1/2 to 7/8} mapping to {0 to 1}.
        // This line is 'line(x)=x*(1/(7/8-1/2))-((1/2)/(7/8-1/2))'.
        // So result is '(line(x)/8)/(1-x)'.
        // Then map a curve to that.
        // Using '(64/9)*x*(x-1)+(16/9)' for the curve.
        // --
        // For the 10:10:10:2 case.
        // It's '(64/9)*f(a)*(f(a)-1)+(16/9)' which reduces simply to 'a^2'.
        cF.a *= cF.a;
//------------------------------------------------------------------------------------------------------------------------------
        // Sharpening via displacement.
        // Allow a maximum of STP_NE_LIM contrast to push outside of exact 'ne' bounds.
        StpMF2 xnyN2 = StpSatMF2(StpMF2_(oL) + StpMF2(ne, -ne) * StpMF2_(STP_NE_LIM));
        xnyN = max(xnyN, StpMF2(xnyN2.x, -xnyN2.y));
        // Convert neighborhood from gamma 2.0 to linear (note this removes the sign on y).
        xnyN *= xnyN;
        // Compute final deblur term, taking into account local contrast changes.
        // Areas with big changes need less de-blur (else they will flicker more).
        // Note 'ko' is a gamma 2.0 computation (feedback is in gamma 2.0).
        StpMF1 ko = StpMax3MF1(cF.r, cF.g, cF.b) - oL;
        // {STP_BI_MAX := no change, 0 := big change}
        ko = StpPrxLoRcpMF1(StpMF1_(1.0 / STP_BI_MAX) + ko * ko * StpMF1_(STP_BI_TUNE / STP_BI_MAX));
        // Turn off displacement if missing {motion} match.
        bi *= clean;
        #if (STP_BUG == 22)
            bug.rgb = StpMF3_(bi);
        #endif
        // Can tune STP_BI_MAX, by setting 'ko = StpF1_(STP_BI_MAX)', and ignoring the jitter.
        // Apply.
        bi *= ko;
        #if STP_SDS
            // Modulate by shaped convergence.
            bi *= cF.a;
        #endif
        // Compute displacement, and fold the gamma to linear conversion in here.
        // Then apply bilinear feedback re-sharpen term.
        // Feedback is a 'blurry' version of input, so can use it as the lowpass of a unsharp mask.
        // Note 'c' is linear at this point, and 'cF' is gamma 2.0 (so this folds the gamma to linear conversion in the FMA).
        StpMF3 d3 = (c.rgb - (cF.rgb * cF.rgb)) * StpMF3_(bi);
        // Both sides {up,down} must be used (else get chroma inversions).
        // Note these values can go over one, because 'bi' can get large.
        // Maximum distance {up,down} range {0 := none, > max}.
        StpMF2 dUD = max(StpMF2_(0.0), StpMax3MF2(StpMF2(d3.r, -d3.r), StpMF2(d3.g, -d3.g), StpMF2(d3.b, -d3.b)));
        // Limit.
        // Note 'cL' is in linear.
        // In case of trouble, look into dUp = 0, and/or dDn = 0 cases.
        // Low precision here shouldn't be a problem.
        StpMF2 dUDT = StpSatMF2((StpMF2(-cL, cL) + StpMF2(xnyN.x, -xnyN.y)) * StpPrxLoRcpMF2(dUD));
        StpMF1 dT = min(dUDT.x, dUDT.y);
        // Can turn off feedback for debug.
        #if (STP_BUG_KILL_DSP == 0) && (STP_BUG_KILL_FEED == 0)
            // Apply displacement, saturate for bug protection.
            c.rgb = StpSatMF3(d3 * StpMF3_(dT) + c.rgb);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // Final process color.
        // Dither from linear to gamma 2.0.
        oC.rgb = StpRgbGamDit8MF3(c, d);
        // Doing non-energy conserving dither of original luma without displacement (must not feed back displacement).
        // This is just used for neighborhood computation, so the dither is expected to add value.
        // Don't need energy preservation, because this doesn't get exponential feedback.
        oL = StpSatMF1(oL + (d * StpMF1(1.0 / 255.0) + StpMF1(-0.5 / 255.0)));
//------------------------------------------------------------------------------------------------------------------------------
        // Debug views.
        #if ((STP_BUG == 21) || (STP_BUG == 22))
            oC.rg = bug.rg;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // Encoding two values.
        // 0-16 ..... {0 to 1.0} for clean (anything >16 = 1.0)
        // 16-255 ... {0 to 1.0} for temporal neighborhood (temporal neighborhood = 0, if clean != 1.0)
        oC.a = clean * StpMF1_(16.0/255.0) + ne * cleanN * StpMF1_(1.0 - 16.0 / 255.0); }
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                         16-BIT PATH
//==============================================================================================================================
#if defined(STP_GPU) && defined(STP_16BIT) && defined(STP_IN)
    void StpInDatH(inout StpF1 r, inout StpH3 c, inout StpF1 z, inout StpF2 m, StpU2 o);
//------------------------------------------------------------------------------------------------------------------------------
    StpH1 StpInDitH(StpU2 o);
//------------------------------------------------------------------------------------------------------------------------------
    // This is different than the 32-bit version of the callback.
    StpH4 StpInPriLumH(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    // These are the same as the 32-bit versions of the callback, just renamed in case STP_32BIT isn't defined.
    #if STP_MAX_MIN
        StpU1 StpInPriMotMinH(StpF2 p);
    #else
        StpU4 StpInPriMot4H(StpF2 p);
    #endif
//------------------------------------------------------------------------------------------------------------------------------
    StpH4 StpInPriFedH(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    #if STP_MAX_MIN
        StpH1 StpInPriFedMinAH(StpF2 p);
    #endif
//==============================================================================================================================
    // See the 32-bit version of this function for comments and docs, only 16-bit specific notes are here.
    void StpInH(
    out StpH4 oC,
    out StpU1 oM,
    out StpH1 oL,
    StpU2 pp,
    StpU4 con0,
    StpU4 con1,
    StpU4 con2,
    StpU4 con3,
    StpU4 con4,
    StpU4 con5,
    StpU4 con6,
    StpU4 con7,
    StpU4 con8,
    StpU4 con9,
    StpU4 conA,
    StpU4 conB,
    StpU4 conC,
    StpU4 conD) {
//------------------------------------------------------------------------------------------------------------------------------
        StpF1 r;
        StpH3 c;
        StpF1 z;
        StpF2 m;
        StpInDatH(r,c,z,m,pp);
//------------------------------------------------------------------------------------------------------------------------------
        StpH4 bug = StpH4_(0.0);
//------------------------------------------------------------------------------------------------------------------------------
        #if defined(STP_HLSL)
            oL = StpH4_(0.0);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 kRcpC = StpF2_U2(con0.xy);
        StpF2 kHalfRcpC = StpF2_U2(con0.zw);
        StpF2 kC = StpF2_U2(con1.xy);
        StpF2 kJitCRcpCUnjitPRcpP = StpF2_U2(con1.zw);
        StpF2 kHalfRcpP = StpF2_U2(con2.xy);
        StpF2 kDepth = StpF2_U2(con2.zw);
        StpF2 kJitCRcpC = StpF2_U2(con3.xy);
        StpF2 kF = StpF2_U2(con3.zw);
        StpF4 kOS = StpF4_U4(con4);
        StpH2 kSharp = StpH2_U1(con5.z);
        StpF1 kMotionMatch = StpF1_U1(con5.w);
        StpF2 kUnDepth = StpF2_U2(con6.xy);
        StpF4 k0123 = StpF4_U4(con7);
        StpF4 k4567 = StpF4_U4(con8);
        StpF4 k89AB = StpF4_U4(con9);
        StpF4 kCDEF = StpF4_U4(conA);
        StpF4 kGHIJ = StpF4_U4(conB);
        StpF4 kKLMN = StpF4_U4(conC);
        StpF4 kOPQR = StpF4_U4(conD);
//------------------------------------------------------------------------------------------------------------------------------
        StpH1 d = StpInDitH(pp);
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 p = StpF2(pp) * kRcpC + kHalfRcpC;
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 pM = (p - m) + kJitCRcpCUnjitPRcpP;
//------------------------------------------------------------------------------------------------------------------------------
        StpU1 mZVPN;
        StpU4 mZVP2a = StpInPriMot4H(pM - kHalfRcpP);
        StpU4 mZVP2b = StpInPriMot4H(pM + kHalfRcpP);
        #if STP_MAX_MIN
            mZVPN = StpInPriMotMinH(pM);
        #else
            StpU4 mZVP4 = StpInPriMot4H(pM);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        StpH4 rL4 = StpInPriLumH(pM);
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 pF = (p - m) + kJitCRcpC;
        StpH4 cF = StpInPriFedH(pF);
        #if STP_MAX_MIN
            cF.a = StpInPriFedMinAH(pF);
        #endif
//==============================================================================================================================
        StpH2 biXY = StpH2(StpFractF2(m * kF));
        biXY = min(biXY, StpH2_(1.0) - biXY);
        StpH1 bi = biXY.x + biXY.y;
        #if (STP_BUG == 21)
            bug.rgb = StpH3_(bi);
        #endif
        bi = bi * kSharp.x + kSharp.y;
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 onXY = StpF2(pM.xy);
        onXY = onXY * kOS.xy + kOS.zw;
        StpF1 onS = StpSignedF1(max(abs(onXY.x), abs(onXY.y)) - r);
//------------------------------------------------------------------------------------------------------------------------------
        StpF1 dd = StpF1_(d);
        z = StpZPack(z, kDepth, dd);
//------------------------------------------------------------------------------------------------------------------------------
        oM = StpMvPack(z, m, dd);
        if(onS == StpF1_(0.0)) oM = StpU1_(0);
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_POSTMAP == 0)
            StpToneH3(c);
        #endif
        StpH1 cL = StpMax3H1(c.r, c.g, c.b);
        oL = sqrt(cL);
//==============================================================================================================================
        #if (STP_MAX_MIN == 0)
            mZVPN = min(StpMin3U1(mZVP4.x, mZVP4.y, mZVP4.z), mZVP4.w);
        #endif
        #if STP_SAFE_DILATE
            mZVPN = StpMin3U1(StpMin3U1(mZVPN, mZVP2a.x, mZVP2a.z), mZVP2b.x, mZVP2b.z);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 mPN;
        StpF1 mZPN;
        StpF2 mE;
        StpMvUnpackE(mZPN, mPN, mE, mZVPN);
        mE = sqrt(abs(m)) + StpF2_(1.0 / 256.0);
        mE = mE * mE - abs(m);
//------------------------------------------------------------------------------------------------------------------------------
        StpF1 sgZ = StpZUnpack(mZPN, kUnDepth);
        StpF2 sgM = StpFor(pM, sgZ, mPN, kMotionMatch, k0123, k4567, k89AB, kCDEF, kGHIJ, kKLMN, kOPQR);
        sgM = StpSatF2(abs(sgM * StpF2_(0.5) - m) - mE) * kC;
        StpF1 sgD = dot(sgM, sgM);
//------------------------------------------------------------------------------------------------------------------------------
        sgD = StpSatF1(sgD * StpF1_(STP_DIF_AMP) - StpF1_(STP_DIF_ADD * STP_DIF_AMP));
        StpF1 mMatch = StpF1_(1.0) - sgD;
//------------------------------------------------------------------------------------------------------------------------------
        StpH2 xnyN = StpMax3H2(StpMax3H2(
            StpH2(oL, -oL), StpH2(rL4.x, -rL4.x), StpH2(rL4.y, -rL4.y)), StpH2(rL4.z, -rL4.z), StpH2(rL4.w, -rL4.w));
//------------------------------------------------------------------------------------------------------------------------------
        StpH2 ns = StpH2(xnyN.x, xnyN.y) + StpH2(-oL, oL);
        StpH1 ne = max(ns.x, ns.y);
//------------------------------------------------------------------------------------------------------------------------------
        StpH1 clean = StpH1(onS * r * mMatch);
        StpH1 cleanN = StpSatH1(clean * StpH1_(2.0) - StpH1_(1.0));
        clean = StpSatH1(clean * StpH1_(2.0));
//------------------------------------------------------------------------------------------------------------------------------
        cF.a *= cF.a;
//------------------------------------------------------------------------------------------------------------------------------
        StpH2 xnyN2 = StpSatH2(StpH2_(oL) + StpH2(ne, -ne) * StpH2_(STP_NE_LIM));
        xnyN = max(xnyN, StpH2(xnyN2.x, -xnyN2.y));
        xnyN *= xnyN;
        StpH1 ko = StpMax3H1(cF.r, cF.g, cF.b) - oL;
        ko = StpPrxLoRcpH1(StpH1_(1.0 / STP_BI_MAX) + ko * ko * StpH1_(STP_BI_TUNE / STP_BI_MAX));
        bi *= clean;
        #if (STP_BUG == 22)
            bug.rgb = StpH3_(bi);
        #endif
        bi *= ko;
        #if STP_SDS
            bi *= cF.a;
        #endif
        StpH3 d3 = (c.rgb - (cF.rgb * cF.rgb)) * StpH3_(bi);
        StpH2 dUD = max(StpH2_(0.0), StpMax3H2(StpH2(d3.r, -d3.r), StpH2(d3.g, -d3.g), StpH2(d3.b, -d3.b)));
        StpH2 dUDT = StpSatH2((StpH2(-cL, cL) + StpH2(xnyN.x, -xnyN.y)) * StpPrxLoRcpH2(dUD));
        StpH1 dT = min(dUDT.x, dUDT.y);
        #if ((STP_BUG_KILL_DSP == 0) && (STP_BUG_KILL_FEED == 0))
            c.rgb = StpSatH3(d3 * StpH3_(dT) + c.rgb);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        oC.rgb = StpRgbGamDit8H3(c, d);
        oL = StpSatH1(oL + (d * StpH1(1.0 / 255.0) + StpH1(-0.5 / 255.0)));
//------------------------------------------------------------------------------------------------------------------------------
        #if ((STP_BUG == 21) || (STP_BUG == 22))
            oC.rg = bug.rg;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        oC.a = clean * StpH1_(16.0/255.0) + ne * cleanN * StpH1_(1.0 - 16.0 / 255.0); }
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//
//                                                   SCALING TAA ENTRY POINT
//
//==============================================================================================================================
#if defined(STP_GPU)&&defined(STP_TAA)&&defined(STP_32BIT)
    // Callbacks.
    // Dither value {0 to 1} this should be output pixel frequency spatial temporal blue noise.
    // Only used if STP_GRAIN is used, and only 'dit.x' if STP_GRAIN=1, 'dit.xyz' if STP_GRAIN=3.
    StpMF3 StpTaaDitF(StpU2 o);
//------------------------------------------------------------------------------------------------------------------------------
    // Current frame {color,anti} input.
    // Gather 4 specific channels.
    StpMF4 StpTaaCol4RF(StpF2 p);
    StpMF4 StpTaaCol4GF(StpF2 p);
    StpMF4 StpTaaCol4BF(StpF2 p);
    StpMF4 StpTaaCol4AF(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    // Gather4 on luma.
    StpMF4 StpTaaLum4F(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    // Current frame motion {z,x,y} packed input.
    // Gather4.
    StpU4 StpTaaMot4F(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    // Feedback {color,convergence}.
    // Bilinear fetch with clamp to edge.
    StpMF4 StpTaaPriFedF(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    #if STP_MAX_MIN
        // Maximum alpha for feedback.
        StpMF1 StpTaaPriFedMaxAF(StpF2 p);
    #endif
//==============================================================================================================================
    void StpTaaF(
    out StpMF4 c, // Color (as RGB).
    out StpMF4 f, // Feedback (to be stored).
    StpU2 o,      // Integer pixel offset in ouput.
    StpU4 con0,   // Constants generated by StpTaaCon().
    StpU4 con1,
    StpU4 con2,
    StpU4 con3,
    StpU4 con4) {
//------------------------------------------------------------------------------------------------------------------------------
        // Used for debug only, will get dead code removal otherwise.
        // Alpha: 1=untone, 0=none
        StpMF4 bug = StpMF4_(0.0);
//------------------------------------------------------------------------------------------------------------------------------
        #if defined(STP_HLSL)
            // Common setup to avoid HLSL compiler warning as bug, resets all for easy debug.
            c = f = StpMF4_(0.0);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // Rename constants.
        StpF2 kCRcpF = StpF2_U2(con0.xy);
        StpF2 kHalfCRcpFUnjitC = StpF2_U2(con0.zw);
        StpF2 kRcpC = StpF2_U2(con1.xy);
        // Note, kDubRcpCX is currently unused.
        StpF1 kDubRcpCX = StpF1_U1(con1.z);
        StpF2 kRcpF = StpF2_U2(con2.xy);
        StpF2 kHalfRcpF = StpF2_U2(con2.zw);
        #if 0
            StpH2 kKRcpF = StpH2_U1(con1.w);
            StpH2 kGrain = StpH2_U1(con3.z);
        #else
            // 32-bit path.
            // Note 'kkRcpF' not used, but left in case it is ever needed in the future.
            StpF2 kKRcpF = StpF2_U2(con3.xy);
            StpF2 kGrain = StpF2_U2(con4.xy);
        #endif
        StpF2 kF = StpF2_U2(con4.zw);
//------------------------------------------------------------------------------------------------------------------------------
        StpMF3 dit = StpTaaDitF(o);
//------------------------------------------------------------------------------------------------------------------------------
        // Locate 2x2 neighborhood.
        // Float version of integer pixel offset in output.
        // All the 'o' prefixed variables are offset (aka position/coordinate) related.
        StpF2 oI = StpF2(o);
        // This gets to the center of the 2x2 quad directly because of possibility of shader/tex precision mismatch.
        // Precision mismatch could yield different 2x2 quads.
        StpF2 oC = oI * kCRcpF + kHalfCRcpFUnjitC;
        // NW of 2x2 quad.
        StpF2 oCNW = floor(oC + StpF2_(-0.5));
        // Center of the 2x2 quad.
        StpF2 oC4 = oCNW * kRcpC + kRcpC;
//------------------------------------------------------------------------------------------------------------------------------
        // Need un-sharpened for base luma (gamma 2.0).
        StpMF4 c4L = StpTaaLum4F(oC4);
//------------------------------------------------------------------------------------------------------------------------------
        // Fetch {z,motion}.
        StpU4 m4 = StpTaaMot4F(oC4);
        // Fetch {color}.
        StpMF4 c4R = StpTaaCol4RF(oC4);
        StpMF4 c4G = StpTaaCol4GF(oC4);
        StpMF4 c4B = StpTaaCol4BF(oC4);
        StpMF4 c4A = StpTaaCol4AF(oC4);
//------------------------------------------------------------------------------------------------------------------------------
        #if ((STP_BUG == 1) || (STP_BUG == 21) || (STP_BUG == 22))
            // Debug view input color {r,g,b}.
            bug.rgb = StpMF3(c4R.x, c4G.x, c4B.x);
            // Need gamma to linear here.
            bug.rgb *= bug.rgb;
            #if (STP_BUG == 1)
                bug.a = StpMF1_(1.0);
            #endif
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // Half screen term for debug only.
        StpMF1 bugH = StpMF1_(oC4.x);
//------------------------------------------------------------------------------------------------------------------------------
        // Setup resolve position {0 to 1} inside 2x2 quad.
        // The extra -0.5 is to get from NW position to center.
        StpMF2 rP = StpMF2(oC - oCNW) - StpMF2_(0.5);
        // Setup for fetch feedback.
        StpF2 oF = oI * kRcpF + kHalfRcpF;
//==============================================================================================================================
        // Angular nearest.
        // This rounds off the stair steps, improving dilated nearest motion quality.
        // Two cases,
        //  (1.) More than 2 of the 2x2 points are near
        //        - Then dilate with minimum of 2x2
        //  (2.) Only one of the 2x2 points is near
        //        - Then take the minimum of the two diagonal half fill cases
        //           +--Z       W--+
        //           |\ |  MIN  | /|
        //           | \|       |/ |
        //           X--+       +--Y
        StpU1 m4min = min(StpMin3U1(m4.x, m4.y, m4.z), m4.w);
        StpU1 m4max = max(StpMax3U1(m4.x, m4.y, m4.z), m4.w);
        // Depth split between {near,far}.
        StpU1 m4mid = (m4max >> 1u) + (m4min >> 1u);
        StpU1 m1 =
            // (1.) If more than one is on the near side, fill with minimum of 2x2.
            (((m4.x < m4mid ? 1u : 0u) +
              (m4.y < m4mid ? 1u : 0u) +
              (m4.z < m4mid ? 1u : 0u) +
              (m4.w < m4mid ? 1u : 0u)) > 1u) ? m4min :
            // (2.) Else fill using diagonal logic.
            min((rP.x - rP.y) < StpF1_(0.0) ? m4.x : m4.z,
                (rP.x + rP.y) < StpF1_(1.0) ? m4.w : m4.y);
        // Using a sample which is more near than the one used for reprojection can cause problems.
        // So they are deweighted in the final blend.
        // But not to zero, because that can cause problems in corner cases.
        StpF4 avoidNear;
        avoidNear.x = (m4.x < m1) ? StpF1_(STP_AVOID_NEAR) : StpF1_(1.0);
        avoidNear.y = (m4.y < m1) ? StpF1_(STP_AVOID_NEAR) : StpF1_(1.0);
        avoidNear.z = (m4.z < m1) ? StpF1_(STP_AVOID_NEAR) : StpF1_(1.0);
        avoidNear.w = (m4.w < m1) ? StpF1_(STP_AVOID_NEAR) : StpF1_(1.0);
        // Responsive AA needs to kill anti-flicker.
        StpP1 rsp = m1 == 0u;
        StpF2 mXY;
        // Motion 'm' units are {1 := move by one screen}.
        StpMvUnpackVF(mXY, m1);
//------------------------------------------------------------------------------------------------------------------------------
        #if ((STP_BUG == 2) || (STP_BUG == 3) || (STP_BUG == 4))
            StpF2 bugV; StpF1 bugZ; StpF2 bugE; StpMvUnpackE(bugZ, bugV, bugE, m4.w); StpP1 bugR = m4.w == 0u;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // Quick estimation of how much blur is introduced by bilinear filtering of feedback.
        // This is used to adjust the amount of 'displacement' (sharpening).
        // {0 := on texel, 1/2 := half texel, 1 := on texel}.
        StpMF2 biXY = StpMF2(StpFractF2(mXY * kF));
        // {1 := on, 1/2 := half, 1 := on}.
        biXY = max(biXY, StpMF2_(1.0) - biXY);
        // {1 := on (no blur), 1/2 := half, 1/4 := between all four texels (max blur)}.
        StpMF1 bi = biXY.x * biXY.y;
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 oFF = oF - mXY;
        StpMF4 cF = StpTaaPriFedF(oFF);
        #if STP_ERODE
            cF.a = StpTaaPriFedMaxAF(oFF);
        #endif
//==============================================================================================================================
        #if (STP_BUG == 2)
            // Depth visualization.
            bug.r = bug.g = StpMF1_(bugZ);
            bug.b = StpMF1_(bugZ) * StpMF1_(0.75) + c4G.w * StpMF1_(0.25);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 3)
            // Motion visualization.
            bug.rg = StpMF2_(1.0) - exp2(abs(StpF2(bugV)) * StpMF2_(-32.0));
            bug.b = c4G.w * StpMF1_(0.25);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 9)
            bug.rg = StpMF2_(1.0) - exp2(abs(StpMF2(mXY)) * StpMF2_(-32.0));
            bug.b = c4G.w * StpMF1_(0.25);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 4)
            // Responsive AA visualization.
            bug.r = bugR ? StpMF1_(1.0) : StpMF1_(0.0);
            bug.g = StpMF1_(0.0);
            bug.b = c4G.w * StpMF1_(0.25);
        #endif
//==============================================================================================================================
        // Decode alpha.
        // 0-16 ..... {0 to 1.0} for clean (anything >16 = 1.0)
        // 16-255 ... {0 to 1.0} for temporal neighborhood (temporal neighborhood = 0, if clean != 1.0)
        StpMF4 clean = StpSatMF4(c4A * StpMF4_(255.0 / 16.0));
        StpMF4 ne = StpSatMF4(c4A * StpMF4_(255.0 / (255.0 - 16.0)) - StpMF4_((255.0 / (255.0 - 16.0)) * (1.0 / 16.0)));
        // Compute average.
        // Using minimum here will strip out the anti-aliasing, maximum is more conservative, less flicker, average is middle.
        StpMF1 neAvg = StpMF1_(0.25) * ne.x + StpMF1_(0.25) * ne.y + StpMF1_(0.25) * ne.z + StpMF1_(0.25) * ne.w;
//------------------------------------------------------------------------------------------------------------------------------
        // Build single frame mono local neighborhood.
        // Maximum and -minimum.
        StpMF2 xnyL = max(StpMax3MF2(StpMF2(c4L.x, -c4L.x), StpMF2(c4L.y, -c4L.y), StpMF2(c4L.z, -c4L.z)),
            StpMF2(c4L.w, -c4L.w));
        StpMF1 me = xnyL.x + xnyL.y;
        StpMF1 bugM = me;
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 5)
            // Alpha visualization.
            bug.rgb = StpMF3(clean.w, ne.w * ne.w, c4G.w * StpMF1_(0.25));
            bug.r = StpMF1_(1.0) - bug.r;
            bug.r *= bug.r;
            if(rsp) bug.b = StpMF1_(1.0);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 6)
            // Debug neighborhood contrast.
            bug.rgb = StpMF3((ne.x + ne.y + ne.z + ne.w) * StpMF1_(0.25), bugM, StpMF1_(0.0));
            bug.rg *= bug.rg;
        #endif
//==============================================================================================================================
        // Limit max blend ratio to maximize responsiveness.
        // Working in gamma 2.0 space (perceptual) below.
        StpMF1 maxB = StpMF1_(STP_SHARP_FLICKER) * neAvg;
        // Reduce the contrast by bilinear blur.
        maxB *= bi;
        // The '4/5' is the lowest amount of feedback allowed.
        // TODO: Can rcp() be approximate here?
        maxB = (maxB - StpMF1_(1.0)) * rcp(maxB);
        maxB = clamp(maxB, StpMF1_(4.0 / 5.0), StpMF1_(STP_FEED_MAX));
        // Grab maxmimum (upper) blend ratio from alpha.
        // Note alpha is interpolated so not actually a 2-bit value.
        // If 10-bit feedback, have 2-bit alpha, must decode.
        //  B  VAL  NEED
        //  =  ===  ====
        //  0  0    1/2
        //  1  1/3  2/3
        //  2  2/3  3/4
        //  3  1    4/5 to approaching 1.0 (dynamic adjustable)
        // Exact fit poly for {0-2/3} input is '-3/8*x*x+5/8*x+1/2' which is 3/4 again at {1}.
        // Can add in an 'x^8' term to bring up mostly the B=3 decode position to the final adjustable blend ratio.
        // So '(maxB-0.75)*x*x*x*x*x*x*x*x+(-3/8)*x*x+(5/8)*x+(1/2)' then in Horner's form.
        StpMF1 cFa2 = cF.a * cF.a;
        StpMF1 cFa4 = cFa2 * cFa2;
        StpMF1 blnU = (((maxB - StpMF1_(0.75)) * cFa4 * cFa2 + StpMF1_(-0.375)) * cF.a + StpMF1_(0.625)) * cF.a + StpMF1_(0.5);
//------------------------------------------------------------------------------------------------------------------------------
        // If responsive then kill blend weight.
        if(rsp) blnU = StpMF1_(0.0);
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 20)
            bug.r = StpMF1_(1.0) - blnU * StpMF1_(1.0 / STP_FEED_MAX); bug.gb = bug.rr;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // Join spatial and temporal contrast.
        ne = max(ne, StpMF4_(me));
//==============================================================================================================================
        // Generate neighborhood min and max in gamma 2.0 space.
        // Note that 'clean' kills the merged {spatial, temporal} contrast.
        // Thus 'clean' is still needed (vs trying to just zero {temporal contrast} in the inline function).
        StpMF4 n4Min = StpSatMF4(c4L - ne * clean);
        StpMF4 n4Max = StpSatMF4(c4L + ne * clean);
//------------------------------------------------------------------------------------------------------------------------------
        // Gamma to linear.
        n4Min *= n4Min;
        n4Max *= n4Max;
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 7)
            bug.rgb = n4Max.wzy;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 8)
            bug.rgb = n4Min.wzy;
        #endif
//==============================================================================================================================
        // Convert from gamma to linear.
        c4R *= c4R;
        c4G *= c4G;
        c4B *= c4B;
        // Can save here to avoid sqrt() later.
        StpMF1 f1Lg = StpMax3MF1(cF.r, cF.g, cF.b);
        cF.rgb *= cF.rgb;
//------------------------------------------------------------------------------------------------------------------------------
        // Generated clamped feedback in linear.
        StpMF1 f1L = StpMax3MF1(cF.r, cF.g, cF.b);
        StpMF4 f4C = clamp(StpMF4_(f1L), n4Min, n4Max);
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 11)
            // Debug {clamped feedback}.
            bug.rgb = f4C.wzy;
        #endif
//==============================================================================================================================
        // Compute blend ratios for feedback.
        // Restore c4L after displacement, and as linear.
        c4L = StpMax3MF4(c4R, c4G, c4B);
        // This is needed to avoid cases where c4L is out of bounds (which it can be from dither).
        // This also retores correct 'bln=0' behavior when 'clean=0'.
        c4L = clamp(c4L, n4Min, n4Max);
        // The saturate protects against 'f-c=0' -> '/0' case.
        // The saturate protects in case 'c' is out of bounds of the {max,min}.
        //  rcp( 0) = +INF
        //  rcp(-0) = -INF
        //  sat(+INF) = 1
        //  sat(-INF) = 0
        // If '(f4L-c4L)==0' then '(f4C-c4L)==0' and 0*INF=NaN, and sat(NaN)=0 (no feedback).
        StpMF4 bln = StpSatMF4((f4C - c4L) * StpPrxLoRcpMF4(StpMF4_(f1L) - c4L));
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 12)
            // Initial blend ratio.
            bug.rgb = bln.wzy;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // Reduce for kernel shaping.
        bln = min(bln, StpMF4_(blnU));
        // Sharpness is adjusted by worst case blend ratio.
        // This is written to be good for packed, min3() platforms should be able to pattern match.
        StpMF2 sharp2 = min(bln.xy, bln.zw);
        // Note the maximum amount of sharpness will be STP_FEED_MAX (the convergence limit), and not 1.
        StpMF1 sharp = min(sharp2.x, sharp2.y);
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 15)
            // Final blend ratio.
            bug.rgb = bln.wzy;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // Debug kill feedback.
        #if STP_BUG_KILL_FEED
            bln = StpMF4_(0.0);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // Blended color.
        StpMF4 blnI = StpMF4_(1.0) - bln;
        StpMF4 b4R = c4R * blnI + StpMF4_(cF.r) * bln;
        StpMF4 b4G = c4G * blnI + StpMF4_(cF.g) * bln;
        StpMF4 b4B = c4B * blnI + StpMF4_(cF.b) * bln;
//==============================================================================================================================
        // Compute and apply anti-flicker here.
        StpMF4 b4L = StpMax3MF4(b4R, b4G, b4B);
        // Perceptual is important.
        StpMF4 b4Lg = StpPrxLoSqrtMF4(b4L);
        StpMF4 anti = b4Lg - StpMF4_(f1Lg);
//------------------------------------------------------------------------------------------------------------------------------
        // Directional analysis.
        // Compute filtering direction, then setup base filtering weights.
        // Resolve direction in 45 deg angle {-1 to 1} range .
        StpMF2 rD = StpMF2(b4Lg.z - b4Lg.x, b4Lg.y - b4Lg.w);
        // Rotate to axis aligned {-2 to 2} range.
        rD = StpMF2(-rD.y, rD.y) + rD.xx;
        // Note {rD} can be {0,0} which won't get correct intersections, but it won't matter (all same value).
        // Take resolve point, and line in resolve direction.
        // Compute where the line intersects the edges of the pixel.
        StpMF2 rRcpD = StpPrxLoRcpMF2(rD);
        // Fix for /0.
        rRcpD = min(StpMF2_(32768.0), rRcpD);
//------------------------------------------------------------------------------------------------------------------------------
        // Visualize edge direction.
        #if (STP_BUG == 14)
            bug.rb = abs(rD * StpMF2_(StpRcpMF1(sqrt(rD.x * rD.x + rD.y * rD.y + StpMF1_(1.0 / 32768.0)))));
            bug.g = StpMF1_(0.0);
            bug.rgb *= bug.rgb;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // Resolve position mapped to {-1 to 1}.
        StpMF2 rPM = rP * StpMF2_(2.0) - StpMF2_(1.0);
        // Find intersections to AABB.
        StpMF2 rTN2 =  rRcpD - rPM * rRcpD;
        StpMF2 rTP2 = -rRcpD - rPM * rRcpD;
        // The {max, -min} of X and Y.
        StpMF2 rTNPX = max(StpMF2(rTN2.x, -rTN2.x), StpMF2(rTP2.x, -rTP2.x));
        StpMF2 rTNPY = max(StpMF2(rTN2.y, -rTN2.y), StpMF2(rTP2.y, -rTP2.y));
        // The {-min, max} of prior terms (for N and P points).
        // Note both get negated, the first term to do the -min, and the 2nd to denegate the prior -min.
        StpMF2 rTNP = max(-rTNPX, -rTNPY);
        // Intersection positions {0 to 1}.
        StpMF2 rP0 = StpSatMF2(rP + rD * StpMF2_(rTNP.x) * StpMF2_(-0.5));
        StpMF2 rP1 = StpSatMF2(rP + rD * StpMF2_(rTNP.y) * StpMF2_( 0.5));
//------------------------------------------------------------------------------------------------------------------------------
        // Position on line for 2nd interpolation.
        // This doesn't bother fixing for /0, lets saturate handle it.
        StpMF2 rT2;
        rT2.y = StpSatMF1(abs(rTNP.x) * StpPrxLoRcpMF1(abs(rTNP.x) + abs(rTNP.y)));
        rT2.x = StpMF1_(1.0) - rT2.y;
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 16)
            // Visualize directional filtering interpolation.
            bug.r = rP0.x;
            bug.g = rP0.y;
            bug.b = rT2.x;
            bug.rgb *= bug.rgb;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // Interpolate at both points (this is on the edge of the pixel).
        StpMF4 wP0,wP1;
        wP0 = StpMF4_(1.0) - max(
            StpMF4(0.0, 1.0, 1.0, 0.0) + StpMF4( rP0.x, -rP0.x, -rP0.x, rP0.x),
            StpMF4(1.0, 1.0, 0.0, 0.0) + StpMF4(-rP0.y, -rP0.y,  rP0.y, rP0.y));
        wP1 = StpMF4_(1.0) - max(
            StpMF4(0.0, 1.0, 1.0, 0.0) + StpMF4( rP1.x, -rP1.x, -rP1.x, rP1.x),
            StpMF4(1.0, 1.0, 0.0, 0.0) + StpMF4(-rP1.y, -rP1.y,  rP1.y, rP1.y));
//------------------------------------------------------------------------------------------------------------------------------
        // Interpolate between points.
        // Base term to interpolate the anti-flicker value (doesn't include anti-flicker weighting).
        StpMF4 wI = wP0 * StpMF4_(rT2.x) + wP1 * StpMF4_(rT2.y);
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 13)
            // Visualize kernel sharpness.
            bug.r = bug.g = bug.b = StpMF1_(1.0) - sharp;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_AKS
            // Adaptive kernel sharpness, goes smooth if feedback gets limited.
            // Unoptimized: wI = StpLerpF4(wI, wI * wI * wI, StpF4_(sharp));
            StpMF4 wIS = wI * wI;
            wIS = wIS * wI - wI;
            wI = wI + wIS * StpMF4_(sharp);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // This output value is {near 1 to 16384.0} to better maintain precision.
        // Approximation results don't get as close to 1, but effects of 'anti' term are normalized out.
        anti = StpPrxLoRcpMF4(StpMF4_(1.0 / STP_ANTI_LIM) + anti * anti);
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_SHAPE_ANTI
            // Shape anti-flicker.
            // Push all but the minimum anti flicker value up.
            // Avoiding adjusting the minimum value maintains more of the noise suppression.
            // Pushing up evens out the anti values as they reach high change, enabling better anti-aliasing.
            StpMF1 antiM = min(StpMin3MF1(anti.x, anti.y, anti.z), anti.w);
            // antiM - larger = signed
            // antiM - same   = 0
            // This gets turned off as 'sharp' approaches zero to avoid artifacts on new data.
            // Note the 1/STP_FEED_MAX factor is due to sharp not going to one.
            anti += StpSignedMF4(StpMF4_(antiM) - anti) * StpMF4_(StpMF1_(STP_PUSH / STP_FEED_MAX) * sharp);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 17)
            // Debug {anti-flicker weights}.
            bug.rgb = anti.wzy * StpMF3_(1.0 / 32768.0);
        #endif
        #if STP_BUG_KILL_ANTI
            // Debug kill anti-flicker weighting.
            rsp = true;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // Anything with a 0u {z,motion} fetches from the wrong place, and thus needs to kill anti-flicker.
        if(!rsp) wI *= anti;
        // Deweight samples that are in-front of the depth used for reprojection.
        wI *= avoidNear;
        // Normalize and interpolate.
        // Have to interpolate blend weight, as blend weight is ultimately used as the cleaner mask.
        #if 0
            // This is used for the packed 16-bit path.
            StpF2 wI2 = wI.xy + wI.zw;
            StpF1 wI1 = wI2.x + wI2.y;
            // Not doing approximation here, as precision could be a problem with color.
            StpF1 wIR = StpRcpF1(wI1);
            // Might as well normalize first (better precision, same number of ops).
            wI *= StpF4_(wIR);
            // This is used for the packed 16-bit path.
            StpF2 cBR = (b4R.xy * wI.xy) + (b4R.zw * wI.zw);
            StpF2 cBG = (b4G.xy * wI.xy) + (b4G.zw * wI.zw);
            StpF2 cBB = (b4B.xy * wI.xy) + (b4B.zw * wI.zw);
            StpF2 cAB = (bln.xy * wI.xy) + (bln.zw * wI.zw);
            f = StpF4(cBR.x + cBR.y, cBG.x + cBG.y, cBB.x + cBB.y, cAB.x + cAB.y);
            c.rgb = f.rgb;
        #else
            // Used for the 32-bit path, might be able to use add3() if platforms have it.
            StpMF1 wIR = StpRcpMF1(wI.x + wI.y + wI.z + wI.w);
            wI *= StpMF4_(wIR);
            // 32-bit path.
            f = StpMF4(b4R.x * wI.x + b4R.y * wI.y + b4R.z * wI.z + b4R.w * wI.w,
                       b4G.x * wI.x + b4G.y * wI.y + b4G.z * wI.z + b4G.w * wI.w,
                       b4B.x * wI.x + b4B.y * wI.y + b4B.z * wI.z + b4B.w * wI.w,
                       bln.x * wI.x + bln.y * wI.y + bln.z * wI.z + bln.w * wI.w);
            c.rgb = f.rgb;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // 2-bit alpha, thus must encode this.
        // Convert to next frame count {1,2,3,4}.
        //  x=1/(1-x) ....... Convert blend weight into frame count advanced by one frame.
        //  x=x*(1/3)-1/3 ... Map {1 to 4} to {0 to 1}
        // This dithers, anything at output=1 is mapped to max blend (so this only gets early frame convergence).
        f.a = StpRcpMF1(StpMF1(1.0) - f.a) + (dit.x * StpMF1_(0.5) - StpMF1_(0.5));
        f.a = StpSatMF1(StpMF1_(1.0 / 3.0) * f.a - StpMF1_(1.0 / 3.0));
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 18)
            // Debug final weights.
            bug.rgb = wI.wzy;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 19)
            // Debug feedback.
            bug.rgb = cF.rgb;
            bug.a = StpMF1_(1.0);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 23)
            // Debug feedback convergence.
            bug.g = StpMF1_(0.0);
            bug.r = StpMF1_(1.0) - cF.a;
            bug.b = cF.b * StpMF1_(1.0 / 8.0);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // Feedback is in gamma 2.0, this needs to be more post-tonemap linear energy conserving else it will band.
        f.rgb = StpRgbGamDit10MF3(f.rgb, dit.x);
//------------------------------------------------------------------------------------------------------------------------------
        // Optionally apply grain.
        #if (STP_GRAIN == 1)
            // Slightly fast monochromatic version.
            c.rgb += min(StpMF3_(1.0) - c.rgb, c.rgb) * StpMF3_(dit.x * kGrain.x + kGrain.y);
        #endif
        #if (STP_GRAIN == 3)
            c.rgb += min(StpMF3_(1.0) - c.rgb, c.rgb) * (dit * StpMF3_(kGrain.x) + StpMF3_(kGrain.y));
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_POSTMAP == 0)
            // Decode so result can be used after return.
            StpToneInvMF3(c.rgb);
        #endif
//==============================================================================================================================
        // Generalized full screen debug view.
        #if (STP_BUG && (STP_BUG_SPLIT == 0))
            #if (STP_POSTMAP == 0)
                if(bug.a == StpF1_(1.0)) StpToneInvMF3(bug.rgb);
            #endif
            c.rgb = bug.rgb;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // Generalized split screen debug view.
        #if (STP_BUG && STP_BUG_SPLIT)
            #if (STP_POSTMAP == 0)
                if(bug.a == StpMF1_(1.0)) StpToneInvMF3(bug.rgb);
            #endif
            c.rgb = StpLerpMF3(c.rgb, bug.rgb, StpMF3_(bugH > StpMF1_(0.5) ? StpMF1_(0.0) : StpMF1_(1.0)));
        #endif
    }
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                         16-BIT PATH
//==============================================================================================================================
#if defined(STP_GPU)&&defined(STP_TAA)&&defined(STP_16BIT)
    StpH3 StpTaaDitH(StpU2 o);
//------------------------------------------------------------------------------------------------------------------------------
    StpH4 StpTaaCol4RH(StpF2 p);
    StpH4 StpTaaCol4GH(StpF2 p);
    StpH4 StpTaaCol4BH(StpF2 p);
    StpH4 StpTaaCol4AH(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    StpH4 StpTaaLum4H(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    // These are the same as the 32-bit version (just renamed).
    StpU4 StpTaaMot4H(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    StpH4 StpTaaPriFedH(StpF2 p);
//------------------------------------------------------------------------------------------------------------------------------
    #if STP_MAX_MIN
        StpH1 StpTaaPriFedMaxAH(StpF2 p);
    #endif
//==============================================================================================================================
    void StpTaaH(
    out StpH4 c, // Color (as RGB).
    out StpH4 f, // Feedback (to be stored).
    StpU2 o,     // Integer pixel offset in ouput.
    StpU4 con0,  // Constants generated by StpTaaCon().
    StpU4 con1,
    StpU4 con2,
    StpU4 con3,
    StpU4 con4) {
//------------------------------------------------------------------------------------------------------------------------------
        StpH4 bug = StpH4_(0.0);
//------------------------------------------------------------------------------------------------------------------------------
        #if defined(STP_HLSL)
            c = f = StpH4_(0.0);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 kCRcpF = StpF2_U2(con0.xy);
        StpF2 kHalfCRcpFUnjitC = StpF2_U2(con0.zw);
        StpF2 kRcpC = StpF2_U2(con1.xy);
        StpF1 kDubRcpCX = StpF1_U1(con1.z);
        StpF2 kRcpF = StpF2_U2(con2.xy);
        StpF2 kHalfRcpF = StpF2_U2(con2.zw);
        #if 1
            // 16-bit path.
            StpH2 kKRcpF = StpH2_U1(con1.w);
            StpH2 kGrain = StpH2_U1(con3.z);
        #else
            StpF2 kKRcpF = StpF2_U2(con3.xy);
            StpF2 kGrain = StpF2_U2(con4.xy);
        #endif
        StpF2 kF = StpF2_U2(con4.zw);
//------------------------------------------------------------------------------------------------------------------------------
        StpH3 dit = StpTaaDitH(o);
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 oI = StpF2(o);
        StpF2 oC = oI * kCRcpF + kHalfCRcpFUnjitC;
        StpF2 oCNW = floor(oC + StpF2_(-0.5));
        StpF2 oC4 = oCNW * kRcpC + kRcpC;
//------------------------------------------------------------------------------------------------------------------------------
        StpH4 c4L = StpTaaLum4H(oC4);
//------------------------------------------------------------------------------------------------------------------------------
        StpU4 m4 = StpTaaMot4H(oC4);
        StpH4 c4R = StpTaaCol4RH(oC4);
        StpH4 c4G = StpTaaCol4GH(oC4);
        StpH4 c4B = StpTaaCol4BH(oC4);
        StpH4 c4A = StpTaaCol4AH(oC4);
//------------------------------------------------------------------------------------------------------------------------------
        #if ((STP_BUG == 1) || (STP_BUG == 21) || (STP_BUG == 22))
            bug.rgb = StpH3(c4R.x, c4G.x, c4B.x);
            bug.rgb *= bug.rgb;
            #if (STP_BUG == 1)
                bug.a = StpH1_(1.0);
            #endif
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        StpH1 bugH = StpH1_(oC4.x);
//------------------------------------------------------------------------------------------------------------------------------
        StpH2 rP = StpH2(oC - oCNW) - StpH2_(0.5);
        StpF2 oF = oI * kRcpF + kHalfRcpF;
//==============================================================================================================================
        StpU1 m4min = min(StpMin3U1(m4.x, m4.y, m4.z), m4.w);
        StpU1 m4max = max(StpMax3U1(m4.x, m4.y, m4.z), m4.w);
        StpU1 m4mid = (m4max >> 1u) + (m4min >> 1u);
        StpU1 m1 = 
            (((m4.x <= m4mid ? 1u : 0u) +
              (m4.y <= m4mid ? 1u : 0u) +
              (m4.z <= m4mid ? 1u : 0u) +
              (m4.w <= m4mid ? 1u : 0u)) > 1u) ? m4min :
            min((rP.x - rP.y) < StpF1_(0.0) ? m4.x : m4.z,
                (rP.x + rP.y) < StpF1_(1.0) ? m4.w : m4.y);
        StpF4 avoidNear;
        avoidNear.x = (m4.x < m1) ? StpF1_(STP_AVOID_NEAR) : StpF1_(1.0);
        avoidNear.y = (m4.y < m1) ? StpF1_(STP_AVOID_NEAR) : StpF1_(1.0);
        avoidNear.z = (m4.z < m1) ? StpF1_(STP_AVOID_NEAR) : StpF1_(1.0);
        avoidNear.w = (m4.w < m1) ? StpF1_(STP_AVOID_NEAR) : StpF1_(1.0);
        StpP1 rsp = m1 == 0u;
        StpF2 mXY;
        StpMvUnpackVF(mXY, m1);
//------------------------------------------------------------------------------------------------------------------------------
        #if ((STP_BUG == 2) || (STP_BUG == 3) || (STP_BUG == 4))
            StpF2 bugV; StpF1 bugZ; StpF2 bugE; StpMvUnpackE(bugZ, bugV, bugE, m4.w); StpP1 bugR = m4.w == 0u;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        StpH2 biXY = StpH2(StpFractF2(mXY * kF));
        biXY = max(biXY, StpH2_(1.0) - biXY);
        StpH1 bi = biXY.x * biXY.y;
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 oFF = oF - mXY;
        StpH4 cF = StpTaaPriFedH(oFF);
        #if STP_ERODE
            cF.a = StpTaaPriFedMaxAH(oFF);
        #endif
//==============================================================================================================================
        #if (STP_BUG == 2)
            bug.r = bug.g = StpH1_(bugZ);
            bug.b = StpH1_(bugZ) * StpH1_(0.75) + c4G.w * StpH1_(0.25);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 3)
            bug.rg = StpH2_(1.0) - exp2(abs(StpH2(bugV)) * StpH2_(-32.0));
            bug.b = c4G.w * StpH1_(0.25);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 9)
            bug.rg = StpH2_(1.0) - exp2(abs(StpH2(mXY)) * StpH2_(-32.0));
            bug.b = c4G.w * StpH1_(0.25);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 4)
            bug.r = bugR ? StpH1_(1.0) : StpH1_(0.0);
            bug.g = StpH1_(0.0);
            bug.b = c4G.w * StpH1_(0.25);
        #endif
//==============================================================================================================================
        StpH4 clean = StpSatH4(c4A * StpH4_(255.0 / 16.0));
        StpH4 ne = StpSatH4(c4A * StpH4_(255.0 / (255.0 - 16.0)) - StpH4_((255.0 / (255.0 - 16.0)) * (1.0 / 16.0)));
        StpH1 neAvg = StpH1_(0.25) * ne.x + StpH1_(0.25) * ne.y + StpH1_(0.25) * ne.z + StpH1_(0.25) * ne.w;
//------------------------------------------------------------------------------------------------------------------------------
        StpH2 xnyL = max(StpMax3H2(StpH2(c4L.x, -c4L.x), StpH2(c4L.y, -c4L.y), StpH2(c4L.z, -c4L.z)), StpH2(c4L.w, -c4L.w));
        StpH1 me = xnyL.x + xnyL.y;
        StpH1 bugM = me;
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 5)
            bug.rgb = StpH3(clean.w, ne.w * ne.w, c4G.w * StpH1_(0.25));
            bug.r = StpH1_(1.0) - bug.r;
            bug.r *= bug.r;
            if(rsp) bug.b = StpH1_(1.0);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 6)
            bug.rgb = StpH3((ne.x + ne.y + ne.z + ne.w) * StpH1_(0.25), bugM, StpH1_(0.0));
            bug.rg *= bug.rg;
        #endif
//==============================================================================================================================
        StpH1 maxB = StpH1_(STP_SHARP_FLICKER) * neAvg;
        maxB *= bi;
        maxB = (maxB - StpH1_(1.0)) * rcp(maxB);
        maxB = clamp(maxB, StpH1_(4.0 / 5.0), StpH1_(STP_FEED_MAX));
        StpH1 cFa2 = cF.a * cF.a;
        StpH1 cFa4 = cFa2 * cFa2;
        StpH1 blnU = (((maxB - StpH1_(0.75)) * cFa4 * cFa2 + StpH1_(-0.375)) * cF.a + StpH1_(0.625)) * cF.a + StpH1_(0.5);
//------------------------------------------------------------------------------------------------------------------------------
        if(rsp) blnU = StpH1_(0.0);
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 20)
            bug.r = StpH1_(1.0) - blnU * StpH1_(1.0 / STP_FEED_MAX); bug.gb = bug.rr;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        ne = max(ne, StpH4_(me));
//==============================================================================================================================
        StpH4 n4Min = StpSatH4(c4L - ne * clean);
        StpH4 n4Max = StpSatH4(c4L + ne * clean);
//------------------------------------------------------------------------------------------------------------------------------
        n4Min *= n4Min;
        n4Max *= n4Max;
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 7)
            bug.rgb = n4Max.wzy;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 8)
            bug.rgb = n4Min.wzy;
        #endif
//==============================================================================================================================
        c4R *= c4R;
        c4G *= c4G;
        c4B *= c4B;
        StpH1 f1Lg = StpMax3H1(cF.r, cF.g, cF.b);
        cF.rgb *= cF.rgb;
//------------------------------------------------------------------------------------------------------------------------------
        StpH1 f1L = StpMax3H1(cF.r, cF.g, cF.b);
        StpH4 f4C = clamp(StpH4_(f1L), n4Min, n4Max);
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 11)
            bug.rgb = f4C.wzy;
        #endif
//==============================================================================================================================
        c4L = StpMax3H4(c4R, c4G, c4B);
        c4L = clamp(c4L, n4Min, n4Max);
        StpH4 bln = StpSatH4((f4C - c4L) * StpPrxLoRcpH4(StpH4_(f1L) - c4L));
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 12)
            bug.rgb = bln.wzy;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        bln = min(bln, StpH4_(blnU));
        StpH2 sharp2 = min(bln.xy, bln.zw);
        StpH1 sharp = min(sharp2.x, sharp2.y);
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 15)
            bug.rgb = bln.wzy;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_BUG_KILL_FEED
            bln = StpH4_(0.0);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        StpH4 blnI = StpH4_(1.0) - bln;
        StpH4 b4R = c4R * blnI + StpH4_(cF.r) * bln;
        StpH4 b4G = c4G * blnI + StpH4_(cF.g) * bln;
        StpH4 b4B = c4B * blnI + StpH4_(cF.b) * bln;
//==============================================================================================================================
        StpH4 b4L = StpMax3H4(b4R, b4G, b4B);
        StpH4 b4Lg = StpPrxLoSqrtH4(b4L);
        StpH4 anti = b4Lg - StpH4_(f1Lg);
//------------------------------------------------------------------------------------------------------------------------------
        StpH2 rD = StpH2(b4Lg.z - b4Lg.x, b4Lg.y - b4Lg.w);
        rD = StpH2(-rD.y, rD.y) + rD.xx;
        StpH2 rRcpD = StpPrxLoRcpH2(rD);
        rRcpD = min(StpH2_(32768.0), rRcpD);
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 14)
            bug.rb = abs(rD * StpH2_(StpRcpH1(sqrt(rD.x * rD.x + rD.y * rD.y + StpH1_(1.0 / 32768.0)))));
            bug.g = StpH1_(0.0);
            bug.rgb *= bug.rgb;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        StpH2 rPM = rP * StpH2_(2.0) - StpH2_(1.0);
        StpH2 rTN2 =  rRcpD - rPM * rRcpD;
        StpH2 rTP2 = -rRcpD - rPM * rRcpD;
        StpH2 rTNPX = max(StpH2(rTN2.x, -rTN2.x), StpH2(rTP2.x, -rTP2.x));
        StpH2 rTNPY = max(StpH2(rTN2.y, -rTN2.y), StpH2(rTP2.y, -rTP2.y));
        StpH2 rTNP = max(-rTNPX, -rTNPY);
        StpH2 rP0 = StpSatH2(rP + rD * StpH2_(rTNP.x) * StpH2_(-0.5));
        StpH2 rP1 = StpSatH2(rP + rD * StpH2_(rTNP.y) * StpH2_( 0.5));
//------------------------------------------------------------------------------------------------------------------------------
        StpH2 rT2;
        rT2.y = StpSatH1(abs(rTNP.x) * StpPrxLoRcpH1(abs(rTNP.x) + abs(rTNP.y)));
        rT2.x = StpH1_(1.0) - rT2.y;
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 16)
            bug.r = rP0.x;
            bug.g = rP0.y;
            bug.b = rT2.x;
            bug.rgb *= bug.rgb;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        StpH4 wP0,wP1;
        wP0 = StpH4_(1.0) - max(
            StpH4(0.0, 1.0, 1.0, 0.0) + StpH4( rP0.x, -rP0.x, -rP0.x, rP0.x),
            StpH4(1.0, 1.0, 0.0, 0.0) + StpH4(-rP0.y, -rP0.y,  rP0.y, rP0.y));
        wP1 = StpH4_(1.0) - max(
            StpH4(0.0, 1.0, 1.0, 0.0) + StpH4( rP1.x, -rP1.x, -rP1.x, rP1.x),
            StpH4(1.0, 1.0, 0.0, 0.0) + StpH4(-rP1.y, -rP1.y,  rP1.y, rP1.y));
//------------------------------------------------------------------------------------------------------------------------------
        StpH4 wI = wP0 * StpH4_(rT2.x) + wP1 * StpH4_(rT2.y);
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 13)
            bug.r = bug.g = bug.b = StpH1_(1.0) - sharp;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_AKS
            StpH4 wIS = wI * wI;
            wIS = wIS * wI - wI;
            wI = wI + wIS * StpH4_(sharp);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        anti = StpPrxLoRcpH4(StpH4_(1.0 / STP_ANTI_LIM) + anti * anti);
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_SHAPE_ANTI
            StpH1 antiM = min(StpMin3H1(anti.x, anti.y, anti.z), anti.w);
            anti += StpSignedH4(StpH4_(antiM) - anti) * StpH4_(StpH1_(STP_PUSH / STP_FEED_MAX) * sharp);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 17)
            bug.rgb = anti.wzy * StpH3_(1.0 / 32768.0);
        #endif
        #if STP_BUG_KILL_ANTI
            rsp = true;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        if(!rsp) wI *= anti;
        wI *= avoidNear;
        #if 1
            // This is used for the packed 16-bit path.
            StpH2 wI2 = wI.xy + wI.zw;
            StpH1 wI1 = wI2.x + wI2.y;
            StpH1 wIR = StpRcpH1(wI1);
            wI *= StpH4_(wIR);
            StpH2 cBR = (b4R.xy * wI.xy) + (b4R.zw * wI.zw);
            StpH2 cBG = (b4G.xy * wI.xy) + (b4G.zw * wI.zw);
            StpH2 cBB = (b4B.xy * wI.xy) + (b4B.zw * wI.zw);
            StpH2 cAB = (bln.xy * wI.xy) + (bln.zw * wI.zw);
            f = StpH4(cBR.x + cBR.y, cBG.x + cBG.y, cBB.x + cBB.y, cAB.x + cAB.y);
            c.rgb = f.rgb;
        #else
            StpH1 wIR = StpRcpH1(wI.x + wI.y + wI.z + wI.w);
            wI *= StpH4_(wIR);
            f = StpH4(b4R.x * wI.x + b4R.y * wI.y + b4R.z * wI.z + b4R.w * wI.w,
                      b4G.x * wI.x + b4G.y * wI.y + b4G.z * wI.z + b4G.w * wI.w,
                      b4B.x * wI.x + b4B.y * wI.y + b4B.z * wI.z + b4B.w * wI.w,
                      bln.x * wI.x + bln.y * wI.y + bln.z * wI.z + bln.w * wI.w);
            c.rgb = f.rgb;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        f.a = StpRcpH1(StpH1(1.0) - f.a) + (dit.x * StpH1_(0.5) - StpH1_(0.5));
        f.a = StpSatH1(StpH1_(1.0 / 3.0) * f.a - StpH1_(1.0 / 3.0));
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 18)
            bug.rgb = wI.wzy;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 19)
            bug.rgb = cF.rgb;
            bug.a = StpH1_(1.0);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 23)
            bug.g = StpH1_(0.0);
            bug.r = StpH1_(1.0) - cF.a;
            bug.b = cF.b * StpH1_(1.0 / 8.0);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        f.rgb = StpRgbGamDit10H3(f.rgb, dit.x);
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_GRAIN == 1)
            c.rgb += min(StpH3_(1.0) - c.rgb, c.rgb) * StpH3_(dit.x * kGrain.x + kGrain.y);
        #endif
        #if (STP_GRAIN == 3)
            c.rgb += min(StpH3_(1.0) - c.rgb, c.rgb) * (dit * StpH3_(kGrain.x) + StpH3_(kGrain.y));
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_POSTMAP == 0)
            StpToneInvH3(c.rgb);
        #endif
//==============================================================================================================================
        #if (STP_BUG && (STP_BUG_SPLIT == 0))
            #if (STP_POSTMAP == 0)
                if(bug.a == StpH1_(1.0)) StpToneInvH3(bug.rgb);
            #endif
            c.rgb = bug.rgb;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG && STP_BUG_SPLIT)
            #if (STP_POSTMAP == 0)
                if(bug.a == StpH1_(1.0)) StpToneInvH3(bug.rgb);
            #endif
            c.rgb = StpLerpH3(c.rgb, bug.rgb, StpH3_(bugH > StpH1_(0.5) ? StpH1_(0.0) : StpH1_(1.0)));
        #endif
    }
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//
//                                                     CLEANER ENTRY POINT
//
//------------------------------------------------------------------------------------------------------------------------------
// CLEANER LOGIC
// =============
// Works with a 3x3 neighborhood of {r,g,b} color loaded from feedback in gamma 2.0, and convergence in alpha.
// Alpha for 10:10:10:2 is {0 := new, 1 := converged}.
// This is sampled directly from the feedback image.
// Neighborhood,
//     b
//   d e f
//     h
// The cleaner applies some amount of lowpass filtering (to the less converged).
// Also applies some amount of highpass filtering (to the converged) using logic based on AMD's RCAS.
// The amount of filtering is driven by non-linear filters.
// This is to avoid highpassing thin converged features surrounded by non-converged (increasing aliasing).
// The amount of lowpass is complex.
// This logic needs to erode high frequency non-converged regions and then dilate that result.
//==============================================================================================================================
#if defined(STP_GPU)&&defined(STP_CLN)&&defined(STP_32BIT)
    // Dither value {0 to 1} this should be output pixel frequency spatial temporal blue noise.
    // Only used if STP_GRAIN is used, and only 'dit.x' if STP_GRAIN=1, 'dit.xyz' if STP_GRAIN=3.
    StpMF3 StpClnDitF(StpU2 o);
    // Callback, load from feedback.
    StpMF4 StpClnFedF(StpU2 o);
//------------------------------------------------------------------------------------------------------------------------------
    void StpClnF(
    out StpMF4 c, // Color (as RGB).
    StpU2 o,      // Integer pixel offset in ouput.
    StpU4 con0,   // Constants generated by StpClnCon().
    StpU4 con1) {
//------------------------------------------------------------------------------------------------------------------------------
        #if defined(STP_HLSL)
            // Avoid compiler warning as error.
            c = StpMF4_(0.0);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // Rename constants.
        #if 0
            // 16-bit path.
            StpH2 kGrain = StpH2_U1(con1.x);
            StpH1 kSharp = StpH2_U1(con1.y).x;
        #else
            // 32-bit path.
            StpMF2 kGrain = StpMF2(StpF2_U2(con0.xy));
            StpMF1 kSharp = StpMF1(StpF1_U1(con0.z));
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        StpMF3 dit = StpClnDitF(o);
//------------------------------------------------------------------------------------------------------------------------------
        // Load color.
        //   b
        // d e f
        //   h
        StpMF4 cB = StpClnFedF(o + StpU2( 0, -1));
        StpMF4 cD = StpClnFedF(o + StpU2(-1,  0));
        StpMF4 cE = StpClnFedF(o + StpU2( 0,  0));
        StpMF4 cF = StpClnFedF(o + StpU2( 1,  0));
        StpMF4 cH = StpClnFedF(o + StpU2( 0,  1));
//------------------------------------------------------------------------------------------------------------------------------
        // Mask, amount of {sharp, lowpass}.
        // Sharp mask is easy, just limit by minimum convergence.
        // Lowpass mask attempts some safe dilation (avoiding dilating thin features).
        StpMF2 amt;
        amt.x = StpMin3MF1(cB.a, cD.a, StpMin3MF1(cE.a, cF.a, cH.a));
        amt.y = StpMax3MF1(cE.a, StpMin3MF1(cD.a, cB.a, cF.a), 
            StpMax3MF1(StpMin3MF1(cB.a, cF.a, cH.a), StpMin3MF1(cF.a, cH.a, cD.a), StpMin3MF1(cH.a, cD.a, cB.a)));
        StpMF3 bug;
        bug.xy = amt;
        bug.z = StpMF1_(1.0);
//------------------------------------------------------------------------------------------------------------------------------
        // Decode gamma 2.0 to linear.
        cB.rgb *= cB.rgb;
        cD.rgb *= cD.rgb;
        cE.rgb *= cE.rgb;
        cF.rgb *= cF.rgb;
        cH.rgb *= cH.rgb;
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_SHARP
            // Modified RCAS is applied first.
            // Shape first.
            amt.x *= amt.x;
            amt.x *= amt.x;
            amt.x *= kSharp;
            // This logic is different than RCAS.
            StpMF2 lBD = StpMax3MF2(StpMF2(cB.r, cD.r), StpMF2(cB.g, cD.g), StpMF2(cB.b, cD.b));
            StpMF2 lHF = StpMax3MF2(StpMF2(cH.r, cF.r), StpMF2(cH.g, cF.g), StpMF2(cH.b, cF.b));
            StpMF1 lE  = StpMax3MF1(       cE.r,               cE.g,               cE.b       );
            StpMF2 nz2 = StpMF2_(0.25) * lBD + StpMF2_(0.25) * lHF - StpMF2_(lE) * StpMF2_(0.5);
            StpMF1 nz = abs(nz2.x + nz2.y);
            nz = StpSatMF1(nz * StpPrxMedRcpMF1(
                StpMax3MF1(StpMax3MF1(lBD.x, lBD.y, lE), lHF.x, lHF.y) -
                StpMin3MF1(StpMin3MF1(lBD.x, lBD.y, lE), lHF.x, lHF.y)));
            nz = StpSatMF1(StpMF1_(-1.0) * nz + StpMF1_(1.0));
            // Min and max of ring.
            StpMF3 mn4 = min(StpMin3MF3(cB.rgb, cD.rgb, cF.rgb), cH.rgb);
            StpMF3 mx4 = max(StpMax3MF3(cB.rgb, cD.rgb, cF.rgb), cH.rgb);
            // Limiters, these need to be high precision RCPs.
            StpMF3 hitMin =                 min(mn4, cE.rgb)  * StpRcpMF3(StpMF3_(4.0) * mx4);
            StpMF3 hitMax = (StpMF3_(1.0) - max(mx4, cE.rgb)) * StpRcpMF3(StpMF3_(4.0) * mn4 - StpMF3_(4.0));
            StpMF3 lobe3 = max(-hitMin, hitMax);
            StpMF1 lobe = max(StpMF1_(-STP_RCAS_LIMIT), min(StpMax3MF1(lobe3.r, lobe3.g, lobe3.b), StpMF1_(0.0))) * amt.x;
            #if (STP_SHARP == 2)
                lobe *= nz;
                bug.z = nz;
            #endif
            // Resolve, which needs the medium precision rcp approximation to avoid visible tonality changes.
            StpMF1 rcpL = StpPrxMedRcpMF1(StpMF1_(4.0) * lobe + StpMF1_(1.0));
            lobe *= rcpL;
            c.rgb = cB.rgb * StpMF3_(lobe) +
                cD.rgb * StpMF3_(lobe) +
                cF.rgb * StpMF3_(lobe) +
                cH.rgb * StpMF3_(lobe) +
                cE.rgb * StpMF3_(rcpL);
        #else
            bug.x = StpMF1_(0.0);
            c.rgb = cE.rgb;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        // Compute the 'lowpass'.
        StpMF3 cL = StpSatMF3(cB.rgb * StpMF3_(2.0 / 9.0) + cD.rgb * StpMF3_(2.0 / 9.0) +
                              cE.rgb * StpMF3_(1.0 / 9.0) +
                              cF.rgb * StpMF3_(2.0 / 9.0) + cH.rgb * StpMF3_(2.0 / 9.0));
        // Apply lowpass (possibly applied over sharpening).
        amt.x = StpMF1_(1.0) - amt.y;
        c.rgb = cL.rgb * StpMF3_(amt.x) + c.rgb * StpMF3_(amt.y);
//------------------------------------------------------------------------------------------------------------------------------
        // Optionally apply grain.
        #if (STP_GRAIN == 1)
            // Slightly fast monochromatic version.
            c.rgb += min(StpMF3_(1.0) - c.rgb, c.rgb) * StpMF3_(dit.x * kGrain.x + kGrain.y);
        #endif
        #if (STP_GRAIN == 3)
            c.rgb += min(StpMF3_(1.0) - c.rgb, c.rgb) * (dit * StpMF3_(kGrain.x) + StpMF3_(kGrain.y));
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_POSTMAP == 0)
            // Decode so result can be used after return.
            StpToneInvMF3(c.rgb);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 10)
            // Visualize masks.
            c.r = StpMF1_(1.0) - bug.y;
            c.g = StpMF1_(0.0);
            c.b = bug.x * bug.z;
        #endif
    }
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                         16-BIT PATH
//==============================================================================================================================
#if defined(STP_GPU)&&defined(STP_CLN)&&defined(STP_16BIT)
    StpH3 StpClnDitH(StpW2 o);
    StpH4 StpClnFedH(StpW2 o);
//------------------------------------------------------------------------------------------------------------------------------
    void StpClnH(
    out StpH4 c,
    StpW2 o,
    StpU4 con0,
    StpU4 con1) {
//------------------------------------------------------------------------------------------------------------------------------
        #if defined(STP_HLSL)
            c = StpH4_(0.0);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if 1
            StpH2 kGrain = StpH2_U1(con1.x);
            StpH1 kSharp = StpH2_U1(con1.y).x;
        #else
            StpF2 kGrain = StpF2_U2(con0.xy);
            StpF1 kSharp = StpF1_U1(con0.z);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        StpH3 dit = StpClnDitH(o);
//------------------------------------------------------------------------------------------------------------------------------
        StpH4 cB = StpClnFedH(o + StpW2( 0, -1));
        StpH4 cD = StpClnFedH(o + StpW2(-1,  0));
        StpH4 cE = StpClnFedH(o + StpW2( 0,  0));
        StpH4 cF = StpClnFedH(o + StpW2( 1,  0));
        StpH4 cH = StpClnFedH(o + StpW2( 0,  1));
//------------------------------------------------------------------------------------------------------------------------------
        StpH2 amt;
        amt.x = StpMin3H1(cB.a, cD.a, StpMin3H1(cE.a, cF.a, cH.a));
        amt.y = StpMax3H1(cE.a, StpMin3H1(cD.a, cB.a, cF.a), 
            StpMax3H1(StpMin3H1(cB.a, cF.a, cH.a), StpMin3H1(cF.a, cH.a, cD.a), StpMin3H1(cH.a, cD.a, cB.a)));
        StpH3 bug;
        bug.xy = amt;
        bug.z = StpH1_(1.0);
//------------------------------------------------------------------------------------------------------------------------------
        cB.rgb *= cB.rgb;
        cD.rgb *= cD.rgb;
        cE.rgb *= cE.rgb;
        cF.rgb *= cF.rgb;
        cH.rgb *= cH.rgb;
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_SHARP
            amt.x *= amt.x;
            amt.x *= amt.x;
            amt.x *= kSharp;
            StpH2 lBD = StpMax3H2(StpH2(cB.r, cD.r), StpH2(cB.g, cD.g), StpH2(cB.b, cD.b));
            StpH2 lHF = StpMax3H2(StpH2(cH.r, cF.r), StpH2(cH.g, cF.g), StpH2(cH.b, cF.b));
            StpH1 lE  = StpMax3H1(      cE.r,              cE.g,              cE.b       );
            StpH2 nz2 = StpH2_(0.25) * lBD + StpH2_(0.25) * lHF - StpH2_(lE) * StpH2_(0.5);
            StpH1 nz = abs(nz2.x + nz2.y);
            nz = StpSatH1(nz * StpPrxMedRcpH1(
                StpMax3H1(StpMax3H1(lBD.x, lBD.y, lE), lHF.x, lHF.y) -
                StpMin3H1(StpMin3H1(lBD.x, lBD.y, lE), lHF.x, lHF.y)));
            nz = StpSatH1(StpH1_(-1.0) * nz + StpH1_(1.0));
            StpH3 mn4 = min(StpMin3H3(cB.rgb, cD.rgb, cF.rgb), cH.rgb);
            StpH3 mx4 = max(StpMax3H3(cB.rgb, cD.rgb, cF.rgb), cH.rgb);
            StpH3 hitMin =                min(mn4, cE.rgb)  * StpRcpH3(StpH3_(4.0) * mx4);
            StpH3 hitMax = (StpH3_(1.0) - max(mx4, cE.rgb)) * StpRcpH3(StpH3_(4.0) * mn4 - StpH3_(4.0));
            StpH3 lobe3 = max(-hitMin, hitMax);
            StpH1 lobe = max(StpH1_(-STP_RCAS_LIMIT), min(StpMax3H1(lobe3.r, lobe3.g, lobe3.b), StpH1_(0.0))) * amt.x;
            #if (STP_SHARP == 2)
                lobe *= nz;
                bug.z = nz;
            #endif
            StpH1 rcpL = StpPrxMedRcpH1(StpH1_(4.0) * lobe + StpH1_(1.0));
            lobe *= rcpL;
            c.rgb = cB.rgb * StpH3_(lobe) +
                cD.rgb * StpH3_(lobe) +
                cF.rgb * StpH3_(lobe) +
                cH.rgb * StpH3_(lobe) +
                cE.rgb * StpH3_(rcpL);
        #else
            bug.x = StpH1_(0.0);
            c.rgb = cE.rgb;
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        StpH3 cL = StpSatH3(cB.rgb * StpH3_(2.0 / 9.0) + cD.rgb * StpH3_(2.0 / 9.0) +
                            cE.rgb * StpH3_(1.0 / 9.0) +
                            cF.rgb * StpH3_(2.0 / 9.0) + cH.rgb * StpH3_(2.0 / 9.0));
        amt.x = StpH1_(1.0) - amt.y;
        c.rgb = cL.rgb * StpH3_(amt.x) + c.rgb * StpH3_(amt.y);
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_GRAIN == 1)
            c.rgb += min(StpH3_(1.0) - c.rgb, c.rgb) * StpH3_(dit.x * kGrain.x + kGrain.y);
        #endif
        #if (STP_GRAIN == 3)
            c.rgb += min(StpH3_(1.0) - c.rgb, c.rgb) * (dit * StpH3_(kGrain.x) + StpH3_(kGrain.y));
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_POSTMAP == 0)
            StpToneInvH3(c.rgb);
        #endif
//------------------------------------------------------------------------------------------------------------------------------
        #if (STP_BUG == 10)
            c.r = StpH1_(1.0) - bug.y;
            c.g = StpH1_(0.0);
            c.b = bug.x * bug.z;
        #endif
    }
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//
//                                                      UBER ENTRY POINT
//
//------------------------------------------------------------------------------------------------------------------------------
// This shader implements either {inline,taa} and {inline,taa,cleaner} all in a single pass.
//==============================================================================================================================
#if defined(STP_GPU)&&defined(STP_UBE)&&defined(STP_32BIT)
    void StpUbeInF(StpMF4 c, StpU1 m, StpMF1 l, StpU2 o);
    void StpUbeFedF(StpMF4 f, StpU2 o);
    void StpUbeColF(StpMF4 c, StpU2 o);
//------------------------------------------------------------------------------------------------------------------------------
    void StpUbeF(
    StpU2 oT,
    StpU2 oP,
    StpU4 conUbe0,
    StpU4 conIn0,
    StpU4 conIn1,
    StpU4 conIn2,
    StpU4 conIn3,
    StpU4 conIn4,
    StpU4 conIn5,
    StpU4 conIn6,
    StpU4 conIn7,
    StpU4 conIn8,
    StpU4 conIn9,
    StpU4 conInA,
    StpU4 conInB,
    StpU4 conInC,
    StpU4 conInD,
    StpU4 conTaa0,
    StpU4 conTaa1,
    StpU4 conTaa2,
    StpU4 conTaa3,
    StpU4 conTaa4,
    StpU4 conCln0,
    StpU4 conCln1) {
//------------------------------------------------------------------------------------------------------------------------------
        StpF1 kCRcpFY = StpF1_U1(conUbe0.x);
        StpU1 kCX = conUbe0.y;
        StpU1 kSpacer = conUbe0.z;
        StpU1 kFY = conUbe0.w;
//------------------------------------------------------------------------------------------------------------------------------
        StpF2 ooo;
        ooo.x = StpF1(oT.y);
        #if STP_SEMI
            ooo.y = ooo.x + StpF1_(16.0);
        #else
            ooo.y = ooo.x + StpF1_(8.0);
        #endif
        ooo *= StpF2_(kCRcpFY);
        ooo = floor(ooo);
        StpU2 oooo = StpU2(ooo);
        #if STP_SEMI
            oooo.x = (oooo.x + StpU1_(15)) & (~StpU1_(15));
        #else
            oooo.x = (oooo.x + StpU1_(7)) & (~StpU1_(7));
        #endif
        StpU2 o;
        o.x = oT.x + oP.x;
        if((oooo.x < oooo.y) && (oT.x < kCX)) {
            o.y = oooo.x + oP.y;
            StpU2 oo = o;
            StpU1 m;
            StpF4 c;
            StpF1 l;
            StpInF(c, m, l, oo, conIn0, conIn1, conIn2, conIn3, conIn4, conIn5, conIn6, conIn7, conIn8, conIn9, conInA, conInB, conInC, conInD); StpUbeInF(c, m, l, oo);
            #if STP_SEMI
                #if STP_64
                    oo.x += StpU1_(8); StpInF(c, m, l, oo, conIn0, conIn1, conIn2, conIn3, conIn4, conIn5, conIn6, conIn7, conIn8, conIn9, conInA, conInB, conInC, conInD); StpUbeInF(c, m, l, oo);
                #endif
                oo.y += StpU1_(8); StpInF(c, m, l, oo, conIn0, conIn1, conIn2, conIn3, conIn4, conIn5, conIn6, conIn7, conIn8, conIn9, conInA, conInB, conInC, conInD); StpUbeInF(c, m, l, oo);
                #if STP_64
                    oo.x -= StpU1_(8); StpInF(c, m, l, oo, conIn0, conIn1, conIn2, conIn3, conIn4, conIn5, conIn6, conIn7, conIn8, conIn9, conInA, conInB, conInC, conInD); StpUbeInF(c, m, l, oo);
                #endif
            #endif
        }
//------------------------------------------------------------------------------------------------------------------------------
        o.y = oT.y + oP.y;
        o.y -= kSpacer;
        if(o.y < kFY) {
            StpF4 c;
            StpF4 f;
            StpU2 oo = o;
            #if (STP_USE_CLN == 0)
                StpTaaF(c, f, oo, conTaa0, conTaa1, conTaa2, conTaa3, conTaa4); StpUbeFedF(f, oo); StpUbeColF(c, StpU2(oo));
                #if STP_SEMI
                    #if STP_64
                        oo.x += StpU1_(8); StpTaaF(c, f, oo, conTaa0, conTaa1, conTaa2, conTaa3, conTaa4); StpUbeFedF(f, oo); StpUbeColF(c, StpU2(oo));
                    #endif
                    oo.y += StpU1_(8); StpTaaF(c, f, oo, conTaa0, conTaa1, conTaa2, conTaa3, conTaa4); StpUbeFedF(f, oo); StpUbeColF(c, StpU2(oo));
                    #if STP_64
                        oo.x -= StpU1_(8); StpTaaF(c, f, oo, conTaa0, conTaa1, conTaa2, conTaa3, conTaa4); StpUbeFedF(f, oo); StpUbeColF(c, StpU2(oo));
                    #endif
                #endif
            #else
                StpTaaF(c, f, oo, conTaa0, conTaa1, conTaa2, conTaa3, conTaa4); StpUbeFedF(f, oo);
                #if STP_SEMI
                    #if STP_64
                        oo.x += StpU1_(8); StpTaaF(c, f, oo, conTaa0, conTaa1, conTaa2, conTaa3, conTaa4); StpUbeFedF(f, oo);
                    #endif
                    oo.y += StpU1_(8); StpTaaF(c, f, oo, conTaa0, conTaa1, conTaa2, conTaa3, conTaa4); StpUbeFedF(f, oo);
                    #if STP_64
                        oo.x -= StpU1_(8); StpTaaF(c, f, oo, conTaa0, conTaa1, conTaa2, conTaa3, conTaa4); StpUbeFedF(f, oo);
                    #endif
                #endif
            #endif
        }
//------------------------------------------------------------------------------------------------------------------------------
        #if STP_USE_CLN
            if(o.y >= kSpacer) {
                StpMF4 c;
                o.y -= kSpacer;
                StpU2 oo = StpU2(o);
                StpClnF(c, oo, conCln0, conCln1); StpUbeColF(c, oo);
                #if STP_SEMI
                    #if STP_64
                        oo.x += StpU1_(8); StpClnF(c, oo, conCln0, conCln1); StpUbeColF(c, oo);
                    #endif
                    oo.y += StpU1_(8); StpClnF(c, oo, conCln0, conCln1); StpUbeColF(c, oo);
                    #if STP_64
                        oo.x -= StpU1_(8); StpClnF(c, oo, conCln0, conCln1); StpUbeColF(c, oo);
                    #endif
                #endif
            }
        #endif
    }
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//_____________________________________________________________.._______________________________________________________________
//==============================================================================================================================
//                                                         16-BIT PATH
//==============================================================================================================================
#if defined(STP_GPU)&&defined(STP_UBE)&&defined(STP_16BIT)
    // Store inline pass outputs.
    void StpUbeInH(StpH4 c, StpU1 m, StpH1 l, StpU2 o);
    // Store feedback.
    void StpUbeFedH(StpH4 f, StpU2 o);
    // Output from STP.
    void StpUbeColH(StpH4 c, StpW2 o);
//------------------------------------------------------------------------------------------------------------------------------
    void StpUbeH(
    StpU2 oT, // Tile position in output kernel.
    StpU2 oP, // Pixel position in output tile.
    StpU4 conUbe0, // Uber shader specific constants.
    StpU4 conIn0, // Inline function specific constants.
    StpU4 conIn1,
    StpU4 conIn2,
    StpU4 conIn3,
    StpU4 conIn4,
    StpU4 conIn5,
    StpU4 conIn6,
    StpU4 conIn7,
    StpU4 conIn8,
    StpU4 conIn9,
    StpU4 conInA,
    StpU4 conInB,
    StpU4 conInC,
    StpU4 conInD,
    StpU4 conTaa0, // TAA specific constants.
    StpU4 conTaa1,
    StpU4 conTaa2,
    StpU4 conTaa3,
    StpU4 conTaa4,
    StpU4 conCln0, // Cleaner specific constants.
    StpU4 conCln1) {
//------------------------------------------------------------------------------------------------------------------------------
        // Rename constants.
        StpF1 kCRcpFY = StpF1_U1(conUbe0.x);
        StpU1 kCX = conUbe0.y;
        StpU1 kSpacer = conUbe0.z;
        StpU1 kFY = conUbe0.w;
//------------------------------------------------------------------------------------------------------------------------------
        // Optionally run the inline function.
        StpF2 ooo;
        ooo.x = StpF1(oT.y); // Beginning of output tile.
        #if STP_SEMI
            ooo.y = ooo.x + StpF1_(16.0); // End of output tile.
        #else
            ooo.y = ooo.x + StpF1_(8.0);
        #endif
        // Convert output tile pixel coords to input tile coords.
        ooo *= StpF2_(kCRcpFY);
        // Check if first line of a tile is in window, if so, do full tile row.
        ooo = floor(ooo);
        StpU2 oooo = StpU2(ooo);
        // Find the first possible line by rounding up.
        #if STP_SEMI
            oooo.x = (oooo.x + StpU1_(15)) & (~StpU1_(15));
        #else
            oooo.x = (oooo.x + StpU1_(7)) & (~StpU1_(7));
        #endif
        // If line is in window, then need to process tile row.
        // Since input is <= output have to do width check.
        // First build common part of composite position (for all three possible kernels).
        StpU2 o;
        o.x = oT.x + oP.x;
        if((oooo.x < oooo.y) && (oT.x < kCX)) {
            // Built composite 2D coordinate for y.
            o.y = oooo.x + oP.y;
            StpU2 oo = o;
            StpU1 m;
            StpH4 c;
            StpH1 l;
            StpInH(c, m, l, oo, conIn0, conIn1, conIn2, conIn3, conIn4, conIn5, conIn6, conIn7, conIn8, conIn9, conInA, conInB, conInC, conInD); StpUbeInH(c, m, l, oo);
            #if STP_SEMI
                #if STP_64
                    oo.x += StpU1_(8); StpInH(c, m, l, oo, conIn0, conIn1, conIn2, conIn3, conIn4, conIn5, conIn6, conIn7, conIn8, conIn9, conInA, conInB, conInC, conInD); StpUbeInH(c, m, l, oo);
                #endif
                oo.y += StpU1_(8); StpInH(c, m, l, oo, conIn0, conIn1, conIn2, conIn3, conIn4, conIn5, conIn6, conIn7, conIn8, conIn9, conInA, conInB, conInC, conInD); StpUbeInH(c, m, l, oo);
                #if STP_64
                    oo.x -= StpU1_(8); StpInH(c, m, l, oo, conIn0, conIn1, conIn2, conIn3, conIn4, conIn5, conIn6, conIn7, conIn8, conIn9, conInA, conInB, conInC, conInD); StpUbeInH(c, m, l, oo);
                #endif
            #endif
        }
//------------------------------------------------------------------------------------------------------------------------------
        // Fix composite position for y.
        o.y = oT.y + oP.y;
        // Run the TAA.
        // First subtract the spacer amount.
        o.y -= kSpacer;
        // Note 'kSpacer.y' is the height of the feedback/output frame.
        // This is an unsigned comparison so "negative" values look huge.
        // So only need to check that 'y' is in range of the final frame size.
        if(o.y < kFY) {
            StpH4 c;
            StpH4 f;
            StpU2 oo = o;
            #if (STP_USE_CLN == 0)
                StpTaaH(c, f, oo, conTaa0, conTaa1, conTaa2, conTaa3, conTaa4); StpUbeFedH(f, oo); StpUbeColH(c, StpW2(oo));
                // Semi-persistent will do a 16x16 tile with 4 8x8 wave tiles if STP_64 else 2 16x8 tiles (for Qualcomm only).
                #if STP_SEMI
                    // This goes if a {64,1,1} workgroup but is skipped if a {128,1,1} workgroup.
                    #if STP_64
                        oo.x += StpU1_(8); StpTaaH(c, f, oo, conTaa0, conTaa1, conTaa2, conTaa3, conTaa4); StpUbeFedH(f, oo); StpUbeColH(c, StpW2(oo));
                    #endif
                    oo.y += StpU1_(8); StpTaaH(c, f, oo, conTaa0, conTaa1, conTaa2, conTaa3, conTaa4); StpUbeFedH(f, oo); StpUbeColH(c, StpW2(oo));
                    #if STP_64
                        oo.x -= StpU1_(8); StpTaaH(c, f, oo, conTaa0, conTaa1, conTaa2, conTaa3, conTaa4); StpUbeFedH(f, oo); StpUbeColH(c, StpW2(oo));
                    #endif
                #endif
            #else
                StpTaaH(c, f, oo, conTaa0, conTaa1, conTaa2, conTaa3, conTaa4); StpUbeFedH(f, oo);
                #if STP_SEMI
                    #if STP_64
                        oo.x += StpU1_(8); StpTaaH(c, f, oo, conTaa0, conTaa1, conTaa2, conTaa3, conTaa4); StpUbeFedH(f, oo);
                    #endif
                    oo.y += StpU1_(8); StpTaaH(c, f, oo, conTaa0, conTaa1, conTaa2, conTaa3, conTaa4); StpUbeFedH(f, oo);
                    #if STP_64
                        oo.x -= StpU1_(8); StpTaaH(c, f, oo, conTaa0, conTaa1, conTaa2, conTaa3, conTaa4); StpUbeFedH(f, oo);
                    #endif
                #endif
            #endif
        }
//------------------------------------------------------------------------------------------------------------------------------
        // Run the cleaner.
        #if STP_USE_CLN
            // Note 'o.y' already had one spacer amount subtracted off prior, so this checks if in the second region.
            // And there is no need for out of bounds check, because no uber sub-shader runs after this one.
            if(o.y >= kSpacer) {
                StpH4 c;
                o.y -= kSpacer;
                StpW2 oo = StpW2(o);
                StpClnH(c, oo, conCln0, conCln1); StpUbeColH(c, oo);
                #if STP_SEMI
                    #if STP_64
                        oo.x += StpW1_(8); StpClnH(c, oo, conCln0, conCln1); StpUbeColH(c, oo);
                    #endif
                    oo.y += StpW1_(8); StpClnH(c, oo, conCln0, conCln1); StpUbeColH(c, oo);
                    #if STP_64
                        oo.x -= StpW1_(8); StpClnH(c, oo, conCln0, conCln1); StpUbeColH(c, oo);
                    #endif
                #endif
            }
        #endif
    }
#endif
