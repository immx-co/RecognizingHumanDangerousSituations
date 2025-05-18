using ClassLibrary.Database.Models;

namespace ClassLibrary.Repository;

public interface IRepository
{
    Task<Video> AddVideoAsync(Video video);

    Task AddFramesAsync(IEnumerable<Frame> frames);

    Task AddDetectionsAsync(IEnumerable<Detection> detections);

    Task SaveChangesAsync();

    User? GetUserByNickname(string nickname);

    void UpdateChatIdOnUser(long? chatId, User user);
}