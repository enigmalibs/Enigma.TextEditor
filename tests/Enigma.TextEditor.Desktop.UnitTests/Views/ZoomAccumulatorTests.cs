using System;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// The pointer's deltas turned into whole zoom steps: notches, fractions of one, and a change of direction.
/// </summary>
public sealed class ZoomAccumulatorTests
{
    [Theory]
    [InlineData(1.0, 1)]
    [InlineData(-1.0, -1)]
    [InlineData(3.0, 3)]
    [InlineData(-2.0, -2)]
    public void WholeNotches_AreAStepEach(double delta, int steps)
        => Assert.Equal(steps, new ZoomAccumulator(1.0).Add(delta));

    [Fact]
    public void FractionsOfANotch_AddUpToAStep()
    {
        ZoomAccumulator zoom = new(1.0);

        Assert.Equal(0, zoom.Add(0.25));
        Assert.Equal(0, zoom.Add(0.25));
        Assert.Equal(0, zoom.Add(0.25));
        Assert.Equal(1, zoom.Add(0.25));
        Assert.Equal(0, zoom.Add(0.25));
    }

    [Fact]
    public void WhatIsLeftOver_CountsTowardsTheNextStep()
    {
        ZoomAccumulator zoom = new(1.0);

        Assert.Equal(1, zoom.Add(1.6));
        Assert.Equal(1, zoom.Add(0.4));
        Assert.Equal(0, zoom.Add(0.9));
    }

    [Fact]
    public void FractionsThatDoNotAddUpExactly_StillMakeTheirSteps()
    {
        ZoomAccumulator zoom = new(0.1);
        int steps = 0;

        for (int index = 0; index < 100; index++)
        {
            steps += zoom.Add(0.01);
        }

        Assert.Equal(10, steps);
    }

    [Fact]
    public void ADirectionChange_DropsWhatWasLeftOver()
    {
        ZoomAccumulator zoom = new(1.0);

        Assert.Equal(0, zoom.Add(0.75));
        Assert.Equal(0, zoom.Add(-0.5));
        Assert.Equal(-1, zoom.Add(-0.5));
        Assert.Equal(0, zoom.Add(0.75));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void NothingOrNonsense_IsNoStep_AndKeepsWhatIsGathering(double delta)
    {
        ZoomAccumulator zoom = new(1.0);
        zoom.Add(0.5);

        Assert.Equal(0, zoom.Add(delta));
        Assert.Equal(1, zoom.Add(0.5));
    }

    [Fact]
    public void AHugeDelta_IsKeptWithinAnInt()
    {
        ZoomAccumulator zoom = new(1.0);

        Assert.Equal(1000, zoom.Add(1e300));
        Assert.Equal(-1000, zoom.Add(-1e300));
        Assert.Equal(0, zoom.Add(-0.5));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void AThreshold_IsMoreThanZero(double threshold)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new ZoomAccumulator(threshold));
}
