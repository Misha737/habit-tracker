using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UsersService.Application;
using UsersService.Domain;

namespace UsersService.Infrastructure;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnName("id");
            e.Property(u => u.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
            e.Property(u => u.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
            e.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired();
            e.HasIndex(u => u.Email).IsUnique().HasDatabaseName("ix_users_email");
        });
    }
}

public class EfUserRepository : IUserRepository
{
    private readonly UserDbContext _db;
    public EfUserRepository(UserDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => _db.Users.AnyAsync(u => u.Email == email.Trim().ToLowerInvariant(), ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _db.Users.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);
    }
}

public static class UsersInfrastructureExtensions
{
    public static IServiceCollection AddUsersInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        var connStr = config.GetConnectionString("UsersDb")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:UsersDb");

        services.AddDbContext<UserDbContext>(o => o.UseNpgsql(connStr));
        services.AddScoped<IUserRepository, EfUserRepository>();
        return services;
    }
}
