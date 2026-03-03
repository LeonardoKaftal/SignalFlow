using Microsoft.EntityFrameworkCore;
using SignalFlowBackend.Entity;

namespace SignalFlowBackend.Data;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
};