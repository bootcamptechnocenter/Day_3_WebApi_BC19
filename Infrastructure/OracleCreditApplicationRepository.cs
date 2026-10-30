using System.Data;
using Dapper;
using EasyCreditApi.Domain.Entities;
using EasyCreditApi.Infrastructure.Oracle;

namespace EasyCreditApi.Infrastructure;

public sealed class OracleCreditApplicationRepository : ICreditApplicationRepository
{
    private const string Table = "EASYCREDIT.CREDIT_APPLICATIONS";
    private const string InstallmentTable = "EASYCREDIT.CREDIT_INSTALLMENTS";

    private const string SelectColumns = """
        ID            AS ApplicationId,
        CUSTOMER_ID   AS CustomerId,
        AMOUNT        AS Amount,
        TENOR_MONTHS  AS TenorMonths,
        INTEREST_RATE AS InterestRate,
        PURPOSE       AS Purpose,
        STATUS        AS Status,
        CREDIT_SCORE  AS CreditScore,
        APPROVED_BY   AS ApprovedBy,
        APPROVED_AT   AS ApprovedAt,
        CREATED_AT    AS CreatedAt,
        UPDATED_AT    AS UpdatedAt
        """;

    private const string InstallmentColumns = """
        ID             AS InstallmentId,
        APPLICATION_ID AS ApplicationId,
        INSTALLMENT_NO AS InstallmentNo,
        DUE_DATE       AS DueDate,
        AMOUNT         AS Amount,
        STATUS         AS Status
        """;

    private readonly OracleConnectionFactory _factory;

    public OracleCreditApplicationRepository(OracleConnectionFactory factory) => _factory = factory;

    public async Task<(IEnumerable<CreditApplication> Items, int Total)> GetPagedAsync(
        int? customerId, string? status, int page, int limit)
    {
        page = Math.Max(page, 1);
        limit = Math.Clamp(limit, 1, 100);

        await using var conn = _factory.Create();

        // Hanya fragmen SQL tetap yang digabung; nilai filter selalu lewat parameter.
        var conditions = new List<string>();
        var p = new DynamicParameters();

        if (customerId is not null)
        {
            conditions.Add("CUSTOMER_ID = :CustomerId");
            p.Add("CustomerId", customerId, DbType.Int32);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add("STATUS = :Status");
            p.Add("Status", status, DbType.String);
        }

        var where = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : "";

        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM {Table}{where}", p);

        p.Add("Offset", (page - 1) * limit, DbType.Int32);
        p.Add("Limit", limit, DbType.Int32);

        var items = await conn.QueryAsync<CreditApplication>(
            $"""
             SELECT {SelectColumns}
             FROM {Table}{where}
             ORDER BY ID
             OFFSET :Offset ROWS FETCH NEXT :Limit ROWS ONLY
             """, p);

        return (items, total);
    }

    public async Task<CreditApplication?> GetByIdAsync(int applicationId)
    {
        await using var conn = _factory.Create();

        return await conn.QueryFirstOrDefaultAsync<CreditApplication>(
            $"SELECT {SelectColumns} FROM {Table} WHERE ID = :ApplicationId",
            new { ApplicationId = applicationId });
    }

