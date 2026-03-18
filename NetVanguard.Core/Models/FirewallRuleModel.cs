using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetVanguard.Core.Models
{
    public enum FirewallAction
    {
        Block = 0,
        Allow = 1
    }

    public enum FirewallDirection
    {
        Inbound = 1,
        Outbound = 2
    }

    public class FirewallRuleModel
    {
        /// <summary>
        /// Gets or sets the name of the firewall rule.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the application executable path the rule applies to.
        /// Can be null/empty if the rule applies system-wide.
        /// </summary>
        public string? ApplicationName { get; set; }

        /// <summary>
        /// Gets or sets the description of the rule.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Action of the rule (Block or Allow).
        /// </summary>
        public FirewallAction Action { get; set; }

        /// <summary>
        /// Direction of the rule (Inbound or Outbound).
        /// </summary>
        public FirewallDirection Direction { get; set; }

        /// <summary>
        /// Indicates if the rule is currently active.
        /// </summary>
        public bool Enabled { get; set; }
        
        /// <summary>
        /// The protocol used (e.g., 6 for TCP, 17 for UDP). Null for Any.
        /// </summary>
        public int? Protocol { get; set; }

        /// <summary>
        /// The local ports the rule applies to (e.g., "80, 443").
        /// </summary>
        public string? LocalPorts { get; set; }

        /// <summary>
        /// The remote ports the rule applies to.
        /// </summary>
        public string? RemotePorts { get; set; }
        
        /// <summary>
        /// Indicates whether this rule was created by Net-Vanguard.
        /// Used to organize/filter rules easily.
        /// </summary>
        public bool IsNetVanguardRule => Name.StartsWith("Net-Vanguard: ");
    }
}
