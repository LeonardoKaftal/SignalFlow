using Microsoft.AspNetCore.Mvc;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Service;

namespace SignalFlowBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class UserController(IUserService userService): ControllerBase
{
   [HttpPost("register")]
   public async Task<ActionResult<LoginResponseDto>> RegisterUser([FromBody] UserRegisterRequestDto registerRequestDto)
   {
      var registered = await userService.RegisterAsync(registerRequestDto);
      if (registered is null) return BadRequest("Email is invalid or username or email is already taken");

      SetRefreshTokenCookie(registered.RefreshToken, registered.RefreshTokenExpiryTime);
      return Ok(registered);
   }

   [HttpPost("login")]
   public async Task<ActionResult<LoginResponseDto>> LoginUser([FromBody] UserLoginRequest request)
   {
      var found = await userService.LoginAsync(request);
      if (found is null) return Unauthorized("Invalid credentials");

      SetRefreshTokenCookie(found.RefreshToken, found.RefreshTokenExpiryTime);
      return Ok(found);
   }

   [HttpPost("refresh-token")] 
   public async Task<ActionResult<LoginResponseDto>> LoginUserWithRefreshToken([FromBody] RefreshTokenRequest tokenRequest)
   {
      var refreshToken = Request.Cookies["refreshToken"];
      if (string.IsNullOrWhiteSpace(refreshToken)) return Unauthorized("Invalid credentials");

      var found = await userService.LoginWithRefreshTokenAsync(tokenRequest.Id, refreshToken);
      if (found is null) return Unauthorized("Invalid credentials");

      SetRefreshTokenCookie(found.RefreshToken, found.RefreshTokenExpiryTime);
      return Ok(found);
   }

   private void SetRefreshTokenCookie(string? refreshToken, DateTime? expiry)
   {
      if (string.IsNullOrWhiteSpace(refreshToken) || expiry is null)
         return;

      Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
      {
         HttpOnly = true,
         Secure = true,
         SameSite = SameSiteMode.None,
         Expires = expiry,
         Path = "/"
      });
   }
}