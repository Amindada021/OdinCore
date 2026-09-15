# OdinCore

OdinCore is a lightweight request-to-result pipeline for ASP.NET Core. It provides a simple handler model with built-in validation, dependency-injection auto-registration, caching, paging helpers, exception mapping, and HTTP result shaping — without requiring a mediator framework.

> Built for real-world ASP.NET Core applications and now open source for the .NET community.

## Features

- Request → validation → execution → result pipeline
- Synchronous and asynchronous handlers
- CancellationToken support for async execution
- DataAnnotations validation, including nested object graphs
- Automatic handler registration through assembly scanning
- Configurable DI lifetime per handler
- Memory and distributed caching
- Shared-cache helpers with get-or-create and invalidation support
- Standard HTTP-oriented `Result<T>` model
- ASP.NET Core `IActionResult` integration
- Paging helpers for `IQueryable<T>`
- Carry-over and running-total helpers for paged financial/reporting scenarios
- Minimal external surface area

## Requirements

- .NET 10 or later
- ASP.NET Core

## Installation

Once the package is published to NuGet:

```bash
dotnet add package OdinCore
```

Or with Package Manager Console:

```powershell
Install-Package OdinCore
```

## Quick start

### 1. Create a request

```csharp
using System.ComponentModel.DataAnnotations;

public sealed class CreateUserRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
}
```

### 2. Create a handler

```csharp
using OdinCore;

public sealed class CreateUser : Odin<CreateUserRequest, int>
{
    protected override Result<int>? Execute()
    {
        // Your business logic
        var newUserId = 42;
        return OK(newUserId);
    }
}
```

For I/O work, prefer the asynchronous pipeline:

```csharp
public sealed class GetUser : Odin<int, UserDto>
{
    protected override async Task<Result<UserDto>?> ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var user = await LoadUserAsync(request, cancellationToken);

        return user is null
            ? NotFound("User was not found.")
            : OK(user);
    }
}
```

### 3. Register handlers

```csharp
using System.Reflection;
using OdinCore;

builder.Services.AddOdin(typeof(CreateUser).Assembly);
```

OdinCore scans the supplied assembly and automatically registers classes derived from `Odin<,>`.

Handlers are transient by default. You can override the lifetime:

```csharp
using Microsoft.Extensions.DependencyInjection;
using OdinCore;

[ServiceLifetime(ServiceLifetime.Scoped)]
public sealed class CreateUser : Odin<CreateUserRequest, int>
{
    // ...
}
```

### 4. Use from an API endpoint

```csharp
[HttpPost]
public async Task<IActionResult> Create(
    [FromBody] CreateUserRequest request,
    [FromServices] CreateUser handler,
    CancellationToken cancellationToken)
{
    return await handler.RunAsActionResultAsync(
        request,
        cancellationToken,
        this);
}
```

## Pipeline

The async pipeline follows this order:

```text
Validation
  ↓
Executing
  ↓
Cache lookup
  ↓
ExecuteAsync / Execute
  ↓
Cache save
  ↓
Executed
  ↓
OnSuccess / OnError
  ↓
Finally
```

Any stage that returns a result can short-circuit the remaining execution stages.

### Main extension points

```csharp
protected virtual Cache? EnableCaching();
protected virtual Result<TResponse>? Validation();
protected virtual Result<TResponse>? Executing();
protected virtual Result<TResponse>? Execute();
protected virtual Task<Result<TResponse>?> ExecuteAsync();
protected virtual Task<Result<TResponse>?> ExecuteAsync(CancellationToken cancellationToken);
protected virtual Result<TResponse> Executed(Result<TResponse>? result);
protected virtual void OnSuccess(Result<TResponse> result);
protected virtual void OnError(Result<TResponse> result);
protected virtual void Finally(Result<TResponse> result);
protected virtual Result<TResponse> UnhandledException(Exception ex);
```

## Result model

OdinCore maps result status codes to HTTP responses.

Common helpers available inside handlers include:

```csharp
return OK(data);
return OKMessage("Saved successfully.");
return BadRequest("Invalid request.");
return Forbidden("Access denied.");
return NotFound("Resource not found.");
return UnprocessableContent("Unable to process request.");
return BadGateway("Upstream service failed.");
```

`Result<T>` exposes the response data and errors while success is determined by the HTTP status range.

## Validation

Requests are validated automatically using DataAnnotations before execution.

```csharp
public sealed class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
```

Nested request objects and collections are validated as an object graph.

You can also call `Validate(model)` from a handler when validating another object.

## Caching

Caching is opt-in per handler by overriding `EnableCaching()`.

### Memory cache

```csharp
public sealed class GetProduct : Odin<int, ProductDto>
{
    private readonly IMemoryCache _cache;

    public GetProduct(IMemoryCache cache)
    {
        _cache = cache;
    }

    protected override Cache? EnableCaching()
        => new Cache(
            _cache,
            $"product:{request}",
            TimeSpan.FromMinutes(5));
}
```

### Distributed cache

```csharp
protected override Cache? EnableCaching()
    => new Cache(
        _distributedCache,
        $"product:{request}",
        TimeSpan.FromMinutes(5));
```

Use `sliding: true` when sliding expiration is preferred.

### Shared cache data

The same `Cache` abstraction can also be used outside the handler-result pipeline for data shared by multiple handlers:

```csharp
var cache = new Cache(
    memoryCache,
    $"coding:{databaseId}",
    TimeSpan.FromHours(2));

var coding = cache.GetOrCreate(() => LoadCoding(databaseId));
```

Async factories are supported as well:

```csharp
var coding = await cache.GetOrCreateAsync(
    ct => LoadCodingAsync(databaseId, ct),
    cancellationToken);
```

Invalidate the shared entry after a successful mutation:

```csharp
cache.Remove();
// or
await cache.RemoveAsync(cancellationToken);
```

These helpers work with both memory and distributed cache providers.

## Paging

Implement `IPagedRequest`:

```csharp
using OdinCore.Tools;

public sealed class ProductListRequest : IPagedRequest
{
    public int? page { get; set; }
    public int? pageSize { get; set; }
}
```

Then page an `IQueryable<T>`:

```csharp
var result = query.ToPaged(request);
```

The result contains page metadata and the current page items.

For financial/reporting scenarios, OdinCore also supports brought-forward and total sums through paging selectors and carry-over helpers.

## Custom application base handler

Large applications often benefit from a project-specific base class:

```csharp
public abstract class AppHandler<TRequest, TResponse>
    : Odin<TRequest, TResponse>
{
    protected override Result<TResponse> UnhandledException(Exception ex)
    {
        // Application logging / telemetry
        return base.UnhandledException(ex);
    }
}
```

Application handlers can then inherit `AppHandler<,>` while OdinCore remains independent from domain code.

## Building locally

```bash
git clone https://github.com/Amindada021/OdinCore.git
cd OdinCore
dotnet restore
dotnet build -c Release
dotnet pack -c Release
```

## NuGet

NuGet package ID:

```text
OdinCore
```

Project repository:

https://github.com/Amindada021/OdinCore

## Contributing

Ideas, bug reports, documentation improvements, performance work, tests, and pull requests are welcome.

Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

If you have an idea but are not ready to implement it, open a GitHub issue and describe the use case. Discussion around API design and real-world usage is especially welcome.

## Philosophy

OdinCore aims to stay small and understandable. New features should solve recurring application concerns without turning the library into a full application framework.

The project values:

- predictable behavior
- small APIs
- low ceremony
- async-first application code
- backward compatibility
- measurable performance
- useful community feedback

## License

OdinCore is open source under the [MIT License](LICENSE).
