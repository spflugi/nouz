using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Nouz.Application.Embeddings;
using Nouz.Application.Preferences;
using IPreferences = Nouz.Application.Preferences.IPreferences;

namespace Nouz.Infrastructure.Embeddings;

internal sealed class EmbeddingService : IEmbeddingService
{
    private const string DefaultEmbeddingModel = "text-embedding-3-small";

    private readonly IPreferences _preferences;

    public EmbeddingService(IPreferences preferences)
    {
        _preferences = preferences;
    }

    private string GetEmbeddingModel()
    {
        var model = _preferences.Get(PreferenceKeys.OpenAiEmbeddingModel);
        return string.IsNullOrWhiteSpace(model) ? DefaultEmbeddingModel : model;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var apiKey = _preferences.Get(PreferenceKeys.OpenAiApiKey);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return [];
        }

        var kernel = CreateKernel(apiKey, GetEmbeddingModel());

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only
        var embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        var embeddings = await embeddingService.GenerateEmbeddingsAsync(
            [text],
            cancellationToken: cancellationToken).ConfigureAwait(false);
#pragma warning restore SKEXP0001

        return embeddings[0].ToArray();
    }

    private static Kernel CreateKernel(string apiKey, string modelId)
    {
        var builder = Kernel.CreateBuilder();

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only
        builder.AddOpenAITextEmbeddingGeneration(
            modelId: modelId,
            apiKey: apiKey);
#pragma warning restore SKEXP0010

        return builder.Build();
    }
}
