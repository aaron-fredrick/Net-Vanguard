using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetVanguard.Core.Models;

namespace NetVanguard.App.Services
{
    public interface ICommandClientService
    {
        Task<IEnumerable<FirewallRuleModel>> GetAllRulesAsync(CancellationToken token = default);
        Task<bool> AddRuleAsync(FirewallRuleModel rule, CancellationToken token = default);
        Task<bool> SetRuleEnabledAsync(string ruleName, bool enabled, CancellationToken token = default);
        Task<bool> DeleteRuleAsync(string ruleName, CancellationToken token = default);
    }
}
