using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetVanguard.App.Services;
using NetVanguard.Core.Models;

namespace NetVanguard.App.ViewModels
{
    public partial class FirewallViewModel : ObservableObject
    {
        private readonly ICommandClientService _commandClient;

        private ObservableCollection<FirewallRuleModel> _inboundRules = new();
        public ObservableCollection<FirewallRuleModel> InboundRules
        {
            get => _inboundRules;
            set => SetProperty(ref _inboundRules, value);
        }

        private ObservableCollection<FirewallRuleModel> _outboundRules = new();
        public ObservableCollection<FirewallRuleModel> OutboundRules
        {
            get => _outboundRules;
            set => SetProperty(ref _outboundRules, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public FirewallViewModel()
        {
            _commandClient = new CommandClientService();
            _ = LoadRulesAsync();
        }

        [RelayCommand]
        public async Task LoadRulesAsync()
        {
            IsLoading = true;
            try
            {
                var rules = await _commandClient.GetAllRulesAsync();
                
                InboundRules = new ObservableCollection<FirewallRuleModel>(rules.Where(r => r.Direction == FirewallDirection.Inbound));
                OutboundRules = new ObservableCollection<FirewallRuleModel>(rules.Where(r => r.Direction == FirewallDirection.Outbound));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading firewall rules: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task ToggleRuleAsync(FirewallRuleModel rule)
        {
            if (rule == null) return;
            
            bool newState = !rule.Enabled;
            bool success = await _commandClient.SetRuleEnabledAsync(rule.Name, newState);
            
            if (success)
            {
                rule.Enabled = newState;
                var list = rule.Direction == FirewallDirection.Inbound ? InboundRules : OutboundRules;
                var index = list.IndexOf(rule);
                if (index != -1)
                {
                    list[index] = rule;
                }
            }
        }

        [RelayCommand]
        public async Task AddRuleAsync(FirewallRuleModel rule)
        {
            if (rule == null) return;
            
            bool success = await _commandClient.AddRuleAsync(rule);
            if (success)
            {
                if (rule.Direction == FirewallDirection.Inbound)
                    InboundRules.Add(rule);
                else
                    OutboundRules.Add(rule);
            }
        }

        [RelayCommand]
        public async Task DeleteRuleAsync(FirewallRuleModel rule)
        {
            if (rule == null) return;

            bool success = await _commandClient.DeleteRuleAsync(rule.Name);
            if (success)
            {
                if (rule.Direction == FirewallDirection.Inbound)
                    InboundRules.Remove(rule);
                else
                    OutboundRules.Remove(rule);
            }
        }
    }
}
