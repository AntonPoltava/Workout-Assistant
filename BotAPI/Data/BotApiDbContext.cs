using Microsoft.EntityFrameworkCore;
using BotAPI.Models;

namespace BotAPI.Data
{
    public class BotApiDbContext : DbContext
    {
        public BotApiDbContext(DbContextOptions<BotApiDbContext> options)
            : base(options)
        {}

        public DbSet<FavoriteExercise> FavoriteExercises { get; set; }

       
    }
}
