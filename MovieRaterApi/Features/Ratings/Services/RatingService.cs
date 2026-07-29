using Microsoft.EntityFrameworkCore;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Ratings.DTOs;
using MovieRaterApi.Features.Ratings.Interfaces;
using MovieRaterApi.Infrastructure.Exceptions;

namespace MovieRaterApi.Features.Ratings.Services;

public class RatingService : IRatingService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RatingService> _logger;

    public RatingService(ApplicationDbContext db, ILogger<RatingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<RatingResponseDto> CreateAsync(
        Guid watchSessionId,
        CreateRatingRequestDto request,
        Guid userId
    )
    {
        var session = await _db
            .WatchSessions.Include(ws => ws.Couple)
            .Include(ws => ws.Movie)
            .FirstOrDefaultAsync(ws => ws.Id == watchSessionId);

        if (session is null)
            throw new NotFoundException("Watch session not found.");

        if (session.Couple.User1Id != userId && session.Couple.User2Id != userId)
            throw new ForbiddenException(
                "You are not part of the couple for this watch session."
            );

        var existingRating = await _db.Ratings.FirstOrDefaultAsync(r =>
            r.WatchSessionId == watchSessionId && r.UserId == userId
        );

        if (existingRating is not null)
            throw new ConflictException("You have already rated this watch session.");

        var rating = new Rating
        {
            Id = Guid.NewGuid(),
            WatchSessionId = watchSessionId,
            UserId = userId,
            RatingValue = request.RatingValue,
            Review = request.Review,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Ratings.Add(rating);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Rating {RatingId} created by user {UserId} for session {SessionId} with value {RatingValue}",
            rating.Id,
            userId,
            watchSessionId,
            request.RatingValue
        );

        var user = await _db.Users.FirstAsync(u => u.Id == userId);

        return new RatingResponseDto
        {
            Id = rating.Id,
            UserId = userId,
            Username = user.Username,
            RatingValue = rating.RatingValue,
            Review = rating.Review,
            CreatedAt = rating.CreatedAt,
            UpdatedAt = rating.UpdatedAt,
        };
    }

    public async Task<RatingResponseDto> UpdateAsync(
        Guid watchSessionId,
        UpdateRatingRequestDto request,
        Guid userId
    )
    {
        var rating = await _db
            .Ratings.Include(r => r.User)
            .FirstOrDefaultAsync(r => r.WatchSessionId == watchSessionId && r.UserId == userId);

        if (rating is null)
            throw new NotFoundException("Rating not found.");

        if (rating.UserId != userId)
            throw new ForbiddenException("You can only update your own ratings.");

        rating.RatingValue = request.RatingValue;
        rating.Review = request.Review;
        rating.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Rating {RatingId} updated by user {UserId}, new value={RatingValue}",
            rating.Id,
            userId,
            request.RatingValue
        );

        return new RatingResponseDto
        {
            Id = rating.Id,
            UserId = rating.UserId,
            Username = rating.User.Username,
            RatingValue = rating.RatingValue,
            Review = rating.Review,
            CreatedAt = rating.CreatedAt,
            UpdatedAt = rating.UpdatedAt,
        };
    }

    public async Task<SessionRatingsResponseDto> GetBySessionAsync(
        Guid watchSessionId,
        Guid coupleId
    )
    {
        var session = await _db.WatchSessions.FirstOrDefaultAsync(ws =>
            ws.Id == watchSessionId && ws.CoupleId == coupleId
        );

        if (session is null)
            throw new NotFoundException("Watch session not found.");

        var ratings = await _db
            .Ratings.Include(r => r.User)
            .Where(r => r.WatchSessionId == watchSessionId)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();

        return new SessionRatingsResponseDto
        {
            WatchSessionId = watchSessionId,
            Ratings = ratings
                .Select(r => new RatingResponseDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    Username = r.User.Username,
                    RatingValue = r.RatingValue,
                    Review = r.Review,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                })
                .ToList(),
        };
    }
}
