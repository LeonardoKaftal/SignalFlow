using Microsoft.EntityFrameworkCore;
using SignalFlow_Backend.Entity;

namespace SignalFlow_Backend.Data;

public class UserDbContext(DbContextOptions options) : DbContext(options)
{
    private DbSet<User> Users { get; set; }
};