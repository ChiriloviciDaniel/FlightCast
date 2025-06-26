using System.ComponentModel.DataAnnotations;
using FlightCast.Enums;
namespace FlightCast.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        public string Username { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
        [Required]
        public List<UserRole> UserRoles { get; set; } = null!;
        public DateTime CreateDate { get; set; } = DateTime.Now;
    }
}