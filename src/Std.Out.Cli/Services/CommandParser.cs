using ContainerExpressions.Containers;
using Std.Out.Cli.Models;

namespace Std.Out.Cli.Services
{
    public interface ICommandParser
    {
        Response<CommandModel> Parse(string[] args);
    }

    public sealed class CommandParser : ICommandParser
    {
        public Response<CommandModel> Parse(string[] args)
        {
            var response = new Response<CommandModel>();
            if (args.Length < 3) return response.LogErrorValue("{Args}(s) args found, but expected at least 3 arguments (verb, and correlation id).".WithArgs(args.Length));

            string key = string.Empty, cid = string.Empty;
            var kv = GetKeyValues(args);

            switch (args[0].ToLowerInvariant())
            {
                case "cw":
                    key = kv.GetOptions(Option.Key, Option.K).LogWhenEmpty("Option {Option} is required.".WithArgs(Option.Key));
                    cid = kv.GetOptions(Option.CorrelationId, Option.C);

                    if (!string.Empty.Equals(key))
                    {
                        response = response.With(new CommandModel { Verb = Verb.CloudWatch, SettingsKey = key, CorrelationId = cid });
                    }
                    break;

                case "s3":
                    key = kv.GetOptions(Option.Key, Option.K).LogWhenEmpty("Option {Option} is required.".WithArgs(Option.Key));
                    cid = kv.GetOptions(Option.CorrelationId, Option.C).LogWhenEmpty("Option {Option} is required.".WithArgs(Option.CorrelationId));

                    if (!string.Empty.Equals(key) && !string.Empty.Equals(cid))
                    {
                        response = response.With(new CommandModel { Verb = Verb.S3, SettingsKey = key, CorrelationId = cid });
                    }
                    break;

                case "db":
                    key = kv.GetOptions(Option.Key, Option.K).LogWhenEmpty("Option {Option} is required.".WithArgs(Option.Key));

                    if (!string.Empty.Equals(key))
                    {
                        response = response.With(new CommandModel { Verb = Verb.DynamoDB, SettingsKey = key, CorrelationId = string.Empty });
                    }
                    break;

                default:
                    response.LogErrorValue("Invalid command verb: [{Verb}].".WithArgs(args[0]));
                    break;
            }

            return response;
        }

        private static Dictionary<string, string> GetKeyValues(string[] args)
        {
            var kv = new Dictionary<string, string>((args.Length - 1) / 2);

            for (var i = 1; (i + 2) <= args.Length; i += 2)
            {
                if (!kv.ContainsKey(args[i]))
                {
                    kv.Add(args[i].ToLowerInvariant(), args[i + 1]);
                }
            }

            return kv;
        }
    }

    file static class CommandExtensions
    {
        public static string GetOptions(this Dictionary<string, string> kv, params string[] options)
        {
            var value = string.Empty;

            foreach (var option in options)
            {
                value = kv.GetValueOrDefault(option, string.Empty);
                if (!string.Empty.Equals(value)) break;
            }

            return value;
        }

        public static string LogWhenEmpty(this string value, Format message)
        {
            if (string.Empty.Equals(value)) value.LogErrorValue(message);
            return value;
        }
    }
}
