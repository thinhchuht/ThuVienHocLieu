using Npgsql;

namespace HocLieu.Common;

/// <summary>
/// Render (và đa số PaaS) cấp connection string dạng URI
/// <c>postgresql://user:pass@host:port/db</c>; Npgsql chỉ hiểu dạng
/// <c>Host=...;Database=...</c> (npgsql/npgsql#3461) — paste thẳng URI vào
/// <c>NpgsqlConnectionStringBuilder</c> ném "Format of the initialization string
/// does not conform to specification starting at index 0".
/// Hàm này đổi URI → dạng Npgsql để "Internal Database URL" copy từ Render
/// dùng được y nguyên; mọi chuỗi khác trả về không đổi.
/// Không ép SSL ở đây: mặc định Npgsql là <c>Prefer</c> (thử SSL trước, rớt
/// về plain nếu server không có) — chạy được cả Render lẫn DB dev local.
/// Muốn ép (vd <c>Require</c>) dùng <see cref="WithSslMode"/> qua setting
/// <c>Db__SslMode</c> trong Program.cs.
/// </summary>
public static class ConnectionString
{
    public static string ToNpgsqlFormat(string connectionString)
    {
        var cs = connectionString.Trim();

        if (!Uri.TryCreate(cs, UriKind.Absolute, out var uri)
            || (uri.Scheme != "postgres" &&
                uri.Scheme != "postgresql"))
        {
            // Đã là connection string Npgsql thông thường.
            return connectionString;
        }

        var parts = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.IdnHost,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(
                uri.AbsolutePath.TrimStart('/'))
        };

        if (parts.Length > 0 && parts[0].Length > 0)
        {
            builder.Username =
                Uri.UnescapeDataString(parts[0]);
        }

        if (parts.Length > 1)
        {
            builder.Password =
                Uri.UnescapeDataString(parts[1]);
        }

        return builder.ConnectionString;
    }

    /// <summary>
    /// Ép chế độ SSL của connection string (setting <c>Db__SslMode</c> —
    /// Require/Allow/Prefer/Disable/VerifyFull/VerifyCa).
    /// </summary>
    public static string WithSslMode(string connectionString, string mode)
    {
        if (!Enum.TryParse<SslMode>(mode, ignoreCase: true, out var ssl))
        {
            throw new ArgumentException(
                $"Chế độ SSL không hợp lệ: {mode}", nameof(mode));
        }

        return new NpgsqlConnectionStringBuilder(connectionString)
        {
            SslMode = ssl
        }.ConnectionString;
    }
}
