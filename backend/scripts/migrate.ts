/**
 * Idempotent SQL migrations for legacy Ast database.
 * Tracks applied scripts in dbo.AmericanloadsSchemaMigrations — skips already-run files.
 *
 * Usage (from backend/):
 *   npm run migrate
 */
import '../src/bootstrap-driver';
import * as fs from 'fs';
import * as path from 'path';
import sql from 'mssql';

const MIGRATIONS_TABLE = 'AmericanloadsSchemaMigrations';

function loadEnvFile(envPath: string) {
  if (!fs.existsSync(envPath)) return;
  const text = fs.readFileSync(envPath, 'utf8');
  for (const line of text.split(/\r?\n/)) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith('#')) continue;
    const eq = trimmed.indexOf('=');
    if (eq < 0) continue;
    const key = trimmed.slice(0, eq).trim();
    let val = trimmed.slice(eq + 1).trim();
    if (
      (val.startsWith('"') && val.endsWith('"')) ||
      (val.startsWith("'") && val.endsWith("'"))
    ) {
      val = val.slice(1, -1);
    }
    if (process.env[key] === undefined) process.env[key] = val;
  }
}

function splitSqlBatches(content: string): string[] {
  return content
    .split(/\r?\n\s*GO\s*\r?\n/gi)
    .map((b) => b.trim())
    .filter((b) => b.length > 0);
}

function buildConfig(): sql.config {
  const host = process.env.DB_HOST || 'localhost';
  const port = process.env.DB_INSTANCE
    ? undefined
    : parseInt(process.env.DB_PORT || '1433', 10);
  const user = process.env.DB_USER || 'sa';
  const password = process.env.DB_PASS || '';
  const database = process.env.DB_NAME || 'Ast';
  const encrypt = process.env.DB_ENCRYPT === 'true';
  const trustServerCertificate = process.env.DB_TRUST_SERVER_CERT !== 'false';

  const cfg: sql.config = {
    server: host,
    port,
    user,
    password,
    database,
    options: {
      encrypt,
      trustServerCertificate,
      instanceName: process.env.DB_INSTANCE || undefined,
    },
  };
  return cfg;
}

async function ensureMigrationsTable(pool: sql.ConnectionPool) {
  await pool.request().query(`
    IF OBJECT_ID(N'dbo.${MIGRATIONS_TABLE}', N'U') IS NULL
    BEGIN
      CREATE TABLE dbo.${MIGRATIONS_TABLE} (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(255) NOT NULL,
        AppliedUtc DATETIME2 NOT NULL CONSTRAINT DF_${MIGRATIONS_TABLE}_AppliedUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_${MIGRATIONS_TABLE}_Name UNIQUE (Name)
      );
    END
  `);
}

async function isApplied(pool: sql.ConnectionPool, name: string): Promise<boolean> {
  const result = await pool
    .request()
    .input('name', sql.NVarChar(255), name)
    .query(`SELECT 1 AS ok FROM dbo.${MIGRATIONS_TABLE} WHERE Name = @name`);
  return (result.recordset?.length ?? 0) > 0;
}

async function markApplied(pool: sql.ConnectionPool, name: string) {
  await pool
    .request()
    .input('name', sql.NVarChar(255), name)
    .query(`INSERT INTO dbo.${MIGRATIONS_TABLE} (Name) VALUES (@name)`);
}

async function runMigrationFile(pool: sql.ConnectionPool, filePath: string, name: string) {
  const sqlText = fs.readFileSync(filePath, 'utf8');
  const batches = splitSqlBatches(sqlText);
  for (const batch of batches) {
    await pool.request().query(batch);
  }
  await markApplied(pool, name);
}

async function main() {
  const backendRoot = path.resolve(__dirname, '..');
  loadEnvFile(path.join(backendRoot, '.env'));

  const migrationsDir = path.join(backendRoot, 'sql', 'migrations');
  const manifestPath = path.join(migrationsDir, 'migrations.json');
  if (!fs.existsSync(manifestPath)) {
    console.error('Missing migrations manifest:', manifestPath);
    process.exit(1);
  }

  const files = JSON.parse(fs.readFileSync(manifestPath, 'utf8')) as string[];
  const cfg = buildConfig();
  console.log(`Connecting to ${cfg.server}${cfg.options?.instanceName ? `\\${cfg.options.instanceName}` : ''}/${cfg.database} ...`);

  const pool = await sql.connect(cfg);
  try {
    await ensureMigrationsTable(pool);

    let applied = 0;
    let skipped = 0;

    for (const file of files) {
      const fullPath = path.join(migrationsDir, file);
      if (!fs.existsSync(fullPath)) {
        throw new Error(`Migration file not found: ${fullPath}`);
      }

      if (await isApplied(pool, file)) {
        console.log(`SKIP  ${file} (already applied)`);
        skipped++;
        continue;
      }

      console.log(`RUN   ${file} ...`);
      await runMigrationFile(pool, fullPath, file);
      console.log(`OK    ${file}`);
      applied++;
    }

    console.log(`\nDone. Applied: ${applied}, skipped: ${skipped}, total in manifest: ${files.length}`);
  } finally {
    await pool.close();
  }
}

main().catch((err) => {
  console.error('Migration failed:', err?.message || err);
  process.exit(1);
});
