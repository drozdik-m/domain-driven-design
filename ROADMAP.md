# Roadmap

Tracked improvements, fixes, and open questions per package. Findings are grouped by category. Severity is indicated where relevant.

---

## MartinDrozdik.DDD

Core DDD primitives. Reviewed (including `Mediator`).

### Bugs

- [x] **`IntIdentity` / `StringIdentity` are unusable as documented** — both declared `where TSelf : ..., new()` (`Identities/Primitive/IntIdentity.cs:5`, `StringIdentity.cs:5`), unlike `GuidIdentity`. A derived type with a primary constructor (e.g. `OrderId(int key) : IntIdentity<OrderId>(key)`, as shown in the skill docs) has no parameterless constructor and **failed the `new()` constraint → CS0310, won't compile**. Only `GuidIdentity` was exercised, so it went unnoticed. **Fixed:** dropped `, new()` from both; added `Identities/Primitive/PrimitiveIdentityTests.cs` whose derived types act as a compile-time regression guard.
- [x] **Broken interpolation in duplicate-detection message** — `Enumerations/Statics/ThrowIfDuplicates.cs:32-33` interpolates an unmaterialized `IEnumerable<(T,int)>`, so the exception prints the iterator type name instead of the duplicates. Fix: report `duplicates.Count` and the offending names.
- [ ] **Wrong variable in `UrlBuilder` port error** — `Integrations/UrlBuilder.cs:120` interpolates `port` (still `null` after a failed parse) instead of `portPart`; message reads "Port  could not be parsed".

### Design

- [ ] **`Error` includes `Exception` in value-equality** (`Errors/Error.cs:97`) — `Exception` uses reference equality + reference-based hash, breaking the `ValueObject` value-semantics contract (dictionary/set behavior, hash stability). Exclude it from `GetEqualityComponents()` (compare on `Code` + `Message` + `Details`), consistent with `ErrorBuilder.WithSubErrors` already ignoring exceptions.
- [ ] **`EnumerationName` validation hole** (`Enumerations/EnumerationName.cs`) — it's a `struct` with `init` setters, so `default(EnumerationName)` and object-initializer usage bypass the validating constructor → `Key == null` despite the "must not be empty" contract.
- [ ] **`EnumerationName.KeyLowercase` is dead state** — computed/stored per instance "for case-insensitive comparisons," but the type doc says "Case sensitive" and `Equals`/`GetHashCode` only use `Key`. Never read. Remove it or actually honor it.
- [ ] **Inconsistent generic-parameter ordering across services** — `IGetByIdService<TItem, TIdentity>` (item-first) vs `ISaveService<TIdentity, TItem>` (identity-first) vs `ICrudService<TEntity, TIdentity>`; `ICrudService` flips order when composing. Pick one convention. Also `IGetByIdService` is missing `where TIdentity : notnull`.
- [ ] **Enumeration static cache: thread-safety & encapsulation** — `StaticEnumeration<TSelf>.EnsureInitialized()` / `InitializableEnumeration` do non-locked check-then-init over mutable statics (double-init / partial-dictionary race). `EnumerationsDictionary`/`EnumerationsList` are `protected internal { get; set; }` (publicly reassignable). Consider `Lazy<T>`/lock and `private` setters.
- [ ] **`ValueObject` is its own `IEqualityComparer<ValueObject>` and lacks `IEquatable<ValueObject>`** (`Templates/ValueObject.cs:14`) — unusual responsibility mix; `IEquatable<ValueObject>` is the conventional, allocation-friendly choice. Reconsider.
- [ ] **`ValueObject`/`Enumeration` equality is untranslatable in EF Core queries** — `Templates/ValueObject.cs` overloads `operator ==`, so a predicate over a value-converted property compiles to a static method call rather than an equality node and EF Core cannot translate it. `context.Invoices.Where(i => i.State == InvoiceState.Draft)` — the obvious way to filter on an `Enumeration` mapped with `HasConversion` (`Demo/Context/Configurations/InvoiceConfiguration.cs:55-60`) — throws "could not be translated" at runtime, verified while writing the Demo recurring task. Since `Enumeration : ValueObject` this hits every enumeration-typed column, i.e. the single most likely thing to appear in a `Where`. Either document the working alternative or give `Enumeration` an EF-friendly comparison path.
- [ ] **Generic CRUD services vs. DDD** — `Services/ICrudService` & friends are a generic repository/CRUD abstraction, which sits in tension with rich aggregate modeling. Keep as pragmatic convenience, but add a doc note clarifying it's not the recommended way to model rich aggregates.

### Dependency issues

