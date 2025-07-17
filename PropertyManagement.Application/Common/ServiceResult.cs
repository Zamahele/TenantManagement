namespace PropertyManagement.Application.Common;

public class ServiceResult<T>
{
    public bool IsSuccess { get; private set; }
    public T Data { get; private set; }
    public string ErrorMessage { get; private set; }
    public IEnumerable<string> Errors { get; private set; }

    private ServiceResult(bool isSuccess, T data, string errorMessage, IEnumerable<string> errors)
    {
        IsSuccess = isSuccess;
        Data = data;
        ErrorMessage = errorMessage;
        Errors = errors ?? new List<string>();
    }

    public static ServiceResult<T> Success(T data)
    {
        return new ServiceResult<T>(true, data, string.Empty, null);
    }

    public static ServiceResult<T> Failure(string errorMessage)
    {
        return new ServiceResult<T>(false, default(T), errorMessage, null);
    }

    public static ServiceResult<T> Failure(IEnumerable<string> errors)
    {
        return new ServiceResult<T>(false, default(T), string.Join("; ", errors), errors);
    }

    public static ServiceResult<T> Failure(string errorMessage, IEnumerable<string> errors)
    {
        return new ServiceResult<T>(false, default(T), errorMessage, errors);
    }
}