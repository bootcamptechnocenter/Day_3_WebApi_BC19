-- 3 customer contoh untuk provider Postgres. Jalankan SETELAH `dotnet ef database update`.
-- Idempotent: NIK yang sudah ada dilewati (unique index ux_customers_nik).
-- Sengaja tanpa kolom id supaya sequence identity tetap sinkron.
INSERT INTO easycredit.customers (cust_name, nik, phone, email, address, created_at)
VALUES
    ('Andi Wijaya',    '3171010101900001', '081211110001', 'andi@mail.com',  'Jakarta',  now()),
    ('Siti Rahmawati', '3273020202910002', '081211110002', 'siti@mail.com',  'Bandung',  now()),
    ('Bambang Susilo', '3578030303920003', '081211110003', 'bambang@mail.com', 'Surabaya', now())
ON CONFLICT (nik) DO NOTHING;