- [ ] **`CSharpFunctionalExtensions` leaks through the public API** — `Result<T>` / `IResult<T,Error>` / `UnitResult<Error>` appear in public signatures (`IGetByIdService.GetAsync`, `StaticEnumeration.FromName`, `ErrorBuilder.BuildUnitResult`, …) and are advertised in the README/`CLAUDE.md` as if first-class library types. Either own the abstraction (thin wrapper / explicit re-export) or document clearly that `Result<T>` *is* CSharpFunctionalExtensions, since consumers inherit a hard third-party dependency and its versioning.

### NuGet split

- [ ] **Non-DDD utilities dilute the core package** — `Extensions/{String,Random,CurrencyFormat,Url,Path}Extensions`, `Integrations/UrlBuilder`, `Disposing/DisposableAction` have nothing to do with DDD. Most egregious: **`CurrencyFormatExtensions.ToCzk()`** hardcodes Czech koruna (`"Kč"`) — application-specific logic in a general library. Consider extracting to a separate `MartinDrozdik.Common`/`.Utilities` package; at minimum drop `ToCzk`.

### Minor

- [ ] **Doc/naming drift** — `CLAUDE.md` says `ValidationException` but the type is `BusinessRuleValidationException`. `StringExtensions` & `CurrencyFormatExtensions` both have the XML summary "Extensions for currency formatting." (copy-paste). `EnumerationErrors.EnumerationNameNotFound` passes `name` twice to `string.Format` — verify the resx uses `{0}`/`{1}`. **Partially fixed:** the `string.Format` bug is gone — resx files were removed library-wide (localization is not wanted, it made error messages culture-dependent) and the message is now an interpolated English string using `name` and `enumName`.
- [ ] **`ToUrlFriendlyFileName` no-extension branch** (`Extensions/UrlExtensions.cs:36-38`) — missing the `.Trim('-')` the other branches apply, and the outer `Substring(0, maxLength)` is redundant since `ToUrlFriendly` already enforced `maxLength`.
- [ ] **`SpecificationResult.NotSatisfied(IEnumerable<Error>)` contract mismatch** — throws on empty, while the private ctor silently normalizes empty → satisfied. Harmless but inconsistent.
- [ ] **`IdentityConverter` only provides `CreateGuid`** — no `CreateInt`/`CreateString` factory counterpart (mirrors the `GuidIdentity`-only reality above).

### Notes (keep doing)

- Specifications module is excellent: clean composition, deferred allocations (`errors ??= []`), correct AND/OR short-circuit semantics.
- `ErrorBuilder` fluent API with fail-fast guards; correct, bounds-safe span/stackalloc usage; immutable `record` `UrlBuilder` with invariant checks; consistent file-scoped namespaces, thorough XML docs, `DebuggerDisplay`.

### Mediator

CQRS mediator (`Mediator/`): marker interfaces (`IMessage`/`IRequest`/`ICommand`/`IQuery`), `ServiceMediator`, DI registration (`AddMediator` → `ServiceMediatorConfig`), composable pipeline behaviors, and the `IServicePipelineIntegrator` extensibility point (`ValidationPipelineIntegrator` here, `LoggingPipelineIntegrator` in `.Web`). Reviewed against the Demo (`InvoiceController`, `Requests/Invoices/*`) and README usage.

#### Bugs

- [ ] **`IMediator.SendCommand<TRequest>` param doc is wrong** — `Mediator/IMediator.cs:37` documents `<param name="request">Type of the response.</param>`; it's the request, not "the response" (copy-paste from the response typeparam). Fix the summary.
- [ ] **Validation pipeline throws synchronously instead of returning a faulted task** — `ValidationPipeline<TRequest>` / `<TRequest,TOutput>` (`Pipelines/Validations/…`) call `ThrowIfInvalid(input)` from a non-`async` `HandleAsync`, so the `BusinessRuleValidationException` is thrown on the *calling* thread before a `Task` is returned. When validation is the outermost behavior (no wrapping behavior), a caller that does `var t = SendCommand(...)` then `await t` gets the throw at call time, not await time — violating Task exception semantics. Inside another behavior it's masked because `await next(ct)` re-wraps it. Make `HandleAsync` `async`, or return `Task.FromException`.

#### Design

