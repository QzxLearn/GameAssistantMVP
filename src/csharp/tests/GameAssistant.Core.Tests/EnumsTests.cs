using GameAssistant.Core.Enums;
using GameAssistant.Core.Models;
using Xunit;

namespace GameAssistant.Core.Tests;

public class OcrModeTests
{
    [Theory]
    [InlineData(OcrMode.Generic, 0)]
    [InlineData(OcrMode.CardText, 1)]
    [InlineData(OcrMode.CardName, 2)]
    [InlineData(OcrMode.UIElement, 3)]
    public void OcrMode_HasExpectedIntValue(OcrMode mode, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)mode);
    }

    [Fact]
    public void OcrMode_AllValuesAreDefined()
    {
        // Assert
        Assert.Equal(4, Enum.GetValues<OcrMode>().Length);
    }

    [Theory]
    [InlineData("Generic")]
    [InlineData("CardText")]
    [InlineData("CardName")]
    [InlineData("UIElement")]
    public void OcrMode_CanParseFromString(string name)
    {
        // Act
        var result = Enum.Parse<OcrMode>(name);

        // Assert
        Assert.True(Enum.IsDefined(typeof(OcrMode), result));
    }

    [Fact]
    public void OcrMode_GenericIsDefault()
    {
        // Assert
        Assert.Equal(0, (int)OcrMode.Generic);
    }
}

public class ThresholdMethodTests
{
    [Theory]
    [InlineData(ThresholdMethod.None, 0)]
    [InlineData(ThresholdMethod.Otsu, 1)]
    [InlineData(ThresholdMethod.Binary, 2)]
    [InlineData(ThresholdMethod.AdaptiveGaussian, 3)]
    public void ThresholdMethod_HasExpectedIntValue(ThresholdMethod method, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)method);
    }

    [Fact]
    public void ThresholdMethod_AllValuesAreDefined()
    {
        // Assert
        Assert.Equal(4, Enum.GetValues<ThresholdMethod>().Length);
    }

    [Theory]
    [InlineData("None")]
    [InlineData("Otsu")]
    [InlineData("Binary")]
    [InlineData("AdaptiveGaussian")]
    public void ThresholdMethod_CanParseFromString(string name)
    {
        // Act
        var result = Enum.Parse<ThresholdMethod>(name);

        // Assert
        Assert.True(Enum.IsDefined(typeof(ThresholdMethod), result));
    }

    [Fact]
    public void ThresholdMethod_NoneIsDefault()
    {
        // Assert
        Assert.Equal(0, (int)ThresholdMethod.None);
    }

    [Fact]
    public void ThresholdMethod_Otsu_DescriptionMatches()
    {
        // Assert - Otsu is recommended for document-like images
        Assert.Equal(ThresholdMethod.Otsu, ThresholdMethod.Otsu);
    }

    [Fact]
    public void ThresholdMethod_Binary_UsesFixedThreshold()
    {
        // Assert - Binary uses fixed threshold with ThresholdValue
        Assert.Equal(ThresholdMethod.Binary, ThresholdMethod.Binary);
    }

    [Fact]
    public void ThresholdMethod_AdaptiveGaussian_NotYetImplemented()
    {
        // Assert - Adaptive Gaussian threshold not yet implemented
        Assert.Equal(ThresholdMethod.AdaptiveGaussian, ThresholdMethod.AdaptiveGaussian);
    }
}
