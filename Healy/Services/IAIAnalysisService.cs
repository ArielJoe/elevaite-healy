using Healy.Models;
using System.Threading.Tasks;

public interface IAiAnalysisService
{
    Task<InsightsData> CsvAnalyzer(string csvContent);
}