- [ ] **`IValidatedMessage<TMessage>` forces the request to own its validator and bypasses DI** — `Pipelines/Validations/IValidatedMessage.cs` requires the message to expose `AbstractValidator<TMessage> Validator { get; }`, so the message must construct its own validator (the Demo/tests do `=> new XValidator()` on every access). Validators with dependencies (e.g. a DB uniqueness check) can't be resolved from the container. Prefer resolving `IValidator<TMessage>` from the `IServiceProvider` inside the pipeline (FluentValidation's idiomatic registration) rather than carrying it on the request. Also couples application requests directly to FluentValidation.
- [ ] **Validator property typed as concrete `AbstractValidator<T>` not `IValidator<T>`** — `IValidatedMessage.cs:14` — using the concrete base prevents composed/decorated/wrapped validators. Type it as `IValidator<TMessage>`.
- [ ] **Validation is sync-only** — `ValidationPipeline<TRequest>.ThrowIfInvalid` calls `validator.Validate(...)`; any async rule (`MustAsync`/`CustomAsync`) throws `AsyncValidatorInvokedSynchronouslyException` at runtime. The rest of the codebase already exposes `ValidateAndThrowBusinessAsync`. Add an async validation path (`await validator.ValidateAsync(..., ct)`).
- [ ] **`IMediator` methods lack the `Async` suffix** — `SendQuery`/`SendCommand` return `Task` but aren't `…Async`, unlike `HandleAsync` everywhere else in the codebase. Inconsistent naming convention.
- [ ] **Dispatch is by compile-time `TRequest`, no polymorphic send** — callers must name the concrete request type (`SendCommand<CreateInvoiceDraftCommand, InvoiceId>`); you can't dispatch from an `IRequest<T>`/`ICommand<T>` reference and have the handler resolved by runtime type (MediatR-style `Send(IRequest<T>)`). Reasonable as a deliberate trade-off, but document it — passing a base type silently resolves the wrong (or no) handler.
- [ ] **Three pipeline builders, two ordering mechanisms** — `PipelineBuilder<TInput,TOutput>.Add` appends and `Build()` does `.Reverse()` (`Pipelines/PipelineBuilder.cs:36`); both `ServicePipelineBuilder<>`/`<,>` instead `Insert(0)` in `Add` and **don't** reverse in `Build`. Same net order, but two strategies for one concept is confusing — and `ServicePipelineBuilder<TInput>.Build` names its local `reversedPipelines` (`ServicePipelineBuilder{TInput}.cs:57`) although nothing is reversed (misleading; the `<,>` variant correctly calls it `pipelines`). Pick one mechanism and align the naming.
- [ ] **Builder asymmetry / dead API** — there is an instance-based `PipelineBuilder<TInput,TOutput>` but no `PipelineBuilder<TInput>` for unit commands, and the instance-based builder appears unused (only the `ServicePipelineBuilder` variants are wired through `ServiceMediatorConfig`/integrators). Either complete the symmetry or drop the unused instance builder.
- [ ] **Redundant/inconsistent `abstract` on interface members** — `IServicePipelineIntegrator` marks the `Build…Pipeline` methods `public abstract` but the `Register…Pipeline` methods just `public` (`Integrators/IServicePipelineIntegrator.cs`). Interface methods are implicitly abstract; drop the modifier for consistency.

#### Tests

- [ ] **Integrator path is untested** — `ValidationPipelineIntegrator`, `EmptyPipelineIntegrator`, `MergedPipelineIntegrator`, and the `ServiceMediatorConfig.WithCommand/WithQuery(IServicePipelineIntegrator)` overloads have no coverage. `ServiceCollectionExtensionsTests` only exercises the manual `ServicePipelineBuilder` overloads; the merged Logging+Validation flow shown in the README/Demo is never integration-tested end-to-end through `ServiceMediator`.
- [ ] **No test that a missing handler throws `MediatorException`** — the `?? throw new MediatorException(...)` branches in `ServiceMediator` (all three send methods) are uncovered.
- [ ] **Ordering tests don't pin execution order** — `PipelineTests` record into the call stack *after* awaiting `next` (post-order), so they pass for either inner/outer arrangement; add a pre-`next` ordering assertion to actually guard behavior order.
- [ ] **Cancellation branch untested** — the `IsCancellationRequested` → `Task.FromCanceled` short-circuit in `Pipeline<TInput>`/`Pipeline<TInput,TOutput>` has no test.

#### Minor

- [ ] **Imprecise diagnostics in exception messages** — `ServiceMediator` uses `typeof(TRequest).Name` (ambiguous across namespaces) and `nameof(IQueryHandler<TRequest,TResponse>)` which drops the generic args (renders just `IQueryHandler`). Consider `FullName` / a formatted closed-generic name.
- [ ] **Stray `code-dump/ServiceMediatorConfig.cs`** — a copy of the config lives outside `src/`; remove if it's leftover scratch (repo hygiene).

