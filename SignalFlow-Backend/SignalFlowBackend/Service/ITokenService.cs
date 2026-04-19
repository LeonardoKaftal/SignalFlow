using SignalFlowBackend.Entity;

namespace SignalFlowBackend.Service;

public interface ITokenService
{
    public string GenerateToken(User user);
    public string GenerateRefreshToken();
}