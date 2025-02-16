using ContainerExpressions.Containers;
using Microsoft.Extensions.Options;
using Std.Out.Cli.Models;
using Std.Out.Cli.Services;
using Std.Out.Core.Models.Config;
using Std.Out.Core.Services;

namespace Std.Out.Cli.Commands
{
    public interface IS3Command
    {
        Task<Response<Either<BadRequest, Unit>>> Execute(CommandModel command);
    }

    public sealed class S3Command(
        IOptions<S3Config> _config, IS3Service _service, IBrowserService _browser
        ) : IS3Command
    {
        public async Task<Response<Either<BadRequest, Unit>>> Execute(CommandModel command)
        {
            var response = new Response<Either<BadRequest, Unit>>();

            var source = GetSourceModel(command.SettingsKey, _config.Value, command.CorrelationId);
            if (!source) response = response.With(new BadRequest());

            var filenames = await source.BindAsync(x => _service.List(x.Bucket, x.Prefix));

            if (filenames)
            {
                var tasks = new Task<Response<string>>[filenames.Value.Length];
                for (int i = 0; i < filenames.Value.Length; i++)
                {
                    tasks[i] = _service.Download(source.Value.Bucket, filenames.Value[i], source.Value.ContentType);
                }
                var files = await Task.WhenAll(tasks);

                var isSuccessful = true;
                if (filenames.Value.Length == files.Count(x => x))
                {
                    foreach (var (file, name) in files.Select(x => x.Value).Zip(filenames.Value))
                    {
                        var header = name.Replace(source.Value.Prefix, "");
                        var extension = header.LastIndexOf('.');
                        if (extension > 0) header = header[..extension];

                        var isOpened = _browser.Open(source.Value.BrowserDisplay, header, file);
                        if (!isOpened) isSuccessful = false;
                    }
                }
                filenames.LogValue(x => "{Count} files found.".WithArgs(x.Value.Length));
                if (isSuccessful) response = response.With(Unit.Instance);
            }

            return response;
        }

        private static Response<S3SourceModel> GetSourceModel(string key, S3Config config, string correlationId)
        {
            var response = new Response<S3SourceModel>();
            var model = new S3SourceModel();
            var @default = config.Defaults;
            var source = config.Sources.GetValueOrDefault(key, default);
            if (source == default) source = config.Sources.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
            if (source == default) return response.LogErrorValue("S3 source [{Key}] not found.".WithArgs(key));

            // Merge source with default model.
            model.Bucket = string.IsNullOrWhiteSpace(source.Bucket) ? @default?.Bucket : source.Bucket;
            model.Prefix = string.IsNullOrWhiteSpace(source.Prefix) ? @default?.Prefix : source.Prefix;
            model.ContentType = string.IsNullOrWhiteSpace(source.ContentType) ? @default?.ContentType : source.ContentType;
            model.BrowserDisplay = source.BrowserDisplay == OpenBrowser.NotSet ? @default.BrowserDisplay : source.BrowserDisplay;

            // Validate source model.
            var isValid = true;
            isValid = isValid && !string.IsNullOrWhiteSpace(model.Bucket);
            isValid = isValid && string.IsNullOrWhiteSpace(model.ContentType) || "json".Equals(model.ContentType, StringComparison.OrdinalIgnoreCase);
            isValid = isValid && model.BrowserDisplay != OpenBrowser.NotSet;
            isValid = isValid && !string.IsNullOrWhiteSpace(model.Prefix) && model.Prefix.Contains("<CID>");

            if (isValid)
            {
                // Align optional fields.
                if (string.IsNullOrWhiteSpace(model.ContentType)) model.ContentType = string.Empty;
                model.ContentType = model.ContentType.ToLowerInvariant();
                model.Prefix = model.Prefix.Replace("<CID>", correlationId);

                response = response.With(model);
            }
            else
            {
                isValid.LogErrorValue("The merged S3 source values are not valid for key: [{Key}].".WithArgs(key));
            }

            return response;
        }
    }
}
