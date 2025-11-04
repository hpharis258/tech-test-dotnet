# My initial thoughts

- The solution had no unit tests and violated several SOLID principles.
- As allowed by the brief, I used xUnit and Moq to enable isolated, fast unit tests.
- The original PaymentService mixed responsibilities (data access selection, validation, and persistence), had hard dependencies, and used configuration statically, which hurt testability and extensibility.

# What I changed and why

- Dependency inversion (DIP) and testability

  - Introduced IBankAccountDataStore and IAccountDataStoreFactory; made AccountDataStore and BackupAccountDataStore implement the interface.
  - Reason: decouple PaymentService from concrete data stores and enable mocking.

- Configuration abstraction (DIP)

  - Added IConfig with a ConfigurationService implementation.
  - Reason: remove direct use of ConfigurationManager.AppSettings and make configuration controllable in tests.

- Centralized data store selection (SRP, OCP, DRY)

  - Added DataStoreFactory that uses IConfig to choose between primary and backup stores.
  - Reason: one place for selection logic; avoids duplication in PaymentService.

- Payment scheme validation by strategy (SRP, OCP)

  - Created IPaymentValidator and concrete validators: BacsPaymentValidator, FasterPaymentsValidator, ChapsPaymentValidator.
  - Reason: adding a new scheme no longer requires changing existing code; just add a new validator class.

- Validator factory (OCP)

  - Added PaymentValidatorFactory to resolve the correct validator by PaymentScheme.
  - Reason: remove switch/if statements from the core flow.

- PaymentService as an orchestrator (SRP, DIP)

  - PaymentService now depends on IAccountDataStoreFactory and IPaymentValidatorFactory (injected).
  - It guards inputs, retrieves the account, delegates validation, updates balance when valid, and returns MakePaymentResult.
  - Reason: isolates orchestration from persistence and validation, improving clarity and testability.
  - Note: Argument guarding remains in PaymentService. If extended further, a separate request validator could own input validation.

- Unit tests (testability, safety)

  - Added tests for PaymentService behavior using mocks.
  - Added validator tests (per-scheme rules and edge cases).
  - Added factory tests for configuration-driven data store selection.
  - Result: fast, isolated feedback and protection against regressions.

- Kept constraints from the brief
  - Did not change the MakePayment method signature.
  - Solution builds; tests pass.

# How to build and test

- Build: `dotnet build`
- Run tests: `dotnet test`
- Test stack: xUnit + Moq on .NET 8

# Remaining trade-offs and rationale

- Magic string for data store choice (“Backup”) is still present for simplicity; handled in one place (factory) to contain risk.
- PaymentService performs simple argument guarding; if requirements grow, move this to a dedicated IRequestValidator.

# What I would improve with more time

- Replace magic strings with strong types

  - Introduce an enum (e.g., DataStoreType) or strongly-typed options via IOptions<PaymentServiceOptions>.

- Richer results and diagnostics

  - Extend MakePaymentResult with FailureReason and timestamps; optionally return a PaymentValidationResult from validators for detailed reasons.

- Input validation layer

  - Add IRequestValidator or FluentValidation for MakePaymentRequest to fully isolate input concerns.

- Observability

  - Inject ILogger<PaymentService> and add structured logs and metrics (success/failure per scheme, duration).

- Domain safety and resilience

  - Central guard against non-positive amounts; consider idempotency keys and concurrency control on balance updates.

- Async I/O

  - Introduce async methods in the data store and expose MakePaymentAsync (keeping the original signature intact).

- Integration tests and coverage

  - Add end-to-end tests with real factories and configuration; enable coverage (coverlet) and quality gates in CI.

- Conventional namespaces and structure
  - Rename folders to PascalCase and align namespaces:
    - Domain (current Types)
    - Application (Services, Validation, Factories, Abstractions)
    - Infrastructure (Persistence, Configuration)
  - Reason: clarity, scalability, and easier navigation.
