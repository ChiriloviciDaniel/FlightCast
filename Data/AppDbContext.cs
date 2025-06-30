using FlightCast.Models;
using Microsoft.EntityFrameworkCore;
namespace FlightCast.Data
{
    public class AppDbContext : DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<WeatherRecord> WeatherRecords { get; set; }
    }

}