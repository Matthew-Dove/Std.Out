using ContainerExpressions.Containers;
using Std.Out.Core.Models;

namespace Std.Out.Cli.Core.Services
{
    internal interface IDisplayService
    {
        Response<Unit> Show(DisplayType browser, string heading, string body);
    }
}
