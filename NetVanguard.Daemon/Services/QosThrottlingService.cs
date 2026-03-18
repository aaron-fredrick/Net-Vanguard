using System;
using System.Diagnostics;
using System.IO;

namespace NetVanguard.Daemon.Services
{
    public interface IQosThrottlingService
    {
        void ApplyThrottleRule(string processName, long bitsPerSecond);
        void RemoveThrottleRule(string processName);
    }

    public class QosThrottlingService : IQosThrottlingService
    {
        private const string PolicyPrefix = "NV_Throttle_";

        public void ApplyThrottleRule(string processName, long bitsPerSecond)
        {
            try
            {
                // Remove existing policy first to avoid duplication conflicts
                RemoveThrottleRule(processName);

                var policyName = $"{PolicyPrefix}{processName.Replace(".exe", "")}";
                
                // Using PowerShell 5.1 built-in NetQosPolicy to shape traffic inherently.
                var psScript = $"New-NetQosPolicy -Name '{policyName}' -AppPathNameMatchCondition '{processName}' -ThrottleRateActionBitsPerSecond {bitsPerSecond} -NetworkProfile All";
                
                ExecutePowerShellCommand(psScript);
                Console.WriteLine($"[QOS] Throttling {processName} to {bitsPerSecond} bps applied.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[QOS ERROR] Cannot apply throttle rule: {ex.Message}");
            }
        }

        public void RemoveThrottleRule(string processName)
        {
            try
            {
                var policyName = $"{PolicyPrefix}{processName.Replace(".exe", "")}";
                var psScript = $"Remove-NetQosPolicy -Name '{policyName}' -Confirm:$false";
                
                ExecutePowerShellCommand(psScript);
                Console.WriteLine($"[QOS] Removed throttling on {processName}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[QOS ERROR] Cannot remove throttle rule: {ex.Message}");
            }
        }

        private void ExecutePowerShellCommand(string script)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                process.WaitForExit(5000); // 5 sec timeout
            }
        }
    }
}
