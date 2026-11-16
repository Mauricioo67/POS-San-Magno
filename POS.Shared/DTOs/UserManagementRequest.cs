using POS.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace POS.Shared.DTOs
{
    public class UserManagementRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public Role Role { get; set; } = Role.Cajero;
        public string Password { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
