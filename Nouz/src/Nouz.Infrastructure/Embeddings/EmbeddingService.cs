using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
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

    private async Task<string> GetEmbeddingModel()
    {
        var model = await _preferences.Get(PreferenceKeys.OpenAiEmbeddingModel).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(model) ? DefaultEmbeddingModel : model;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var apiKey = await _preferences.Get(PreferenceKeys.OpenAiApiKey).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return [];
        }

        var kernel = CreateKernel(apiKey, await GetEmbeddingModel().ConfigureAwait(false));

        var embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        var embeddings = await embeddingGenerator.GenerateAsync(
            [text],
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return embeddings[0].Vector.ToArray();
    }

    private static Kernel CreateKernel(string apiKey, string modelId)
    {
        var builder = Kernel.CreateBuilder();

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only
        builder.AddOpenAIEmbeddingGenerator(
            modelId: modelId,
            apiKey: apiKey);
#pragma warning restore SKEXP0010

        return builder.Build();
    }
}
