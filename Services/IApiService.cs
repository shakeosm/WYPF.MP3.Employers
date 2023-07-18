using MCPhase3.Models;
using MCPhase3.ViewModels;
using System.Threading.Tasks;

namespace MCPhase3.Services
{
    public interface IApiService
    {
        /// <summary>This will update Score for a Remittance</summary>
        /// <param name="rBO">ReturnSubmitBO Model</param>
        /// <returns>ApiCallResult View Model</returns>
        Task<ApiCallResultVM> UpdateScore(ReturnSubmitBO rBO);
    }
}
