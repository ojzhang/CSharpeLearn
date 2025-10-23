using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace TodoList.API.Services
{
    public interface IJwtBlacklistService
    {
        Task<bool> IsTokenBlacklistedAsync(string tokenId);
        Task BlacklistTokenAsync(string tokenId, DateTime expirationDate);
    }

    public class JwtBlacklistService : IJwtBlacklistService
    {
        // In-memory blacklist - in production you would use Redis or database
        private readonly ConcurrentDictionary<string, DateTime> _blacklistedTokens = new ConcurrentDictionary<string, DateTime>();

        // Clean up expired blacklisted tokens periodically
        public Task CleanupExpiredTokensAsync()
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _blacklistedTokens)
            {
                if (kvp.Value <= now)
                {
                    _blacklistedTokens.TryRemove(kvp.Key, out _);
                }
            }

            return Task.CompletedTask;
        }

        public Task<bool> IsTokenBlacklistedAsync(string tokenId)
        {
            // Handle null or empty tokenId
            if (string.IsNullOrEmpty(tokenId))
            {
                return Task.FromResult(false);
            }

            if (_blacklistedTokens.TryGetValue(tokenId, out DateTime expiration))
            {
                // If token is expired, remove it from blacklist
                if (expiration <= DateTime.UtcNow)
                {
                    _blacklistedTokens.TryRemove(tokenId, out _);
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task BlacklistTokenAsync(string tokenId, DateTime expirationDate)
        {
            // Handle null or empty tokenId
            if (string.IsNullOrEmpty(tokenId))
            {
                return Task.CompletedTask;
            }

            _blacklistedTokens[tokenId] = expirationDate;
            return Task.CompletedTask;
        }
    }
}