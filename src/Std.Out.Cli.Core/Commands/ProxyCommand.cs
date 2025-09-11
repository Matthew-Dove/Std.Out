using ContainerExpressions.Containers;
using FrameworkContainers.Network.HttpCollective;
using FrameworkContainers.Network.HttpCollective.Models;
using Std.Out.Cli.Core.Models;
using Std.Out.Cli.Core.Services;
using Std.Out.Core.Models;
using Std.Out.Core.Models.Config;

namespace Std.Out.Cli.Core.Commands
{
    internal readonly record struct DisplayModel(string Heading, string Body);

    internal interface IProxyCommand
    {
        Task<Response<Either<BadRequest, Unit>>> Execute(CommandModel command, string args);
    }

    internal sealed class ProxyCommand(
        ProxyConfig _config, IDisplayService _display, IHttpResponse<DisplayModel[]> _service
        ) : IProxyCommand
    {
        public async Task<Response<Either<BadRequest, Unit>>> Execute(CommandModel command, string args)
        {
            var response = new Response<Either<BadRequest, Unit>>();

            var src = GetSourceModel(command.ProxySettingsKey, _config);
            if (!src) return response.With(new BadRequest());
            var source = src.Value;

            var url = source.Url.Replace("<ARGS>", Uri.EscapeDataString(args));
            var proxy = await _service.GetJsonAsync(url, source.Headers.Select(x => new Header(x.Key, x.Value)).ToArray());

            if (proxy)
            {
                foreach (var display in proxy.Value) { _display.Show(source.Display, display.Heading, display.Body); }
                proxy.Value.Length.LogValue(x => "{Count} display result{Plural} gathered.".WithArgs(x, x == 1 ? "" : "s"));
                response = response.With(Unit.Instance);
            }

            return response;
        }

        internal static Response<ProxySourceModel> GetSourceModel(string key, ProxyConfig config)
        {
            var response = new Response<ProxySourceModel>();
            var model = new ProxySourceModel();
            var @default = config.Defaults;
            var source = string.IsNullOrEmpty(key) ? default : config.Sources.GetValueOrDefault(key, default);
            if (source == default) source = config.Sources.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
            if (source == default) return response.LogErrorValue("Proxy source [{Key}] not found.".WithArgs(key ?? string.Empty));

            // Merge source with default model.
            model.Display = source.Display == DisplayType.NotSet ? @default.Display : source.Display;
            model.Url = source.Url == null ? @default?.Url : source.Url;
            model.Headers = (source.Headers == null || source.Headers.Length == 0) ? @default?.Headers : source.Headers;

            // Validate source model.
            var isValid = true;
            isValid = isValid && !string.IsNullOrWhiteSpace(model.Url) && Uri.TryCreate(model.Url, UriKind.Absolute, out _);
            isValid = isValid && (
                (model.Headers == null || model.Headers.Length == 0) ||
                (model.Headers.Count(x => !string.IsNullOrEmpty(x.Key) && !string.IsNullOrEmpty(x.Value)) == model.Headers.Length)
            );

            if (isValid)
            {
                // Align optional fields.
                if (model.Display == DisplayType.NotSet) model.Display = DisplayType.Console;
                if (model.Headers == null || model.Headers.Length == 0) model.Headers = Array.Empty<ProxyHeader>();

                response = response.With(model);
            }
            else
            {
                isValid.LogErrorValue("The merged Proxy source values are not valid for key: [{Key}].".WithArgs(key));
            }

            return response;
        }
    }
}
