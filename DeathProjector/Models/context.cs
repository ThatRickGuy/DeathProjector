using Microsoft.EntityFrameworkCore;

namespace DeathProjector.Models
{
    public class context : DbContext
    {
        public context(DbContextOptions<context> options) : base(options)
        {
        }

        public DbSet<RegionDateDeath> RegionDateDeaths { get; set; }
    }
}
