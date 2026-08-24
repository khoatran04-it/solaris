using Microsoft.EntityFrameworkCore;

namespace backend.Helpers
{
    /// <summary>
    /// Extension method bọc transaction an toàn với SqlServerRetryingExecutionStrategy của EF Core.
    /// Đảm bảo tương thích hoàn hảo với EnableRetryOnFailure trên Azure SQL Database.
    /// </summary>
    public static class DbContextTransactionExtensions
    {
        public static async Task ExecuteInTransactionAsync(this DbContext context, Func<Task> action)
        {
            var strategy = context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await context.Database.BeginTransactionAsync();
                await action();
                await transaction.CommitAsync();
            });
        }

        public static async Task<T> ExecuteInTransactionAsync<T>(this DbContext context, Func<Task<T>> action)
        {
            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await context.Database.BeginTransactionAsync();
                var result = await action();
                await transaction.CommitAsync();
                return result;
            });
        }
    }
}
