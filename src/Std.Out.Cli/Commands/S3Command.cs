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
        IOptions<S3Config> _config, IS3Service _service, IDisplayService _display
        ) : IS3Command
    {
        public async Task<Response<Either<BadRequest, Unit>>> Execute(CommandModel command)
        {
            var response = new Response<Either<BadRequest, Unit>>();

            var src = GetSourceModel(command.SettingsKey, _config.Value, command.CorrelationId, command.Path);
            if (!src) return response.With(new BadRequest());
            var source = src.Value;

            var filenames = await _service.List(source);
            if (filenames)
            {
                var tasks = new Task<Response<string>>[filenames.Value.Length];
                for (int i = 0; i < filenames.Value.Length; i++)
                {
                    tasks[i] = _service.Download(source, filenames.Value[i]);
                }
                var files = await Task.WhenAll(tasks);

                if (filenames.Value.Length == files.Count(x => x))
                {
                    foreach (var (file, name) in files.Select(x => x.Value).Zip(filenames.Value))
                    {
                        var header = name.Replace(source.Prefix, "");
                        var extension = header.LastIndexOf('.');
                        if (extension > 0) header = header[..extension];

                        _display.Show(source.Display, header, file);
                    }
                    response = response.With(Unit.Instance);
                }
                filenames.LogValue(x => "{Count} files found.".WithArgs(x.Value.Length));
            }

            return response;
        }

        private static Response<S3SourceModel> GetSourceModel(string key, S3Config config, string correlationId, string path)
        {
            var response = new Response<S3SourceModel>();
            var model = new S3SourceModel();
            var @default = config.Defaults;
            var source = config.Sources.GetValueOrDefault(key, default);
            if (source == default) source = config.Sources.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
            if (source == default) return response.LogErrorValue("S3 source [{Key}] not found.".WithArgs(key));

            // Merge source with default model.
            model.Display = source.Display == DisplayType.NotSet ? @default.Display : source.Display;
            model.Bucket = string.IsNullOrWhiteSpace(source.Bucket) ? @default?.Bucket : source.Bucket;
            model.Prefix = string.IsNullOrWhiteSpace(source.Prefix) ? @default?.Prefix : source.Prefix;
            model.ContentType = string.IsNullOrWhiteSpace(source.ContentType) ? @default?.ContentType : source.ContentType;
            model.Files = (source.Files == null || source.Files.Length == 0) ? @default?.Files : source.Files;

            // Validate source model.
            var isValid = true;
            isValid = isValid && !string.IsNullOrWhiteSpace(model.Bucket);
            isValid = isValid && string.IsNullOrWhiteSpace(model.ContentType) || "json".Equals(model.ContentType, StringComparison.OrdinalIgnoreCase) || "text".Equals(model.ContentType, StringComparison.OrdinalIgnoreCase);
            isValid = isValid && ((!string.IsNullOrWhiteSpace(model.Prefix) && model.Prefix.Contains(CliConstants.CidMask)) || !string.Empty.Equals(path));
            isValid = isValid && ((source.Files == null || source.Files.Length == 0) || source.Files.Count(string.IsNullOrWhiteSpace) == 0);

            if (isValid)
            {
                // Align optional fields.
                if (model.Display == DisplayType.NotSet) model.Display = DisplayType.Chrome;
                if (string.IsNullOrWhiteSpace(model.ContentType)) model.ContentType = string.Empty;
                if (source.Files == null || source.Files.Length == 0) model.Files = Array.Empty<string>();

                model.ContentType = model.ContentType.ToLowerInvariant();
                model.Prefix = string.Empty.Equals(correlationId) ? path : model.Prefix.Replace(CliConstants.CidMask, correlationId);

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
