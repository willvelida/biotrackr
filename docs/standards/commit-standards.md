# Git Commit Standards

**Biotrackr Conventional Commits & AI Contribution Tracking**

This document defines commit message formatting standards and AI contribution tracking requirements for the Biotrackr project. All commits must follow the Conventional Commits specification with structured trailers for AI coding agent usage.

## Conventional Commits Format

All commits must follow the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) specification:

### Basic Structure

```text
{type}[optional scope]: {description}

[optional body]

[optional footer(s)]
```

### Required Components

#### Commit Types

Six standard types are accepted:

| Type | Purpose | Example Use Case |
|------|---------|------------------|
| `feat` | New feature or capability | Adding new API endpoint, implementing new service |
| `fix` | Bug fix or defect resolution | Fixing validation error, correcting integration logic |
| `core` | Infrastructure or tooling changes | Bicep module updates, CI/CD pipeline changes |
| `docs` | Documentation updates | README changes, decision records, code comments |
| `refactor` | Code restructuring without behavior change | Extracting methods, reorganizing modules |
| `test` | Test additions or modifications | Adding unit tests, updating test fixtures |

#### Scope (Optional)

Scope provides additional context about the area affected:

```text
feat(weight-api): add weight entry validation endpoint
fix(activity-svc): handle null response from Fitbit API
core(infra): update Azure Function app configuration
docs(decision-records): document service lifetime patterns
test(sleep-api): add E2E tests for sleep data retrieval
```

**Scope Guidelines:**
- Use lowercase, alphanumeric characters with dashes
- Keep concise and meaningful
- Common scopes: 
  - Services: `weight-svc`, `activity-svc`, `sleep-svc`, `food-svc`, `auth-svc`
  - APIs: `weight-api`, `activity-api`, `sleep-api`
  - Infrastructure: `infra`, `bicep`, `cosmos`, `apim`
  - Cross-cutting: `tests`, `workflows`, `ci-cd`, `docker`

#### Description

The description summarizes the change:

- **Maximum 50 characters** (subject line limit)
- Use imperative mood ("add" not "added" or "adds")
- No period at the end
- Start with lowercase after the colon

### Message Body (Optional)

For complex changes, add a body after a blank line:

- **Maximum 72 characters per line**
- Explain the **what** and **why**, not the how
- Use bullet points for multiple changes
- Separate from subject with a blank line

**Example:**
```text
feat(weight-svc): add Cosmos DB integration tests

- Implement E2E tests with Cosmos DB Emulator
- Add test isolation with container cleanup
- Configure Gateway mode for local testing
- Achieve 97% code coverage
```

## AI Contribution Tracking

### Required Trailers

When AI coding agents contribute to a commit, capture this information using git trailers. All three trailers must be present together—if you include one, you must include all three.

### Trailer Format

```text
agent: {agent-name}
model: {model-name}
contribution: {contribution-type}
```

### Trailer Values

#### Agent Names
Common AI coding agents:
- `github-copilot`
- `cursor`
- `claude-code`
- `codeium`
- `tabnine`
- `cline`

#### Model Names
Specific AI models used:
- `GPT-4.1`
- `GPT-4o`
- `GPT-5-Codex`
- `o4-mini`
- `Claude Sonnet 4`
- `Claude Sonnet 4.5`
- `Claude Haiku 4.5`
- `Gemini 2.5 Pro`

#### Contribution Types
Four standard contribution categories:

| Type | Description | Example Use |
|------|-------------|-------------|
| `code-generation` | AI generated new code | Generated service implementation, created new endpoint |
| `refactoring` | AI restructured existing code | Extracted methods, improved code organization |
| `documentation` | AI authored documentation | Generated docstrings, wrote decision records |
| `test-generation` | AI created test code | Generated unit tests, created test fixtures |

### Trailer Consistency Rule

**All three trailers are required together—if any trailer is present, all three must be included.**

✅ **Valid:**
```text
fix(auth-svc): handle expired token gracefully

agent: github-copilot
model: Claude Sonnet 4.5
contribution: code-generation
```

❌ **Invalid (incomplete trailers):**
```text
fix(auth-svc): handle expired token gracefully

agent: github-copilot
```

### Git Command Format

