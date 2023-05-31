using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using MimeKit;
using UploadFile.Helpers;
using WebDAVClient;
using static System.IO.File;
using Directory = System.IO.Directory;

namespace UploadFile;

static class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello World!");
        await UploadNextCloud();

        // #region upload file to onedrive
        //
        // // UploadOneDrive();
        //
        // #endregion
    }

    private static bool UploadOneDrive()
    {
        var config = LoadAppSettings();
        if (config == null)
        {
            Console.WriteLine("Invalid appsettings.json file.");
            return true;
        }


        //var username = ReadUserName();
        //var password = ReadPassword();
        var username = "user";
        var password = "passwork";
        var client = GetAuthenticatedGraphClient(config, username, password);

        //display message to user
        var profileResponse = client.Me.Request().GetAsync().Result;
        Console.WriteLine("Hello " + profileResponse.DisplayName);

        var fileName = "CNTT-2018_CTDT.pdf";
        var currentFolder = Directory.GetCurrentDirectory();
        var filePath = Path.Combine(currentFolder, fileName);

        // load resource as a stream
        using Stream fileStream = new FileStream(filePath, FileMode.Open);
        var graphClient = GetAuthenticatedGraphClient(config, username, password);
        var uploadSession = graphClient.Me.Drive.Root
            .ItemWithPath(fileName)
            .CreateUploadSession()
            .Request()
            .PostAsync()
            .Result;
        // create upload task
        var maxChunkSize = 320 * 1024;
        var largeUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, fileStream, maxChunkSize);

        // create upload progress reporter
        IProgress<long> uploadProgress = new Progress<long>(uploadBytes =>
        {
            Console.WriteLine($"Uploaded {uploadBytes} bytes of {fileStream.Length} bytes");
        });

        // upload file
        var uploadResult = largeUploadTask.UploadAsync(uploadProgress).Result;
        if (uploadResult.UploadSucceeded)
        {
            Console.WriteLine("File uploaded to user's OneDrive root folder.");
        }

        return false;
    }

    private static async Task UploadNextCloud()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var filePath = $"{currentDir}/67 QD thuc hien che do bao cao.pdf";
        var fileName = Path.GetFileName(filePath);
        var fileContentType = MimeTypes.GetMimeType(fileName);
        await using var fileStream = OpenRead(filePath);
        var content = new StreamContent(fileStream)
        {
            Headers =
            {
                ContentType = new MediaTypeHeaderValue(fileContentType)
            }
        };
        const string username = "hoaithanh";
        const string password = "0364745036";
        const string nextcloudUrl = @"https://storage.hahaho.xyz";
        var destinationPath = $"remote.php/dav/files/{username}/{fileName}";

        var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
        var client = new HttpClient
        {
            DefaultRequestHeaders =
            {
                Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray))
            }
        };

        var destinationUrl = $"{nextcloudUrl}/{destinationPath}";
        try
        {
            Console.WriteLine($"Begin upload file {fileName} to {destinationUrl}");
            var message = await client.PutAsync(destinationUrl, content);
            Console.WriteLine($"{message}");
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw new Exception(exception.Message);
        }
    }


    #region read name and password

    //private static string ReadUserName()
    //{
    //    string username;
    //    Console.WriteLine("Enter your username");
    //    username = Console.ReadLine();
    //    return username;
    //}

    //    private static SecureString ReadPassword()
    //{
    //    Console.WriteLine("Enter your password");
    //    SecureString password = new SecureString();
    //    while (true)
    //    {
    //        ConsoleKeyInfo conso = Console.ReadKey(true);
    //        if (conso.Key == ConsoleKey.Enter)
    //        {
    //            break;
    //        }
    //        password.AppendChar(conso.KeyChar);
    //        Console.Write("*");
    //    }

    //    Console.WriteLine();
    //    return password;
    //}

    #endregion

    private static IAuthenticationProvider CreateAuthorizationProvider(IConfiguration config, string username,
        string password)
    {
        var clientId = config["applicationId"];
        var authority = $"https://login.microsoftonline.com/{config["tenantId"]}/v2.0";

        var scopes = new List<string>
        {
            "User.Read",
            "Files.Read",
            "Files.ReadWrite",
            "Files.ReadWrite.All",
            "Sites.ReadWrite.All"
        };

        var cca = PublicClientApplicationBuilder.Create(clientId)
            .WithAuthority(authority)
            .WithDefaultRedirectUri()
            .Build();

        return MsalAuthenticationProvider.GetInstance(cca, scopes.ToArray(), username, password);
    }

    private static IConfigurationRoot? LoadAppSettings()
    {
        try
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(@"C:/Project/UploadFile")
                .AddJsonFile("appsettings.json", false, true)
                .Build();

            if (string.IsNullOrEmpty(config["applicationId"]) ||
                string.IsNullOrEmpty(config["tenantId"]))
            {
                return null;
            }

            return config;
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }


    private static GraphServiceClient GetAuthenticatedGraphClient(IConfigurationRoot config, string username,
        string password)
    {
        var authenticationProvider = CreateAuthorizationProvider(config, username, password);
        var graphClient = new GraphServiceClient(authenticationProvider);
        return graphClient;
    }
}