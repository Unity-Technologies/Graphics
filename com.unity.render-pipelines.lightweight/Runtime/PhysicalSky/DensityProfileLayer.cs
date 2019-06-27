using System;
using UnityEngine;

namespace UnityEngine.Rendering.LWRP
{

    /// <summary>
    /// An atmosphere layer of width 'width' (in m), and whose density is defined as
    /// 'exp_term' * exp('exp_scale' * h) + 'linear_term' * h + 'constant_term',
    /// clamped to [0,1], and where h is the altitude (in m). 'exp_term' and
    /// 'constant_term' are unitless, while 'exp_scale' and 'linear_term' are in m^-1.
    /// </summary>
    public struct DensityProfileLayer
    {

        public string Name { get; private set; }

        public double Width { get; private set; }

        public double ExpTerm { get; private set; }

        public double ExpScale { get; private set; }

        public double LinearTerm { get; private set; }

        public double ConstantTerm { get; private set; }

        public DensityProfileLayer(string name, double width, double exp_term, double exp_scale, double linear_term, double constant_term)
        {
            Name = name;
            Width = width;
            ExpTerm = exp_term;
            ExpScale = exp_scale;
            LinearTerm = linear_term;
            ConstantTerm = constant_term;
        }

    };

}