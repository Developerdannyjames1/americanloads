# Idempotent SQL migrations (sqlcmd). Skips scripts already recorded in AmericanloadsSchemaMigrations.
# Usage (from backend/):  .\scripts\migrate.ps1
# Env: DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASS — or pass -Server -Database -User -Password

param(
  [string]$Server = $env:DB_HOST,
  [string]$Database = $env:DB_NAME,
  [string]$User = $env:DB_USER,
  [string]$Password = $env:DB_PASS,
  [int]$Port = $(if ($env:DB_PORT) { [int]$env:DB_PORT } else { 1433 })
)

$ErrorActionPreference = 'Stop'
$backendRoot = Split-Path $PSScriptRoot -Parent
$envFile = Join-Path $backendRoot '.env'
if (Test-Path $envFile) {
  Get-Content $envFile | ForEach-Object {
    if ($_ -match '^\s*([^#=]+)=(.*)$') {
      $k = $matches[1].Trim()
      $v = $matches[2].Trim()
      if (-not (Test-Path "env:$k")) { Set-Item -Path "env:$k" -Value $v }
    }
  }
  if (-not $Server) { $Server = $env:DB_HOST }
  if (-not $Database) { $Database = $env:DB_NAME }
  if (-not $User) { $User = $env:DB_USER }
  if (-not $Password) { $Password = $env:DB_PASS }
}

if (-not $Server -or -not $Database -or -not $User) {
  Write-Error 'Set DB_HOST, DB_NAME, DB_USER (and DB_PASS) in .env or pass parameters.'
}

$migrationsDir = Join-Path $backendRoot 'sql\migrations'
$manifestPath = Join-Path $migrationsDir 'migrations.json'
$files = Get-Content $manifestPath -Raw | ConvertFrom-Json
$table = 'AmericanloadsSchemaMigrations'

function Invoke-Sql([string]$Query) {
  sqlcmd -S $Server -d $Database -U $User -P $Password -b -I -Q $Query
  if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed: $Query" }
}

Invoke-Sql @"
IF OBJECT_ID(N'dbo.$table', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.$table (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    AppliedUtc DATETIME2 NOT NULL CONSTRAINT DF_${table}_AppliedUtc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT UQ_${table}_Name UNIQUE (Name)
  );
END
"@

$applied = 0
$skipped = 0

foreach ($f in $files) {
  $p = Join-Path $migrationsDir $f
  if (-not (Test-Path $p)) { throw "Missing migration: $p" }

  $check = sqlcmd -S $Server -d $Database -U $User -P $Password -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(1) FROM dbo.$table WHERE Name = N'$f';"
  if ($LASTEXITCODE -ne 0) { throw "Failed checking migration status for $f" }
  if ([int]($check.Trim()) -gt 0) {
    Write-Host "SKIP  $f (already applied)"
    $skipped++
    continue
  }

  Write-Host "RUN   $f ..."
  sqlcmd -S $Server -d $Database -U $User -P $Password -b -I -i $p
  if ($LASTEXITCODE -ne 0) { throw "FAILED: $f" }

  Invoke-Sql "INSERT INTO dbo.$table (Name) VALUES (N'$f');"
  Write-Host "OK    $f"
  $applied++
}

Write-Host "`nDone. Applied: $applied, skipped: $skipped, total: $($files.Count)"
