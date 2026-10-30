namespace EasyCreditApi.Infrastructure.Oracle;

// Hasil pemetaan satu kode ORA-xxxxx ke response HTTP.
// Transient = aman dicoba lagi (koneksi putus, timeout, lock); response diberi header Retry-After.
public sealed record OracleErrorInfo(int StatusCode, string Message, LogLevel LogLevel, bool Transient = false);

// Peta kode error Oracle -> status HTTP. Pesan untuk client sengaja umum;
// detail asli (kode + pesan Oracle) hanya masuk log.
public static class OracleErrorMap
{
    private static readonly OracleErrorInfo Unknown = new(
        StatusCodes.Status500InternalServerError,
        "Terjadi kesalahan pada database.",
        LogLevel.Error);

    private static readonly Dictionary<int, OracleErrorInfo> Map = new()
    {
        // ── Constraint: data bentrok dengan data lain ──
        [1] = new(StatusCodes.Status409Conflict, "Data sudah ada (melanggar unique constraint).", LogLevel.Warning),           // unique constraint violated
        [2292] = new(StatusCodes.Status409Conflict, "Data masih dipakai data lain dan tidak bisa dihapus.", LogLevel.Warning), // child record found
        [2291] = new(StatusCodes.Status400BadRequest, "Data referensi tidak ditemukan.", LogLevel.Warning),                    // parent key not found

        // ── Data tidak valid (seharusnya sudah dicegat validasi DTO) ──
        [1400] = new(StatusCodes.Status400BadRequest, "Ada kolom wajib yang kosong.", LogLevel.Warning),          // cannot insert NULL
        [1407] = new(StatusCodes.Status400BadRequest, "Ada kolom wajib yang kosong.", LogLevel.Warning),          // cannot update to NULL
        [2290] = new(StatusCodes.Status400BadRequest, "Data melanggar aturan validasi database.", LogLevel.Warning), // check constraint violated
        [12899] = new(StatusCodes.Status400BadRequest, "Ada nilai yang terlalu panjang.", LogLevel.Warning),       // value too large for column
        [1438] = new(StatusCodes.Status400BadRequest, "Ada angka yang melebihi presisi kolom.", LogLevel.Warning), // value larger than precision
        [1722] = new(StatusCodes.Status400BadRequest, "Format angka tidak valid.", LogLevel.Warning),              // invalid number
        [1840] = new(StatusCodes.Status400BadRequest, "Format tanggal tidak valid.", LogLevel.Warning),
        [1841] = new(StatusCodes.Status400BadRequest, "Format tanggal tidak valid.", LogLevel.Warning),
        [1843] = new(StatusCodes.Status400BadRequest, "Format tanggal tidak valid.", LogLevel.Warning),
        [1847] = new(StatusCodes.Status400BadRequest, "Format tanggal tidak valid.", LogLevel.Warning),

        // ── Lock / deadlock: coba lagi ──
        [54] = new(StatusCodes.Status409Conflict, "Data sedang dipakai proses lain; coba lagi.", LogLevel.Warning, Transient: true),    // resource busy (NOWAIT)
        [60] = new(StatusCodes.Status409Conflict, "Data sedang dipakai proses lain; coba lagi.", LogLevel.Warning, Transient: true),    // deadlock detected
        [8177] = new(StatusCodes.Status409Conflict, "Data berubah bersamaan; coba lagi.", LogLevel.Warning, Transient: true),           // can't serialize access

        // ── Database tidak bisa dihubungi / timeout ──
        [1013] = new(StatusCodes.Status503ServiceUnavailable, "Database timeout; coba lagi.", LogLevel.Error, Transient: true),         // user requested cancel (command timeout)
        [1033] = new(StatusCodes.Status503ServiceUnavailable, "Database sedang tidak tersedia.", LogLevel.Error, Transient: true),     // initialization/shutdown in progress
        [1034] = new(StatusCodes.Status503ServiceUnavailable, "Database sedang tidak tersedia.", LogLevel.Error, Transient: true),     // Oracle not available
        [1089] = new(StatusCodes.Status503ServiceUnavailable, "Database sedang tidak tersedia.", LogLevel.Error, Transient: true),     // immediate shutdown
        [3113] = new(StatusCodes.Status503ServiceUnavailable, "Koneksi database terputus; coba lagi.", LogLevel.Error, Transient: true), // end-of-file on channel
        [3114] = new(StatusCodes.Status503ServiceUnavailable, "Koneksi database terputus; coba lagi.", LogLevel.Error, Transient: true), // not connected
        [3135] = new(StatusCodes.Status503ServiceUnavailable, "Koneksi database terputus; coba lagi.", LogLevel.Error, Transient: true), // connection lost contact
        [12170] = new(StatusCodes.Status503ServiceUnavailable, "Database tidak bisa dihubungi.", LogLevel.Error, Transient: true),      // connect timeout
        [12514] = new(StatusCodes.Status503ServiceUnavailable, "Database tidak bisa dihubungi.", LogLevel.Error, Transient: true),      // listener: service unknown
        [12516] = new(StatusCodes.Status503ServiceUnavailable, "Database sedang sibuk; coba lagi.", LogLevel.Error, Transient: true),   // no available handler
        [12519] = new(StatusCodes.Status503ServiceUnavailable, "Database sedang sibuk; coba lagi.", LogLevel.Error, Transient: true),   // no appropriate handler
        [12520] = new(StatusCodes.Status503ServiceUnavailable, "Database sedang sibuk; coba lagi.", LogLevel.Error, Transient: true),
        [12528] = new(StatusCodes.Status503ServiceUnavailable, "Database sedang tidak tersedia.", LogLevel.Error, Transient: true),     // instances blocking connections
        [12537] = new(StatusCodes.Status503ServiceUnavailable, "Koneksi database terputus; coba lagi.", LogLevel.Error, Transient: true), // TNS connection closed
        [12541] = new(StatusCodes.Status503ServiceUnavailable, "Database tidak bisa dihubungi.", LogLevel.Error, Transient: true),      // no listener
        [12543] = new(StatusCodes.Status503ServiceUnavailable, "Database tidak bisa dihubungi.", LogLevel.Error, Transient: true),      // destination host unreachable
        [12545] = new(StatusCodes.Status503ServiceUnavailable, "Database tidak bisa dihubungi.", LogLevel.Error, Transient: true),      // target host does not exist

        // ── Salah konfigurasi / bug: bukan salah client, jangan dicoba ulang ──
        [1017] = new(StatusCodes.Status503ServiceUnavailable, "Database sedang tidak tersedia.", LogLevel.Critical), // invalid username/password
        [28000] = new(StatusCodes.Status503ServiceUnavailable, "Database sedang tidak tersedia.", LogLevel.Critical), // account locked
        [28001] = new(StatusCodes.Status503ServiceUnavailable, "Database sedang tidak tersedia.", LogLevel.Critical), // password expired
        [942] = new(StatusCodes.Status500InternalServerError, "Terjadi kesalahan pada database.", LogLevel.Critical), // table or view does not exist
        [904] = new(StatusCodes.Status500InternalServerError, "Terjadi kesalahan pada database.", LogLevel.Critical), // invalid identifier (kolom salah)
        [1031] = new(StatusCodes.Status500InternalServerError, "Terjadi kesalahan pada database.", LogLevel.Critical), // insufficient privileges
        [6550] = new(StatusCodes.Status500InternalServerError, "Terjadi kesalahan pada database.", LogLevel.Critical), // PL/SQL compilation error
    };

