using HocLieu.Common;
using Npgsql;
using Shouldly;
using Xunit;

namespace HocLieu.Api.Tests;

/// <summary>
/// Internal Database URL của Render là URI postgresql://user:pass@host:port/db;
/// Npgsql chỉ hiểu dạng keyword=value — paste thẳng URI ném ArgumentException
/// "starting at index 0" (sự cố deploy Render 2026-10-12).
/// </summary>
public class ConnectionStringTests
{
    [Fact]
    public void ToNpgsqlFormat_RenderInternalUrl_DoiThanhKeyword()
    {
        var result = ConnectionString.ToNpgsqlFormat(
            "postgresql://hoclieu_db_user:MatKhauTest123@dpg-db4cihu0tbcc73du5u10-a:5432/hoclieu_db");

        var parsed = new NpgsqlConnectionStringBuilder(result);
        parsed.Host.ShouldBe("dpg-db4cihu0tbcc73du5u10-a");
        parsed.Port.ShouldBe(5432);
        parsed.Database.ShouldBe("hoclieu_db");
        parsed.Username.ShouldBe("hoclieu_db_user");
        parsed.Password.ShouldBe("MatKhauTest123");
    }

    [Fact]
    public void ToNpgsqlFormat_SchemePostgres_KhongPort_Ke()
    {
        var result = ConnectionString.ToNpgsqlFormat("postgres://user:pass@host.example.com/hoclieu");

        var parsed = new NpgsqlConnectionStringBuilder(result);
        parsed.Host.ShouldBe("host.example.com");
        parsed.Port.ShouldBe(5432); // mặc định Npgsql
        parsed.Database.ShouldBe("hoclieu");
        parsed.Username.ShouldBe("user");
        parsed.Password.ShouldBe("pass");
    }

    [Fact]
    public void ToNpgsqlFormat_MatKhauPercentEncoded_GiaiMa()
    {
        // @ trong mật khẩu bị encode thành %40 khi nằm trong URI
        var result = ConnectionString.ToNpgsqlFormat("postgresql://user:a%40b%3Bc@host/db");

        var parsed = new NpgsqlConnectionStringBuilder(result);
        parsed.Password.ShouldBe("a@b;c");
    }

    [Fact]
    public void ToNpgsqlFormat_KhongCoUser_Ke()
    {
        var result = ConnectionString.ToNpgsqlFormat("postgresql://host.example.com/db");

        var parsed = new NpgsqlConnectionStringBuilder(result);
        parsed.Host.ShouldBe("host.example.com");
        parsed.Database.ShouldBe("db");
        parsed.Username.ShouldBeNull();
    }

    [Theory]
    [InlineData("Host=localhost;Database=hoclieu;Username=hoclieu;Password=hoclieu")]
    [InlineData("")]
    public void ToNpgsqlFormat_KhongPhaiUri_TraLaiNhuCu(string cs)
    {
        ConnectionString.ToNpgsqlFormat(cs).ShouldBe(cs);
    }

    [Fact]
    public void WithSslMode_Require_DatVaoChuoi_KeDuLich()
    {
        var result = ConnectionString.WithSslMode(
            "Host=localhost;Database=hoclieu;Username=hoclieu;Password=hoclieu",
            "Require");

        var parsed = new NpgsqlConnectionStringBuilder(result);
        parsed.SslMode.ShouldBe(SslMode.Require);
        parsed.Host.ShouldBe("localhost");
        parsed.Password.ShouldBe("hoclieu");
    }

    [Fact]
    public void WithSslMode_GiaTriKhongHoppLe_Throw()
    {
        Should.Throw<ArgumentException>(() =>
            ConnectionString.WithSslMode("Host=localhost", "BanCao"));
    }
}