#### Notes (keep doing)

- Clean marker-interface hierarchy with correct variance: `IRequest<out TResponse>`, `ICommand<out TResponse>`, `in TCommand`/`in TQuery` on handlers.
- `EmptyPipeline<…>` singletons avoid allocation/branching when no behaviors are registered; per-behavior cancellation checks between steps.
- Integrator + `Merge` composition is a genuinely nice, open/closed extension point — `.Web`'s `LoggingPipelineIntegrator` extends the core pipeline story without touching core, and `MergedPipelineIntegrator` flattens nested merges.
- Thorough XML docs, file-scoped namespaces, `nameof` in messages, and DI lifetimes (`Scoped` handlers/pipelines, singleton empty pipeline) are all consistent.

---

## MartinDrozdik.DDD.Web

ASP.NET Core infrastructure. Reviewed folder-by-folder (`Options`, `Middlewares` + `Middlewares/Exceptions`, `Databases`, `Health`, `Telemetry`, `Resilience`, `OpenApi`, `Logging`, `FilePathProviders`, `Proxy`, `Environments`, `Mediator/Pipelines/Logging`) against the README and the Demo `Program.cs`.

### Bugs

- [ ] **Production `RequestLogging` ships under a `.Tests.` namespace** — `Middlewares/RequestLogging.cs:5` declares `namespace MartinDrozdik.DDD.Web.Tests.Middlewares` even though it's a `public` type in the shipping package, used by all four exception handlers and `RequestResponseLoggingMiddleware`. Consumers must `using MartinDrozdik.DDD.Web.Tests.Middlewares`, and it collides with the real test project's `MartinDrozdik.DDD.Web.Tests.*` root. Move it to `MartinDrozdik.DDD.Web.Middlewares` (update the 5 importers).
- [ ] **Health-check request timeout is never enforced** — `Health/HostApplicationBuilderExtensions.cs` registers a `"HealthChecks"` timeout policy and `Health/WebApplicationExtensions.cs` tags the endpoints with `WithRequestTimeout("HealthChecks")`, but `UseAppMiddlewares` (`WebApplicationExtensions.cs:33`) never calls `app.UseRequestTimeouts()`. Without that middleware the timeout metadata is inert — health endpoints can still hang. Add `app.UseRequestTimeouts()` before the health mapping (or drop the dead config).
- [ ] **`AddAppDbContext(DatabaseOptions, …)` builds a throwaway `ServiceProvider`** — `Databases/HostApplicationBuilderExtensions.cs:58` does `builder.Services.BuildServiceProvider().GetRequiredService<IOptions<DatabaseOptions>>()` at registration time: the documented ASP.NET anti-pattern. It spins up a second container, duplicates singletons, captures a one-shot options snapshot, and forces binding/validation before the rest of registration and outside `ValidateOnStart`. Bind directly via `builder.Configuration.GetRequiredValidatedOptions<DatabaseOptions>()` (the helper already exists in `Options/ConfigurationManagerExtensions.cs`).

### Design

