namespace BlackBeam.Services.Orders.Model;

public class PayResult<T>
{
    public T? Value { get; set; }
    public List<string>? Error { get; set; }
    public string? Message {get; set;}
    public bool IsSuccess {get; set;} = false;

    public static PayResult<T> Success(T data , string message = "تم العملية بنجاح")
    {
        return new PayResult<T>
        {
            Value = data,
            Message = message,
            IsSuccess = true,
            Error  = []
        };
    }
    public static PayResult<T> Failure(List<string>? error, string? message = "لم تتم العملية !")
        {

            return new PayResult<T>
            {
                IsSuccess = false,
                Value = default,
                Message = message,
                Error = error
            };
        }

}

