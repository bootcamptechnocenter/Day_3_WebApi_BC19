# Oracle scripts

Run in order, connected as a user that can create objects in the `EASYCREDIT` schema
(the `EASYCREDIT` user itself, or `SYSTEM`). Both scripts are idempotent: running them again skips
objects that already exist.

| Script | Creates |
|---|---|
| `01_credit_applications.sql` | `EASYCREDIT.CREDIT_APPLICATIONS` (+ FK to `CUSTOMERS`, indexes on `CUSTOMER_ID`, `STATUS`) |
| `02_credit_installments.sql` | `EASYCREDIT.CREDIT_INSTALLMENTS` (+ FK to `CREDIT_APPLICATIONS`, unique `APPLICATION_ID, INSTALLMENT_NO`) |

`EASYCREDIT.CUSTOMERS` must already exist.

SQL*Plus or SQLcl:

```bash
sql EASYCREDIT/<password>@localhost:1521/FREEPDB1 @db/oracle/01_credit_applications.sql
sql EASYCREDIT/<password>@localhost:1521/FREEPDB1 @db/oracle/02_credit_installments.sql
```

Oracle in Docker (no client installed locally):

```bash
docker exec -i <container> sqlplus -s EASYCREDIT/<password>@//localhost:1521/FREEPDB1 < db/oracle/01_credit_applications.sql
docker exec -i <container> sqlplus -s EASYCREDIT/<password>@//localhost:1521/FREEPDB1 < db/oracle/02_credit_installments.sql
```

SQL Developer / DBeaver: open the file and run it as a script (F5 in SQL Developer), not as a single statement.
