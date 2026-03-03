using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SignalFlowBackend.Entity;

namespace SignalFlowBackend.Service;

public class TokenService(IConfiguration conf): ITokenService
{
    public string GenerateToken(User user)
    {
        List<Claim> claims = [
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        ];

        var bKey = Encoding.UTF8.GetBytes(conf.GetValue<string>("Token")!);
        var key = new SymmetricSecurityKey(bKey);
        var token = new JwtSecurityToken(
            claims: claims,
            issuer: conf.GetValue<string>("Issuer"),
            audience: conf.GetValue<string>("Audience"),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            expires: DateTime.UtcNow.AddHours(2)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var num = new byte[32];
        RandomNumberGenerator.Fill(num);
        return Convert.ToBase64String(num);
    }
}