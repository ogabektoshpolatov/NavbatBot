using bot.Models;
using Microsoft.Extensions.Caching.Memory;

namespace bot.Sercvices;

public class SessionService(IMemoryCache cache)
{
    private readonly TimeSpan _expiry = TimeSpan.FromMinutes(30);

    public UserSession GetOrCreateSession(long userId)
    {
        return cache.GetOrCreate(userId, entry =>
        {
            entry.SlidingExpiration = _expiry;
            return new UserSession { UserId = userId };
        })!;
    }

    public void UpdateSession(UserSession session)
    {
        cache.Set(session.UserId, session, _expiry);
    }

    public void ClearSession(long userId) => cache.Remove(userId);
}