﻿using ClassLibrary.Database;
using ClassLibrary.Database.Models;

namespace ClassLibrary.Repository;

public class Repository : IRepository
{
    private readonly ApplicationContext _dbContext;

    public Repository(ApplicationContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Video> AddVideoAsync(Video video)
    {
        var addedVideo = await _dbContext.Videos.AddAsync(video);
        return addedVideo.Entity;
    }

    public async Task AddFramesAsync(IEnumerable<Frame> frames)
    {
        await _dbContext.Frames.AddRangeAsync(frames);
    }

    public async Task AddDetectionsAsync(IEnumerable<Detection> detections)
    {
        await _dbContext.Detections.AddRangeAsync(detections);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }

    public User? GetUserByNickname(string nickname)
    {
        User? dbUser = _dbContext.Users.SingleOrDefault(user => user.Name == nickname);
        return dbUser;
    }

    public async void UpdateChatIdOnUser(long? chatId, User user)
    {
        user.TgChatId = chatId;
        await _dbContext.SaveChangesAsync();
    }
}