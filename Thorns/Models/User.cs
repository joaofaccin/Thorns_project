using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thorns.Models.Enum;

namespace Thorns.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.User;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? LastLogin { get; set; }

        public DateTime? DeletedAt { get; set; }
    }
}