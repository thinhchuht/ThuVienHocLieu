using System.Collections.Concurrent;
using HocLieu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace HocLieu.Api.Tests;

/// <summary>
/// Build model EF offline (không cần kết nối DB) và bắt các warning model.
/// Phòng tái phát EF10622 — "Entity has a global query filter defined and is the
/// required end of a relationship" (log Render 2026-10-12): thực thể có query
/// filter xóa mềm phải có filter khớp ở phía con, hoặc navigation thành optional.
/// </summary>
public class ModelValidationTests
{
    [Fact]
    public void Model_KhongCoWarningQueryFilterTranNavigationRequired()
    {
        var warnings = new ConcurrentBag<string>();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=hoclieu;Username=hoclieu;Password=hoclieu")
            .LogTo(warnings.Add, LogLevel.Warning)
            .Options;

        using var db = new AppDbContext(options, TimeProvider.System);
        _ = db.Model; // ép build model — warning model log ở bước này

        warnings.ShouldNotContain(
            w => w.Contains("global query filter"),
            string.Join('\n', warnings));
    }
}
