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

        public AccountsController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration config, ILogger<AccountsController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _logger = logger;
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
            var tokenOptions = new JwtSecurityToken(
                issuer: _config.GetSection("Authentication:JWT:Issuer").Value,
                audience: _config.GetSection("Authentication:JWT:Audience").Value,
                claims: new List<Claim>(),
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: signInCreds
                );
            var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

            _logger.LogInformation($"User with email {userLogin.Email} logged in successfully.");
            return Ok(new { token = tokenString });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            _logger.LogInformation("User logged out successfully.");
            return NoContent();
        }
    }
}