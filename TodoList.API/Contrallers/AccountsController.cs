using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TodoList.API.Models;
using TodoList.Core.Models;
using TodoList.API.Services;

namespace TodoList.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountsController> _logger;
        private readonly IConfiguration _config;
        private readonly IJwtBlacklistService _jwtBlacklistService;

        public AccountsController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration config,
            ILogger<AccountsController> logger,
            IJwtBlacklistService jwtBlacklistService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _logger = logger;
            _jwtBlacklistService = jwtBlacklistService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto userLogin)
        {
            if (!ModelState.IsValid || userLogin == null)
            {
                _logger.LogError("Invalid login object provided.");
                return BadRequest();
            }

            var signInResult = await _signInManager.PasswordSignInAsync(userLogin.Email, userLogin.Password, true, false);
            if (!signInResult.Succeeded)
            {
                _logger.LogError($"Unable to login user with email {userLogin.Email}.");
                return Unauthorized();
            }

            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("Authentication:JWT:SecurityKey").Value));
            var signInCreds = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);
            // Include standard claims: sub (user id), email and jti (token id)
            var user = await _userManager.FindByEmailAsync(userLogin.Email);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user?.Id ?? string.Empty),
                new Claim(ClaimTypes.Email, userLogin.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenOptions = new JwtSecurityToken(
                issuer: _config.GetSection("Authentication:JWT:Issuer").Value,
                audience: _config.GetSection("Authentication:JWT:Audience").Value,
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: signInCreds
                );
            var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

            _logger.LogInformation($"User with email {userLogin.Email} logged in successfully.");
            return Ok(new { token = tokenString });
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Models.UserRegisterDto registerDto)
        {
            if (registerDto == null || !ModelState.IsValid)
            {
                _logger.LogError("Invalid register object provided.");
                return BadRequest(ModelState);
            }

            var user = new ApplicationUser { UserName = registerDto.Email, Email = registerDto.Email };
            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                _logger.LogError($"Failed to create user {registerDto.Email}: {string.Join(';', result.Errors)}");
                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError(err.Code, err.Description);
                }
                return BadRequest(ModelState);
            }

            // Generate JWT for the newly created user
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("Authentication:JWT:SecurityKey").Value));
            var signInCreds = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);
            // Include standard claims: sub (user id), email and jti (token id)
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(ClaimTypes.Email, registerDto.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenOptions = new JwtSecurityToken(
                issuer: _config.GetSection("Authentication:JWT:Issuer").Value,
                audience: _config.GetSection("Authentication:JWT:Audience").Value,
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: signInCreds
                );
            var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

            _logger.LogInformation($"User with email {registerDto.Email} registered successfully.");
            return Created(string.Empty, new { token = tokenString });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // Get the token ID and add it to the blacklist
            if (Request.Headers.ContainsKey("Authorization"))
            {
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var jsonToken = handler.ReadJwtToken(token);
                    // Only blacklist token if it has an ID
                    if (!string.IsNullOrEmpty(jsonToken.Id))
                    {
                        var expirationDate = jsonToken.ValidTo;
                        await _jwtBlacklistService.BlacklistTokenAsync(jsonToken.Id, expirationDate);
                    }
                }
            }

            await _signInManager.SignOutAsync();

            _logger.LogInformation("User logged out successfully.");
            return NoContent();
        }
    }
}