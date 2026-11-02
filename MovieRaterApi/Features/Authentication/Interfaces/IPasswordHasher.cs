namespace MovieRaterApi.Features.Authentication.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);

    /// <summary>
    /// Spends the same time as <see cref="Verify"/> without a real hash, so a login for an
    /// unknown email cannot be told apart from a wrong password by its response time.
    /// </summary>
    void SimulateVerify(string password);
}