- [ ] **`GlobalExceptionHandler` leaks `exception.Message` to clients in every environment** — `Middlewares/Exceptions/GlobalExceptionHandler.cs:25` sets `detail: GetExceptionDetail(exception)` (= raw `exception.Message`) on the catch-all 500 ProblemDetails regardless of environment, while `GetExtensionDataWithDetails` is careful to gate the full `exception.ToString()` to Development only. The README promises "clean and safe error messages" in production; a raw catch-all message can disclose internals (DB/driver text, file paths). Return a generic detail in production, detailed only in Development.
- [x] **4xx client errors logged at `Error` level** — every handler (`BusinessRuleValidationException`/`ValidationException` → 400, `BusinessNotFoundException` → 404) calls `RequestLogging.LogError`, which is `[LoggerMessage(Level = LogLevel.Error)]`. Client faults will spam error logs and trip alerting. Log 4xx at `Warning`/`Information`. Also inconsistent with `RequestResponseLoggingMiddleware`, which treats a bare 404 as *success*. **Fixed:** the level is now derived from the status class in one place — `RequestLogging.LogResponseInformation` (1xx/2xx/3xx → `Information`, 4xx → `Warning` via the new `LogClientErrorResponseInformation`, 5xx → `Error`). `RequestResponseLoggingMiddleware` lost its `404 == success` special case, and all four exception handlers now go through `ExceptionHandler.WriteResponseAndLogAsync`, which writes the `IResult` *first* and then logs — so the logged `{StatusCode}` is the one the client actually receives instead of the uninitialised `200`. Guarded by `Middlewares/Logging/RequestResponseLoggingMiddlewareTests.cs` and `Middlewares/Exceptions/ExceptionHandlerLoggingTests.cs`.
- [x] **Exceptions are error-logged twice** — in `UseAppMiddlewares`, `RequestResponseLoggingMiddleware` is registered *after* `UseExceptionHandler`, so it sits downstream of the exception handler. A thrown exception is logged once by the middleware's `catch` (then rethrown) and again by the matched `IExceptionHandler`. Pick a single place to log. **Fixed as far as accuracy goes:** `RequestResponseLoggingMiddleware` is now registered *before* `UseExceptionHandler` (`WebApplicationExtensions.cs:42`), so it wraps the handler. A handled exception no longer propagates into its `catch` — the handler writes the response and returns normally, so the middleware's success path sees the final status code and logs it at a matching level. A `BusinessNotFoundException` is therefore logged as a `Warning` twice (middleware + handler) rather than `Error` + `Warning`. The remaining duplication is **deliberate** — twice is preferable to zero, and the middleware's `catch` is still the only thing that logs an exception no handler claims. Guarded end-to-end by `Middlewares/Logging/ExceptionLoggingPipelineTests.cs`.
- [ ] **Validated-options `Validator` typed as concrete `AbstractValidator<T>`** — `Options/IValidatedAppOptions.cs:15` (and the `DatabaseOptions`/`StaticFileVersioningOptions` implementations) expose `AbstractValidator<T>` rather than `IValidator<T>`, preventing composed/decorated validators. Same finding as the Mediator `IValidatedMessage` one — worth fixing consistently.
- [ ] **`IsBehindProxy()` enables `ForwardedHeaders.All` with no trusted-proxy config** — `Proxy/WebApplicationExtensions.cs:21` forwards *all* headers (including `X-Forwarded-Host`) without setting `KnownProxies`/`KnownNetworks`. Same-host loopback proxies are saved by the framework defaults, but the one-liner invites host-header/IP spoofing with an off-box proxy. Default to `XForwardedFor | XForwardedProto` and document trusted-proxy setup.
- [ ] **Recurring tasks have no health-check or telemetry surface** — `RecurringTasks/RecurringTaskHost.cs` logs each iteration but exposes nothing queryable: an operator cannot ask "when did `CleanupTask` last succeed?" and a failing task is invisible to `/health`. Record last-run/last-success/last-failure per task in a singleton and add an opt-in health check. Related: the loop starts no `Activity`, so an iteration's work is untraced and unlinked in OpenTelemetry despite the package configuring it — add an `ActivitySource` around `RunIterationAsync`.
- [ ] **Recurring tasks run on every instance, with no distributed coordination** — `RecurringTasks/HostApplicationBuilderExtensions.cs` registers a plain `BackgroundService`, so scaling to N replicas runs the job N times concurrently. Fine for reports, wrong for anything that mutates. Needs either documented "single instance only" guidance or opt-in leader election / advisory locking.
- [ ] **Recurring task schedules are fixed intervals only** — no cron/calendar expressions, so "every day at 02:00" is not expressible via `RecurringTaskOptions` (`RecurringTasks/Options/RecurringTaskOptions.cs`). Deliberate for now; revisit if consumers keep hand-rolling it. A failing iteration also retries only on the next period — there is no per-iteration backoff.
- [ ] **Recurring task schedules cannot be bound from configuration** — `AddRecurringTask` takes an `Action<RecurringTaskOptions<TTask>>` only, so changing a period per environment means a code change and a redeploy. Deliberately simple for now (a configuration-bound options overload existed and was removed before release, because the schedule is usually decided once). Revisit if consumers start wanting per-environment schedules. Now that the schedule type is per-task, the restore is much smaller than the removed one: an overload taking a section path and calling `.Bind(configuration.GetSection(path))` on `RecurringTaskOptions<TTask>` — no consumer-declared options type needed at all.
- [ ] **`DddDbContext` audit shadow properties are `DateTimeOffset`, which SQLite cannot compare** — the documented audit pattern registers `CreatedAt`/`UpdatedAt` as `DateTimeOffset` shadow properties (`Demo/Context/InvoiceDbContext.cs:27-30`, mirrored in `Web.Tests.App/TestDbContext.cs`), but the EF Core SQLite provider cannot translate relational comparisons on `DateTimeOffset`. `Where(i => EF.Property<DateTimeOffset>(i, "UpdatedAt") < cutoff)` throws "could not be translated" at runtime — verified while writing the Demo recurring task, which had to be rewritten to avoid it. Any "records older than X" query, the most obvious use of an audit column, is therefore impossible on SQLite. Consider storing the audit stamps as `DateTime` (UTC) or documenting the provider limitation next to the pattern.

