using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NetVanguard.Core.Models;
using NetVanguard.Core.Services;

namespace NetVanguard.Daemon.Services
{
    public class WindowsFirewallService : IFirewallManagerService
    {
        private const string PROG_ID_FWPOLICY2 = "HNetCfg.FwPolicy2";
        private const string PROG_ID_FWRULE = "HNetCfg.FWRule";

        public IEnumerable<FirewallRuleModel> GetAllRules()
        {
            var results = new List<FirewallRuleModel>();
            try
            {
                Type typeFwPolicy2 = Type.GetTypeFromProgID(PROG_ID_FWPOLICY2);
                if (typeFwPolicy2 == null) return results;

                dynamic fwPolicy2 = Activator.CreateInstance(typeFwPolicy2);
                dynamic rules = fwPolicy2.Rules;

                foreach (dynamic rule in (IEnumerable)rules)
                {
                    try
                    {
                        var model = new FirewallRuleModel
                        {
                            Name = rule.Name ?? string.Empty,
                            ApplicationName = rule.ApplicationName,
                            Description = rule.Description,
                            Action = (int)rule.Action == 1 ? FirewallAction.Allow : FirewallAction.Block,
                            Direction = (int)rule.Direction == 1 ? FirewallDirection.Inbound : FirewallDirection.Outbound,
                            Enabled = rule.Enabled,
                            Protocol = rule.Protocol == 256 ? null : (int)rule.Protocol,
                            LocalPorts = rule.LocalPorts == "*" ? null : rule.LocalPorts,
                            RemotePorts = rule.RemotePorts == "*" ? null : rule.RemotePorts
                        };
                        results.Add(model);
                    }
                    catch
                    {
                        // Some rules might throw exception on certain properties if invalid. Ignore them.
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] WindowsFirewallService Get Rules: {ex.Message}");
            }
            return results;
        }

        public void AddRule(FirewallRuleModel rule)
        {
            try
            {
                Type typeFwPolicy2 = Type.GetTypeFromProgID(PROG_ID_FWPOLICY2);
                Type typeFwRule = Type.GetTypeFromProgID(PROG_ID_FWRULE);

                if (typeFwPolicy2 == null || typeFwRule == null) return;

                dynamic fwPolicy2 = Activator.CreateInstance(typeFwPolicy2);
                dynamic fwRule = Activator.CreateInstance(typeFwRule);

                fwRule.Name = rule.Name;
                if (!string.IsNullOrEmpty(rule.Description)) fwRule.Description = rule.Description;
                if (!string.IsNullOrEmpty(rule.ApplicationName)) fwRule.ApplicationName = rule.ApplicationName;
                fwRule.Action = rule.Action == FirewallAction.Allow ? 1 : 0;
                fwRule.Direction = rule.Direction == FirewallDirection.Inbound ? 1 : 2;
                fwRule.Enabled = rule.Enabled;

                if (rule.Protocol.HasValue) fwRule.Protocol = rule.Protocol.Value;
                if (!string.IsNullOrEmpty(rule.LocalPorts)) fwRule.LocalPorts = rule.LocalPorts;
                if (!string.IsNullOrEmpty(rule.RemotePorts)) fwRule.RemotePorts = rule.RemotePorts;

                fwPolicy2.Rules.Add(fwRule);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] WindowsFirewallService Add Rule: {ex.Message}");
                throw;
            }
        }

        public void SetRuleEnabled(string ruleName, bool enabled)
        {
            try
            {
                Type typeFwPolicy2 = Type.GetTypeFromProgID(PROG_ID_FWPOLICY2);
                if (typeFwPolicy2 == null) return;

                dynamic fwPolicy2 = Activator.CreateInstance(typeFwPolicy2);
                dynamic rule = fwPolicy2.Rules.Item(ruleName);
                if (rule != null)
                {
                    rule.Enabled = enabled;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] WindowsFirewallService Enabled Rule: {ex.Message}");
                throw;
            }
        }

        public void DeleteRule(string ruleName)
        {
            try
            {
                Type typeFwPolicy2 = Type.GetTypeFromProgID(PROG_ID_FWPOLICY2);
                if (typeFwPolicy2 == null) return;

                dynamic fwPolicy2 = Activator.CreateInstance(typeFwPolicy2);
                fwPolicy2.Rules.Remove(ruleName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] WindowsFirewallService Delete Rule: {ex.Message}");
                throw;
            }
        }
    }
}
