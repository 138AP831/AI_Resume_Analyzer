using System.Text.Json;
using AIResumeAnalyzer.Data;
using AIResumeAnalyzer.DTOs;
using AIResumeAnalyzer.Models;
using Microsoft.EntityFrameworkCore;

namespace AIResumeAnalyzer.Services;

public class ResumeAnalyzerService : IResumeAnalyzerService
{
    private readonly AppDbContext _db;

    // Common skills used for the MVP. This can later be replaced by an LLM.
    private static readonly string[] SkillDictionary =
    [
        "c#", ".net", "asp.net", "asp.net core", "entity framework",
        "sql", "sql server", "sqlite", "postgresql", "mongodb",
        "python", "java", "javascript", "typescript", "react",
        "angular", "node.js", "node", "html", "css",
        "git", "docker", "azure", "aws", "google cloud",
        "rest api", "rest", "web api", "microservices",
        "machine learning", "deep learning", "generative ai",
        "genai", "llm", "langchain", "fastapi", "redis"
    ];

    public ResumeAnalyzerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ResumeAnalysisResponse> AnalyzeAsync(AnalyzeResumeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ResumeText))
            throw new ArgumentException("Resume text is required.");

        if (string.IsNullOrWhiteSpace(request.JobDescription))
            throw new ArgumentException("Job description is required.");

        var resume = request.ResumeText.ToLowerInvariant();
        var job = request.JobDescription.ToLowerInvariant();

        var requiredSkills = SkillDictionary
            .Where(skill => job.Contains(skill))
            .Distinct()
            .ToList();

        var matchedSkills = requiredSkills
            .Where(skill => resume.Contains(skill))
            .ToList();

        var missingSkills = requiredSkills
            .Where(skill => !resume.Contains(skill))
            .ToList();

        var matchPercentage = requiredSkills.Count == 0
            ? 0
            : (int)Math.Round((double)matchedSkills.Count / requiredSkills.Count * 100);

        var suggestions = new List<string>();

        if (missingSkills.Count > 0)
            suggestions.Add($"Consider highlighting experience with: {string.Join(", ", missingSkills.Take(5))}.");

        if (resume.Length < 500)
            suggestions.Add("Your resume text appears short. Add measurable project or internship achievements.");

        if (!resume.Contains("project"))
            suggestions.Add("Add a dedicated Projects section with technologies and measurable outcomes.");

        if (!resume.Contains("experience") && !resume.Contains("intern"))
            suggestions.Add("Highlight internships, work experience, or relevant practical experience.");

        if (!resume.Contains("github") && !resume.Contains("linkedin"))
            suggestions.Add("Consider adding GitHub and LinkedIn links.");

        if (suggestions.Count == 0)
            suggestions.Add("Your resume covers the detected job requirements well. Quantify achievements where possible.");

        var entity = new ResumeAnalysis
        {
            ResumeText = request.ResumeText,
            JobDescription = request.JobDescription,
            MatchPercentage = matchPercentage,
            MatchedSkillsJson = JsonSerializer.Serialize(matchedSkills),
            MissingSkillsJson = JsonSerializer.Serialize(missingSkills),
            SuggestionsJson = JsonSerializer.Serialize(suggestions),
            CreatedAt = DateTime.UtcNow
        };

        _db.ResumeAnalyses.Add(entity);
        await _db.SaveChangesAsync();

        return ToResponse(entity);
    }

    public async Task<IEnumerable<ResumeAnalysisResponse>> GetAllAsync()
    {
        var records = await _db.ResumeAnalyses
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return records.Select(ToResponse);
    }

    public async Task<ResumeAnalysisResponse?> GetByIdAsync(int id)
    {
        var record = await _db.ResumeAnalyses.FindAsync(id);
        return record == null ? null : ToResponse(record);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var record = await _db.ResumeAnalyses.FindAsync(id);
        if (record == null)
            return false;

        _db.ResumeAnalyses.Remove(record);
        await _db.SaveChangesAsync();
        return true;
    }

    private static ResumeAnalysisResponse ToResponse(ResumeAnalysis entity)
    {
        return new ResumeAnalysisResponse
        {
            Id = entity.Id,
            MatchPercentage = entity.MatchPercentage,
            MatchedSkills = JsonSerializer.Deserialize<List<string>>(entity.MatchedSkillsJson) ?? [],
            MissingSkills = JsonSerializer.Deserialize<List<string>>(entity.MissingSkillsJson) ?? [],
            Suggestions = JsonSerializer.Deserialize<List<string>>(entity.SuggestionsJson) ?? [],
            CreatedAt = entity.CreatedAt
        };
    }
}
