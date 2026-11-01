using MovieRaterApi.Features.Authentication.Interfaces;

namespace MovieRaterApi.Features.Authentication.Services;

public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    // Random password nobody knows, hashed once with the same work factor as real hashes.
    private static readonly Lazy<string> DummyHash = new(() =>
        BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), workFactor: WorkFactor)
    );

    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);
    }

    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    public void SimulateVerify(string password)
    {
        BCrypt.Net.BCrypt.Verify(password, DummyHash.Value);
    }
}