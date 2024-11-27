namespace Decisions.ODBC.DB2;

public static class StringConstants
{
    public const string DRIVER_NAME = "IBM DB2 ODBC DRIVER";
    public const string MODULE_NAME = "Decisions.ODBC.DB2";
    public const string LOG_CATEGORY = MODULE_NAME + " Initialization";
    public const string DEPLOY_FOLDER_NAME = "DB2ODBC";
    public const string BASE_HOST_PATH = "https://github.com/Decisions-Modules/Decisions.ODBC.DB2/raw/refs/heads/main/redist/";
    public const string WIN_DEPLOY_FILENAME = "v11.5.9_ntx64_odbc_cli.zip";
    public const string WIN_DRIVER_FILENAME = "db2clio.dll";
    public const string WIN_SETUP_FILENAME = "db2odbc64.dll";
    public static readonly string WIN_DEPLOY_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DEPLOY_FOLDER_NAME);
    public const string NIX_DEPLOY_FILENAME = "v11.5.9_linuxx64_odbc_cli.tar.gz";
    public static readonly string NIX_DEPLOY_PATH = Path.Combine("/opt/decisions", DEPLOY_FOLDER_NAME);
    
}