-- ============================================================
-- EasyCredit - PostgreSQL schema + seed
-- Database : easycredit
-- Tabel    : mst_user, mst_customer
--
-- Jalankan setelah database easycredit ada:
--   PGPASSWORD='easycredit_pass' psql -h localhost -U easycredit_user \
--     -d easycredit -f EasyCreditApi/Database/postgres_schema.sql
-- ============================================================

DROP TABLE IF EXISTS mst_customer;
DROP TABLE IF EXISTS mst_user;

-- ------------------------------------------------------------
-- mst_user
-- ------------------------------------------------------------
CREATE TABLE mst_user (
    user_id       SERIAL       PRIMARY KEY,
    name          VARCHAR(100) NOT NULL,
    email         VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role          VARCHAR(20)  NOT NULL,
    created_at    TIMESTAMP    NOT NULL DEFAULT NOW()
);

-- ------------------------------------------------------------
-- mst_customer
-- ------------------------------------------------------------
CREATE TABLE mst_customer (
    customer_id SERIAL       PRIMARY KEY,
    name        VARCHAR(100) NOT NULL,
    nik         VARCHAR(20)  NOT NULL UNIQUE,
    phone       VARCHAR(20),
    email       VARCHAR(100),
    address     VARCHAR(255),
    created_at  TIMESTAMP    NOT NULL DEFAULT NOW()
);

-- ------------------------------------------------------------
-- Seed user
-- Password: admin123 (ADMIN), staff123 (STAFF) - BCrypt
-- ------------------------------------------------------------
INSERT INTO mst_user (name, email, password_hash, role) VALUES
    ('Administrator', 'admin@easycredit.test',
     '$2a$11$ACDxIbAkUhjG6e8vBa5PjOdXir3PmYFnj4REMxw63r7jdCiok7l2m', 'ADMIN'),
    ('Staff CS', 'staff@easycredit.test',
     '$2a$11$r7blHbOXR8wjFYaMor0vp.P.nRzMQg870/q1lGH05razxT.1Mahmm', 'STAFF');

-- ------------------------------------------------------------
-- Seed customer
-- ------------------------------------------------------------
INSERT INTO mst_customer (name, nik, phone, email, address) VALUES
    ('Budi Santoso', '3201010101010001', '081234567001', 'budi@mail.test', 'Jl. Merdeka No. 1, Jakarta'),
    ('Siti Aminah',  '3201010101010002', '081234567002', 'siti@mail.test', 'Jl. Sudirman No. 2, Bandung'),
    ('Andi Wijaya',  '3201010101010003', '081234567003', 'andi@mail.test', 'Jl. Diponegoro No. 3, Surabaya');

-- ------------------------------------------------------------
-- Hak akses (aman dijalankan sebagai owner)
-- ------------------------------------------------------------
GRANT ALL PRIVILEGES ON ALL TABLES    IN SCHEMA public TO easycredit_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO easycredit_user;

-- ------------------------------------------------------------
-- Verifikasi
-- ------------------------------------------------------------
SELECT user_id, name, email, role, created_at FROM mst_user ORDER BY user_id;
SELECT customer_id, name, nik, phone, created_at FROM mst_customer ORDER BY customer_id;