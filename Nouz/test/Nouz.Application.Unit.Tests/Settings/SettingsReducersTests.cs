using Nouz.Application.OpenAi;
using Nouz.Application.Settings;
using Shouldly;

namespace Nouz.Application.Unit.Tests.Settings;

public class SettingsReducersTests
{
    private readonly IEnumerable<Nouz.ReduxSimple.On<TestState>> _reducers;

    public SettingsReducersTests()
    {
        _reducers = SettingsReducers.Create<TestState>(s => s.Settings);
    }

    private TestState ApplyAction(TestState state, object action)
    {
        foreach (var reducer in _reducers)
        {
            if (reducer.Reduce != null)
            {
                state = reducer.Reduce(state, action);
            }
        }
        return state;
    }

    #region OpenAiApiKeyUpdated Tests

    [Fact]
    public void OpenAiApiKeyUpdated_ShouldUpdateApiKey()
    {
        // Arrange
        var state = new TestState();
        var newApiKey = "sk-test-123";

        // Act
        var newState = ApplyAction(state, new SettingsActions.OpenAiApiKeyUpdated(newApiKey));

        // Assert
        newState.Settings.OpenAiApiKey.ShouldBe(newApiKey);
    }

    [Fact]
    public void OpenAiApiKeyUpdated_WithNull_ShouldSetNull()
    {
        // Arrange
        var state = new TestState
        {
            Settings = new SettingsState { OpenAiApiKey = "existing-key" }
        };

        // Act
        var newState = ApplyAction(state, new SettingsActions.OpenAiApiKeyUpdated(null));

        // Assert
        newState.Settings.OpenAiApiKey.ShouldBeNull();
    }

    [Fact]
    public void OpenAiApiKeyUpdated_ShouldNotAffectOtherSettings()
    {
        // Arrange
        var state = new TestState
        {
            Settings = new SettingsState
            {
                OpenAiChatModel = "gpt-4",
                TopNRelevantNotes = 5
            }
        };

        // Act
        var newState = ApplyAction(state, new SettingsActions.OpenAiApiKeyUpdated("new-key"));

        // Assert
        newState.Settings.OpenAiChatModel.ShouldBe("gpt-4");
        newState.Settings.TopNRelevantNotes.ShouldBe(5);
    }

    #endregion

    #region OpenAiAdminKeyUpdated Tests

    [Fact]
    public void OpenAiAdminKeyUpdated_ShouldUpdateAdminKey()
    {
        // Arrange
        var state = new TestState();
        var newAdminKey = "admin-key-123";

        // Act
        var newState = ApplyAction(state, new SettingsActions.OpenAiAdminKeyUpdated(newAdminKey));

        // Assert
        newState.Settings.OpenAiAdminKey.ShouldBe(newAdminKey);
    }

    [Fact]
    public void OpenAiAdminKeyUpdated_WithNull_ShouldSetNull()
    {
        // Arrange
        var state = new TestState
        {
            Settings = new SettingsState { OpenAiAdminKey = "existing-admin-key" }
        };

        // Act
        var newState = ApplyAction(state, new SettingsActions.OpenAiAdminKeyUpdated(null));

        // Assert
        newState.Settings.OpenAiAdminKey.ShouldBeNull();
    }

    #endregion

    #region OpenAiChatModelUpdated Tests

    [Fact]
    public void OpenAiChatModelUpdated_ShouldUpdateChatModel()
    {
        // Arrange
        var state = new TestState();
        var newModel = "gpt-4-turbo";

        // Act
        var newState = ApplyAction(state, new SettingsActions.OpenAiChatModelUpdated(newModel));

        // Assert
        newState.Settings.OpenAiChatModel.ShouldBe(newModel);
    }

    [Fact]
    public void OpenAiChatModelUpdated_ShouldReplaceExistingModel()
    {
        // Arrange
        var state = new TestState
        {
            Settings = new SettingsState { OpenAiChatModel = "gpt-3.5-turbo" }
        };

        // Act
        var newState = ApplyAction(state, new SettingsActions.OpenAiChatModelUpdated("gpt-4o"));

        // Assert
        newState.Settings.OpenAiChatModel.ShouldBe("gpt-4o");
    }

    #endregion

    #region OpenAiEmbeddingModelUpdated Tests

    [Fact]
    public void OpenAiEmbeddingModelUpdated_ShouldUpdateEmbeddingModel()
    {
        // Arrange
        var state = new TestState();
        var newModel = "text-embedding-3-large";

        // Act
        var newState = ApplyAction(state, new SettingsActions.OpenAiEmbeddingModelUpdated(newModel));

        // Assert
        newState.Settings.OpenAiEmbeddingModel.ShouldBe(newModel);
    }

