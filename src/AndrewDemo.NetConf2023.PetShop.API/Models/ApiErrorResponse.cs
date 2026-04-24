namespace AndrewDemo.NetConf2023.PetShop.API.Models
{
    public sealed class ApiErrorResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public static ApiErrorResponse Create(string code, string message)
        {
            return new ApiErrorResponse
            {
                Code = code,
                Message = message
            };
        }
    }
}
