# Fiji Enterprise Payroll System — Coding Standards

**Version:** 1.0.0  
**Date:** June 2026  
**Status:** Approved  
**Owner:** Senior C# Developer  

---

## 1. Language & Framework

| Item | Standard |
|------|----------|
| Language | C# 12 |
| Runtime | .NET 8 (LTS) |
| UI Framework | WPF with Prism 9 |
| Target OS | Windows 10/11, Windows Server 2016+ |
| IDE | Visual Studio 2022 (17.8+) |
| SDK | .NET 8 SDK |

---

## 2. File Organisation

- One class per file
- File name exactly matches class name (including case)
- Namespace must match folder structure
- No partial classes (except auto-generated code and XAML code-behind)

---

## 3. Naming Conventions

### General Rules
- Use **PascalCase** for: classes, interfaces, methods, properties, events, namespaces, enums, constants
- Use **camelCase** for: local variables, method parameters, private fields
- Use **_camelCase** (underscore prefix) for: private instance fields
- Never use Hungarian notation (no `strName`, `intCount`)
- Abbreviations of 3+ characters: PascalCase (`HtmlParser`, not `HTMLParser`)
- Well-known 2-letter abbreviations: all caps (`Id`, `OK`, `IO`)

### Specific Conventions

| Type | Convention | Example |
|------|-----------|---------|
| Interface | `I` prefix + PascalCase | `IEmployeeRepository` |
| Abstract class | No special prefix | `BaseEntity` |
| Generic type param | `T` or descriptive `TEntity` | `TEntity`, `TResult` |
| Enum | PascalCase (singular) | `PayrollStatus` |
| Enum values | PascalCase | `PayrollStatus.Approved` |
| Constants | PascalCase | `MaxRetryCount` |
| Private field | `_camelCase` | `_employeeService` |
| Event | PascalCase verb past/noun | `PayrollProcessed`, `EmployeeCreated` |
| Event handler | `On` + EventName | `OnPayrollProcessed` |
| Async method | PascalCase + `Async` suffix | `GetEmployeeByIdAsync` |
| Extension method | PascalCase in static class | `StringExtensions.ToTitleCase()` |
| Test class | `[ClassUnderTest]Tests` | `EmployeeServiceTests` |
| Test method | `[Method]_[Scenario]_[Expected]` | `CalculatePAYE_ForResident_ReturnsCorrectTax` |

---

## 4. Code Style

### 4.1 Braces
```csharp
// CORRECT — always use braces, even for single-line if
if (employee.IsActive)
{
    ProcessPayroll(employee);
}

// INCORRECT
if (employee.IsActive)
    ProcessPayroll(employee);
```

### 4.2 var Usage
```csharp
// CORRECT — use var when type is obvious from assignment
var employees = new List<Employee>();
var result = await _repository.GetByIdAsync(id);

// CORRECT — use explicit type when not obvious
IEnumerable<Employee> employees = GetEmployees();
decimal taxAmount = CalculatePAYE(grossPay);

// INCORRECT — never use var for primitives
var count = 0;  // use int count = 0;
var name = "John";  // use string name = "John";
```

### 4.3 String Handling
```csharp
// CORRECT — use interpolation for simple cases
var message = $"Employee {employee.FullName} processed";

// CORRECT — use StringBuilder for loops
var sb = new StringBuilder();
foreach (var emp in employees)
{
    sb.AppendLine($"{emp.Code}: {emp.FullName}");
}

// CORRECT — use string.IsNullOrWhiteSpace for null checks
if (string.IsNullOrWhiteSpace(employeeCode))
{
    throw new ArgumentException("Employee code is required", nameof(employeeCode));
}
```

### 4.4 Null Handling
```csharp
// CORRECT — use null-coalescing
var name = employee?.FullName ?? "Unknown";

// CORRECT — use null-conditional 
var salary = employee?.PayrollDetails?.AnnualSalary;

// CORRECT — use ArgumentNullException.ThrowIfNull
ArgumentNullException.ThrowIfNull(employee, nameof(employee));

// CORRECT — nullable reference types enabled
public string? MiddleName { get; set; }
public string FirstName { get; set; } = string.Empty;
```

