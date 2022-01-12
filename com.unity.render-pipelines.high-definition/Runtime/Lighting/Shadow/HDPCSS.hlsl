#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

#define DISK_SAMPLE_COUNT 64
// Fibonacci Spiral Disk Sampling Pattern
// https://people.irisa.fr/Ricardo.Marques/articles/2013/SF_CGF.pdf
//
// Normalized direction vector portion of fibonacci spiral can be baked into a LUT, regardless of sampleCount.
// This allows us to treat the directions as a progressive sequence, using any sampleCount in range [0, n <= LUT_LENGTH]
// the radius portion of spiral construction is coupled to sample count, but is fairly cheap to compute at runtime per sample.
// Generated (in javascript) with:
// var res = "";
// for (var i = 0; i < 64; ++i)
// {
//     var a = Math.PI * (3.0 - Math.sqrt(5.0));
//     var b = a / (2.0 * Math.PI);
//     var c = i * b;
//     var theta = (c - Math.floor(c)) * 2.0 * Math.PI;
//     res += "float2 (" + Math.cos(theta) + ", " + Math.sin(theta) + "),\n";
// }

static const float2 fibonacciSpiralDirection[DISK_SAMPLE_COUNT] =
{
    float2 (1, 0),
    float2 (-0.7373688780783197, 0.6754902942615238),
    float2 (0.08742572471695988, -0.9961710408648278),
    float2 (0.6084388609788625, 0.793600751291696),
    float2 (-0.9847134853154288, -0.174181950379311),
    float2 (0.8437552948123969, -0.5367280526263233),
    float2 (-0.25960430490148884, 0.9657150743757782),
    float2 (-0.46090702471337114, -0.8874484292452536),
    float2 (0.9393212963241182, 0.3430386308741014),
    float2 (-0.924345556137805, 0.3815564084749356),
    float2 (0.423845995047909, -0.9057342725556143),
    float2 (0.29928386444487326, 0.9541641203078969),
    float2 (-0.8652112097532296, -0.501407581232427),
    float2 (0.9766757736281757, -0.21471942904125949),
    float2 (-0.5751294291397363, 0.8180624302199686),
    float2 (-0.12851068979899202, -0.9917081236973847),
    float2 (0.764648995456044, 0.6444469828838233),
    float2 (-0.9991460540072823, 0.04131782619737919),
    float2 (0.7088294143034162, -0.7053799411794157),
    float2 (-0.04619144594036213, 0.9989326054954552),
    float2 (-0.6407091449636957, -0.7677836880006569),
    float2 (0.9910694127331615, 0.1333469877603031),
    float2 (-0.8208583369658855, 0.5711318504807807),
    float2 (0.21948136924637865, -0.9756166914079191),
    float2 (0.4971808749652937, 0.8676469198750981),
    float2 (-0.952692777196691, -0.30393498034490235),
    float2 (0.9077911335843911, -0.4194225289437443),
    float2 (-0.38606108220444624, 0.9224732195609431),
    float2 (-0.338452279474802, -0.9409835569861519),
    float2 (0.8851894374032159, 0.4652307598491077),
    float2 (-0.9669700052147743, 0.25489019011123065),
    float2 (0.5408377383579945, -0.8411269468800827),
    float2 (0.16937617250387435, 0.9855514761735877),
    float2 (-0.7906231749427578, -0.6123030256690173),
    float2 (0.9965856744766464, -0.08256508601054027),
    float2 (-0.6790793464527829, 0.7340648753490806),
    float2 (0.0048782771634473775, -0.9999881011351668),
    float2 (0.6718851669348499, 0.7406553331023337),
    float2 (-0.9957327006438772, -0.09228428288961682),
    float2 (0.7965594417444921, -0.6045602168251754),
    float2 (-0.17898358311978044, 0.9838520605119474),
    float2 (-0.5326055939855515, -0.8463635632843003),
    float2 (0.9644371617105072, 0.26431224169867934),
    float2 (-0.8896863018294744, 0.4565723210368687),
    float2 (0.34761681873279826, -0.9376366819478048),
    float2 (0.3770426545691533, 0.9261958953890079),
    float2 (-0.9036558571074695, -0.4282593745796637),
    float2 (0.9556127564793071, -0.2946256262683552),
    float2 (-0.50562235513749, 0.8627549095688868),
    float2 (-0.2099523790012021, -0.9777116131824024),
    float2 (0.8152470554454873, 0.5791133210240138),
    float2 (-0.9923232342597708, 0.12367133357503751),
    float2 (0.6481694844288681, -0.7614961060013474),
    float2 (0.036443223183926, 0.9993357251114194),
    float2 (-0.7019136816142636, -0.7122620188966349),
    float2 (0.998695384655528, 0.05106396643179117),
    float2 (-0.7709001090366207, 0.6369560596205411),
    float2 (0.13818011236605823, -0.9904071165669719),
    float2 (0.5671206801804437, 0.8236347091470047),
    float2 (-0.9745343917253847, -0.22423808629319533),
    float2 (0.8700619819701214, -0.49294233692210304),
    float2 (-0.30857886328244405, 0.9511987621603146),
    float2 (-0.4149890815356195, -0.9098263912451776),
    float2 (0.9205789302157817, 0.3905565685566777)
};

