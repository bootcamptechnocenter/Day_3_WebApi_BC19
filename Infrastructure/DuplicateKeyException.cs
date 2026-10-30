namespace EasyCreditApi.Infrastructure;

// Dilempar repository saat unique constraint dilanggar (Oracle ORA-00001 / Postgres 23505),
// supaya service tidak bergantung pada exception milik provider database tertentu.
public sealed class DuplicateKeyException : Exception
{
    public DuplicateKeyException(string message, Exception innerException) : base(message, innerException) { }
}
