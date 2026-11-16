using Microsoft.EntityFrameworkCore;
using POS.Shared.Entities;
using POS.Shared.Enums;

namespace POS.Server.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAdminAsync(IServiceProvider services, IConfiguration configuration)
        {
            using var scope = services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            try
            {
                await context.Database.MigrateAsync();

                if (await context.Users.AnyAsync(u => u.Role == Role.Administrador && u.IsActive))
                {
                    return;
                }

                var email = configuration["AdminSeed:Email"]?.Trim().ToLowerInvariant();
                var password = configuration["AdminSeed:Password"];
                var name = configuration["AdminSeed:Name"]?.Trim();

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(name))
                {
                    logger.LogWarning(
                        "No hay un administrador activo y AdminSeed no está configurado. " +
                        "Configura AdminSeed:Name, AdminSeed:Email y AdminSeed:Password.");
                    return;
                }

                if (await context.Users.AnyAsync(u => u.Email == email))
                {
                    logger.LogWarning(
                        "AdminSeed:Email ya existe pero no corresponde a un administrador activo. " +
                        "No se creó el administrador inicial.");
                    return;
                }

                context.Users.Add(new User
                {
                    Name = name,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    Role = Role.Administrador,
                    IsActive = true
                });
                await context.SaveChangesAsync();
                logger.LogInformation("Administrador inicial creado con el correo {Email}.", email);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "No se pudo ejecutar el sembrado del administrador inicial.");
            }
        }
    }
}
