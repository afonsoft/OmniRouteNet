using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LLMRouter.Core.Data;

/// <summary>
/// SPEC-163: per-connection SQLite pragmas for the gateway hot path.
/// synchronous=NORMAL + busy_timeout cut the commit/lock latency that showed
/// up as p95/p99 tail under concurrent load. journal_mode=WAL is applied once
/// at startup (Program.cs) — it persists in the db file and must not run on
/// connections opened before the file exists (readonly-database error).
/// </summary>
public sealed class SqlitePragmaInterceptor : DbConnectionInterceptor
{
    public static readonly SqlitePragmaInterceptor Instance = new();

    private const string Pragmas =
        "PRAGMA synchronous=NORMAL; PRAGMA busy_timeout=5000;";

    public override async Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = Pragmas;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    public override void ConnectionOpened(
        DbConnection connection, ConnectionEndEventData eventData)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = Pragmas;
        cmd.ExecuteNonQuery();
        base.ConnectionOpened(connection, eventData);
    }
}
