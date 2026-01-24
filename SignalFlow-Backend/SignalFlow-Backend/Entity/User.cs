using System.ComponentModel.DataAnnotations;

namespace SignalFlow_Backend.Entity;

public class User
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(30)]
    public required string Username { get; set; }
    [MaxLength(30)] 
    public required string Email { get; set; }

    public required string PasswordHash { get; set; }
    public required DateTime RegistrationTime { get; set; }
}