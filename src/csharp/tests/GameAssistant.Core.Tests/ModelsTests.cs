using System;
using System.Text.Json;
using GameAssistant.Core.Models;
using Xunit;

namespace GameAssistant.Core.Tests;

public class GameStateTests
{
    [Fact]
    public void GameState_Timestamp_DefaultsToUtcNow()
    {
        // Arrange & Act
        var before = DateTime.UtcNow.AddSeconds(-1);
        var state = new GenericGameState { GameName = "TestGame" };
        var after = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.True(state.Timestamp >= before);
        Assert.True(state.Timestamp <= after);
    }

    [Fact]
    public void GameState_GameName_CanBeSet()
    {
        // Arrange & Act
        var state = new GenericGameState { GameName = "Diablo2" };

        // Assert
        Assert.Equal("Diablo2", state.GameName);
    }

    [Fact]
    public void GameState_RawOcrText_IsNullByDefault()
    {
        // Arrange & Act
        var state = new GenericGameState();

        // Assert
        Assert.Null(state.RawOcrText);
    }

    [Fact]
    public void GameState_RawOcrText_CanBeSet()
    {
        // Arrange
        var state = new GenericGameState { RawOcrText = "Original OCR text" };

        // Assert
        Assert.Equal("Original OCR text", state.RawOcrText);
    }

    [Fact]
    public void GenericGameState_RecognizedText_DefaultsToEmpty()
    {
        // Arrange & Act
        var state = new GenericGameState();

        // Assert
        Assert.Equal(string.Empty, state.RecognizedText);
    }

    [Fact]
    public void GenericGameState_RecognizedText_CanBeSet()
    {
        // Arrange & Act
        var state = new GenericGameState { RecognizedText = "Parsed text content" };

        // Assert
        Assert.Equal("Parsed text content", state.RecognizedText);
    }

    [Fact]
    public void GameState_SerializesWithJsonInclude()
    {
        // Arrange
        var state = new GenericGameState
        {
            GameName = "TestGame",
            RecognizedText = "Test"
        };

        // Act
        var json = JsonSerializer.Serialize(state);

        // Assert
        Assert.Contains("GameName", json);
        Assert.Contains("TestGame", json);
    }

    [Fact]
    public void GameState_RawOcrText_NotSerializedWhenNull()
    {
        // Arrange
        var state = new GenericGameState
        {
            GameName = "TestGame",
            RawOcrText = null
        };

        // Act
        var json = JsonSerializer.Serialize(state);

        // Assert
        Assert.DoesNotContain("RawOcrText", json);
    }
}

public class CaptureRegionTests
{
    [Fact]
    public void CaptureRegion_CreatesRecordWithCorrectValues()
    {
        // Arrange & Act
        var region = new CaptureRegion(10.5, 20.5, 100.0, 200.0);

        // Assert
        Assert.Equal(10.5, region.X);
        Assert.Equal(20.5, region.Y);
        Assert.Equal(100.0, region.Width);
        Assert.Equal(200.0, region.Height);
    }

    [Fact]
    public void CaptureRegion_IsRecord_SupportsEquality()
    {
        // Arrange
        var region1 = new CaptureRegion(10, 20, 100, 200);
        var region2 = new CaptureRegion(10, 20, 100, 200);
        var region3 = new CaptureRegion(30, 40, 50, 60);

        // Assert
        Assert.Equal(region1, region2);
        Assert.NotEqual(region1, region3);
    }

    [Fact]
    public void CaptureRegion_WithZeroValues_IsValid()
    {
        // Arrange & Act
        var region = new CaptureRegion(0, 0, 0, 0);

        // Assert
        Assert.Equal(0, region.X);
        Assert.Equal(0, region.Y);
        Assert.Equal(0, region.Width);
        Assert.Equal(0, region.Height);
    }

    [Fact]
    public void CaptureRegion_WithNegativeValues_IsAllowed()
    {
        // Arrange & Act
        var region = new CaptureRegion(-100, -50, 200, 150);

        // Assert
        Assert.Equal(-100, region.X);
        Assert.Equal(-50, region.Y);
        Assert.Equal(200, region.Width);
        Assert.Equal(150, region.Height);
    }
}

public class GameSessionRecordTests
{
    [Fact]
    public void GameSessionRecord_Defaults()
    {
        // Arrange & Act
        var record = new GameSessionRecord();

        // Assert
        Assert.Equal(0, record.Id);
        Assert.Equal(string.Empty, record.GameName);
        Assert.Equal(string.Empty, record.GameStateJson);
        Assert.False(record.IsReviewed);
        Assert.Equal(100, record.Confidence);
    }

    [Fact]
    public void GameSessionRecord_Timestamp_DefaultsToUtcNow()
    {
        // Arrange & Act
        var before = DateTime.UtcNow.AddSeconds(-1);
        var record = new GameSessionRecord();
        var after = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.True(record.Timestamp >= before);
        Assert.True(record.Timestamp <= after);
    }

    [Fact]
    public void GameSessionRecord_CanSetAllProperties()
    {
        // Arrange
        var timestamp = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var reviewedAt = new DateTime(2025, 1, 15, 11, 0, 0, DateTimeKind.Utc);

        // Act
        var record = new GameSessionRecord
        {
            Id = 42,
            Timestamp = timestamp,
            GameName = "StardewValley",
            GameStateJson = "{\"health\": 100}",
            ScreenshotPath = "/path/to/screenshot.png",
            OcrResult = "Health: 100",
            IsReviewed = true,
            ReviewedAt = reviewedAt,
            CardType = "HealthPotion",
            Confidence = 95
        };

        // Assert
        Assert.Equal(42, record.Id);
        Assert.Equal(timestamp, record.Timestamp);
        Assert.Equal("StardewValley", record.GameName);
        Assert.Equal("{\"health\": 100}", record.GameStateJson);
        Assert.Equal("/path/to/screenshot.png", record.ScreenshotPath);
        Assert.Equal("Health: 100", record.OcrResult);
        Assert.True(record.IsReviewed);
        Assert.Equal(reviewedAt, record.ReviewedAt);
        Assert.Equal("HealthPotion", record.CardType);
        Assert.Equal(95, record.Confidence);
    }

    [Fact]
    public void GameSessionRecord_ReviewedAt_IsNullByDefault()
    {
        // Arrange & Act
        var record = new GameSessionRecord();

        // Assert
        Assert.Null(record.ReviewedAt);
    }

    [Fact]
    public void GameSessionRecord_ScreenshotPath_IsNullByDefault()
    {
        // Arrange & Act
        var record = new GameSessionRecord();

        // Assert
        Assert.Null(record.ScreenshotPath);
    }

    [Fact]
    public void GameSessionRecord_CardType_IsNullByDefault()
    {
        // Arrange & Act
        var record = new GameSessionRecord();

        // Assert
        Assert.Null(record.CardType);
    }

    [Fact]
    public void GameSessionRecord_Confidence_DefaultsTo100()
    {
        // Arrange & Act
        var record = new GameSessionRecord();

        // Assert
        Assert.Equal(100, record.Confidence);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void GameSessionRecord_Confidence_CanBeSetToValidValues(int confidence)
    {
        // Arrange & Act
        var record = new GameSessionRecord { Confidence = confidence };

        // Assert
        Assert.Equal(confidence, record.Confidence);
    }
}
