using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.Extensions.Logging;
using TeamHubConnect.Application.Common.Interfaces;

namespace TeamHubConnect.Infrastructure.Services.AI;

public class MLNetService : IMLService
{
    private readonly MLContext _mlContext;
    private readonly ILogger<MLNetService> _logger;
    private ITransformer? _sentimentModel;
    private ITransformer? _suggestionModel;

    public MLNetService(ILogger<MLNetService> logger)
    {
        _mlContext = new MLContext(seed: 0);
        _logger = logger;
    }

    public async Task<SentimentAnalysisResult> AnalyzeSentimentAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_sentimentModel == null)
            {
                await LoadSentimentModelAsync();
            }

            var input = new SentimentData { Text = text };
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(_sentimentModel!);
            var prediction = predictionEngine.Predict(input);

            return new SentimentAnalysisResult
            {
                IsPositive = prediction.IsPositive,
                Confidence = prediction.Probability,
                Score = prediction.Score
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing sentiment for text: {Text}", text);
            return new SentimentAnalysisResult { IsPositive = true, Confidence = 0.5f, Score = 0f };
        }
    }

    public async Task<List<string>> GetSmartSuggestionsAsync(
        string partialText,
        List<string> context,
        int maxSuggestions = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple rule-based suggestions for now
            var suggestions = new List<string>();

            // Common phrases and responses
            var commonPhrases = new[]
            {
                "Thanks for the update!",
                "Can you please provide more details?",
                "I'll look into this and get back to you.",
                "Great work on this!",
                "Let's schedule a meeting to discuss this.",
                "Could you share the files?",
                "I agree with this approach.",
                "Let me know if you need any help.",
                "This looks good to me.",
                "When is the deadline for this?"
            };

            // Filter suggestions based on partial text
            if (!string.IsNullOrEmpty(partialText))
            {
                suggestions.AddRange(commonPhrases
                    .Where(phrase => phrase.StartsWith(partialText, StringComparison.OrdinalIgnoreCase))
                    .Take(maxSuggestions));
            }

            // Add context-based suggestions
            if (context.Any())
            {
                var contextKeywords = ExtractKeywords(context);
                suggestions.AddRange(GenerateContextBasedSuggestions(contextKeywords, partialText)
                    .Take(maxSuggestions - suggestions.Count));
            }

            // Fill remaining with common phrases if needed
            if (suggestions.Count < maxSuggestions)
            {
                suggestions.AddRange(commonPhrases
                    .Where(phrase => !suggestions.Contains(phrase))
                    .Take(maxSuggestions - suggestions.Count));
            }

            return suggestions.Take(maxSuggestions).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating smart suggestions for text: {Text}", partialText);
            return [];
        }
    }

    public async Task<List<string>> ExtractKeywordsAsync(string text, int maxKeywords = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple keyword extraction using frequency analysis
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.Length > 3)
                .Where(word => !IsStopWord(word))
                .Select(word => word.ToLowerInvariant().Trim('.', ',', '!', '?', ';', ':'))
                .GroupBy(word => word)
                .OrderByDescending(group => group.Count())
                .Take(maxKeywords)
                .Select(group => group.Key)
                .ToList();

            return words;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting keywords from text");
            return [];
        }
    }

    public async Task<string> SummarizeConversationAsync(
        List<string> messages,
        int maxLength = 200,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!messages.Any())
                return "No messages to summarize.";

            // Simple extractive summarization
            var allText = string.Join(" ", messages);
            var sentences = allText.Split('.', '!', '?')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList();

            if (sentences.Count <= 3)
                return string.Join(". ", sentences) + ".";

            // Score sentences based on keyword frequency
            var keywords = await ExtractKeywordsAsync(allText, 10, cancellationToken);
            var sentenceScores = sentences.Select(sentence => new
            {
                Sentence = sentence,
                Score = keywords.Count(keyword => sentence.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            }).OrderByDescending(x => x.Score).ToList();

            // Take top sentences and arrange in original order
            var topSentences = sentenceScores.Take(3).Select(x => x.Sentence).ToList();
            var summary = string.Join(". ", topSentences);

            if (summary.Length > maxLength)
            {
                summary = summary[..maxLength].TrimEnd() + "...";
            }

            return summary + ".";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error summarizing conversation");
            return "Unable to summarize conversation.";
        }
    }

    public async Task<List<string>> DetectMentionsAsync(string text, List<string> availableUsers, CancellationToken cancellationToken = default)
    {
        try
        {
            var mentions = new List<string>();
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in words)
            {
                if (word.StartsWith("@"))
                {
                    var username = word[1..].ToLowerInvariant();
                    var matchedUser = availableUsers.FirstOrDefault(user => 
                        user.ToLowerInvariant() == username);
                    
                    if (matchedUser != null)
                    {
                        mentions.Add(matchedUser);
                    }
                }
            }

            return mentions.Distinct().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting mentions in text");
            return [];
        }
    }

    private async Task LoadSentimentModelAsync()
    {
        try
        {
            // For demo purposes, create a simple sentiment model
            // In production, you would load a pre-trained model
            var sampleData = new[]
            {
                new SentimentData { Text = "This is great!", IsPositive = true },
                new SentimentData { Text = "I love this", IsPositive = true },
                new SentimentData { Text = "Excellent work", IsPositive = true },
                new SentimentData { Text = "This is terrible", IsPositive = false },
                new SentimentData { Text = "I hate this", IsPositive = false },
                new SentimentData { Text = "This is bad", IsPositive = false }
            };

            var data = _mlContext.Data.LoadFromEnumerable(sampleData);

            var pipeline = _mlContext.Transforms.Text.FeaturizeText("Features", nameof(SentimentData.Text))
                .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression("Label", "Features"));

            _sentimentModel = pipeline.Fit(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading sentiment model");
        }
    }

    private List<string> ExtractKeywords(List<string> context)
    {
        var allText = string.Join(" ", context);
        return ExtractKeywordsAsync(allText).Result;
    }

    private List<string> GenerateContextBasedSuggestions(List<string> keywords, string partialText)
    {
        var suggestions = new List<string>();

        foreach (var keyword in keywords.Take(3))
        {
            suggestions.Add($"Can you tell me more about {keyword}?");
            suggestions.Add($"I have a question about {keyword}.");
            suggestions.Add($"Thanks for the information about {keyword}.");
        }

        return suggestions;
    }

    private bool IsStopWord(string word)
    {
        var stopWords = new HashSet<string>
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by",
            "is", "are", "was", "were", "be", "been", "being", "have", "has", "had", "do", "does", "did",
            "will", "would", "could", "should", "may", "might", "must", "can", "this", "that", "these", "those"
        };

        return stopWords.Contains(word.ToLowerInvariant());
    }
}

public class SentimentData
{
    [LoadColumn(0)]
    public string Text { get; set; } = "";

    [LoadColumn(1), ColumnName("Label")]
    public bool IsPositive { get; set; }
}

public class SentimentPrediction : SentimentData
{
    [ColumnName("PredictedLabel")]
    public bool IsPositive { get; set; }

    public float Probability { get; set; }
    public float Score { get; set; }
}