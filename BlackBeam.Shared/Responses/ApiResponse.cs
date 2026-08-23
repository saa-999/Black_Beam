namespace BlackBeam.Shared.Responses
{
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }

        public List<string>? Error { get; set; }

        public static ApiResponse<T> Success(T? data, string message = "تمت العملية بنجاح ")
        {
            return new ApiResponse<T>
            {
                IsSuccess = true,
                Message = message,
                Error = [],
                Data = data
            };
        }

        public static ApiResponse<T> Failure(List<string>? error, string? message = "لم تتم العملية !")
        {

            return new ApiResponse<T>
            {
                IsSuccess = false,
                Data = default,
                Message = message,
                Error = error
            };

        }

    }
}
