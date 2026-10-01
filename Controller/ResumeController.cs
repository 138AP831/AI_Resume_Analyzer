using AIResumeAnalyzer.DTOs;
using AIResumeAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIResumeAnalyzer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResumeController : ControllerBase
{
    private readonly IResumeAnalyzerService _service;

    public ResumeController(IResumeAnalyzerService service)
    {
        _service = service;
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<ResumeAnalysisResponse>> Analyze(
        [FromBody] AnalyzeResumeRequest request)
    {
        try
        {
            var result = await _service.AnalyzeAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<ResumeAnalysisResponse>>> GetHistory()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ResumeAnalysisResponse>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
            return NotFound(new { message = "Analysis not found." });

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
            return NotFound(new { message = "Analysis not found." });

        return NoContent();
    }
}
