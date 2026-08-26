using AutoMapper;
using backend.Data;
using backend.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Tests.Common
{
    /// <summary>
    /// Factory tạo DbContext ảo trong RAM, AutoMapper và IConfiguration cho các bài Unit Test.
    /// Giúp cô lập dữ liệu hoàn toàn giữa các bài test (Mỗi bài test 1 Database riêng biệt).
    /// </summary>
    public static class TestFactories
    {
        public static SolarisDbContext CreateInMemoryDbContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<SolarisDbContext>()
                .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new SolarisDbContext(options);
        }

        public static IMapper CreateAutoMapper()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg =>
            {
                // Quét và tự động nạp toàn bộ Profiles trong Assembly backend
                cfg.AddMaps(typeof(IAProfile).Assembly);
            });

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetRequiredService<IMapper>();
        }

        public static IConfiguration CreateTestConfiguration()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "JwtSettings:SecretKey", "SolarisSecretKeyForTestingJwtTokenGenerationMustBeLongEnough123456" },
                { "JwtSettings:Issuer", "SolarisApi" },
                { "JwtSettings:Audience", "SolarisClient" },
                { "JwtSettings:ExpiryMinutes", "1440" }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }
    }
}
