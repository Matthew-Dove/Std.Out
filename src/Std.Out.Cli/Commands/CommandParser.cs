using ContainerExpressions.Containers;
using Std.Out.Cli.Models;
using System.Reflection;

namespace Std.Out.Cli.Commands;

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
        var kv = GetOptionKeyValues(args);

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
                cid = kv.GetOptions(Option.CorrelationId, Option.C);

                var path = kv.GetOptions(Option.Path, Option.P);

                if (!string.Empty.Equals(cid) && !string.Empty.Equals(path)) // Allow either cid, or path to be passed (but not both).
                {
                    response.LogErrorValue("Cannot pass both {CorrelationId}, and {Path} options at once.".WithArgs(Option.CorrelationId, Option.Path));
                }
                else if (string.Empty.Equals(cid) && string.Empty.Equals(path)) // Must have one of path, or cid.
                {
                    response.LogErrorValue("Must have at one of {CorrelationId}, and {Path} options.".WithArgs(Option.CorrelationId, Option.Path));
                }
                else if (!string.Empty.Equals(key))
                {
                    response = response.With(new CommandModel { Verb = Verb.S3, SettingsKey = key, CorrelationId = cid, Path = path });
                }
                break;

            case "db":
                key = kv.GetOptions(Option.Key, Option.K).LogWhenEmpty("Option {Option} is required.".WithArgs(Option.Key));
                cid = kv.GetOptions(Option.CorrelationId, Option.C);

                var pk = kv.GetOptions(Option.PartitionKey, Option.Pk);
                var sk = kv.GetOptions(Option.SortKey, Option.Sk);

                if (!string.Empty.Equals(pk) && !string.Empty.Equals(cid)) // Allow either pk, or cid to be passed (but not both).
                {
                    response.LogErrorValue("Cannot pass both {PartitionKey}, and {CorrelationId} options at once.".WithArgs(Option.PartitionKey, Option.CorrelationId));
                }
                else if (string.Empty.Equals(pk) && string.Empty.Equals(cid)) // Must have one of pk, or cid.
                {
                    response.LogErrorValue("Must have at one of {PartitionKey}, and {CorrelationId} options.".WithArgs(Option.PartitionKey, Option.CorrelationId));
                }
                else if (!string.Empty.Equals(sk) && !string.Empty.Equals(cid)) // Not valid to send sk, when using cid.
                {
                    response.LogErrorValue("Cannot pass {SortKey}, when using the {CorrelationId} option.".WithArgs(Option.SortKey, Option.CorrelationId));
                }
                else if (!string.Empty.Equals(key))
                {
                    response = response.With(new CommandModel { Verb = Verb.DynamoDB, SettingsKey = key, CorrelationId = cid, PartitionKey = pk, SortKey = sk });
                }
                break;

            case "ld":
                key = kv.GetOptions(Option.Key, Option.K).LogWhenEmpty("Option {Option} is required.".WithArgs(Option.Key));

                var action = kv.GetOptions(Option.Action, Option.A).LogWhenEmpty("Option {Option} is required.".WithArgs(Option.Action));

                if (!string.Empty.Equals(key) && !string.Empty.Equals(action))
                {
                    response = response.With(new CommandModel { Verb = Verb.Load, SettingsKey = key, Action = action });
                }
                break;

            case "qy":
                key = kv.GetOptions(Option.Key, Option.K).LogWhenEmpty("Option {Option} is required.".WithArgs(Option.Key));

                if (!string.Empty.Equals(key))
                {
                    response = response.With(new CommandModel { Verb = Verb.Query, SettingsKey = key });
                }
                break;

            default:
                response.LogErrorValue("Invalid command verb: [{Verb}].".WithArgs(args[0]));
                break;
        }

        return response;
    }

    private static Dictionary<string, string> GetOptionKeyValues(string[] args)
    {
        var options = args.Where(x => !x.ToLowerInvariant().IsFlag()).ToArray();
        var kv = new Dictionary<string, string>((options.Length - 1) / 2);

        for (var i = 1; (i + 2) <= options.Length; i += 2)
        {
            if (!kv.ContainsKey(options[i]))
            {
                kv.Add(options[i].ToLowerInvariant(), options[i + 1]);
            }
        }

        return kv;
    }
}

file static class CommandExtensions
{
    private static readonly string[] _flags = typeof(Flag)
        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
        .Where(fi => fi.IsLiteral && !fi.IsInitOnly)
        .Select(fi => fi.GetValue(null).ToString().ToLowerInvariant())
        .ToArray();

    public static bool IsFlag(this string value) => value.StartsWith('-') && _flags.Contains(value);

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
