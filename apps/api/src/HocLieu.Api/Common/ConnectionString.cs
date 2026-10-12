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
/// </summary>
public static class ConnectionString
{
    public static string ToNpgsqlFormat(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "Connection string không được để trống.",
                nameof(connectionString));
        }

        var cs = connectionString.Trim();

        if (!Uri.TryCreate(cs, UriKind.Absolute, out var uri)
            || (uri.Scheme != "postgres" &&
                uri.Scheme != "postgresql"))
        {
            // Đã là connection string Npgsql thông thường.
            return new NpgsqlConnectionStringBuilder(cs)
            {
                SslMode = SslMode.Require
            }.ConnectionString;
        }

        var parts = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.IdnHost,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(
                uri.AbsolutePath.TrimStart('/')),
            SslMode = SslMode.Require
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
}
