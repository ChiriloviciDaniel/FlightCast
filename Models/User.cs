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
        [EmailAddress(ErrorMessage = "Email invalid")]
        public string Email { get; set; } = null!;
        [Required]
        public UserRole Role { get; set; } 
        public DateTime CreateDate { get; set; } = DateTime.Now;
    }
}