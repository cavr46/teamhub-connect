namespace TeamHubConnect.Application.Common.Interfaces;

public interface IMLService
{
    Task<SentimentAnalysisResult> AnalyzeSentimentAsync(string text, CancellationToken cancellationToken = default);
    Task<List<string>> GetSmartSuggestionsAsync(string partialText, List<string> context, int maxSuggestions = 5, CancellationToken cancellationToken = default);
    Task<List<string>> ExtractKeywordsAsync(string text, int maxKeywords = 10, CancellationToken cancellationToken = default);
    Task<string> SummarizeConversationAsync(List<string> messages, int maxLength = 200, CancellationToken cancellationToken = default);
    Task<List<string>> DetectMentionsAsync(string text, List<string> availableUsers, CancellationToken cancellationToken = default);
}

public class SentimentAnalysisResult
{
    public bool IsPositive { get; set; }
    public float Confidence { get; set; }
    public float Score { get; set; }
}