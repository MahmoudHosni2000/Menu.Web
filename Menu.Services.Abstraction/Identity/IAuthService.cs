using Menu.Shared.Dtos.Identity;

namespace Menu.Services.Abstraction.Identity
{
    public interface IAuthService
    {
        Task<UserDto> RegisterAsync(
            UserRegisterDto registerDto);

        Task<UserDto> LoginAsync(
            UserLoginDto loginDto);

        Task<UserDto?> GetCurrentUserAsync(string email);

        Task<bool> CheckEmailExistsAsync(string email);

        Task<AddressDto?> GetCurrentUserAddressAsync(
            string email);

        Task<AddressDto?> UpdateCurrentUserAddressAsync(
            string email,
            AddressDto addressDto);
    }
}