### Dependency issues

- [x] **Transitive `Microsoft.OpenApi` 2.0.0 carries a high-severity advisory** — `Microsoft.AspNetCore.OpenApi` 10.0.10 (`MartinDrozdik.DDD.Web.csproj:34`) declares `Microsoft.OpenApi` version `2.0.0` as a *minimum*, and NuGet resolved exactly that, tripping NU1903 on every project in the solution (GHSA-v5pm-xwqc-g5wc — circular schema references may terminate OpenAPI parsing; patched in 2.7.5). **Fixed:** direct `PackageReference` to `Microsoft.OpenApi` 2.11.0 (head of the 2.x line ASP.NET Core 10 targets) in both `MartinDrozdik.DDD.Web.csproj` and `MartinDrozdik.DDD.Testing.csproj` — the latter is needed separately because `Testing` reaches `Web` through the published package, not a `ProjectReference`. Revisit once an ASP.NET Core 10.0.11+ patch ships with a raised floor; the pin can then be dropped.
- [ ] **`SQLitePCLRaw.lib.e_sqlite3` 2.1.11 has a high-severity advisory with no 2.x fix** — GHSA-2m69-gcr7-jv3q; arrives via `Microsoft.EntityFrameworkCore.Sqlite` 10.0.10 in `MartinDrozdik.DDD.Demo` and `MartinDrozdik.DDD.Web.Tests.App` only, so it does not reach any shipped package. No patched 2.x release exists, and the 3.x line is a native-code major bump that EF Core 10 does not declare support for. Track upstream rather than forcing it.

### NuGet split

### Minor

- [ ] **Static file provider forces a `Version` even in Development** — `FilePathProviders/HostApplicationBuilderExtensions.cs:22` calls `AddValidatedAppOptions<StaticFileVersioningOptions>()` unconditionally (with `ValidateOnStart`), but Development resolves `TimestampedStaticFilePathProvider`, which never reads `Version`. Dev startup then fails unless an unused `App:StaticFileVersioning:Version` is configured. Register/validate the options only on the production branch.
- [ ] **Health-check timeout comment/code drift** — `Health/HostApplicationBuilderExtensions.cs` comment says "10 seconds is reasonable" but the policy is `TimeSpan.FromSeconds(5)`.
- [ ] **README ↔ API drift** — README shows `app.MigrateDatabaseAsync<T>()` but the method is `EnsureMigratedDatabaseAsync<T>`; the "Mediator" README section is really request/response logging; and `AddAppErrorHandling`'s doc lists `BusinessRuleException` as if it had a dedicated handler — it's actually covered by `GlobalExceptionHandler` (fine, but the list implies otherwise).
- [ ] **Misleading comment in `CustomSchemaIds`** — `OpenApi/OpenApiExtensions.cs:29-30` skips schemas lacking `x-schema-id`, but the comment claims it's "because we don't want to decorate the schema multiple times"; the real reason is "only rename schemas that became named components."
- [ ] **`IStaticFilePathProvider` doc typos** — summary says "intefrace" and advertises "method overloads for RazorClassLibrary resources" that don't exist on the interface.

### Notes (keep doing)

- Clean split of `AddAppServices` (host builder) vs `UseAppMiddlewares` (app), everything optional behind a `WebApplicationOptions.Default` record; individual module extensions are independently usable.
- `IExceptionHandler` chain ordered specific→general with `GlobalExceptionHandler` last, mapping DDD exceptions to RFC 7807 ProblemDetails; `traceId` surfaced in extensions; full exception detail gated to Development.
- Health endpoints correctly split into `live`/`ready`/all by tag with proper probe semantics; OpenTelemetry filters health-check requests out of traces and honors `OTEL_*` env vars.
- `DddDbContext` hooks (`OnAggregatesSave`/`OnDomainEntitiesSave`/`OnObjectsSave`) are wired through every `SaveChanges`/`SaveChangesAsync` overload; `IValidatedAppOptions` gives fail-fast config with `ValidateOnStart` and strict binding (`ErrorOnUnknownConfiguration = true`).
- `TimeProvider`-based cache-busting in `TimestampedStaticFilePathProvider` (testable); consistent file-scoped namespaces and thorough XML docs throughout.
- `RecurringTasks` keeps the job (`IRecurringTask`, scoped, resolved per iteration) separate from the loop (`RecurringTaskHost<TTask>`, internal): the job is a plain unit-testable class with constructor-injected scoped services, and the loop owns resilience, scoping and timing. Every delay goes through the injected `TimeProvider` via `new CancellationTokenSource(delay, timeProvider)` rather than `Task.Delay`, so the whole suite drives it with `FakeTimeProvider` — 22 tests, no sleeps, a few seconds.
- `RecurringTaskOptions<TTask>` carries the task as a phantom type parameter, so a schedule's dependency injection identity *is* the task type. No named options, no string key, and two tasks that happen to share a short type name (or two closed generics of one task type) cannot silently end up on the same schedule — guarded by `RecurringTaskRegistrationTests.Schedules_of_two_tasks_do_not_bleed_into_each_other`.

