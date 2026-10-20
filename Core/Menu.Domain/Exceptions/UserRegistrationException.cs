namespace Menu.Domain.Exceptions
{
    public class UserRegistrationException : Exception
    {
        public IReadOnlyCollection<string> Errors { get; }

        public UserRegistrationException(
            IEnumerable<string> errors)
            : base("User registration failed.")
        {
            Errors = errors.ToArray();
        }
    }
}