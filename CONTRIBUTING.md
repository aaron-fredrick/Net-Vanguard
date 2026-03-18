# Contributing to Net-Vanguard

First off, thank you for considering contributing to Net-Vanguard! It's people like you that make Net-Vanguard a robust and powerful network traffic dashboard.

## Code of Conduct

By participating in this project, you are expected to uphold our Code of Conduct. Please be respectful and professional in all interactions.

## How Can I Contribute?

### Reporting Bugs
This section guides you through submitting a bug report for Net-Vanguard.
- **Check existing issues**: Before creating a new issue, please check if it has already been reported.
- **Use the Bug Report Template**: When creating a new issue, select the "Bug report" template and fill out all the requested information, including steps to reproduce, expected behavior, and environment details.

### Suggesting Enhancements
This section guides you through submitting an enhancement suggestion, including completely new features and minor improvements.
- **Use the Feature Request Template**: Select the "Feature request" template and clearly describe the problem you are trying to solve and your proposed solution.

### Pull Requests
We welcome pull requests! To ensure a smooth review process, please follow these guidelines:
1. **Fork the repository** and create your branch from `main`.
2. **Follow the Architecture**: Net-Vanguard uses a specific MVVM architecture and `CommunityToolkit.Mvvm`. Ensure your code aligns with these patterns.
3. **Follow the Global Rules**:
   - Write clean, DRY, and well-structured code.
   - Use meaningful, pronounceable names.
   - Keep functions small and focused on a single responsibility.
4. **Testing**: Add or update unit tests for your changes. Run the test suite before submitting your PR.
5. **Documentation**: Update any relevant documentation (e.g., in `.md` files) if your changes affect how the application is used or configured.
6. **Commit Messages**: Use clear and descriptive commit messages that follow conventional commit formats (e.g., `feat: Add new multi-view layout`).

## Development Setup
1. Ensure you have the .NET 8 SDK and the Windows App SDK workloads installed in Visual Studio.
2. Clone the repository and open `NetVanguard.sln`.
3. Set `NetVanguard.App` as the startup project.
4. Build and run. Note that the application requires Administrator privileges to start the ETW monitoring daemon.

Thank you for contributing!
