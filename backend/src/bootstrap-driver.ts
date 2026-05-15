// Must run BEFORE the `mssql` package is first required.
// `mssql/msnodesqlv8` is just `MSSQL_DRIVER=msnodesqlv8 require('./')`,
// so setting this env var lets us keep `require('mssql')` (used by TypeORM)
// while still routing through the ODBC driver, which is what supports
// LocalDB named pipes + Windows Integrated Auth.
process.env.MSSQL_DRIVER = 'msnodesqlv8';
