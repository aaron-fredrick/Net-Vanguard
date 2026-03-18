namespace NetVanguard.Core.Models
{
    public enum CommandType
    {
        GetAllRules,
        AddRule,
        SetRuleEnabled,
        DeleteRule,
        SetLimit,
        GetLimits,
        DeleteLimit
    }

    public class CommandMessage
    {
        public CommandType Command { get; set; }
        
        /// <summary>
        /// JSON serialized payload (e.g., FirewallRuleModel) if required.
        /// </summary>
        public string? Payload { get; set; }
    }

    public class CommandResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// JSON serialized response data if applicable.
        /// </summary>
        public string? Payload { get; set; }
    }
}
