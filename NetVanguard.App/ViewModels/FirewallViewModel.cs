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

        private ObservableCollection<FirewallRuleModel> _rules = new();
        public ObservableCollection<FirewallRuleModel> Rules
        {
            get => _rules;
            set => SetProperty(ref _rules, value);
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
                
                // Switch to UI thread to update collection if needed, 
                // but usually ObservableCollection takes care of it or we replace the reference.
                Rules = new ObservableCollection<FirewallRuleModel>(rules);
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
            
            // Toggle the state
            bool newState = !rule.Enabled;
            bool success = await _commandClient.SetRuleEnabledAsync(rule.Name, newState);
            
            if (success)
            {
                rule.Enabled = newState;
                // Force UI update for the specific item
                var index = Rules.IndexOf(rule);
                if (index != -1)
                {
                    Rules[index] = rule;
                }
            }
        }

        [RelayCommand]
        public async Task DeleteRuleAsync(FirewallRuleModel rule)
        {
            if (rule == null) return;

            bool success = await _commandClient.DeleteRuleAsync(rule.Name);
            if (success)
            {
                Rules.Remove(rule);
            }
        }
    }
}
