namespace AIResumeAnalyzer.Models;

public class ResumeAnalysis
{
    public int Id { get; set; }

    public string ResumeText { get; set; } = string.Empty;

    public string JobDescription { get; set; } = string.Empty;

    public int MatchPercentage { get; set; }

    public string MatchedSkillsJson { get; set; } = "[]";

    public string MissingSkillsJson { get; set; } = "[]";

    public string SuggestionsJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
