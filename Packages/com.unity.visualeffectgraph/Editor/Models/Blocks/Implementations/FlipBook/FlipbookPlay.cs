using UnityEngine;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Serialization;

namespace UnityEditor.VFX.Block
{
    [VFXHelpURL("Block-FlipbookPlayer")]
    [VFXInfo(name = "Flipbook Player", category = "FlipBook", synonyms = new []{ "Animation", "Atlas", "Frame", "Rate", "Sheet", "Sprite", "SubUV", "Texture" })]
    class FlipbookPlay : VFXBlock
    {
        public enum Mode
        {
            FrameRate,
            Cycles
        }

        [VFXSetting, Tooltip("How to control the flipbook animation: setting the frame rate (speed) or the frames to be played (specifying the range and the number of cycles over the particle lifetime).")]
        public Mode mode = Mode.FrameRate;

        public enum FrameRateMode
        {
            Constant,
            Random,
            OverLifetime,
            BySpeed,
            Custom
        }
        [VFXSetting, Tooltip("Different modes to determine the frame rate.")]
        public FrameRateMode frameRateMode = FrameRateMode.Constant;

        public enum CyclesMode
        {
            Constant,
            Random,
            RandomFullCycle
        }
        [VFXSetting, Tooltip("How to specify the number of cycles (loops) in the animation.")]
        public CyclesMode cyclesMode = CyclesMode.Constant;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Enable range options for frame rate mode. When turned off, range is the entire flipbook.")]
        public bool useCustomRange = false;

        public enum AnimationRange
        {
            EntireFlipbook,
            FlipbookRow,
            FlipbookColumn,
            StartEndFrames,
        }
        [VFXSetting, Tooltip("How to specify the range of frames used by the animation."), FormerlySerializedAs("rangeMode")]
        public AnimationRange animationRange = AnimationRange.EntireFlipbook;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Play the animation backwards.")]
        public bool reverse = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Clamp the animation to the last frame when the output is using blending between frames.")]
        public bool clampBlending = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("Remap the texIndex along each cycle of the animation using a custom curve from 0 to 1.")]
        public bool customCurve = false;

        private bool needsRandom =>
            (mode == Mode.FrameRate && frameRateMode == FrameRateMode.Random) ||
            (mode == Mode.Cycles && (cyclesMode == CyclesMode.Random || cyclesMode == CyclesMode.RandomFullCycle));

        private bool needsRange => mode == Mode.Cycles || useCustomRange;

        private bool needsTexIndexBlend => needsRange && animationRange != AnimationRange.EntireFlipbook;

        private bool needsFrameBlending => mode == Mode.Cycles && outputHasBlending;

        private bool outputHasBlending
        {
            get
            {
                var parent = GetParent();
                bool hasBlendFrames = false;
                foreach (var context in parent.outputContexts)
                {
                    if (context is VFXAbstractParticleOutput output && output.usesFlipbook)
                    {
                        hasBlendFrames = output.flipbookHasInterpolation;
                        break;
                    }
                }
                return hasBlendFrames;
            }
        }

        public override string name => "Flipbook Player";

        public override VFXContextType compatibleContexts { get { return VFXContextType.Update; } }

        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (mode != Mode.FrameRate)
                {
                    yield return nameof(frameRateMode);
                    yield return nameof(useCustomRange);
                }

                if (mode != Mode.Cycles)
                {
                    yield return nameof(cyclesMode);
                    yield return nameof(customCurve);
                }

                if (!needsRange)
                {
                    yield return nameof(animationRange);
                }

                if (!needsFrameBlending)
                {
                    yield return nameof(clampBlending);
                }

