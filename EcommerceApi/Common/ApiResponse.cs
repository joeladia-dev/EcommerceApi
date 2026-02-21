namespace EcommerceApi.Common;

/// <summary>
/// Standardized API response wrapper for all endpoints
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message ?? "Request successful"
        };
    }

    public static ApiResponse<T> FailureResponse(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }

    public static ApiResponse<T> ValidationErrorResponse(List<string> errors)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default,
            Message = "Validation failed",
            Errors = errors
        };
    }
}

/// <summary>
/// Non-generic version for responses without data
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse SuccessResponse(string? message = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message ?? "Request successful"
        };
    }

    public static ApiResponse FailureResponse(string message, List<string>? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }

    public static ApiResponse ValidationErrorResponse(List<string> errors)
    {
        return new ApiResponse
        {
            Success = false,
            Message = "Validation failed",
            Errors = errors
        };
    }
}