// Samples uniformly spread across the disk kernel
real2 ComputeFibonacciSpiralDiskSampleUniform(const in int sampleIndex, const in real sampleCountInverse, out real sampleDistNorm)
{
    sampleDistNorm = (real)sampleIndex * sampleCountInverse;
    
    // sqrt results in uniform distribution
    sampleDistNorm = sqrt(sampleDistNorm);
    
    return fibonacciSpiralDirection[sampleIndex] * sampleDistNorm;
}

// Samples denser near the center - important for blocker search
real2 ComputeFibonacciSpiralDiskSample(const in int sampleIndex, const in real sampleCountInverse, out real sampleDistNorm)
{
    sampleDistNorm = (real)sampleIndex * sampleCountInverse;

    // Third power chosen arbitrarily - center area is really that much more important
    // TODO: experiment with other radial functions, still overweighing the center though
    sampleDistNorm = sampleDistNorm * sampleDistNorm * sampleDistNorm;

    return fibonacciSpiralDirection[sampleIndex] * sampleDistNorm;
}

real PenumbraSizePunctual(real receiver, real blocker)
{
    return abs((receiver - blocker) / blocker);
}

real PenumbraSizeDirectional(real receiver, real blocker, real rangeScale)
{
    return abs(receiver - blocker) * rangeScale;
}

void FilterScaleOffset(real3 coord, real maxSampleZDistance, real shadowmapSamplingScale, out real2 filterScalePos, out real2 filterScaleNeg, out real2 filterOffset)
{
    real d = shadowmapSamplingScale * maxSampleZDistance / (1 - coord.z);
    real2 target = (coord.xy + 0.5) * 0.5;

    filterScalePos = (1 - target) * d;
    filterScaleNeg = target * d;
    filterOffset = (target - coord.xy) * d;
}

