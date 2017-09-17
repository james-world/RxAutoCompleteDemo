using System.Threading.Tasks;
using RxAutoCompleteDemo.Model;

namespace RxAutoCompleteDemo.Services
{
    public interface IAutoCompleteService
    {
        Task<AutoCompleteResult> Query(string term);
    }
}