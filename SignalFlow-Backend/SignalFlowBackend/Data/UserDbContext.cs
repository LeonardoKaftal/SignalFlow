using Microsoft.EntityFrameworkCore;
using SignalFlowBackend.Entity;

namespace SignalFlowBackend.Data;

public class UserDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
};