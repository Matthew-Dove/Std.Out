using ContainerExpressions.Containers;
using Std.Out.Cli.Models;

namespace Std.Out.Cli.Commands
{
    public interface ICommandService
    {
        Task<Response<Either<BadRequest, Unit>>> Execute(string[] args);
    }

    public sealed class CommandService(
        ICommandParser _parser, ICloudWatchCommand _cw, IS3Command _s3, IDynamodbCommand _db, IQueryCommand _qy, ILoadCommand _ld
        ) : ICommandService
    {
        public async Task<Response<Either<BadRequest, Unit>>> Execute(string[] args)
        {
            var response = new Response<Either<BadRequest, Unit>>();

            var cmd = _parser.Parse(args);
            if (!cmd) return response.With(new BadRequest());
            var command = cmd.Value;

            if (Verb.CloudWatch.Equals(command.Verb))
            {
                response = await _cw.Execute(command);
            }
            else if (Verb.S3.Equals(command.Verb))
            {
                response = await _s3.Execute(command);
            }
            else if (Verb.DynamoDB.Equals(command.Verb))
            {
                response = await _db.Execute(command);
            }
            else if (Verb.Query.Equals(command.Verb))
            {
                response = await _qy.Execute(command);
            }
            else if (Verb.Load.Equals(command.Verb))
            {
                response = await _ld.Execute(command);
            }

            return response;
        }
    }
}
