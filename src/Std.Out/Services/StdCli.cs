using ContainerExpressions.Containers;
using Microsoft.Extensions.Logging;
using Std.Out.Cli.Commands;

namespace Std.Out.Services
{
    public readonly record struct Display(string Heading, string Body);

    public interface IStdCli
    {
        Task<Response<Either<BadRequest, Display[]>>> Execute(string[] args);
    }

    internal sealed class StdCli(ICommandService _cmd, ICollectorService _collector) : IStdCli
    {
        public async Task<Response<Either<BadRequest, Display[]>>> Execute(string[] args)
        {
            var response = new Response<Either<BadRequest, Display[]>>();

            try
            {
                var result = await _cmd.Execute(args);
                if (result)
                {
                    response = result.Value.Match(
                        badRequest => response.With(new Either<BadRequest, Display[]>(badRequest)),
                        _ => response.With(new Either<BadRequest, Display[]>(_collector.Collect()))
                    );
                }
            }
            catch (Exception ex)
            {
                ex.LogError("Top level StdCli error.");
            }

            return response;
        }
    }
}
