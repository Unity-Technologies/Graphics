// ---------------------------------------------------
// Options
// ---------------------------------------------------

#define SHARPEN_ALPHA 0 // switch to 1 if you want to enable TAA sharpenning on alpha channel

// History sampling options
#define BILINEAR 0
#define BICUBIC_5TAP 1

/// Neighbourhood sampling options
#define PLUS 0    // Faster! Can allow for read across twice (paying cost of 2 samples only)
#define CROSS 1   // Can only do one fast read diagonal
#define SMALL_NEIGHBOURHOOD_SHAPE PLUS

// Neighbourhood AABB options
#define MINMAX 0
#define VARIANCE 1

// Central value filtering options
#define NO_FILTERING 0
#define BOX_FILTER 1
#define BLACKMAN_HARRIS 2
#define UPSAMPLE 3

// Clip option
#define DIRECT_CLIP 0
#define BLEND_WITH_CLIP 1
#define SIMPLE_CLAMP 2

// Motion Vector Dilation Mode
#define DEPTH_DILATION 0
#define LARGEST_MOTION_VEC 1


// Upsample pixel confidence factor (used for tuning the blend factor when upsampling)
// See A Survey of Temporal Antialiasing Techniques [Yang et al 2020], section 5.1
#define GAUSSIAN_WEIGHT 0
#define BOX_REJECT 1
#define CONFIDENCE_FACTOR BOX_REJECT