    // ODP.NET managed driver: ORA-50000..50999 adalah error sisi client (network/transport/timeout).
    private const int ManagedClientErrorMin = 50000;
    private const int ManagedClientErrorMax = 50999;

    private static readonly OracleErrorInfo ClientConnectionError = new(
        StatusCodes.Status503ServiceUnavailable,
        "Database tidak bisa dihubungi.",
        LogLevel.Error,
        Transient: true);

    // Satu OracleException bisa membawa beberapa error (OracleException.Errors), mis. saat gagal konek:
    // ORA-50201 (pembungkus) + ORA-12514 (penyebab). Kode spesifik yang dikenal dipakai lebih dulu,
    // lalu fallback rentang ORA-50xxx, terakhir error umum.
    public static (int Number, OracleErrorInfo Info) Resolve(IReadOnlyList<int> oracleErrorNumbers)
    {
        foreach (var number in oracleErrorNumbers)
        {
            if (Map.TryGetValue(number, out var info))
                return (number, info);
        }

        foreach (var number in oracleErrorNumbers)
        {
            if (number is >= ManagedClientErrorMin and <= ManagedClientErrorMax)
                return (number, ClientConnectionError);
        }

        return (oracleErrorNumbers.Count > 0 ? oracleErrorNumbers[0] : 0, Unknown);
    }
}
