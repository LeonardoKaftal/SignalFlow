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
      var id = await userService.RegisterAsync(registerRequestDto);
      return id is null ? BadRequest("Email is invalid or username is already taken") : Ok(id);
   }

   [HttpPost("/login")]
   public async Task<ActionResult<string>> LoginUser([FromBody] UserLoginRequest request)
   {
      var found = await userService.LoginAsync(request);
      if (found is null) return Unauthorized("Invalid credentials");
      const string token = "JWT";
      return Ok(token);
   }
}