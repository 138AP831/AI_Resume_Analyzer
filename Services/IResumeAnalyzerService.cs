using AIResumeAnalyzer.DTOs;

namespace AIResumeAnalyzer.Services;

public interface IResumeAnalyzerService
{
    Task<ResumeAnalysisResponse> AnalyzeAsync(AnalyzeResumeRequest request);
    Task<IEnumerable<ResumeAnalysisResponse>> GetAllAsync();
    Task<ResumeAnalysisResponse?> GetByIdAsync(int id);
    Task<bool> DeleteAsync(int id);
}