---

## MartinDrozdik.DDD.Testing

xUnit v3 test helpers. Reviewed folder-by-folder (`TestedApp*`/`ITestedApp`/`TestedAppExtensions`, `ResultAssert`, `EqualityAssert`, `Attributes`, `Smoke`, `Errors`, `Contexts`) against the package README.

### Bugs

- [ ] **Environment setter is misnamed `WithOutput(string)`; the documented `WithEnvironment` doesn't exist** — `TestedAppBuilder.cs:37` declares `WithOutput(string newEnvironment)` that sets `_environment`, overloading the real `WithOutput(ITestOutputHelper)`. The README (lines 98, 110) calls `.WithEnvironment(...)`, which won't compile. Rename the string overload to `WithEnvironment` and keep `WithOutput` for the helper.
- [ ] **`EndpointTest.WithAcceptableCodes` silently drops `Content` and `ContentType`** — `Smoke/EndpointTest.cs:56-62` builds `new EndpointTest(Method, Url) { AcceptableCodes = … }` without copying `Content`/`ContentType`, so a body set before `WithAcceptableCodes(...)` is lost. Copy all properties.
- [ ] **`EndpointSmokeTester` ignores `EndpointTest.ContentType`** — `Smoke/EndpointSmokeTester.cs:30-33` hardcodes `MediaTypeNames.Application.Json` for the request body instead of `testCase.ContentType`, so the configurable property has no effect.
- [ ] **`RequestResult<T>.IsSuccess` is always `true` for value-type `T`** — `TestedAppExtensions.cs:208` uses `IsSuccess => _value is not null`. For a value-type `TResponse` (`int`, enum, record struct), the unconstrained `T? _value` is never null, so `Failure(...)` reports success and `Value` returns `default`. Track success explicitly (store the bool or the status code).

### Design

- [ ] **`EqualityAssert.TestEqualityComparer` asserts hash codes differ for unequal values** — `EqualityAssert.cs:81` `Assert.NotEqual(comparer.GetHashCode(value1), comparer.GetHashCode(differentValue))`. Distinct hashes for unequal values is *not* part of the `GetHashCode` contract (collisions are legal); a correct type with a colliding sample fails spuriously. Drop the assertion or document it as a heuristic.
- [ ] **`EqualityAssert` "null" checks actually compare to `default(T)`** — `TestEquatable`/`TestEqualityOperators`/`TestEqualityComparer` pass `default` with messages saying "compared to null". For a value-type `T`, `default` is a real value, so `value1 != default` may be wrong and the messages mislead. Constrain the null-semantics helpers to `where T : class` (or split value/reference variants).
- [ ] **`SqlDbContextIntegrationTests` bundles migration checks with mapping checks** — `Contexts/SqlDbContextIntegrationTests.cs:92` `No_pending_migrations` calls `GetPendingMigrations()`, which fails for apps that create their schema via `EnsureCreated` (no migrations assembly) — like the Demo. Split migration-specific tests into an opt-in base class or guard them.
- [ ] **`Entity_can_be_queried_from_database` ends with `Assert.True(true)`** — `Contexts/SqlDbContextIntegrationTests.cs:85` the real check is "no exception thrown"; the tautology is a smell (and is exactly the S2699 that `[AssertionMethod]` was introduced to suppress elsewhere). Mark the method `[AssertionMethod]` and drop the no-op, or assert something concrete.
- [ ] **`ErrorHandlingTests` bakes in the production message leak** — `Errors/ErrorHandlingTests.cs:48,114` assert `problemDetails.Detail == "This is a general exception"` *unconditionally* (only the `exception` extension is environment-gated). This locks in the `GlobalExceptionHandler` behavior flagged in the Web review (raw `exception.Message` returned in all environments); fixing that Web bug requires updating these base tests.

### Dependency issues

