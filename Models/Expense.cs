using System.ComponentModel.DataAnnotations;

namespace FlightCast.Models
{
    public class Expense
    {
        [Required]
        public int Id { get; set; }
        public string Description { get; set; } = null!;
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }
        [Required]
        public string Category { get; set; } = null!;
        [Required]    
        public DateTime Date { get; set; } = DateTime.Now;
        /*
        [Required]
        public int UserId { get; set; }*/
    }
}