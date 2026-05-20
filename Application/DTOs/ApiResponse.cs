using System.Net;

namespace Application.DTOs;
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> ok(T? data, string Message)
    {
        return new ApiResponse<T>
        {
            Success =true,
            Message = Message,
            Data = data
        };
    } 
    public static ApiResponse<T> Fail(string Message)
    {
        return new ApiResponse<T>
        {
            Success =false,
            Message = Message
        };
    }
}