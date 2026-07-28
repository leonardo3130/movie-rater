using Microsoft.EntityFrameworkCore;
using MovieRaterApi.Data;
using MovieRaterApi.Features.UserMovie.DTOs;
using MovieRaterApi.Features.UserMovie.Interfaces;

namespace MovieRaterApi.Features.UserMovie.Services;

public class UserMovieService : IUserMovieService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<UserMovieService> _logger;

    public UserMovieService(ApplicationDbContext db, ILogger<UserMovieService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<UserMovieResponseDto> SetFavoriteAsync(
        Guid movieId,
        Guid userId,
        bool isFavorite
    )
    {
        var movieExists = await _db.Movies.AnyAsync(m => m.Id == movieId);
        if (!movieExists)
            throw new InvalidOperationException("Movie not found.");

        var existing = await _db.UserMovies.FirstOrDefaultAsync(um =>
            um.UserId == userId && um.MovieId == movieId
        );

        if (!isFavorite)
        {
            if (existing is null)
                return DefaultResponse(movieId, userId);

            existing.IsFavorite = false;
            existing.UpdatedAt = DateTime.UtcNow;

            if (!existing.IsFavorite && !existing.IsInWatchlist)
            {
                _db.UserMovies.Remove(existing);
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "Removed UserMovie for user {UserId}, movie {MovieId} (both flags false)",
                    userId,
                    movieId
                );

                return DefaultResponse(movieId, userId);
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} movie {MovieId} isFavorite={IsFavorite}",
                userId,
                movieId,
                existing.IsFavorite
            );

            return ToDto(existing);
        }

        if (existing is null)
        {
            existing = new Data.Entities.UserMovie
            {
                UserId = userId,
                MovieId = movieId,
                IsFavorite = true,
                IsInWatchlist = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.UserMovies.Add(existing);
        }
        else
        {
            existing.IsFavorite = true;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} movie {MovieId} isFavorite={IsFavorite}",
            userId,
            movieId,
            isFavorite
        );

        return ToDto(existing);
    }

    public async Task<UserMovieResponseDto> SetWatchlistAsync(
        Guid movieId,
        Guid userId,
        bool isInWatchlist
    )
    {
        var movieExists = await _db.Movies.AnyAsync(m => m.Id == movieId);
        if (!movieExists)
            throw new InvalidOperationException("Movie not found.");

        var existing = await _db.UserMovies.FirstOrDefaultAsync(um =>
            um.UserId == userId && um.MovieId == movieId
        );

        if (!isInWatchlist)
        {
            if (existing is null)
                return DefaultResponse(movieId, userId);

            existing.IsInWatchlist = false;
            existing.UpdatedAt = DateTime.UtcNow;

            if (!existing.IsFavorite && !existing.IsInWatchlist)
            {
                _db.UserMovies.Remove(existing);
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "Removed UserMovie for user {UserId}, movie {MovieId} (both flags false)",
                    userId,
                    movieId
                );

                return DefaultResponse(movieId, userId);
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserId} movie {MovieId} isInWatchlist={IsInWatchlist}",
                userId,
                movieId,
                existing.IsInWatchlist
            );

            return ToDto(existing);
        }

        if (existing is null)
        {
            existing = new Data.Entities.UserMovie
            {
                UserId = userId,
                MovieId = movieId,
                IsFavorite = false,
                IsInWatchlist = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.UserMovies.Add(existing);
        }
        else
        {
            existing.IsInWatchlist = true;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} movie {MovieId} isInWatchlist={IsInWatchlist}",
            userId,
            movieId,
            isInWatchlist
        );

        return ToDto(existing);
    }

    public async Task<UserMovieResponseDto> GetAsync(Guid movieId, Guid userId)
    {
        var existing = await _db.UserMovies.FirstOrDefaultAsync(um =>
            um.UserId == userId && um.MovieId == movieId
        );

        if (existing is null)
            return DefaultResponse(movieId, userId);

        return ToDto(existing);
    }

    private static UserMovieResponseDto ToDto(Data.Entities.UserMovie um)
    {
        return new UserMovieResponseDto
        {
            UserId = um.UserId,
            MovieId = um.MovieId,
            IsFavorite = um.IsFavorite,
            IsInWatchlist = um.IsInWatchlist,
            CreatedAt = um.CreatedAt,
            UpdatedAt = um.UpdatedAt,
        };
    }

    private static UserMovieResponseDto DefaultResponse(Guid movieId, Guid userId)
    {
        return new UserMovieResponseDto
        {
            UserId = userId,
            MovieId = movieId,
            IsFavorite = false,
            IsInWatchlist = false,
            CreatedAt = DateTime.MinValue,
            UpdatedAt = DateTime.MinValue,
        };
    }
}
