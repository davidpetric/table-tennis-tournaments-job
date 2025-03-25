namespace TenisDeMasa.Worker.Data;

// https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
 
    }

    public DbSet<Tournament> Tournaments { get; set; }
}
