using HocLieu.Infrastructure.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HocLieu.Infrastructure.Data;

/// <summary>
/// Migrate + seed chạy trong task nền SAU khi Kestrel đã mở port (spec §15.2).
///
/// Render free tier: Postgres lạnh / provision chậm khi api khởi động. Code cũ
/// chạy migrate TRƯỚC app.Run() → port đóng tới ~5 phút (retry 15s × 20) →
/// health check deploy timeout ("Timed Out / No response from service").
/// Giờ: port mở ngay → deploy pass; migrate chạy song song. Request tới trong
/// lúc migrate có thể 500 — chấp nhận vì site vừa mới deploy.
/// Hết lượt retry vẫn lỗi → exit(1) → Render redeploy (hành vi như cũ).
/// </summary>
public sealed class StartupMigrator(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<StartupMigrator> logger) : IHostedService
{
    private const int MaxAttempts = 20;
    private const int RetryDelaySeconds = 15;

    /// <summary>Task migrate + seed — completed khi xong hoặc hết lượt retry.</summary>
    public Task Completed { get; private set; } = Task.CompletedTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!config.GetValue("Db:MigrateOnStartup", false))
            return Task.CompletedTask;

        // Task.Run + không truyền cancellationToken: shutdown không được cancel
        // giữa chừng migrate (để DB nửa vời); host stop không cần chờ task này.
        Completed = Task.Run(async () =>
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await db.Database.MigrateAsync();
                    await DbSeeder.SeedAsync(db);
                    logger.LogInformation("Đã migrate + seed database");
                    return;
                }
                catch (Exception ex) when (attempt < MaxAttempts)
                {
                    logger.LogWarning(
                        ex,
                        "Migrate + seed thử {Attempt}/{Max} chưa được, thử lại sau {Delay} giây",
                        attempt, MaxAttempts, RetryDelaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds));
                }
                catch (Exception ex)
                {
                    logger.LogCritical(
                        ex,
                        "Migrate + seed thất bại sau {Max} lần thử — thoát để Render redeploy", MaxAttempts);
                    Environment.Exit(1);
                }
            }
        });

        logger.LogInformation("Migrate + seed chạy nền — port mở ngay, không chặn deploy");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Chờ migrate xong — test integration (giữ ngữ cảnh "migrate xong mới phục vụ")
    /// và FileProcessingWorker (requeue cần bảng đã tồn tại).
    /// </summary>
    public Task WaitUntilCompletedAsync(CancellationToken ct = default)
        => Completed.WaitAsync(TimeSpan.FromMinutes(10), ct);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