    [Fact]
    public void OpenAiEmbeddingModelUpdated_ShouldReplaceExistingModel()
    {
        // Arrange
        var state = new TestState
        {
            Settings = new SettingsState { OpenAiEmbeddingModel = "text-embedding-ada-002" }
        };

        // Act
        var newState = ApplyAction(state, new SettingsActions.OpenAiEmbeddingModelUpdated("text-embedding-3-small"));

        // Assert
        newState.Settings.OpenAiEmbeddingModel.ShouldBe("text-embedding-3-small");
    }

    #endregion

    #region TopNRelevantNotesUpdated Tests

    [Fact]
    public void TopNRelevantNotesUpdated_ShouldUpdateCount()
    {
        // Arrange
        var state = new TestState();

        // Act
        var newState = ApplyAction(state, new SettingsActions.TopNRelevantNotesUpdated(7));

        // Assert
        newState.Settings.TopNRelevantNotes.ShouldBe(7);
    }

    [Fact]
    public void TopNRelevantNotesUpdated_WithZero_ShouldSetZero()
    {
        // Arrange
        var state = new TestState
        {
            Settings = new SettingsState { TopNRelevantNotes = 5 }
        };

        // Act
        var newState = ApplyAction(state, new SettingsActions.TopNRelevantNotesUpdated(0));

        // Assert
        newState.Settings.TopNRelevantNotes.ShouldBe(0);
    }

    #endregion

    #region OpenAiUsageLoadingStarted Tests

    [Fact]
    public void OpenAiUsageLoadingStarted_ShouldSetIsLoadingUsageTrue()
    {
        // Arrange
        var state = new TestState
        {
            Settings = new SettingsState { IsLoadingUsage = false }
        };

        // Act
        var newState = ApplyAction(state, new SettingsActions.OpenAiUsageLoadingStarted());

        // Assert
        newState.Settings.IsLoadingUsage.ShouldBeTrue();
    }

    [Fact]
    public void OpenAiUsageLoadingStarted_ShouldNotClearExistingUsageData()
    {
        // Arrange
        var existingUsage = new OpenAiUsageData { InputTokens = 100, OutputTokens = 50 };
        var state = new TestState
        {
            Settings = new SettingsState { OpenAiUsage = existingUsage }
        };

        // Act
        var newState = ApplyAction(state, new SettingsActions.OpenAiUsageLoadingStarted());

        // Assert
        newState.Settings.OpenAiUsage.ShouldBe(existingUsage);
    }

    #endregion

    #region OpenAiUsageLoaded Tests

    [Fact]
    public void OpenAiUsageLoaded_ShouldSetUsageAndStopLoading()
    {
        // Arrange
        var state = new TestState
        {
            Settings = new SettingsState { IsLoadingUsage = true }
        };
        var usage = new OpenAiUsageData { InputTokens = 1000, OutputTokens = 500 };

        // Act
        var newState = ApplyAction(state, new SettingsActions.OpenAiUsageLoaded(usage));

        // Assert
        newState.Settings.OpenAiUsage.ShouldBe(usage);
        newState.Settings.IsLoadingUsage.ShouldBeFalse();
    }

    [Fact]
    public void OpenAiUsageLoaded_WithNull_ShouldClearUsageAndStopLoading()
    {
        // Arrange
        var existingUsage = new OpenAiUsageData { InputTokens = 100, OutputTokens = 50 };
        var state = new TestState
        {
            Settings = new SettingsState
            {
                OpenAiUsage = existingUsage,
                IsLoadingUsage = true
            }
        };

        // Act
        var newState = ApplyAction(state, new SettingsActions.OpenAiUsageLoaded(null));

        // Assert
        newState.Settings.OpenAiUsage.ShouldBeNull();
        newState.Settings.IsLoadingUsage.ShouldBeFalse();
    }

    [Fact]
    public void OpenAiUsageLoaded_ShouldNotAffectOtherSettings()
    {
        // Arrange
        var state = new TestState
        {
            Settings = new SettingsState
            {
                OpenAiApiKey = "key",
                OpenAiChatModel = "gpt-4",
                IsLoadingUsage = true
            }
        };
        var usage = new OpenAiUsageData { InputTokens = 1000, OutputTokens = 500 };

        // Act
        var newState = ApplyAction(state, new SettingsActions.OpenAiUsageLoaded(usage));

        // Assert
        newState.Settings.OpenAiApiKey.ShouldBe("key");
        newState.Settings.OpenAiChatModel.ShouldBe("gpt-4");
    }

    #endregion

    private sealed record TestState
    {
        public SettingsState Settings { get; init; } = new();
    }
}
