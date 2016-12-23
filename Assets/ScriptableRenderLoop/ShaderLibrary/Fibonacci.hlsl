#ifndef UNITY_FIBONACCI_INCLUDED
#define UNITY_FIBONACCI_INCLUDED

// Computes a point using the Fibonacci sequence of length N.
// Input: Fib[N - 1], Fib[N - 2], and the index 'i' of the point.
// Ref: Integration of nonperiodic functions of two variables by Fibonacci lattice rules
float2 Fibonacci2dSeq(float fibN1, float fibN2, int i)
{
    // 3 cycles on GCN if 'fibN1' and 'fibN2' are known at compile time.
    return float2(i / fibN1, frac(i * (fibN2 / fibN1)));
}

static const int k_FibonacciSeq[] = {
    0, 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377, 610
};

static const float2 k_Fibonacci2dSeq21[] = {
    float2(0.00000000, 0.00000000),
    float2(0.04761905, 0.61904764),
    float2(0.09523810, 0.23809528),
    float2(0.14285715, 0.85714293),
    float2(0.19047619, 0.47619057),
    float2(0.23809524, 0.09523821),
    float2(0.28571430, 0.71428585),
    float2(0.33333334, 0.33333349),
    float2(0.38095239, 0.95238113),
    float2(0.42857143, 0.57142878),
    float2(0.47619048, 0.19047642),
    float2(0.52380955, 0.80952406),
    float2(0.57142860, 0.42857170),
    float2(0.61904764, 0.04761887),
    float2(0.66666669, 0.66666698),
    float2(0.71428573, 0.28571510),
    float2(0.76190478, 0.90476227),
    float2(0.80952382, 0.52380943),
    float2(0.85714287, 0.14285755),
    float2(0.90476191, 0.76190567),
    float2(0.95238096, 0.38095284)
};

static const float2 k_Fibonacci2dSeq34[] = {
    float2(0.00000000, 0.00000000),
    float2(0.02941176, 0.61764705),
    float2(0.05882353, 0.23529410),
    float2(0.08823530, 0.85294116),
    float2(0.11764706, 0.47058821),
    float2(0.14705883, 0.08823538),
    float2(0.17647059, 0.70588231),
    float2(0.20588236, 0.32352924),
    float2(0.23529412, 0.94117641),
    float2(0.26470590, 0.55882359),
    float2(0.29411766, 0.17647076),
    float2(0.32352942, 0.79411745),
    float2(0.35294119, 0.41176462),
    float2(0.38235295, 0.02941132),
    float2(0.41176471, 0.64705849),
    float2(0.44117647, 0.26470566),
    float2(0.47058824, 0.88235283),
    float2(0.50000000, 0.50000000),
    float2(0.52941179, 0.11764717),
    float2(0.55882353, 0.73529434),
    float2(0.58823532, 0.35294151),
    float2(0.61764705, 0.97058773),
    float2(0.64705884, 0.58823490),
    float2(0.67647058, 0.20588207),
    float2(0.70588237, 0.82352924),
    float2(0.73529410, 0.44117641),
    float2(0.76470590, 0.05882263),
    float2(0.79411763, 0.67646980),
    float2(0.82352942, 0.29411697),
    float2(0.85294116, 0.91176414),
    float2(0.88235295, 0.52941132),
    float2(0.91176468, 0.14705849),
    float2(0.94117647, 0.76470566),
    float2(0.97058821, 0.38235283)
};

static const float2 k_Fibonacci2dSeq55[] = {
    float2(0.00000000, 0.00000000),
    float2(0.01818182, 0.61818182),
    float2(0.03636364, 0.23636365),
    float2(0.05454545, 0.85454547),
    float2(0.07272727, 0.47272730),
    float2(0.09090909, 0.09090900),
    float2(0.10909091, 0.70909095),
    float2(0.12727273, 0.32727289),
    float2(0.14545454, 0.94545460),
    float2(0.16363636, 0.56363630),
    float2(0.18181819, 0.18181801),
    float2(0.20000000, 0.80000019),
    float2(0.21818182, 0.41818190),
    float2(0.23636363, 0.03636360),
    float2(0.25454545, 0.65454578),
    float2(0.27272728, 0.27272701),
    float2(0.29090908, 0.89090919),
    float2(0.30909091, 0.50909138),
    float2(0.32727271, 0.12727261),
    float2(0.34545454, 0.74545479),
    float2(0.36363637, 0.36363602),
    float2(0.38181818, 0.98181820),
    float2(0.40000001, 0.60000038),
    float2(0.41818181, 0.21818161),
    float2(0.43636364, 0.83636379),
    float2(0.45454547, 0.45454597),
    float2(0.47272727, 0.07272720),
    float2(0.49090910, 0.69090843),
    float2(0.50909090, 0.30909157),
    float2(0.52727270, 0.92727280),
    float2(0.54545456, 0.54545403),
    float2(0.56363636, 0.16363716),
    float2(0.58181816, 0.78181839),
    float2(0.60000002, 0.39999962),
    float2(0.61818182, 0.01818275),
    float2(0.63636363, 0.63636398),
    float2(0.65454543, 0.25454521),
    float2(0.67272729, 0.87272835),
    float2(0.69090909, 0.49090958),
    float2(0.70909089, 0.10909081),
    float2(0.72727275, 0.72727203),
    float2(0.74545455, 0.34545517),
    float2(0.76363635, 0.96363640),
    float2(0.78181821, 0.58181763),
    float2(0.80000001, 0.20000076),
    float2(0.81818181, 0.81818199),
    float2(0.83636361, 0.43636322),
    float2(0.85454547, 0.05454636),
    float2(0.87272727, 0.67272758),
    float2(0.89090908, 0.29090881),
    float2(0.90909094, 0.90909195),
    float2(0.92727274, 0.52727318),
    float2(0.94545454, 0.14545441),
    float2(0.96363634, 0.76363754),
    float2(0.98181820, 0.38181686)
};

// Loads elements from one of the precomputed tables for sample counts of 21, 34, 55.
// Computes sample positions at runtime otherwise.
// Sample count must be a Fibonacci number (see 'k_FibonacciSeq').
float2 Fibonacci2d(uint i, uint sampleCount)
{
    switch (sampleCount)
    {
        case 21: return k_Fibonacci2dSeq21[i];
        case 34: return k_Fibonacci2dSeq34[i];
        case 55: return k_Fibonacci2dSeq55[i];
        default:
        {
            int fibN1 = sampleCount;
            int fibN2 = sampleCount;

            // These are all constants, so this loop will be optimized away.
            for (int j = 0; j < 16; j++)
            {
                if (k_FibonacciSeq[j] == fibN1)
                {
                    fibN2 = k_FibonacciSeq[j - 1];
                }
            }

            return Fibonacci2dSeq(fibN1, fibN2, i);
        }
    }
}

#endif // UNITY_FIBONACCI_INCLUDED
