

using Microsoft.EntityFrameworkCore;
using POS.Shared.Entities;
using POS.Shared.Enums;

namespace POS.Server.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAdminAsync(
            IServiceProvider services,
            IConfiguration configuration)
        {
            using var scope = services.CreateScope();

            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("DbSeeder");

            var context = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            try
            {
                await context.Database.MigrateAsync();

                var email = configuration["AdminSeed:Email"]
                    ?.Trim().ToLowerInvariant();

                var password = configuration["AdminSeed:Password"];
                var name = configuration["AdminSeed:Name"]?.Trim();

                var resetPassword = configuration
                    .GetValue<bool>("AdminSeed:ResetPassword");

                if (string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(password) ||
                    string.IsNullOrWhiteSpace(name))
                {
                    logger.LogWarning(
                        "Falta configurar AdminSeed.");
                    return;
                }

                var existingUser = await context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                // Recuperación controlada de la cuenta existente
                if (existingUser != null && resetPassword)
                {
                    existingUser.Name = name;
                    existingUser.PasswordHash =
                        BCrypt.Net.BCrypt.HashPassword(password);

                    existingUser.Role = Role.Administrador;
                    existingUser.IsActive = true;

                    await context.SaveChangesAsync();

                    logger.LogInformation(
                        "Cuenta administradora recuperada: {Email}",
                        email);

                    return;
                }

                // No duplicar administradores
                if (await context.Users.AnyAsync(
                    u => u.Role == Role.Administrador && u.IsActive))
                {
                    return;
                }

                if (existingUser != null)
                {
                    logger.LogWarning(
                        "El correo configurado ya pertenece a otro usuario.");
                    return;
                }

                // Crear administrador inicial si no existe ninguno
                context.Users.Add(new User
                {
                    Name = name,
                    Email = email,
                    PasswordHash =
                        BCrypt.Net.BCrypt.HashPassword(password),
                    Role = Role.Administrador,
                    IsActive = true
                });

                await context.SaveChangesAsync();

                logger.LogInformation(
                    "Administrador inicial creado: {Email}", email);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error al recuperar o crear el administrador.");
            }
        }
    }
}
