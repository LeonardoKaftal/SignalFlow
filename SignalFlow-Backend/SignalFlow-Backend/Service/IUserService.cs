using SignalFlow_Backend.Dto;
using SignalFlow_Backend.Entity;

namespace SignalFlow_Backend.Service;

public interface IUserService
{
    public User? GetUserById(Guid id);
    public User? GetUserByUsername();
    public UserRegisterResponseDto? Register(UserRegisterResponseDto request);
    public User? Login(UserRequestDto request);
}