### 4.5 Collections
```csharp
// CORRECT — return IReadOnlyList for immutable data
public IReadOnlyList<Employee> GetAll() => _employees.AsReadOnly();

// CORRECT — use collection expressions (C# 12)
List<string> names = ["Alice", "Bob", "Charlie"];

// CORRECT — LINQ for query, not mutation
var activeEmployees = employees
    .Where(e => e.IsActive)
    .OrderBy(e => e.LastName)
    .ThenBy(e => e.FirstName)
    .ToList();
```

---

## 5. Async/Await Standards

```csharp
// CORRECT — all I/O must be async
public async Task<Employee?> GetEmployeeByIdAsync(int id, CancellationToken ct = default)
{
    return await _repository.GetByIdAsync(id, ct);
}

// CORRECT — use ConfigureAwait(false) in library code (non-UI layers)
var employee = await _repository.GetByIdAsync(id).ConfigureAwait(false);

// INCORRECT — blocking on async code
var employee = _repository.GetByIdAsync(id).Result; // Never do this
var employee = _repository.GetByIdAsync(id).GetAwaiter().GetResult(); // Never

// CORRECT — CancellationToken propagation
public async Task ProcessPayrollRunAsync(int runId, CancellationToken ct)
{
    var employees = await _repository.GetActiveEmployeesAsync(runId, ct);
    foreach (var employee in employees)
    {
        ct.ThrowIfCancellationRequested();
        await CalculatePayAsync(employee, ct);
    }
}
```

---

## 6. Exception Handling Standards

```csharp
// CORRECT — catch specific exceptions
try
{
    await _payrollService.ProcessAsync(runId, ct);
}
catch (PayrollCalculationException ex)
{
    _logger.LogError(ex, "Payroll calculation failed for run {RunId}", runId);
    return Result.Failure($"Calculation error: {ex.Message}");
}
catch (DbUpdateConcurrencyException ex)
{
    _logger.LogWarning(ex, "Concurrency conflict on run {RunId}", runId);
    return Result.Failure("Data was modified by another user. Please refresh and retry.");
}

// INCORRECT — swallowing exceptions
try { ... }
catch { } // Never

// INCORRECT — catching Exception without re-throw or logging
try { ... }
catch (Exception ex)
{
    return false; // Lost the exception context
}

// CORRECT — using domain exceptions with meaningful messages
throw new PayrollCalculationException(
    $"Cannot calculate PAYE for employee {employeeCode}: Tax table not found for period {period}");
```

---

## 7. Logging Standards

```csharp
// CORRECT — structured logging (not string formatting)
_logger.LogInformation("Processing payroll run {RunId} for company {CompanyId}", runId, companyId);

// INCORRECT — string interpolation in logging
_logger.LogInformation($"Processing payroll run {runId}"); // Loses structured data

// CORRECT — log method entry/exit for key operations
_logger.LogDebug("Starting payroll calculation for employee {EmployeeId}", employeeId);
// ... do work ...
_logger.LogDebug("Completed payroll calculation for employee {EmployeeId}. Net: {NetPay:C}", employeeId, netPay);

// CORRECT — always include exception object
_logger.LogError(ex, "Failed to process employee {EmployeeId}", employeeId);
```

---

## 8. SOLID & Clean Code

### Single Responsibility
```csharp
// CORRECT — one responsibility per class
public class PAYECalculationService  // only calculates PAYE
public class FNPFCalculationService  // only calculates FNPF
public class PayslipGeneratorService // only generates payslips

// INCORRECT — God class
public class PayrollService  // calculates PAYE + FNPF + generates payslips + sends emails...
```

### Dependency Injection
```csharp
// CORRECT — constructor injection
public class CreateEmployeeCommandHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateEmployeeCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public CreateEmployeeCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateEmployeeCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }
}

// INCORRECT — service locator / static access
public class SomeHandler
{
    public void Handle()
    {
        var service = ServiceLocator.Get<IEmployeeService>(); // Anti-pattern
    }
}
```

