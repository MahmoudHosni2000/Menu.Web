using Microsoft.AspNetCore.Identity;

namespace Menu.Persistence.Data.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public Address? Address { get; set; }
    }
}