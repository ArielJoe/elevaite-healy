using System.Threading.Tasks;

namespace Healy.Services
{
    public interface IAiAnalysisService<T>
    {
        Task<T> CsvAnalyzer(string csvContent);
    }
}