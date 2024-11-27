using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using DecisionsFramework;
using DecisionsFramework.ServiceLayer;
using DecisionsFramework.ServiceLayer.Services.FileReference;
using Microsoft.Win32;

namespace Decisions.ODBC.DB2;

public class OdbcDriverInstallerInitializer : IInitializable
{
    static Log log = new Log(StringConstants.LOG_CATEGORY);
    
    public void Initialize()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            InitializeForWindows();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            InitializeForLinux();
        else
        {
            string foundOs = Environment.OSVersion.ToString();
            log.Warn($"Found OS: {foundOs}. Unable to install DB2 ODBC drivers.");
        }
    }

    private void InitializeForLinux()
    {
        log.Warn("Deleting old deploy " + StringConstants.NIX_DEPLOY_PATH);
        if (Directory.Exists(StringConstants.NIX_DEPLOY_PATH))
            Directory.Delete(StringConstants.NIX_DEPLOY_PATH, true);
        
        // 1. Download
        var url = Path.Combine(StringConstants.BASE_HOST_PATH, StringConstants.NIX_DEPLOY_FILENAME);
        var filePath = DownloadFile(url, StringConstants.NIX_DEPLOY_PATH, StringConstants.NIX_DEPLOY_FILENAME);
        if (string.IsNullOrEmpty(filePath))
            return;
        
        log.Warn("Downloaded file " + filePath);
        
        // 2. Extract
        var outputPath = UnTarGzFile(filePath, StringConstants.NIX_DEPLOY_PATH);
        log.Warn("Output path is ", outputPath);
        
        // 3. Export environment variables
        SetupEnvironmentVariables(StringConstants.NIX_DEPLOY_PATH);
    }
    
    private void InitializeForWindows()
    {
        // 1. Download
        var url = Path.Combine(StringConstants.BASE_HOST_PATH, StringConstants.WIN_DEPLOY_FILENAME);
        var filePath = DownloadFile(url, StringConstants.WIN_DEPLOY_PATH, StringConstants.WIN_DEPLOY_FILENAME);
        if (string.IsNullOrEmpty(filePath))
            return;
        
        // 2. Extract
        var output = UnZipFile(filePath, StringConstants.WIN_DEPLOY_PATH);
        if (string.IsNullOrEmpty(output))
            return;
        
        //db2cli install -setup
        var installerPath = Path.Combine(output, "clidriver", "bin", "db2cli.exe");
        try
        {
            Process.Start(installerPath, "install -setup");
        }
        catch (Exception ex)
        {
            log.Warn($"Unable to start the DB2 CLI executable. Error: {ex.Message}");
        }
    }

    private string DownloadFile(string url, string outputPath, string filename)
    {
        log.Warn("Creating output directory " + outputPath);
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);
        
        // If the file was already downloaded, use that.
        var destinationPath = Path.Combine(outputPath, filename);
        if (File.Exists(destinationPath))
            return destinationPath;
        
        log.Warn($"Downloading file {filename} to {destinationPath}");
        using HttpClient client = new HttpClient();
        try
        {
            HttpResponseMessage response = client.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();

            byte[] fileBytes = response.Content.ReadAsByteArrayAsync().Result;

            
            File.WriteAllBytes(destinationPath, fileBytes);
            return destinationPath;
        }
        catch (Exception ex)
        {
            log.Warn("Could not download DB2 ODBC Driver package. ", ex.Message);
        }

        return string.Empty;
    }

    private string UnZipFile(string zipFilePath, string outputPath)
    {
        try
        {
            var driverPath = Path.Combine(outputPath, "clidriver");
            if (Directory.Exists(driverPath))
            {
                log.Warn("ODBC driver has already been extracted. Skipping.");
                return string.Empty;
            }
            else
                ZipFile.ExtractToDirectory(zipFilePath, outputPath);
        }
        catch (Exception ex)
        {
            log.Warn("Could not unzip DB2 ODBC Driver package. ", ex.Message);
            return string.Empty;
        }

        return outputPath;
    }
    
    private string UnTarGzFile(string tarGzFilePath, string outputPath)
    {
        // remove .gz extension
        var tarPath = tarGzFilePath[..tarGzFilePath.LastIndexOf('.')];
        log.Warn("TarPath is " + tarPath);
        //if (File.Exists(tarPath))
        //    File.Delete(tarPath);

        using FileStream fs = new(tarGzFilePath, FileMode.Open, FileAccess.Read);
        using GZipStream gz = new(fs, CompressionMode.Decompress, leaveOpen: true);

        TarFile.ExtractToDirectory(gz, outputPath, overwriteFiles: true);
        
        var fileCount = Directory.GetFiles(outputPath).Length;
        if (fileCount > 0) 
            return outputPath;
        
        log.Warn("Could not extract tar file from driver.");
        return string.Empty;
    }

    private void SetupEnvironmentVariables(string basePath)
    {
        log.Warn("Setting up environment variables");

        // Construct paths
        string db2Path = $"{basePath}/odbc_cli/clidriver";
        string libPath = $"{db2Path}/lib";
        string binPath = $"{db2Path}/bin";
        string admPath = $"{db2Path}/adm";

        log.Warn("db2path =" + db2Path);
        log.Warn("libPath =" + libPath);
        log.Warn("binPath =" + binPath);
        log.Warn("admPath =" + admPath);
        
        // Export environment variables
        Environment.SetEnvironmentVariable("DB2_CLI_DRIVER_INSTALL_PATH", db2Path);
        Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", libPath);
        Environment.SetEnvironmentVariable("LIBPATH", libPath);

        // Update PATH environment variable
        string currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        log.Warn("Setting PATH =" + $"{binPath}:{admPath}:{currentPath}");
        Environment.SetEnvironmentVariable("PATH", $"{binPath}:{admPath}:{currentPath}");

        log.Warn("Environment variables set successfully.");

        DoBashSource();
    }

    private void DoBashSource()
    {
        string bashrcPath = $"~/.bashrc";

        // Command to source the file and print environment variable as an example
        string command = $"bash -c 'source {bashrcPath} && echo $DB2_CLI_DRIVER_INSTALL_PATH'";
        log.Warn("Running " + command);
        
        Process process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        log.Warn($"Command Output: {output}");
    }
}