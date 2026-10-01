using AIResumeAnalyzer.Models;
using Microsoft.EntityFrameworkCore;

namespace AIResumeAnalyzer.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ResumeAnalysis> ResumeAnalyses => Set<ResumeAnalysis>();
}