bool BlockerSearch(inout real averageBlockerDepth, inout real numBlockers, real maxSampleZDistance, real2 shadowmapInAtlasScale, real2 posTCAtlas, real3 posTCShadowmap, real2 sampleJitter, Texture2D shadowMap, SamplerState pointSampler, int sampleCount)
{
    real blockerSum = 0.0;
    real sampleCountInverse = rcp((real)sampleCount);
    real ditherRotation = sampleJitter.x;

    // The z extent of the filter cone shouldn't go beyond the near plane of the shadow. Near plane at 1.
    maxSampleZDistance = min(1 - posTCShadowmap.z, maxSampleZDistance);

    real2 filterScalePos, filterScaleNeg;
    real2 filterOffset;
    FilterScaleOffset(posTCShadowmap, maxSampleZDistance, shadowmapInAtlasScale.x, filterScalePos, filterScaleNeg, filterOffset);

    for (int i = 0; i < sampleCount && i < DISK_SAMPLE_COUNT; ++i)
    {
        real sampleDistNorm;
        real2 offset = ComputeFibonacciSpiralDiskSample(i, sampleCountInverse, sampleDistNorm);
        offset = real2(offset.x *  sampleJitter.y + offset.y * sampleJitter.x,
                       offset.x * -sampleJitter.x + offset.y * sampleJitter.y);

        offset = offset * (offset > 0 ? filterScalePos : filterScaleNeg) + filterOffset * sampleDistNorm;

        real blocker = SAMPLE_TEXTURE2D_LOD(shadowMap, pointSampler, posTCAtlas + offset, 0.0).x;

        real zoffset = maxSampleZDistance * sampleDistNorm;

        if (COMPARE_DEVICE_DEPTH_CLOSER(blocker, posTCShadowmap.z + zoffset))
        {
            blockerSum  += blocker;
            numBlockers += 1.0;
        }
    }
    averageBlockerDepth = numBlockers > 0 ? blockerSum / numBlockers : posTCShadowmap.z;

    return numBlockers >= 1;
}

real PCSS(real2 posTCAtlas, real3 posTCShadowmap, real maxSampleZDistance, real2 shadowmapInAtlasScale, real2 shadowmapInAtlasOffset, real2 sampleJitter, Texture2D shadowMap, SamplerComparisonState compSampler, int sampleCount)
{
    real UMin = shadowmapInAtlasOffset.x;
    real UMax = shadowmapInAtlasOffset.x + shadowmapInAtlasScale.x;

    real VMin = shadowmapInAtlasOffset.y;
    real VMax = shadowmapInAtlasOffset.y + shadowmapInAtlasScale.y;

    real sum = 0.0;
    real sampleCountInverse = rcp((real)sampleCount);
    real sampleCountBias = 0.5 * sampleCountInverse;
    real ditherRotation = sampleJitter.x;

    real2 filterScalePos, filterScaleNeg;
    real2 filterOffset;
    FilterScaleOffset(posTCShadowmap, maxSampleZDistance, shadowmapInAtlasScale.x, filterScalePos, filterScaleNeg, filterOffset);

    for (int i = 0; i < sampleCount && i < DISK_SAMPLE_COUNT; ++i)
    {
        real sampleDistNorm;
        real2 offset = ComputeFibonacciSpiralDiskSampleUniform(i, sampleCountInverse, sampleDistNorm);
        offset = real2(offset.x *  sampleJitter.y + offset.y * sampleJitter.x,
                       offset.x * -sampleJitter.x + offset.y * sampleJitter.y);

        offset = offset * (offset > 0 ? filterScalePos : filterScaleNeg) + filterOffset * sampleDistNorm;

        real U = posTCAtlas.x + offset.x;
        real V = posTCAtlas.y + offset.y;

        real zoffset = maxSampleZDistance * sampleDistNorm;

        //NOTE: We must clamp the sampling within the bounds of the shadow atlas.
        //        Overfiltering will leak results from other shadow lights.
        //TODO: Investigate moving this to blocker search.
        // coord.xy = clamp(posTCAtlas.xy, float2(UMin, VMin), float2(UMax, VMax));

        // TODO: vectorize into two comparisons?
        if (U <= UMin || U >= UMax || V <= VMin || V >= VMax)
            // TODO: why wasn't it just not sampling here? Investigate before removing
            sum += 1;//SAMPLE_TEXTURE2D_SHADOW(shadowMap, compSampler, real3(posTCAtlas, posTCShadowmap.z + zoffset)).r;
        else
            sum += SAMPLE_TEXTURE2D_SHADOW(shadowMap, compSampler, real3(U, V, posTCShadowmap.z + zoffset)).r;
    }

    return sum / sampleCount;
}
