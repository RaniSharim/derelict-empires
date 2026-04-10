using Xunit;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Visibility;

namespace DerlictEmpires.Tests.Visibility;

public class DetectionCalculatorTests
{
    [Fact]
    public void ZeroDistance_FullDetection()
    {
        var level = DetectionCalculator.Calculate(10f, 10f, 0f);
        Assert.Equal(DetectionLevel.Full, level);
    }

    [Fact]
    public void HighVisibility_CloseRange_FullDetection()
    {
        var level = DetectionCalculator.Calculate(50f, 30f, 5f);
        Assert.Equal(DetectionLevel.Full, level);
    }

    [Fact]
    public void LowVisibility_LongRange_NoDetection()
    {
        var level = DetectionCalculator.Calculate(5f, 10f, 100f);
        Assert.Equal(DetectionLevel.None, level);
    }

    [Fact]
    public void SilentRunning_DrasticallyReducesDetection()
    {
        float vis = 30f, sensor = 20f, dist = 10f;
        var normal = DetectionCalculator.Calculate(vis, sensor, dist, false);
        var silent = DetectionCalculator.Calculate(vis, sensor, dist, true);

        Assert.True(silent < normal, $"Silent {silent} should be less than normal {normal}");
    }

    [Fact]
    public void DetectionLevels_AreOrdered()
    {
        // As distance increases, detection level decreases
        float vis = 30f, sensor = 30f;
        var d1 = DetectionCalculator.Calculate(vis, sensor, 1f);
        var d5 = DetectionCalculator.Calculate(vis, sensor, 5f);
        var d20 = DetectionCalculator.Calculate(vis, sensor, 20f);
        var d100 = DetectionCalculator.Calculate(vis, sensor, 100f);

        Assert.True(d1 >= d5);
        Assert.True(d5 >= d20);
        Assert.True(d20 >= d100);
    }
}

public class VisibilitySystemTests
{
    [Fact]
    public void NewSystem_IsUnexplored()
    {
        var sys = new VisibilitySystem();
        Assert.Equal(VisibilityState.Unexplored, sys.GetVisibility(0, 42));
    }

    [Fact]
    public void SetVisible_Works()
    {
        var sys = new VisibilitySystem();
        sys.SetVisible(0, 5);
        Assert.Equal(VisibilityState.Visible, sys.GetVisibility(0, 5));
    }

    [Fact]
    public void SetExplored_DoesNotOverrideVisible()
    {
        var sys = new VisibilitySystem();
        sys.SetVisible(0, 5);
        sys.SetExplored(0, 5);
        Assert.Equal(VisibilityState.Visible, sys.GetVisibility(0, 5));
    }

    [Fact]
    public void DifferentEmpires_IndependentVisibility()
    {
        var sys = new VisibilitySystem();
        sys.SetVisible(0, 5);
        Assert.Equal(VisibilityState.Visible, sys.GetVisibility(0, 5));
        Assert.Equal(VisibilityState.Unexplored, sys.GetVisibility(1, 5));
    }
}
