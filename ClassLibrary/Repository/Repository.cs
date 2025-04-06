using ClassLibrary.Database;
using ClassLibrary.Database.Models;

namespace ClassLibrary.Repository;

public class Repository : IRepository
{
    private readonly ApplicationContext _dbContext;

    public Repository(ApplicationContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddVideoAsync(Video video)
    {
        ;
    }

    public async Task AddFrameAsync(Frame frame)
    {
        ;
    }

    public async Task AddDetectionAsync(Detection detection)
    {
        ;
    }
}
