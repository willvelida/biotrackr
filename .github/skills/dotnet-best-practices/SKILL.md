---
name: dotnet-best-practices
description: 'Ensure .NET/C# code meets best practices for the solution/project.'
last_updated: 2026-06-04
framework_revision: dotnet-10
---

# .NET/C# Best Practices

Your task is to ensure .NET/C# code in ${selection} meets the best practices specific to this solution/project. This includes:

## Documentation & Structure

- Create comprehensive XML documentation comments for all public classes, interfaces, methods, and properties
- Include parameter descriptions and return value descriptions in XML comments
- Follow the Biotrackr namespace pattern: `Biotrackr.{Domain}.{Type}` (e.g., `Biotrackr.Activity.Api`, `Biotrackr.Sleep.Svc`)

## Design Patterns & Architecture

- Use primary constructor syntax for dependency injection (e.g., `public class MyClass(IDependency dependency)`)
- Implement the Command Handler pattern with generic base classes (e.g., `CommandHandler<TOptions>`)
- Use interface segregation with clear naming conventions (prefix interfaces with 'I')
- Follow the Factory pattern for complex object creation.

## Dependency Injection & Services

- Use constructor dependency injection with null checks via ArgumentNullException
- Register services with appropriate lifetimes (Singleton, Scoped, Transient)
- Use Microsoft.Extensions.DependencyInjection patterns
- Implement service interfaces for testability

## Resource Management & Localization

- Use ResourceManager for localized messages and error strings
- Separate LogMessages and ErrorMessages resource files
- Access resources via `_resourceManager.GetString("MessageKey")`

## Async/Await Patterns

- Use async/await for all I/O operations and long-running tasks
- Return Task or Task<T> from async methods
- Use ConfigureAwait(false) where appropriate
- Handle async exceptions properly

## Testing Standards

- Use xUnit as the test runner
- Use FluentAssertions for assertions
- Use Moq for mocking dependencies
- Use AutoFixture for test data generation
- Follow AAA pattern (Arrange, Act, Assert) with explicit `// Arrange` / `// Act` / `// Assert` comments
- Test both success and failure scenarios
- Include null parameter validation tests

## Configuration & Settings

- Use strongly-typed configuration classes with data annotations
- Implement validation attributes (Required, NotEmptyOrWhitespace)
- Use IConfiguration binding for settings
- Support appsettings.json configuration files

## AI Integration (Microsoft Agent Framework + Copilot SDK)

- Use Microsoft Agent Framework (MAF) for conversational agents and tool orchestration
- Use the Microsoft Copilot SDK for sidecar-style code generation and reviewer workflows
- Register agent services with appropriate lifetimes; prefer managed identity for inter-service authentication
- Handle AI model settings (chat completion, embedding, structured output) via MAF configuration
- Use structured output patterns for reliable AI responses

## Error Handling & Logging

- Use structured logging with Microsoft.Extensions.Logging
- Include scoped logging with meaningful context
- Throw specific exceptions with descriptive messages
- Use try-catch blocks for expected failure scenarios

## Performance & Security

- Use C# 14 features and .NET 10 optimizations where applicable
- Implement proper input validation and sanitization
- Use parameterized queries for database operations
- Follow secure coding practices for AI/ML operations

## Code Quality

- Ensure SOLID principles compliance
- Avoid code duplication through base classes and utilities
- Use meaningful names that reflect domain concepts
- Keep methods focused and cohesive
- Implement proper disposal patterns for resources
