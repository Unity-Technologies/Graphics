
// Blue noise sampling dimensions for various effects
#define RAND_DIM_AA_X               0    // used for jittering within a texel
#define RAND_DIM_AA_Y               1
#define RAND_DIM_SURF_SCATTER_X     2    // used for determining the scattering direction when we have hit a surface
#define RAND_DIM_SURF_SCATTER_Y     3
#define RAND_DIM_TRANSMISSION       4    // used to determine if a ray is transmitted or reflected
#define RAND_DIM_RUSSIAN_ROULETTE   5    // used for Russian roulette
#define RAND_DIM_JITTERED_SHADOW_X  6    // Used for jittered shadows, ideally should be removed and the jitter handled in the SampleLightShape code instead but that's a refactor for another day
#define RAND_DIM_JITTERED_SHADOW_Y  7
#define RAND_DIM_LIGHT_SELECTION    8    // used for light selection (arbitrarily many dimensions after this point)

#define RUSSIAN_ROULETTE_MIN_BOUNCES 2 // Min bounces for russian roulette. Matches PLM_DEFAULT_MIN_BOUNCES

// Currently we use maximum 5 random numbers per light:
// 1 number to select a (mesh) light,
// 1 to select a triangle index,
// 2 numbers to sample the area/solid angle
// 1 number for RIS
// the total light samples need to be even, so we round to 6
#define RAND_SAMPLES_PER_LIGHT    6

// Max number of lights to evaluate per bounce
#define MAX_LIGHT_EVALUATIONS 8

// The number of dimensions used per bounce (depends on the number of light evaluations)
#define QRNG_SAMPLES_PER_BOUNCE (RAND_DIM_LIGHT_SELECTION + RAND_SAMPLES_PER_LIGHT * MAX_LIGHT_EVALUATIONS)
#define QRNG_METHOD_SOBOL
#define QRNG_SOBOL_02
#include "Packages/com.unity.render-pipelines.core/Runtime/PathTracing/PathTracingSampler.hlsl"
