using EnterpriseAI.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace EnterpriseAI.Infrastructure.AI;

public class LocalEmbeddingService : IEmbeddingService
{
    private const int Dimensions = 128;

    public Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var vector = new float[Dimensions];

        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(vector);
        }

        var words = text
            .ToLowerInvariant()
            .Split(
                [' ', '\r', '\n', '\t', '.', ',', ':', ';', '(', ')'],
                StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            var hash = SHA256.HashData(
                Encoding.UTF8.GetBytes(word));

            var index =
                BitConverter.ToUInt32(hash, 0) % Dimensions;

            vector[index] += 1;
        }

        Normalize(vector);

        return Task.FromResult(vector);
    }

    private static void Normalize(float[] vector)
    {
        var magnitude = Math.Sqrt(
            vector.Sum(x => x * x));

        if (magnitude == 0)
        {
            return;
        }

        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] =
                (float)(vector[i] / magnitude);
        }
    }
}