namespace LinkittyDo.Api.Models;

public class ApiResponse<T>
{
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;

    public ApiResponse() { }

    public ApiResponse(T data, string message = "Operation successful")
    {
        Data = data;
        Message = message;
    }
}
