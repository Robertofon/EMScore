# Coding Style Guide for EMScore

This document outlines the coding conventions and style guidelines for the EMScore project.

## Naming Conventions

- Use PascalCase for classes, methods, properties
- Use camelCase for local variables and parameters
- Prefix interfaces with "I" (e.g., `IEnergyMeasurementRepository`)
- Use descriptive, meaningful names
- Use noun-based names for classes (e.g., `EnergyMeasurement`)
- Use verb-based names for methods (e.g., `CalculateTotal`, `ProcessData`)
- Use prefix "Is", "Has", "Can" for boolean properties (e.g., `IsActive`, `HasValue`)
- Use suffix "Async" for asynchronous methods (e.g., `LoadDataAsync`, `SaveChangesAsync`)

## Code Style Guidelines

- Follow .NET naming conventions
- Keep methods focused and small (< 50 lines when possible)
- Prefer async/await for I/O operations
- Use dependency injection throughout
- Handle exceptions appropriately with meaningful error messages
- Use null-aware operators (?. , ??) for null checking
- Use expression-bodied members for simple methods and properties
- Use object and collection initializers when appropriate
- Use var keyword when type is obvious from context
- Use named arguments for clarity when calling methods with multiple parameters
- Use string interpolation ($"Hello {name}") instead of string concatenation or String.Format
- Use readonly fields when possible
- Use properties instead of public fields
- Use access modifiers appropriately (private by default)

## Comments and Documentation

- Use XML documentation for public APIs
- Use /// for single-line comments
- Use /**/ for multi-line comments when needed
- Add comments for complex logic or non-obvious implementations
- Use TODO comments for future work items (with optional assignee and date)
- Avoid redundant comments that just repeat what code says
- Use comment blocks to separate logical sections within methods
- Document public and protected members with XML comments
- Include <summary>, <param>, <returns>, and <exception> tags in XML documentation
- Use <remarks> for additional information
- Use <example> for code examples when beneficial

## Formatting

- Use 4 spaces for indentation (no tabs)
- Place opening braces on the same line as the declaration
- Place closing braces on their own line
- Use blank lines to separate logical sections within methods
- Limit line length to 120 characters when possible
- Use spaces around operators and after commas
- No spaces inside parentheses
- Use empty lines between method definitions and property definitions
- Organize using statements: System namespaces first, then third-party, then project-specific
- Place using statements inside namespace when appropriate to reduce scope

## Specific Language Features

### Async/Await
- Always use async/await for I/O-bound operations
- Avoid async void methods except for event handlers
- Use ConfigureAwait(false) when appropriate for library code
- Handle exceptions properly in async methods
- Use Task.WhenAll for parallel operations when appropriate
- Use Task.WhenAny for racing operations when appropriate

### LINQ
- Use LINQ queries for readable data transformation
- Prefer method syntax over query syntax for complex operations
- Be aware of deferred execution
- Use ToList() or ToArray() when immediate execution is needed
- Use FirstOrDefault() or SingleOrDefault() with null checks
- Use Any() instead of Count() > 0 for efficiency

### Exception Handling
- Catch specific exceptions rather than general Exception
- Use exception filters when appropriate (C# 6+)
- Throw exceptions using throw; (not throw ex;) to preserve stack trace
- Validate arguments at the beginning of methods
- Use ArgumentNullException, ArgumentOutOfRangeException, etc. for argument validation
- Consider using custom exception types for domain-specific errors

### Resources and Disposal
- Use using statements for IDisposable objects
- Implement IDisposable correctly when managing unmanaged resources
- Consider using SafeHandle for unmanaged resources
- Implement finalizers only when necessary
- Follow the dispose pattern correctly

## File Organization

- One class per file (preferred)
- Partial classes only when necessary (e.g., designer-generated code)
- Group related files in folders by feature or layer
- Use consistent folder structure across the project
- Keep files focused and cohesive
- Use region sparingly and only for logical grouping within large files

## Reviews and Quality

- Participate in code reviews constructively
- Address review comments promptly
- Ensure code builds without warnings
- Run unit tests before submitting code
- Follow the team's branching strategy
- Write meaningful commit messages
- Keep changes focused and atomic