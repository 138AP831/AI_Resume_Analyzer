namespace AIResumeAnalyzer.DTOs;

public class ResumeAnalysisResponse
{
    public int Id { get; set; }
    public int MatchPercentage { get; set; }
    public List<string> MatchedSkills { get; set; } = [];
    public List<string> MissingSkills { get; set; } = [];
    public List<string> Suggestions { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
