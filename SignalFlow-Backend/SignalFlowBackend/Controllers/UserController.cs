using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalFlowBackend.Dto;
using SignalFlowBackend.Service;

namespace SignalFlowBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class UserController(IUserService userService): ControllerBase
{
   [HttpPost("/register")]
   public async Task<ActionResult<Guid>> RegisterUser([FromBody] UserRegisterRequestDto registerRequestDto)
   {
      var registered = await userService.RegisterAsync(registerRequestDto);
      return registered is null ? BadRequest("Email is invalid or username or email is already taken") : Ok(registered);
   }

   [HttpPost("/login")]
   public async Task<ActionResult<UserDto>> LoginUser([FromBody] UserLoginRequest request)
   {
      var found = await userService.LoginAsync(request);
      if (found is null) return Unauthorized("Invalid credentials");
      return Ok(found);
   }

   [HttpPost("/refresh-token")] 
   public async Task<ActionResult<UserDto>> LoginUserWithRefreshToken([FromBody] RefreshTokenRequest tokenRequest)
   {
      var found = await userService.LoginWithRefreshTokenAsync(tokenRequest);
      if (found is null) return Unauthorized("Invalid credentials");
      return Ok(found);
   }
   
   [HttpGet("/test")]
   [Authorize]
   public ActionResult<string> ProtectedEndpoint()
   {
      return "test";
   }
}