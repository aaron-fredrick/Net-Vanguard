using NetVanguard.Core.Models;
using System.Collections.Generic;

namespace NetVanguard.Core.Services
{
    /// <summary>
    /// Service responsible for interrogating and managing Windows Firewall Rules.
    /// Implemented primarily in the Daemon to utilize elevated privileges.
    /// </summary>
    public interface IFirewallManagerService
    {
        /// <summary>
        /// Gets a list of all current firewall rules.
        /// </summary>
        /// <returns>A collection of FirewallRuleModel.</returns>
        IEnumerable<FirewallRuleModel> GetAllRules();

        /// <summary>
        /// Adds a new rule to the firewall.
        /// </summary>
        /// <param name="rule">The rule definition to add.</param>
        void AddRule(FirewallRuleModel rule);

        /// <summary>
        /// Updates an existing rule by its name.
        /// </summary>
        /// <param name="ruleName">The name of the rule to update.</param>
        /// <param name="enabled">True to enable, false to disable.</param>
        void SetRuleEnabled(string ruleName, bool enabled);

        /// <summary>
        /// Deletes a rule from the firewall natively.
        /// </summary>
        /// <param name="ruleName">The rule to delete.</param>
        void DeleteRule(string ruleName);
    }
}
