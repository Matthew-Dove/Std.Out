using ContainerExpressions.Containers;
using Std.Out.Core.Models.Config;
using System.Diagnostics;
using System.Net;

namespace Std.Out.Cli.Services
{
    public interface IBrowserService
    {
        Response<Unit> Open(OpenBrowser browser, string heading, string body);
    }

    public sealed class BrowserService : IBrowserService
    {
        public Response<Unit> Open(OpenBrowser browser, string heading, string body)
        {
            var response = Unit.ResponseError;

            try
            {
                var encodedHeading = WebUtility.HtmlEncode(heading);
                var encodedBody = WebUtility.HtmlEncode(body);
                var html = $"""
                    <html>
                    <head>
                    <title>Standard Out</title>
                    </head>
                    <body style='font-family:sans-serif; font-size:20px; padding:20px;'>
                    <h1>{encodedHeading}</h1>
                    <pre>{encodedBody}</pre>
                    </body>
                    </html>
                    """;

                var data = "data:text/html;charset=utf-8," + Uri.EscapeDataString(html).Replace("\"", "");
                var processArgs = browser switch
                {
                    OpenBrowser.Chrome => $"start chrome \"{data}\"",
                    OpenBrowser.Firefox => $"start firefox \"{data}\"",
                    _ => Lambda.Identity<string>(new ArgumentOutOfRangeException(nameof(S3SourceModel.BrowserDisplay), $"Value must be one of: '{string.Join(", ", Enum.GetNames<OpenBrowser>().Where(x => x != OpenBrowser.NotSet.ToString()))}'."))
                };

                Process.Start(new ProcessStartInfo("cmd.exe", "/C " + processArgs) { UseShellExecute = true, CreateNoWindow = true });
                response = Unit.ResponseSuccess;
            }
            catch (Exception ex)
            {
                ex.LogError("Error displaying data in web browser.");
            }

            return response;
        }
    }
}
