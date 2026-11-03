-- ============================================================
-- EasyCredit - PostgreSQL : buat role + database
-- Jalankan sebagai superuser (postgres), SEKALI saja.
--
--   PGPASSWORD='Password1!' psql -h localhost -U postgres \
--     -f EasyCreditApi/Database/postgres_create_db.sql
-- ============================================================

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'easycredit_user') THEN
        CREATE ROLE easycredit_user LOGIN PASSWORD 'easycredit_pass';
    ELSE
        ALTER ROLE easycredit_user WITH LOGIN PASSWORD 'easycredit_pass';
    END IF;
END
$$;

-- CREATE DATABASE tidak boleh di dalam transaksi/DO block.
-- \gexec hanya menjalankan perintah jika database belum ada.
SELECT 'CREATE DATABASE easycredit OWNER easycredit_user'
WHERE NOT EXISTS (
    SELECT 1 FROM pg_database WHERE datname = 'easycredit'
)\gexec

GRANT ALL PRIVILEGES ON DATABASE easycredit TO easycredit_user;