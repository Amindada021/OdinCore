using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OdinCore;

internal class ResultDto<T>
{
    public bool success { get; set; }
    public int status { get; set; }
    public string message { get; set; }
    public T? data { get; set; }
    public Dictionary<string, object?>? errors { get; set; }

    public ResultDto(Result<T> result)
    {
        success = result.IsSuccess;
        status = result._statusCode;
        message = ResolveMessage(result);
        data = result.data;
        errors = result.errors;
    }

    private static string ResolveMessage(Result<T> result)
    {
        if (!string.IsNullOrWhiteSpace(result.message))
            return result.message;

        if (result.data is string textData && !string.IsNullOrWhiteSpace(textData))
            return textData;

        if (result.data is not null)
        {
            var property = result.data.GetType().GetProperty(
                "message",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property?.GetValue(result.data) is string dataMessage &&
                !string.IsNullOrWhiteSpace(dataMessage))
            {
                return dataMessage;
            }
        }

        var errorMessage = result.errors
            .Select(x => x.Value?.ToString())
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        return errorMessage ?? string.Empty;
    }
}