                foreach (var setting in base.filteredOutSettings)
                    yield return setting;
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.TexIndex, VFXAttributeMode.ReadWrite);
                if (needsTexIndexBlend)
                {
                    yield return new VFXAttributeInfo(VFXAttribute.TexIndexBlend, VFXAttributeMode.Write);
                }
                if (mode == Mode.Cycles || (mode == Mode.FrameRate && frameRateMode == FrameRateMode.OverLifetime))
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);
                }
                if (needsRandom)
                {
                    yield return new VFXAttributeInfo(VFXAttribute.ParticleId, VFXAttributeMode.Read);
                }
                if (frameRateMode == FrameRateMode.BySpeed)
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.Read);
                }
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                switch (mode)
                {
                    case Mode.FrameRate:
                        switch (frameRateMode)
                        {
                            case FrameRateMode.Constant:
                                yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "frameRate", new TooltipAttribute("The frame rate of the animation, in frames per second.")), 24.0f);
                                break;
                            case FrameRateMode.Random:
                                yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "frameRate", new TooltipAttribute("The frame rate of the animation, in frames per second, choosing a random value in the specified range.")), new Vector2(24.0f, 30.0f));
                                yield return new VFXPropertyWithValue(new VFXProperty(typeof(uint), "seed", new TooltipAttribute("Random seed.")));
                                break;
                            case FrameRateMode.OverLifetime:
                                yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), "frameRate", new TooltipAttribute("The frame rate of the animation, in frames per second, from a curve over the lifetime.")), AnimationCurve.Constant(0.0f, 1.0f, 24.0f));
                                break;
                            case FrameRateMode.BySpeed:
                                yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), "frameRate", new TooltipAttribute("The frame rate of the animation, in frames per second, from a curve depending on the speed.")), AnimationCurve.Constant(0.0f, 1.0f, 24.0f));
                                yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "speedRange", new TooltipAttribute("Min and max speeds to map the current speed value to the normalized curve.")), new Vector2(0.0f, 1.0f));
                                break;
                            case FrameRateMode.Custom:
                                yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), "frameRate", new TooltipAttribute("The frame rate of the animation, in frames per second, from a curve depending on a custom value.")), AnimationCurve.Constant(0.0f, 1.0f, 24.0f));
                                yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "sampleTime", new TooltipAttribute("Value that controls the framerate.")), 0.0f);
                                break;
                        }
                        break;
                    case Mode.Cycles:
                        switch (cyclesMode)
                        {
                            case CyclesMode.Constant:
                                yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "cycles", new TooltipAttribute("Fixed number of cycles of the animation.")), 1.0f);
                                break;
                            case CyclesMode.Random:
                                yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "cycles", new TooltipAttribute("Number of cycles of the animation, between two values, can be a decimal value.")), new Vector2(1.0f, 3.0f));
                                yield return new VFXPropertyWithValue(new VFXProperty(typeof(uint), "seed", new TooltipAttribute("Random seed.")));
                                break;
                            case CyclesMode.RandomFullCycle:
                                yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "cycles", new TooltipAttribute("Number of cycles of the animation, between two values, clamped to full cycles.")), new Vector2(1, 3));
                                yield return new VFXPropertyWithValue(new VFXProperty(typeof(uint), "seed", new TooltipAttribute("Random seed.")));
                                break;
                        }
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "startOffset", new TooltipAttribute("Offset the texture index for the first frame of the animation.")), 0.0f);
                        if (customCurve)
                        {
                            yield return new VFXPropertyWithValue(new VFXProperty(typeof(AnimationCurve), "customCurve", new TooltipAttribute("Custom curve to remap the animation.")), AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f));
                        }
                        break;
                }

                if (needsRange)
                {
                    if (animationRange == AnimationRange.FlipbookRow || animationRange == AnimationRange.FlipbookColumn)
                    {
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(int), "index", new TooltipAttribute($"Index of the selected { (animationRange == AnimationRange.FlipbookRow? "row" : "column") }, starting at 0.")), 0);
                    }
                    else if (animationRange == AnimationRange.StartEndFrames)
                    {
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "frameRange", new TooltipAttribute("First and last frames of the animation, both included.")), new Vector2(0, 16));
                    }
                }
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                // this function gets all the InputProperties members and sends them to the node block, except for speedRange
                VFXExpression speedRangeExp = null;
                VFXExpression flipBookIndexExp = null;
                VFXExpression frameRangeExp = null;
                foreach (var p in GetExpressionsFromSlots(this))
                {
                    switch (p.name)
                    {
                        case "speedRange":
                            speedRangeExp = p.exp;
                            break;
                        case "index":
                            flipBookIndexExp = p.exp;
                            break;
                        case "frameRange":
                            frameRangeExp = p.exp;
                            break;
                        default:
                            yield return p;
                            break;
                    }
                }

                if (speedRangeExp != null)
                {
                    var speedRangeComponents = (VFXOperatorUtility.ExtractComponents(speedRangeExp) as List<VFXExpression>).ToArray();
                    // speedRange.y = 1 / (speedRange.y - speedRange.x)
                    speedRangeComponents[1] = VFXOperatorUtility.Reciprocal(speedRangeComponents[1] - speedRangeComponents[0]);
                    yield return new VFXNamedExpression(new VFXExpressionCombine(speedRangeComponents), "speedRange");
                }

                // this statement enables using deltaTime builtin expression in the nodeblock Source.
                yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");

                if (needsRandom)
                {
                    yield return new VFXNamedExpression(VFXBuiltInExpression.SystemSeed, "systemSeed");
                }

                if (needsRange)
                {
                    VFXExpression flipBookSizeXExp = null;
                    VFXExpression flipBookSizeYExp = null;

                    bool needsFlipbook = animationRange == AnimationRange.EntireFlipbook || animationRange == AnimationRange.FlipbookRow || animationRange == AnimationRange.FlipbookColumn;
                    if (needsFlipbook)
                    {
                        var parent = GetParent();
                        foreach (var context in parent.outputContexts)
                        {
                            if (context is VFXAbstractParticleOutput output && output.usesFlipbook)
                            {
                                foreach (var p in GetExpressionsFromSlots(context))
                                {
                                    if (p.name == "flipBookSize")
                                    {
                                        flipBookSizeXExp = p.exp.x;
                                        flipBookSizeYExp = p.exp.y;
                                    }
                                    if (flipBookSizeXExp != null && flipBookSizeYExp != null)
                                    {
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                    }

                    if (flipBookSizeXExp == null || flipBookSizeYExp == null)
                    {
                        flipBookSizeXExp = new VFXValue<float>(1);
                        flipBookSizeYExp = new VFXValue<float>(1);
                    }

                    // FrameRange variable: (startFrame, frameCount, [stride])
                    switch (animationRange)
                    {
                        case AnimationRange.EntireFlipbook:
                            yield return new VFXNamedExpression(new VFXExpressionCombine(new VFXValue<float>(0), flipBookSizeXExp * flipBookSizeYExp, new VFXValue<float>(1)), "frameRange");
                            break;
                        case AnimationRange.FlipbookRow:
                            yield return new VFXNamedExpression(new VFXExpressionCombine(new VFXExpressionCastIntToFloat(flipBookIndexExp) * flipBookSizeXExp, flipBookSizeXExp, new VFXValue<float>(1)), "frameRange");
                            break;
                        case AnimationRange.FlipbookColumn:
                            yield return new VFXNamedExpression(new VFXExpressionCombine(new VFXExpressionCastIntToFloat(flipBookIndexExp), flipBookSizeYExp, flipBookSizeXExp), "frameRange");
                            break;
                        case AnimationRange.StartEndFrames:
                            yield return new VFXNamedExpression(new VFXExpressionCombine(frameRangeExp.x, frameRangeExp.y - frameRangeExp.x + new VFXValue<float>(1), new VFXValue<float>(1)), "frameRange");
                            break;
                    }
                }
            }
        }

        // Source code is the actual code of your nodeblock where you can access properties, attributes and optionally parameters.
        public override string source
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();

                bool needsFrameBlendingCache = needsFrameBlending;

                if (needsRange)
                {
                    stringBuilder.AppendLine($"float stride = {(animationRange == AnimationRange.FlipbookColumn ? "frameRange.z" : "1.0f")};");
                    stringBuilder.AppendLine("float rangeLength = frameRange.y;");
                    stringBuilder.AppendLine("float firstFrame = frameRange.x;");
                    stringBuilder.AppendLine("float lastFrame = firstFrame + (rangeLength - 1.0f) * stride;");
                    if (mode == Mode.Cycles)
                    {
                        stringBuilder.AppendLine($"float startFrameOffset = fmod({(reverse ? "-" : "")}startOffset, rangeLength) * stride;");
                        stringBuilder.AppendLine($"float {(reverse && needsFrameBlendingCache ? "endFrame" : "startFrame")} = startFrameOffset >= 0 ? firstFrame + startFrameOffset : lastFrame + startFrameOffset + stride;");
                        stringBuilder.AppendLine($"float {(reverse && needsFrameBlendingCache ? "startFrame" : "endFrame")} = startFrameOffset > 0 ? firstFrame + startFrameOffset - stride : lastFrame + startFrameOffset;");

                        stringBuilder.AppendLine("\nif (age <= 0.0f) texIndex = startFrame;");
                    }
                    if (animationRange == AnimationRange.FlipbookColumn)
                    {
                        stringBuilder.AppendLine("\nfloat previousFrame = floor(texIndex);");
                    }
                }

                string framerate = "";
                string cycleCount = "";
                switch (mode)
                {
                    case Mode.FrameRate:
                        switch (frameRateMode)
                        {
                            case FrameRateMode.Constant: framerate = "frameRate"; break;
                            case FrameRateMode.Random: framerate = "FIXED_RAND(seed) * (frameRate.y - frameRate.x) + frameRate.x"; break;
                            case FrameRateMode.OverLifetime: framerate = "SampleCurve(frameRate, age/lifetime)"; break;
                            case FrameRateMode.BySpeed: framerate = "SampleCurve(frameRate, saturate((length(velocity) - speedRange.x) * speedRange.y))"; break;
                            case FrameRateMode.Custom: framerate = "SampleCurve(frameRate, saturate(sampleTime))"; break;
                        }
                        break;
                    case Mode.Cycles:
                        switch (cyclesMode)
                        {
                            case CyclesMode.Constant: cycleCount = "cycles"; break;
                            case CyclesMode.Random: cycleCount = "FIXED_RAND(seed) * (cycles.y - cycles.x) + cycles.x"; break;
                            case CyclesMode.RandomFullCycle: cycleCount = "FIXED_RAND_INT(seed) % (int)(cycles.y - cycles.x + 1) + cycles.x"; break;
                        }
                        stringBuilder.AppendLine($"float cycleCount = {cycleCount};");
                        framerate = "cycleCount * rangeLength / lifetime";
                        break;
                }

                if (mode == Mode.Cycles && customCurve)
                {
                    stringBuilder.AppendLine($"\nfloat framerateValue = {framerate};\ntexIndex += RemapCurve(customCurve, age * framerateValue / rangeLength, framerateValue * {(reverse ? "-" : "")}deltaTime);");
                }
                else
                {
                    stringBuilder.AppendLine($"\nfloat framerateValue = {framerate};\ntexIndex += framerateValue * {(reverse ? "-" : "")}deltaTime;");
                }


                if (needsRange)
                {
                    if (animationRange != AnimationRange.FlipbookColumn)
                    {
                        stringBuilder.AppendLine("\ntexIndex = fmod(texIndex - firstFrame + rangeLength, rangeLength) + firstFrame;");
                        if (animationRange != AnimationRange.EntireFlipbook)
                        {
                            stringBuilder.AppendLine("float currentFrame = floor(texIndex);");
                            stringBuilder.AppendLine("\ntexIndexBlend = texIndex > lastFrame ? firstFrame - currentFrame : 1.0f;");
                        }
                    }
                    else
                    {
                        stringBuilder.AppendLine("\nfloat currentFrame = floor(texIndex);");
                        stringBuilder.AppendLine("float frameOffset = texIndex - currentFrame;"); // offset always 0 or positive
                        stringBuilder.AppendLine("float previousRow = floor(previousFrame / stride);");
                        stringBuilder.AppendLine("float advanceFrames = currentFrame - previousFrame;");
                        stringBuilder.AppendLine("texIndex = fmod((previousRow + advanceFrames) + rangeLength, rangeLength) * stride + firstFrame + frameOffset;");
                        stringBuilder.AppendLine("\ntexIndexBlend = stride;");
                    }
                }

                if (mode == Mode.Cycles && customCurve && (!needsFrameBlendingCache || clampBlending))
                {
                    stringBuilder.AppendLine("\nfloat currentCycle = cycleCount * age / lifetime;");
                    stringBuilder.AppendLine($"if (currentCycle > cycleCount - 0.9f) texIndex = {(reverse ? "max" : "min")}(texIndex, endFrame);");
                }
                else if (needsFrameBlendingCache && clampBlending)
                {
                    stringBuilder.AppendLine("\nfloat halfFrame = 0.5f / framerateValue;");
                    stringBuilder.AppendLine("if (age <= halfFrame) texIndex = startFrame;");
                    stringBuilder.AppendLine("if ((lifetime - age) <= halfFrame) texIndex = endFrame;");
                }

                return stringBuilder.ToString();
            }
        }

        internal sealed override void GenerateErrors(VFXErrorReporter report)
        {
            base.GenerateErrors(report);

            var data = GetData();// as VFXDataParticle;
            if (mode == Mode.Cycles && data != null && !data.IsAttributeStored(VFXAttribute.Alive))
            {
                report.RegisterError("FlipbookAnimLengthUnavailable", VFXErrorType.Warning, "Cycles mode only works with particles that have the Lifetime attribute", this);
            }

            if (needsRange && (animationRange == AnimationRange.EntireFlipbook || animationRange == AnimationRange.FlipbookRow || animationRange == AnimationRange.FlipbookColumn))
            {
                var parent = GetParent();
                bool usesFlipbook = false;
                foreach (var context in parent.outputContexts)
                {
                    if (context is VFXAbstractParticleOutput output && output.usesFlipbook)
                    {
                        usesFlipbook = true;
                        break;
                    }
                }
                if (!usesFlipbook)
                {
                    report.RegisterError("NoFlipbookOutput", VFXErrorType.Warning, "Flipbook mode requires an output with Uv Mode set to Flipbook", this);
                }
            }
        }

        public override void Sanitize(int version)
        {
            if (version < 17)
            {
                switch ((int)mode)
                {
                    case 0: // Constant
                        mode = Mode.FrameRate;
                        frameRateMode = FrameRateMode.Constant;
                        break;
                    case 1: // CurveOverLife
                        mode = Mode.FrameRate;
                        frameRateMode = FrameRateMode.OverLifetime;
                        break;
                }
            }
            base.Sanitize(version);
        }
    }
}
