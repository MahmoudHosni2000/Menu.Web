namespace Menu.Persistence.Data.Identity;

public class Address
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string ApplicationUserId { get; set; } =
        string.Empty;

    public ApplicationUser ApplicationUser { get; set; } =
        null!;
}