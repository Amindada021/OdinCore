using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace OdinCore;

public class Odin<TRequest, TResponse> : IOdin<TRequest, TResponse>
{
    protected TRequest? request;

    private Cache? cache;

    protected virtual Cache? EnableCaching() { return null; }
    protected virtual Result<TResponse>? FromCache() { cache = EnableCaching(); if (cache == null) return null; cache.found = cache.TryGetValue<Result<TResponse>>(out Result<TResponse>? result); return result; }
    protected virtual async Task<Result<TResponse>?> FromCacheAsync() { cache = EnableCaching(); if (cache == null) return null; Result<TResponse>? result = await cache.GetValueAsync<Result<TResponse>>(); cache.found = result != null; return result; }
    protected virtual void SaveCache(Result<TResponse>? result, Cache? cache) { if (this.cache != null && !this.cache.found && result != null && result.IsSuccess) this.cache.Set(result); }
    protected virtual async Task SaveCacheAsync(Result<TResponse>? result, Cache? cache) { if (this.cache != null && !this.cache.found && result != null && result.IsSuccess) await this.cache.SetAsync(result); }

    protected virtual Result<TResponse>? Validation()
    {
        if (request == null)
        {
            Type requestType = typeof(TRequest);
            if (!requestType.IsClass || requestType == typeof(string)) return null;
            request = (TRequest)Activator.CreateInstance(requestType)!;
        }
        var errors = ValidateObjectGraph(request);
        if (errors.Count == 0) return null;
        return new Result<TResponse>(HttpStatusCode.BadRequest, errors) { message = BuildValidationMessage(errors) };
    }

    protected virtual Result<TResponse>? Executing() { return null; }
    protected virtual Result<TResponse>? Execute() { return null; }
    protected virtual async Task<Result<TResponse>?> ExecuteAsync() { return await Task.FromResult<Result<TResponse>?>(null); }
    protected virtual Task<Result<TResponse>?> ExecuteAsync(CancellationToken cancellationToken) { cancellationToken.ThrowIfCancellationRequested(); return ExecuteAsync(); }
    protected virtual Result<TResponse> Executed(Result<TResponse>? result) { return result!; }
    protected virtual void OnSuccess(Result<TResponse> result) { }
    protected virtual void OnError(Result<TResponse> result) { }
    protected virtual void Finally(Result<TResponse> result) { }

    protected virtual Result<TResponse> UnhandledException(Exception ex)
    {
        if (ex is OdinCore.Tools.PagingException pagingEx)
            return new Result<TResponse>(HttpStatusCode.BadRequest, "page", pagingEx.Message) { message = pagingEx.Message };
        return new Result<TResponse>(HttpStatusCode.InternalServerError) { message = "خطای داخلی سرور" };
    }

    public virtual Result<TResponse> RunExecute(TRequest? request)
    {
        this.request = request; Result<TResponse> result;
        try { result = (Validation() ?? Executing() ?? FromCache() ?? Execute() ?? ExecuteAsync().GetAwaiter().GetResult())!; SaveCache(result, cache); result = Executed(result) ?? new Result<TResponse>(HttpStatusCode.NoContent); }
        catch (Exception ex) { result = UnhandledException(ex); }
        try { if (result.IsSuccess) OnSuccess(result); else OnError(result); Finally(result); } catch (Exception ex2) { UnhandledException(ex2); }
        return result;
    }

    public virtual Task<Result<TResponse>> RunExecuteAsync(TRequest? request) { return RunExecuteAsync(request, CancellationToken.None); }
    public virtual async Task<Result<TResponse>> RunExecuteAsync(TRequest? request, CancellationToken cancellationToken)
    {
        this.request = request; Result<TResponse> result4;
        try
        {
            cancellationToken.ThrowIfCancellationRequested(); Result<TResponse>? result = Validation();
            if (result == null)
            {
                cancellationToken.ThrowIfCancellationRequested(); Result<TResponse>? result2 = Executing();
                if (result2 == null)
                {
                    cancellationToken.ThrowIfCancellationRequested(); Result<TResponse>? result3 = await FromCacheAsync(); cancellationToken.ThrowIfCancellationRequested();
                    if (result3 == null) result3 = (await ExecuteAsync(cancellationToken)) ?? Execute(); result2 = result3;
                }
                result = result2;
            }
            result4 = result!; cancellationToken.ThrowIfCancellationRequested(); await SaveCacheAsync(result4, cache); cancellationToken.ThrowIfCancellationRequested(); result4 = Executed(result4) ?? new Result<TResponse>(HttpStatusCode.NoContent);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) { result4 = UnhandledException(ex); }
        try { if (result4.IsSuccess) OnSuccess(result4); else OnError(result4); Finally(result4); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex2) { UnhandledException(ex2); }
        return result4;
    }

