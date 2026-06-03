# Roadmap

Tracked improvements, fixes, and open questions per package. Findings are grouped by category. Severity is indicated where relevant.

---

## MartinDrozdik.DDD

Core DDD primitives. Reviewed (excluding `Mediator`).

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
- [ ] **Generic CRUD services vs. DDD** — `Services/ICrudService` & friends are a generic repository/CRUD abstraction, which sits in tension with rich aggregate modeling. Keep as pragmatic convenience, but add a doc note clarifying it's not the recommended way to model rich aggregates.

### Dependency issues

- [ ] **`CSharpFunctionalExtensions` leaks through the public API** — `Result<T>` / `IResult<T,Error>` / `UnitResult<Error>` appear in public signatures (`IGetByIdService.GetAsync`, `StaticEnumeration.FromName`, `ErrorBuilder.BuildUnitResult`, …) and are advertised in the README/`CLAUDE.md` as if first-class library types. Either own the abstraction (thin wrapper / explicit re-export) or document clearly that `Result<T>` *is* CSharpFunctionalExtensions, since consumers inherit a hard third-party dependency and its versioning.

### NuGet split

- [ ] **Non-DDD utilities dilute the core package** — `Extensions/{String,Random,CurrencyFormat,Url,Path}Extensions`, `Integrations/UrlBuilder`, `Disposing/DisposableAction` have nothing to do with DDD. Most egregious: **`CurrencyFormatExtensions.ToCzk()`** hardcodes Czech koruna (`"Kč"`) — application-specific logic in a general library. Consider extracting to a separate `MartinDrozdik.Common`/`.Utilities` package; at minimum drop `ToCzk`.

### Minor

- [ ] **Doc/naming drift** — `CLAUDE.md` says `ValidationException` but the type is `BusinessRuleValidationException`. `StringExtensions` & `CurrencyFormatExtensions` both have the XML summary "Extensions for currency formatting." (copy-paste). `EnumerationErrors.EnumerationNameNotFound` passes `name` twice to `string.Format` — verify the resx uses `{0}`/`{1}`.
- [ ] **`ToUrlFriendlyFileName` no-extension branch** (`Extensions/UrlExtensions.cs:36-38`) — missing the `.Trim('-')` the other branches apply, and the outer `Substring(0, maxLength)` is redundant since `ToUrlFriendly` already enforced `maxLength`.
- [ ] **`SpecificationResult.NotSatisfied(IEnumerable<Error>)` contract mismatch** — throws on empty, while the private ctor silently normalizes empty → satisfied. Harmless but inconsistent.
- [ ] **`IdentityConverter` only provides `CreateGuid`** — no `CreateInt`/`CreateString` factory counterpart (mirrors the `GuidIdentity`-only reality above).

### Notes (keep doing)

- Specifications module is excellent: clean composition, deferred allocations (`errors ??= []`), correct AND/OR short-circuit semantics.
- `ErrorBuilder` fluent API with fail-fast guards; correct, bounds-safe span/stackalloc usage; immutable `record` `UrlBuilder` with invariant checks; consistent file-scoped namespaces, thorough XML docs, `DebuggerDisplay`.

---

## MartinDrozdik.DDD.Web

ASP.NET Core infrastructure. _Not yet reviewed._

### Bugs

### Design

### Dependency issues

### NuGet split

### Minor

---

## MartinDrozdik.DDD.Testing

xUnit test helpers. _Not yet reviewed._

### Bugs

### Design

### Dependency issues

### NuGet split

### Minor

### Tests
