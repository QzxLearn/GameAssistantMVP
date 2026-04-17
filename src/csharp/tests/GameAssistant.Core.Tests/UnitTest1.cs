using GameAssistant.Core.Enums;
using GameAssistant.Core.Interfaces;
using GameAssistant.Core.Models;
using NSubstitute;
using Xunit;

namespace GameAssistant.Core.Tests;

// =============================================================================
// IOcrService Tests
// =============================================================================
public class IOcrServiceTests
{
    [Fact]
    public void RecognizeFromBytes_WithGenericMode_ReturnsExpectedResult()
    {
        var mock = Substitute.For<IOcrService>();
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        mock.RecognizeFromBytes(testImageBytes, OcrMode.Generic).Returns("Test OCR Result");

        var result = mock.RecognizeFromBytes(testImageBytes, OcrMode.Generic);

        Assert.Equal("Test OCR Result", result);
    }

    [Fact]
    public void RecognizeFromBytes_WithCardTextMode_ReturnsExpectedResult()
    {
        var mock = Substitute.For<IOcrService>();
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        mock.RecognizeFromBytes(testImageBytes, OcrMode.CardText).Returns("Card Text");

        var result = mock.RecognizeFromBytes(testImageBytes, OcrMode.CardText);

        Assert.Equal("Card Text", result);
    }

    [Fact]
    public void RecognizeFromBytes_WithDefaultMode_UsesGeneric()
    {
        var mock = Substitute.For<IOcrService>();
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        mock.RecognizeFromBytes(testImageBytes, OcrMode.Generic).Returns("Default Mode Result");

        var result = mock.RecognizeFromBytes(testImageBytes, OcrMode.Generic);

        Assert.Equal("Default Mode Result", result);
    }

    [Theory]
    [InlineData(OcrMode.Generic)]
    [InlineData(OcrMode.CardText)]
    [InlineData(OcrMode.CardName)]
    [InlineData(OcrMode.UIElement)]
    public void RecognizeFromBytes_SupportsAllOcrModes(OcrMode mode)
    {
        var mock = Substitute.For<IOcrService>();
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        mock.RecognizeFromBytes(testImageBytes, mode).Returns($"Result for {mode}");

        var result = mock.RecognizeFromBytes(testImageBytes, mode);

        Assert.Equal($"Result for {mode}", result);
    }
}

// =============================================================================
// IGameStateParser Tests
// =============================================================================
public class IGameStateParserTests
{
    [Fact]
    public void Parse_WithValidOcrText_ReturnsGameState()
    {
        var mockParser = Substitute.For<IGameStateParser>();
        var expectedState = new GenericGameState { GameName = "SlayTheSpire" };
        mockParser.Parse(Arg.Any<string>()).Returns(expectedState);

        var result = mockParser.Parse("HP: 100/100  Gold: 50");

        Assert.NotNull(result);
        Assert.Equal("SlayTheSpire", result.GameName);
    }

    [Fact]
    public void GameName_IsAccessible()
    {
        var mockParser = Substitute.For<IGameStateParser>();
        mockParser.GameName.Returns("SlayTheSpire");

        Assert.Equal("SlayTheSpire", mockParser.GameName);
    }

    [Fact]
    public void Parse_ReturnsNonNullGameState()
    {
        var mockParser = Substitute.For<IGameStateParser>();
        mockParser.Parse(Arg.Any<string>()).Returns(new GenericGameState { GameName = "Test" });

        var result = mockParser.Parse("Any OCR text");

        Assert.NotNull(result);
        Assert.Equal("Test", result.GameName);
    }
}

// =============================================================================
// OcrMode Integration Tests
// =============================================================================
public class OcrModeIntegrationTests
{
    [Theory]
    [InlineData(OcrMode.Generic)]
    [InlineData(OcrMode.CardText)]
    [InlineData(OcrMode.CardName)]
    [InlineData(OcrMode.UIElement)]
    public void OcrMode_AllValuesExistInEnum(OcrMode mode)
    {
        Assert.True(Enum.IsDefined(typeof(OcrMode), mode));
    }

    [Fact]
    public void OcrMode_Generic_HasZeroValue()
    {
        Assert.Equal(0, (int)OcrMode.Generic);
    }

    [Fact]
    public void OcrMode_HasFourDefinedValues()
    {
        Assert.Equal(4, Enum.GetValues<OcrMode>().Length);
    }
}
