using ClassLibrary.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary.Database;

public class ApplicationContext : DbContext
{
    public DbSet<RecognitionResult> RecognitionResults { get; set; } = null!;

    public DbSet<Frame> Frames { get; set; } = null!;

    public DbSet<Video> Videos { get; set; } = null!;

    public DbSet<Detection> Detections { get; set; } = null!;

    public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
    {
        Database.EnsureCreated();
    }
}