All commits must be signed with the `-s` flag to comply with the [Developer Certificate of Origin (DCO)](https://developercertificate.org/). Use the `--trailer` flag to add structured trailers:

```bash
git commit -s -m "feat(weight-api): add weight entry validation endpoint

- Implement request model validation
- Add custom validators for weight ranges
- Return structured error responses
- Add unit tests for validation logic" \
  --trailer "agent: github-copilot" \
  --trailer "model: Claude Sonnet 4.5" \
  --trailer "contribution: code-generation"
```

**Note:** The `-s` flag automatically adds a `Signed-off-by` trailer with your name and email from your Git configuration.

## Complete Examples

### Simple Commit (No AI)

```bash
git commit -s -m "fix(activity-svc): correct Fitbit API response parsing"
```

Result:
```text
fix(activity-svc): correct Fitbit API response parsing

Signed-off-by: Your Name <your.email@example.com>
```

### Simple Commit (With AI)

```bash
git commit -s -m "fix(auth-svc): handle expired token gracefully" \
  --trailer "agent: github-copilot" \
  --trailer "model: Claude Sonnet 4.5" \
  --trailer "contribution: code-generation"
```

Result:
```text
fix(auth-svc): handle expired token gracefully

Signed-off-by: Your Name <your.email@example.com>
agent: github-copilot
model: Claude Sonnet 4.5
contribution: code-generation
```

### Complex Commit (No AI)

```bash
git commit -s -m "feat(weight-svc): add Cosmos DB integration tests

- Implement E2E tests with Cosmos DB Emulator
- Add test isolation with container cleanup
- Configure Gateway mode for local testing
- Achieve 97% code coverage"
```

### Complex Commit (With AI)

```bash
git commit -s -m "docs(decision-records): document service lifetime patterns

- Added comprehensive guide for DI service lifetimes
- Documented HttpClient service registration patterns
- Included examples for Azure SDK client registration
- Added prevention strategies for common issues" \
  --trailer "agent: github-copilot" \
  --trailer "model: Claude Sonnet 4.5" \
  --trailer "contribution: documentation"
```

### Multiple Scope Example

A scope is a single lowercase token with dashes. Commas are not valid, so a change
spanning two areas takes the broader of the two rather than listing both.

```bash
git commit -s -m "core(infra): migrate function config to bicep

- Migrate to Bicep modules structure
- Add environment-specific parameters
- Update Key Vault references
- Configure managed identity access" \
  --trailer "agent: cline" \
  --trailer "model: Claude Sonnet 4.5" \
  --trailer "contribution: refactoring"
```

## Validation & Enforcement

### Developer Certificate of Origin (DCO)

All commits must include a `Signed-off-by` line to certify that you have the right to submit the code under the project's license. This is enforced via DCO checks on GitHub.

**How to Sign Commits:**
Always use the `-s` flag when committing:
```bash
git commit -s -m "your commit message"
```

This automatically adds:
```text
Signed-off-by: Your Name <your.email@example.com>
```

**Bot exemption:** commits authored by `github-actions[bot]` or `dependabot[bot]` are
exempt from the sign-off requirement, and `.githooks/commit-msg` skips the check for them.
Every other author, human or agent, must sign off.

**Fixing Unsigned Commits:**
If you forgot to sign a commit, you can amend it:
```bash
git commit --amend -s --no-edit
```

For multiple unsigned commits, use interactive rebase:
```bash
git rebase --signoff HEAD~3  # Sign last 3 commits
```

### GitHub Actions Validation

Pull request workflows validate commit standards automatically:

**Validation Checks:**
- DCO sign-off presence (required)
- Conventional Commit format compliance
- Trailer consistency (all three or none)
- Branch naming convention adherence
- Commit message length limits

**Bypass Protection:**
Validation runs as a required status check on pull requests to `main` branch.

### Validation Patterns

The following regex patterns are used for validation:

**Commit Subject Pattern:**
```regex
^(feat|fix|core|docs|refactor|test)(\([a-z0-9._-]+\))?: .{1,72}$
```

**Branch Naming Pattern:**
```regex
^(feat|fix|core|docs|refactor|test)/[a-z0-9][a-z0-9-]*$
```

## Best Practices

### DO:
- **Always use `-s` flag** to sign commits (DCO requirement)
- Keep subject lines under 50 characters  
- Use imperative mood in descriptions  
- Include all three trailers when AI contributes  
- Add body for non-trivial changes  
- Reference GitHub issue numbers in branch names  
- Use meaningful scope names  

### DON'T:
- Forget to sign commits with `-s` flag
- Exceed 72 characters per body line  
- Use past tense in commit messages  
- Include partial trailer sets  
- Commit directly to `main` branch  
- Use vague descriptions like "fix bug" or "update code"  
- Mix multiple unrelated changes in one commit  

## Common Scenarios

### Adding Tests
```bash
git commit -s -m "test(weight-api): add unit tests for weight controller

- Add tests for GET /api/weights endpoint
- Add tests for POST /api/weights validation
- Achieve 100% controller code coverage" \
  --trailer "agent: github-copilot" \
  --trailer "model: Claude Sonnet 4.5" \
  --trailer "contribution: test-generation"
```

### Infrastructure Updates
```bash
git commit -s -m "core(workflows): add E2E test workflow for Sleep Service

- Configure Cosmos DB Emulator in GitHub Actions
- Add test isolation and cleanup steps
- Set up code coverage reporting"
```

### Documentation Changes
```bash
git commit -s -m "docs(readme): update local development setup instructions

- Add Cosmos DB Emulator setup steps
- Document required environment variables
- Include troubleshooting section"
```

### Bug Fixes
```bash
git commit -s -m "fix(activity-svc): prevent duplicate activity entries

- Add unique constraint validation
- Check for existing entries before insert
- Return 409 Conflict for duplicates
- Add integration test for duplicate detection"
```

## References

- **Conventional Commits Specification**: [conventionalcommits.org](https://www.conventionalcommits.org/en/v1.0.0/)
- **Developer Certificate of Origin**: [developercertificate.org](https://developercertificate.org/)
- **Project Structure**: [README.md](../../README.md)
- **Decision Records**: [docs/decision-records/](../decision-records/)
- **Contributing Guide**: [CONTRIBUTING.md](../../CONTRIBUTING.md) *(if available)*

## Quick Reference Card

| Element | Format | Max Length | Required |
|---------|--------|------------|----------|
| Type | `feat\|fix\|core\|docs\|refactor\|test` | N/A | ✅ Yes |
| Scope | `(scope-name)` | ~20 chars | ❌ Optional |
| Description | Imperative, lowercase start | 50 chars | ✅ Yes |
| Body line | Bullet or paragraph | 72 chars | ❌ Optional |
| Trailer: agent | `agent: {name}` | N/A | ⚠️ If any trailer |
| Trailer: model | `model: {name}` | N/A | ⚠️ If any trailer |
| Trailer: contribution | `contribution: {type}` | N/A | ⚠️ If any trailer |

---

**Document Version**: 1.0  
**Last Updated**: November 2025  
**Maintained By**: Biotrackr Project Contributors
