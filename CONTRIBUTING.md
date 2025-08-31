# Contributing to Archistrateia

Thank you for your interest in contributing to Archistrateia! This document provides guidelines and information for contributors.

## 🚀 Quick Start

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Commit using conventional commit format
5. Push to your branch (`git push origin feature/amazing-feature`)
6. Open a Pull Request

## 📝 Conventional Commits

We use [Conventional Commits](https://www.conventionalcommits.org/) for automatic versioning and changelog generation. This means your commit messages should follow this format:

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Commit Types

| Type | Description | Version Impact |
|------|-------------|----------------|
| `feat` | A new feature | **Minor** version bump |
| `fix` | A bug fix | **Patch** version bump |
| `refactor` | Code refactoring (no functional changes) | **Patch** version bump |
| `perf` | Performance improvements | **Patch** version bump |
| `docs` | Documentation changes | No version bump |
| `style` | Code style changes (formatting, etc.) | No version bump |
| `test` | Adding or updating tests | No version bump |
| `chore` | Maintenance tasks | No version bump |
| `ci` | CI/CD changes | No version bump |
| `build` | Build system changes | No version bump |

### Breaking Changes

To indicate a breaking change, add `!` after the type/scope and include `BREAKING CHANGE:` in the footer:

```
feat!: add new API that breaks existing functionality

BREAKING CHANGE: The old API has been removed in favor of the new one.
```

### Examples

```bash
# New feature
git commit -m "feat: add multiplayer support for up to 4 players"

# Bug fix
git commit -m "fix: resolve movement pathfinding issue on complex terrain"

# Refactoring
git commit -m "refactor: modernize movement system to use Godot's AStar2D"

# Performance improvement
git commit -m "perf: optimize pathfinding algorithm for 3x faster performance"

# Documentation
git commit -m "docs: update README with new installation instructions"

# Breaking change
git commit -m "feat!: redesign combat system API

BREAKING CHANGE: Combat resolution methods have been completely redesigned"
```

## 🧪 Testing

Before submitting your changes:

1. **Run all tests**: `./run_tests.sh`
2. **Ensure 100% pass rate**: All tests must pass
3. **Add tests for new features**: New functionality should include tests
4. **Update existing tests**: If you change existing behavior, update relevant tests

## 📚 Code Style

- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Keep methods focused and single-purpose
- Use consistent formatting (let your editor handle this)

## 🔄 Pull Request Process

1. **Title**: Use conventional commit format (e.g., "feat: add multiplayer support")
2. **Description**: Clearly describe what your PR does and why
3. **Tests**: Ensure all tests pass
4. **Screenshots**: If UI changes, include before/after screenshots
5. **Breaking Changes**: Clearly document any breaking changes

### PR Template

```markdown
## Description
Brief description of what this PR accomplishes.

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update

## Testing
- [ ] All tests pass
- [ ] New tests added for new functionality
- [ ] Existing tests updated if needed

## Breaking Changes
If this PR includes breaking changes, describe them here.

## Screenshots
If applicable, add screenshots to help explain your changes.
```

## 🏷️ Issue Labels

We use the following labels for issues and PRs:

- `bug` - Something isn't working
- `enhancement` - New feature or request
- `documentation` - Improvements or additions to documentation
- `good first issue` - Good for newcomers
- `help wanted` - Extra attention is needed
- `priority: high` - High priority issues
- `priority: low` - Low priority issues
- `refactor` - Code refactoring
- `test` - Adding or updating tests

## 📋 Development Workflow

1. **Create Issue**: Start with an issue describing what you want to work on
2. **Assign Yourself**: Comment on the issue to claim it
3. **Create Branch**: Use `feature/issue-number-description` format
4. **Develop**: Make your changes with regular commits
5. **Test**: Ensure all tests pass
6. **Submit PR**: Create a pull request with clear description
7. **Review**: Address any feedback from code review
8. **Merge**: Once approved, your changes will be merged

## 🆘 Getting Help

- **Discussions**: Use GitHub Discussions for questions and ideas
- **Issues**: Report bugs or request features via Issues
- **Wiki**: Check the project wiki for additional documentation

## 📄 License

By contributing to Archistrateia, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to Archistrateia! 🎮✨
