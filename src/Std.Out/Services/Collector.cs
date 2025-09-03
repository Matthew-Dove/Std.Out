using ContainerExpressions.Containers;
using Std.Out.Cli.Services;
using Std.Out.Core.Models;

namespace Std.Out.Services
{
    internal interface ICollectorService
    {
        Display[] Collect();
    }

    internal sealed class CollectorService(IDisplayService _display) : ICollectorService
    {
        public Display[] Collect()
        {
            if (_display is Collector collector)
            {
                return collector.Collect();
            }
            return Array.Empty<Display>();
        }
    }

    internal sealed class Collector : IDisplayService
    {
        private readonly Queue<Display> _viewData = new();

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