    protected virtual IActionResult ActionResult(Controller? controller, Result<TResponse> result) { return new ObjectResult(new ResultDto<TResponse>(result)) { StatusCode = result._statusCode }; }
    public virtual IActionResult RunAsActionResult(TRequest? request, Controller? controller = null) { Result<TResponse> result = RunExecute(request); return ActionResult(controller, result); }
    public virtual Task<IActionResult> RunAsActionResultAsync(TRequest? request, Controller? controller = null) { return RunAsActionResultAsync(request, CancellationToken.None, controller); }
    public virtual async Task<IActionResult> RunAsActionResultAsync(TRequest? request, CancellationToken cancellationToken, Controller? controller = null) { return ActionResult(controller, await RunExecuteAsync(request, cancellationToken)); }
    protected virtual Result<TResponse> OK(TResponse data, string message = "") { return new Result<TResponse>(HttpStatusCode.OK) { data = data, message = message }; }
    protected virtual Result<TResponse> OKMessage(string message) { return new Result<TResponse>(HttpStatusCode.OK) { data = default, message = message }; }
    protected virtual Result<TResponse> BadRequest(string value, string key = "") { return new Result<TResponse>(HttpStatusCode.BadRequest, key, value) { message = value }; }
    protected virtual Result<TResponse> Forbidden(string value, string key = "") { return new Result<TResponse>(HttpStatusCode.Forbidden, key, value) { message = value }; }
    protected virtual Result<TResponse> NotFound(string value, string key = "") { return new Result<TResponse>(HttpStatusCode.NotFound, key, value) { message = value }; }
    protected virtual Result<TResponse> UnprocessableContent(string value, string key = "") { return new Result<TResponse>(HttpStatusCode.UnprocessableEntity, key, value) { message = value }; }
    protected virtual Result<TResponse> BadGateway(string value, string key = "") { return new Result<TResponse>(HttpStatusCode.BadGateway, key, value) { message = value }; }
    protected virtual Result<TResponse> Error(Result result) { return new Result<TResponse>(result.GetStatus()) { errors = result.errors, message = result.message }; }

    protected virtual Result? Validate<T>(T model)
    {
        if (model == null) { Type modelType = typeof(T); if (!modelType.IsClass || modelType == typeof(string)) return null; model = (T)Activator.CreateInstance(modelType)!; }
        var errors = ValidateObjectGraph(model);
        if (errors.Count > 0) return new Result<TResponse>(HttpStatusCode.BadRequest, errors) { message = BuildValidationMessage(errors) };
        return new Result(HttpStatusCode.OK);
    }

    private static Dictionary<string, object?> ValidateObjectGraph(object model) { var errors = new Dictionary<string, object?>(); var visited = new HashSet<object>(ReferenceEqualityComparer.Instance); ValidateObjectGraph(model, string.Empty, errors, visited); return errors; }
    private static void ValidateObjectGraph(object? model, string path, Dictionary<string, object?> errors, HashSet<object> visited)
    {
        if (model == null || IsSimpleType(model.GetType()) || !visited.Add(model)) return;
        var validationResults = new List<ValidationResult>(); Validator.TryValidateObject(model, new ValidationContext(model), validationResults, validateAllProperties: true);
        foreach (var validationResult in validationResults) { var memberName = validationResult.MemberNames.FirstOrDefault() ?? string.Empty; var key = CombinePath(path, memberName); AddValidationError(errors, key, validationResult.ErrorMessage); }
        foreach (var property in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0) continue; object? value; try { value = property.GetValue(model); } catch { continue; }
            if (value == null || IsSimpleType(property.PropertyType)) continue; var propertyPath = CombinePath(path, property.Name);
            if (value is IEnumerable items && value is not string) { var index = 0; foreach (var item in items) { ValidateObjectGraph(item, $"{propertyPath}[{index}]", errors, visited); index++; } }
            else ValidateObjectGraph(value, propertyPath, errors, visited);
        }
    }
    private static bool IsSimpleType(Type type) { type = Nullable.GetUnderlyingType(type) ?? type; return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid); }
    private static string CombinePath(string path, string memberName) { if (string.IsNullOrWhiteSpace(path)) return memberName; if (string.IsNullOrWhiteSpace(memberName)) return path; return $"{path}.{memberName}"; }
    private static void AddValidationError(Dictionary<string, object?> errors, string key, string? errorMessage)
    {
        var message = errorMessage ?? "مقدار وارد شده معتبر نیست";
        if (errors.TryGetValue(key, out var current) && current is not null) errors[key] = $"{current}#{message}"; else errors[key] = message;
    }
    private static string BuildValidationMessage(Dictionary<string, object?> errors)
    {
        return string.Join("#", errors.Values.Select(value => value?.ToString()).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct());
    }
}
