namespace Menu.Shared.Dtos.Errors
{
    public class ValidationErrorResponse : ErrorResponse
    {
        public IEnumerable<string> Errors { get; set; }
        = Enumerable.Empty<string>();
    }
}