---

## 9. Result Pattern

All Application layer operations return `Result<T>` instead of throwing exceptions:

```csharp
// Result<T> definition
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string? Error { get; }
    public IReadOnlyList<string> Errors { get; }

    public static Result<T> Success(T value) => new(true, value, null, []);
    public static Result<T> Failure(string error) => new(false, default, error, [error]);
    public static Result<T> Failure(IReadOnlyList<string> errors) => new(false, default, null, errors);
}

// Usage in handler
public async Task<Result<EmployeeDto>> Handle(GetEmployeeByIdQuery query, CancellationToken ct)
{
    var employee = await _repository.GetByIdAsync(query.EmployeeId, ct);
    if (employee is null)
    {
        return Result<EmployeeDto>.Failure($"Employee {query.EmployeeId} not found");
    }
    return Result<EmployeeDto>.Success(_mapper.Map<EmployeeDto>(employee));
}

// Usage in ViewModel
var result = await _mediator.Send(query, ct);
if (result.IsFailure)
{
    ErrorMessage = result.Error;
    return;
}
CurrentEmployee = result.Value;
```

---

## 10. WPF / MVVM Standards

### ViewModel Rules
- ViewModels must not reference Views directly
- Use `ICommand` (RelayCommand / DelegateCommand) for all button actions
- Properties must implement `INotifyPropertyChanged`
- Use `ObservableCollection<T>` for bound lists
- ViewModels must be unit-testable (no UI dependencies in constructor)

### Command Naming
```csharp
public ICommand SaveCommand { get; }
public ICommand CancelCommand { get; }
public ICommand DeleteCommand { get; }
public ICommand SearchCommand { get; }
public ICommand ExportCommand { get; }
```

### Property Pattern
```csharp
private string _firstName = string.Empty;
public string FirstName
{
    get => _firstName;
    set => SetProperty(ref _firstName, value);
}
```

---

## 11. Unit Test Standards

### Framework
- xUnit for test runner
- FluentAssertions for assertions
- NSubstitute for mocking
- Bogus for test data generation

### Test Organisation
```
FijiPayroll.Application.Tests/
├── Features/
│   ├── Employees/
│   │   ├── Commands/
│   │   │   └── CreateEmployeeCommandHandlerTests.cs
│   │   └── Queries/
│   │       └── GetEmployeeByIdQueryHandlerTests.cs
│   └── Payroll/
│       └── CalculatePAYETests.cs
└── Common/
    └── ValidationBehaviourTests.cs
```

### Test Method Template
```csharp
[Fact]
public async Task CalculatePAYE_ForResidentWith30000AnnualSalary_Returns3850()
{
    // Arrange
    var grossPay = 30_000.00m;
    var sut = new PAYECalculationService(_taxTableRepository);

    // Act
    var result = await sut.CalculateAsync(grossPay, ResidencyStatus.Resident, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().Be(3_850.00m);
}
```

---

## 12. NuGet Package Standards

| Package | Version | Purpose |
|---------|---------|---------|
| MediatR | 12.x | CQRS mediator |
| FluentValidation | 11.x | Validation |
| Serilog | 3.x | Logging |
| Serilog.Sinks.File | 5.x | File logging |
| Serilog.Sinks.MSSqlServer | 7.x | SQL logging |
| Microsoft.EntityFrameworkCore.SqlServer | 8.x | ORM |
| Dapper | 2.x | Micro-ORM for reports |
| xUnit | 2.x | Testing |
| FluentAssertions | 6.x | Assertions |
| NSubstitute | 5.x | Mocking |
| Bogus | 34.x | Test data |
| Prism.Wpf | 9.x | MVVM framework |
| BCrypt.Net-Next | 4.x | Password hashing |

---

*Document maintained by: Senior C# Developer*  
*Last updated: June 2026*
