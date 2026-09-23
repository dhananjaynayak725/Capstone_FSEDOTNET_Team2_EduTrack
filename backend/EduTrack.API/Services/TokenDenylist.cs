using System.Collections.Concurrent;

namespace EduTrack.API.Services;

/// <summary>
/// Remembers revoked token ids (jti) until they would have expired anyway, so Logout really
/// invalidates a JWT. In-memory: fine for a single instance / localhost. Use a shared cache
/// (e.g. Redis) if the API is ever scaled out.
/// </summary>
public interface ITokenDenylist
{
    void Revoke(string tokenId, DateTime expiresAtUtc);
    bool IsRevoked(string tokenId);
}

public class InMemoryTokenDenylist : ITokenDenylist
{
    private readonly ConcurrentDictionary<string, DateTime> _revoked = new();

    public void Revoke(string tokenId, DateTime expiresAtUtc)
    {
        Prune();
        _revoked[tokenId] = expiresAtUtc;
    }

    public bool IsRevoked(string tokenId) =>
        _revoked.TryGetValue(tokenId, out var expiresAt) && expiresAt > DateTime.UtcNow;

    private void Prune()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in _revoked)
        {
            if (entry.Value <= now)
            {
                _revoked.TryRemove(entry.Key, out _);
            }
        }
    }
}
