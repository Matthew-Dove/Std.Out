using ContainerExpressions.Containers;
using Std.Out.Core.Models.Config;
using System.Diagnostics;
using System.Net;

namespace Std.Out.Cli.Services
{
    public interface IDisplayService
    {
        Response<Unit> Show(DisplayType browser, string heading, string body);
    }

    public sealed class DisplayService : IDisplayService
    {
        public Response<Unit> Show(DisplayType display, string heading, string body)
        {
            var response = Unit.ResponseError;

            try
            {
                if (display == DisplayType.Console) response = ConsoleDisplay(display, heading, body);
                else response = BrowserDisplay(display, heading, body);
            }
            catch (Exception ex)
            {
                ex.LogError("Error displaying data in web browser.");
            }

            return response;
        }

        private static Response<Unit> ConsoleDisplay(DisplayType display, string heading, string body)
        {
            Console.ForegroundColor = ConsoleColor.Cyan; Console.WriteLine(heading); Console.ResetColor();
            Console.WriteLine(body);
            return Unit.ResponseSuccess;
        }

        private static Response<Unit> BrowserDisplay(DisplayType display, string heading, string body)
        {
            var html = $"""
                    <html>
                    <head>
                    <title>Standard Out</title>
                    </head>
                    <body style="font-family:sans-serif; font-size:20px; padding:20px;">
                    <h1>{WebUtility.HtmlEncode(heading)}</h1>
                    <pre>
                    {WebUtility.HtmlEncode(body)}
                    </pre>
                    </body>
                    </html>
                    """;

            var data = "data:text/html;charset=utf-8," + Uri.EscapeDataString(html).Replace("\"", "").Replace("'", "");
            var processArgs = display switch
            {
                DisplayType.Chrome => $"start chrome '{data}'",
                DisplayType.Firefox => $"start firefox '{data}'",
                _ => Lambda.Identity<string>(new ArgumentOutOfRangeException(nameof(S3SourceModel.Display), $"Value must be one of: '{string.Join(", ", Enum.GetNames<DisplayType>().Where(x => x != DisplayType.NotSet.ToString()))}'."))
            };

            return RunCli(processArgs);
        }

        private static Response<Unit> RunCli(string command)
        {
            var response = Unit.ResponseSuccess;

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                RedirectStandardOutput = false,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var p = new Process { StartInfo = psi })
            {
                p.Start();
                var error = p.StandardError.ReadToEnd();
                p.WaitForExit();
                if (p.ExitCode != 0 || !string.IsNullOrEmpty(error))
                {
                    response = Unit.ResponseError;
                    p.LogErrorValue("Error running cli command, exit code: {ExitCode}, error: {Error}.".WithArgs(p.ExitCode, error));
                }
            }

            return response;
        }
    }
}