- [ ] **Test-helper package forces heavy/opinionated deps on every consumer** — `MartinDrozdik.DDD.Testing.csproj` pulls `YamlDotNet` (only for optional YAML OpenAPI validation), `FluentValidation`, `CSharpFunctionalExtensions` (transitively, surfaced through `ResultAssert`), and hard-locks to **xUnit v3** (`xunit.v3.*`, `MartinCostello.Logging.XUnit.v3`, `Mvc.Testing`). Anyone referencing the package inherits all of it. Consider gating YAML validation behind a separate package/extension and documenting the xUnit-v3-only constraint.
- [ ] **Version skew: Testing `0.7.1.1` pins `MartinDrozdik.DDD.Web` `0.7.0`** — the helper ships newer than the Web package it builds on, so consumers may not pick up the latest Web fixes through this dependency. Keep the referenced version in lockstep.
- [ ] **`TestedAppBuilder.WithoutRecurringTasks()` blocked until Web `0.8.0` is published** — `MartinDrozdik.DDD.Testing.csproj:29` consumes `MartinDrozdik.DDD.Web` as a **package** (`0.7.1.1`), not a `ProjectReference`, so the new `IServiceCollection.RemoveRecurringTasks()` is not visible to this project until the Web package ships. The helper was written and reverted for exactly this reason. Once Web `0.8.0` is on NuGet, bump the reference and add:
  ```csharp
  public TestedAppBuilder<TProgram> WithoutRecurringTasks()
      => WithServices(services => services.RemoveRecurringTasks());
  ```
  Until then consumers must call `.WithServices(s => s.RemoveRecurringTasks())` themselves.

### NuGet split

### Minor

- [ ] **`async` methods without `await` in `ErrorEndpoints`** — `Errors/ErrorEndpoints.cs:63,68` `GetException`/`GetBusinessNotFound` are `async Task<string>` with no `await` (CS1998) and inconsistent with the sibling sync throwers. Make them synchronous.
- [ ] **`Health_endpoint_returns_healthy` reads the body twice and over-asserts** — `Smoke/WebApplicationSmokeTests.cs:70,74` reads the content twice and asserts an exact `"Healthy"` / `text/plain` body, which breaks if the app customizes the health writer. De-dup the read; consider relaxing.
- [ ] **Typo `AssertSensityHeaderNotPresent`** — `Smoke/WebApplicationSmokeTests.cs:108` (should be "Sensitive").
- [ ] **Leftover empty-folder include** — `<Folder Include="Errors\" />` in the csproj is unnecessary now that `Errors/` has files.
- [ ] **Shared mutable builder risk** — `WebApplicationSmokeTests.All_services_are_valid` calls `factoryBuilder.With(...)`, mutating the injected builder. Safe under xUnit's per-test instantiation, but a consumer sharing the builder via a fixture would leak `ValidateScopes`/`ValidateOnBuild` into sibling tests. Build from a copy.

### Tests

- [ ] **The helpers themselves are largely unverified** — `EqualityAssert`, `ResultAssert`, `RequestResult<T>`, and `EndpointTest` serialization/`WithAcceptableCodes` have no unit tests in `MartinDrozdik.DDD.Testing.Tests` (would have caught the value-type `IsSuccess` and `WithAcceptableCodes` bugs above). Add self-tests for the assertion/result utilities. `Logging/TestLogger` belongs on that list too — it is currently only covered indirectly, through the Web tests that consume it, and its lock-free concurrent append has no test of its own.

### Notes (keep doing)

- `TestedApp`/`TestedAppBuilder` is a clean fluent wrapper over `WebApplicationFactory`: xUnit output logging, `IStartupFilter`-based test-endpoint and `ClaimsPrincipal` injection, `FakeTimeProvider` wiring, and tracked scope/disposable cleanup.
- Ready-made base classes (`WebApplicationSmokeTests`, `OpenApiSmokeTests`, `ErrorHandlingTests`, `SqlDbContextIntegrationTests`) deliver real coverage out of the box; `Entity_can_be_queried_from_database` exercising the full EF pipeline via reflected `Set<T>()` is a genuinely strong mapping smoke test.
- `EndpointTest` implements `IXunitSerializable` for proper Test Explorer enumeration; the `[AssertionMethod]` attribute is thoughtfully added for S2699; the security-header smoke test guards against info leaks; `All_options_are_valid`/`All_services_are_valid` validate `ValidateOnStart` options and DI scope/build correctness.
- `RequestResult`/`RequestResult<T>` ergonomics (`EnsureSuccessAsync` surfaces the response body on failure) and consistent `JsonSerializerOptions.Web` usage throughout.
