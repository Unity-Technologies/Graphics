// https://en.wikipedia.org/wiki/Algorithms_for_calculating_variance#Welford's_Online_algorithm
struct VarianceEstimator
{
    int num;
    float oldM;
    float oldS;
    float newM;
    float newS;
};

void InitializeVarianceEstimator(out VarianceEstimator variance)
{
    variance.num = 0;
    variance.oldM = 0.0;
    variance.oldS = 0.0;
    variance.newM = 0.0;
    variance.newS = 0.0;
}

void PushValue(inout VarianceEstimator variance, float value)
{
     variance.num++;

    // See Knuth TAOCP vol 2, 3rd edition, page 232
    if (variance.num == 1)
    {
        variance.oldM = variance.newM = value;
        variance.oldS = 0.0;
    }
    else
    {
        variance.newM = variance.oldM + (value - variance.oldM)/variance.num;
        variance.newS = variance.oldS + (value - variance.oldM)*(value - variance.newM);

        // set up for next iteration
        variance.oldM = variance.newM; 
        variance.oldS = variance.newS;
    }
}

float Variance(in VarianceEstimator variance)
{
    return ( (variance.num > 1) ? variance.newS/(variance.num - 1) : 0.0 );
}