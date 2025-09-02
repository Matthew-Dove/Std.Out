using ContainerExpressions.Containers;
using Std.Out.Cli.Services;
using Std.Out.Core.Models;

namespace Std.Out.Services
{
    internal interface ICollectorService : IDisplayService
    {
        Display[] Collect();
    }

    internal sealed class Collector : ICollectorService
    {
        private readonly Queue<Display> _viewData = new();
        private readonly Guid _id = Guid.NewGuid(); // TODO: Remove this, it's just for debugging.

        public Response<Unit> Show(DisplayType _, string heading, string body)
        {
            _viewData.Enqueue(new Display(heading, body));
            return Unit.ResponseSuccess;
        }

        public Display[] Collect()
        {
            var viewData = _viewData.ToArray();
            _viewData.Clear();
            return viewData;
        }
    }
}
