using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthApiController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IAuditService _auditService;

        public AuthApiController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IAuditService auditService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _configuration = configuration;
            _auditService = auditService;
        }

        /// <summary>
        /// Authenticate user and receive a JWT Bearer token
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto { Success = false, Message = "Invalid payload." });
            }

            var user = await _userManager.FindByEmailAsync(request.Email) 
                       ?? await _userManager.FindByNameAsync(request.Email);

            if (user == null || !user.IsActive)
            {
                await _auditService.LogAsync("API Auth", "Failed Login", details: $"Failed API login attempt for '{request.Email}'", result: "Failed");
                return Unauthorized(new AuthResponseDto { Success = false, Message = "Invalid email or password." });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    await _auditService.LogAsync("API Auth", "Lockout", entityName: "User", recordId: user.Id, details: "User locked out via API login.", result: "Failed");
                    return StatusCode(423, new AuthResponseDto { Success = false, Message = "Account is locked out. Try again later." });
                }

                await _auditService.LogAsync("API Auth", "Failed Login", entityName: "User", recordId: user.Id, details: "Invalid API password.", result: "Failed");
                return Unauthorized(new AuthResponseDto { Success = false, Message = "Invalid email or password." });
            }

            // Generate JWT Token
            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? "SalesExecutive";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, primaryRole)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["JwtSettings:Key"] ?? "AcxiomCRM_Super_Secret_Security_Key_2026_Secure_Key!"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(
                Convert.ToDouble(_configuration["JwtSettings:DurationInMinutes"] ?? "120"));

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"] ?? "AcxiomCRM",
                audience: _configuration["JwtSettings:Audience"] ?? "AcxiomCRM_Users",
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            await _auditService.LogAsync("API Auth", "Login", entityName: "User", recordId: user.Id, details: "User authenticated via REST API.", result: "Success");

            return Ok(new AuthResponseDto
            {
                Success = true,
                Message = "Authentication successful.",
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                UserId = user.Id,
                Email = user.Email,
                Role = primaryRole,
                ExpiresAt = expires
            });
        }

        /// <summary>
        /// Terminate API session
        /// </summary>
        [HttpPost("logout")]
        [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},Identity.Application")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _auditService.LogAsync("API Auth", "Logout", entityName: "User", recordId: userId, details: "User logged out via API.", result: "Success");
            return Ok(new { message = "Logged out successfully." });
        }
    }
}
