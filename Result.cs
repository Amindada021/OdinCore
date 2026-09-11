using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;

namespace OdinCore;

public class Result
{
    protected HttpStatusCode _status;

    internal int _statusCode => (int)_status;

    public Dictionary<string, object?> errors { get; set; }

    /// <summary>
    /// Human-readable message for the whole operation. Field validation details
    /// remain in <see cref="errors"/>.
    /// </summary>
    public string message { get; set; } = string.Empty;

    public bool IsSuccess => _statusCode >= 200 && _statusCode < 300;

    public HttpStatusCode status => _status;

    private Result()
    {
        errors = new Dictionary<string, object?>();
    }

    public Result(HttpStatusCode status)
    {
        _status = status;
        errors = new Dictionary<string, object?>();
    }

    public Result(HttpStatusCode status, string errorKey, object? errorValue)
    {
        _status = status;
        errors = new Dictionary<string, object?>
        {
            [errorKey] = errorValue
        };
    }

    internal Result(HttpStatusCode status, Dictionary<string, object?> errors)
    {
        _status = status;
        this.errors = errors;
    }

    public void AddError(string key, object? value)
    {
        errors.Add(key, value);
    }

    public HttpStatusCode GetStatus()
    {
        return _status;
    }
}

public class Result<T> : Result
{
    public T? data { get; set; }

    [JsonConstructor]
    public Result(HttpStatusCode status)
        : base(status)
    {
    }

    public Result(HttpStatusCode status, string errorKey, object? errorValue)
        : base(status, errorKey, errorValue)
    {
    }

    public Result(HttpStatusCode status, Dictionary<string, object?> errors)
        : base(status, errors)
    {
    }
}