    public async Task<int> CreateAsync(CreditApplication application)
    {
        await using var conn = _factory.Create();

        var p = new DynamicParameters();
        p.Add("CustomerId", application.CustomerId, DbType.Int32);
        p.Add("Amount", application.Amount, DbType.Decimal);
        p.Add("TenorMonths", application.TenorMonths, DbType.Int32);
        p.Add("InterestRate", application.InterestRate, DbType.Decimal);
        p.Add("Purpose", application.Purpose, DbType.String);
        p.Add("NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        p.Add("CreatedAt", dbType: DbType.DateTime, direction: ParameterDirection.Output);

        await conn.ExecuteAsync(
            $"""
             INSERT INTO {Table} (CUSTOMER_ID, AMOUNT, TENOR_MONTHS, INTEREST_RATE, PURPOSE, STATUS, CREATED_AT, UPDATED_AT)
             VALUES (:CustomerId, :Amount, :TenorMonths, :InterestRate, :Purpose, 'DRAFT', SYSDATE, SYSDATE)
             RETURNING ID, CREATED_AT INTO :NewId, :CreatedAt
             """, p);

        application.ApplicationId = p.Get<int>("NewId");
        application.Status = CreditApplicationStatus.Draft;
        application.CreatedAt = p.Get<DateTime>("CreatedAt");
        application.UpdatedAt = application.CreatedAt;
        return application.ApplicationId;
    }

    public async Task<bool> UpdateDraftAsync(CreditApplication application)
    {
        await using var conn = _factory.Create();

        var affected = await conn.ExecuteAsync(
            $"""
             UPDATE {Table}
                SET AMOUNT        = :Amount,
                    TENOR_MONTHS  = :TenorMonths,
                    INTEREST_RATE = :InterestRate,
                    PURPOSE       = :Purpose,
                    UPDATED_AT    = SYSDATE
              WHERE ID = :ApplicationId
                AND STATUS = 'DRAFT'
             """,
            new
            {
                application.Amount,
                application.TenorMonths,
                application.InterestRate,
                application.Purpose,
                application.ApplicationId
            });

        return affected > 0;
    }

    public async Task<bool> DeleteDraftAsync(int applicationId)
    {
        await using var conn = _factory.Create();

        var affected = await conn.ExecuteAsync(
            $"DELETE FROM {Table} WHERE ID = :ApplicationId AND STATUS = 'DRAFT'",
            new { ApplicationId = applicationId });

        return affected > 0;
    }

    public async Task<bool> SubmitAsync(int applicationId, int? creditScore)
    {
        await using var conn = _factory.Create();

        var p = new DynamicParameters();
        p.Add("ApplicationId", applicationId, DbType.Int32);
        p.Add("CreditScore", creditScore, DbType.Int32);   // null -> CREDIT_SCORE tetap NULL

        var affected = await conn.ExecuteAsync(
            $"""
             UPDATE {Table}
                SET STATUS       = 'SUBMITTED',
                    CREDIT_SCORE = :CreditScore,
                    UPDATED_AT   = SYSDATE
              WHERE ID = :ApplicationId
                AND STATUS = 'DRAFT'
             """, p);

        return affected > 0;
    }

    // Day 6: ganti isi method ini dengan pemanggilan sp_approve_application_v2.
    public async Task<bool> ApproveAsync(
        int applicationId, string approvedBy, IReadOnlyList<CreditInstallment> installments)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();

        using var tx = conn.BeginTransaction();

        try
        {
            // Satu UPDATE ber-guard: hanya SUBMITTED yang bisa di-approve.
            var affected = await conn.ExecuteAsync(
                $"""
                 UPDATE {Table}
                    SET STATUS      = 'APPROVED',
                        APPROVED_BY = :ApprovedBy,
                        APPROVED_AT = SYSDATE,
                        UPDATED_AT  = SYSDATE
                  WHERE ID = :ApplicationId
                    AND STATUS = 'SUBMITTED'
                 """,
                new { ApprovedBy = approvedBy, ApplicationId = applicationId }, tx);

            if (affected == 0)
            {
                tx.Rollback();
                return false;
            }

            if (installments.Count > 0)
            {
                // Dapper mengeksekusi statement ini sekali per item.
                await conn.ExecuteAsync(
                    $"""
                     INSERT INTO {InstallmentTable} (APPLICATION_ID, INSTALLMENT_NO, DUE_DATE, AMOUNT, STATUS)
                     VALUES (:ApplicationId, :InstallmentNo, :DueDate, :Amount, 'PENDING')
                     """,
                    installments.Select(i => new
                    {
                        ApplicationId = applicationId,
                        i.InstallmentNo,
                        i.DueDate,
                        i.Amount
                    }), tx);
            }

            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> RejectAsync(int applicationId, string rejectedBy)
    {
        await using var conn = _factory.Create();

        // Tabel tidak punya kolom REJECTED_BY; APPROVED_BY/APPROVED_AT mencatat siapa yang memutuskan.
        var affected = await conn.ExecuteAsync(
            $"""
             UPDATE {Table}
                SET STATUS      = 'REJECTED',
                    APPROVED_BY = :RejectedBy,
                    APPROVED_AT = SYSDATE,
                    UPDATED_AT  = SYSDATE
              WHERE ID = :ApplicationId
                AND STATUS = 'SUBMITTED'
             """,
            new { RejectedBy = rejectedBy, ApplicationId = applicationId });

        return affected > 0;
    }

    public async Task<IEnumerable<CreditInstallment>> GetInstallmentsAsync(int applicationId)
    {
        await using var conn = _factory.Create();

        return await conn.QueryAsync<CreditInstallment>(
            $"""
             SELECT {InstallmentColumns}
             FROM {InstallmentTable}
             WHERE APPLICATION_ID = :ApplicationId
             ORDER BY INSTALLMENT_NO
             """,
            new { ApplicationId = applicationId });
    }
}
