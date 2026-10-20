using Menu.Persistence.Data.Identity;
using Menu.Services.Abstraction.Identity;
using Menu.Shared.Dtos.Identity;
using Microsoft.AspNetCore.Identity;
using Menu.Domain.Exceptions;
using Menu.Shared.Settings;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Menu.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Menu.Persistence.Identity
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOptions<JwtOptions> _jwtOptions;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IOptions<JwtOptions> jwtOptions)
        {
            _userManager = userManager;
            _jwtOptions = jwtOptions;
        }

        // This method creates a list of claims for the given user.
        private async Task<List<Claim>> CreateClaims(
            ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.Name,
                    user.UserName ?? string.Empty),

                new Claim(
                    ClaimTypes.Email,
                    user.Email ?? string.Empty)
            };

            claims.AddRange(
                roles.Select(role =>
                    new Claim(ClaimTypes.Role, role)));

            return claims;
        }

        // This method creates signing credentials using a symmetric security key and the HMAC-SHA256 algorithm.
        private SigningCredentials CreateSigningCredentials()
        {
            // Create a symmetric security key using the secret key from JwtOptions
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtOptions.Value.Key));

            // Create signing credentials using the security key and HMAC-SHA256 algorithm
            return new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);
        }

        private async Task<string> CreateToken(ApplicationUser user)
        {
            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Value.Issuer,
                audience: _jwtOptions.Value.Audience,
                claims: await CreateClaims(user),
                expires: DateTime.UtcNow.AddMinutes(
                    _jwtOptions.Value.DurationInMinutes),
                signingCredentials: CreateSigningCredentials());

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<UserDto> RegisterAsync(
            UserRegisterDto registerDto)
        {
            var user = new ApplicationUser
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email,
                PhoneNumber = registerDto.PhoneNumber
            };

            var result = await _userManager.CreateAsync(
                user,
                registerDto.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors
                    .Select(error => error.Description);

                throw new UserRegistrationException(errors);
            }
            
            await _userManager.AddToRoleAsync(user, Roles.User);
            
            return new UserDto
            {
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Token = await CreateToken(user)
            };
        }

        public async Task<UserDto> LoginAsync(
            UserLoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(
                loginDto.Email);

            if (user == null)
            {
                throw new InvalidCredentialsException();
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(
                user,
                loginDto.Password);

            if (!isPasswordValid)
            {
                throw new InvalidCredentialsException();
            }

            return new UserDto
            {
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Token = await CreateToken(user)
            };
        }

        // This method retrieves the current user based on their email.
        public async Task<UserDto?> GetCurrentUserAsync(
            string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user is null)
                return null;

            return new UserDto
            {
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Token = await CreateToken(user)
            };
        }

        public async Task<bool> CheckEmailExistsAsync(
            string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            return user is not null;
        }

        // This method retrieves the address of the current user based on their email.
        public async Task<AddressDto?>
            GetCurrentUserAddressAsync(string email)
        {
            var user = await _userManager.Users
                .Include(user => user.Address)
                .FirstOrDefaultAsync(
                    user => user.Email == email);

            if (user?.Address is null)
                return null;

            return new AddressDto
            {
                FirstName = user.Address.FirstName,
                LastName = user.Address.LastName,
                Street = user.Address.Street,
                City = user.Address.City,
                Country = user.Address.Country
            };
        }

        public async Task<AddressDto?>
            UpdateCurrentUserAddressAsync(
                string email,
                AddressDto addressDto)
        {
            var user = await _userManager.Users
                .Include(user => user.Address)
                .FirstOrDefaultAsync(
                    user => user.Email == email);

            if (user is null)
                return null;

            if (user.Address is null)
            {
                user.Address = new Address
                {
                    FirstName = addressDto.FirstName,
                    LastName = addressDto.LastName,
                    Street = addressDto.Street,
                    City = addressDto.City,
                    Country = addressDto.Country,
                    ApplicationUserId = user.Id
                };
            }
            else
            {
                user.Address.FirstName = addressDto.FirstName;
                user.Address.LastName = addressDto.LastName;
                user.Address.Street = addressDto.Street;
                user.Address.City = addressDto.City;
                user.Address.Country = addressDto.Country;
            }

            var result = await _userManager.UpdateAsync(user);

            return result.Succeeded
                ? addressDto
                : null;
        }
    }
}