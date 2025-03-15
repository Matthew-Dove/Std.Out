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

        string key = string.Empty, cid = string.Empty, action = string.Empty, actionKey = string.Empty;
        var isValid = true;
        var kv = GetOptionKeyValues(args);

        switch (args[0].ToLowerInvariant())
        {
            case "cw":
                key = kv.GetOptions(Option.Key, Option.K).LogWhenEmpty("Option {Option} is required.".WithArgs(Option.Key));
                cid = kv.GetOptions(Option.CorrelationId, Option.C);
                action = kv.GetOptions(Option.Action, Option.A);

                /**
                 * Allowed option combos for CloudWatch:
                 * - cid
                 * - action actionkey
                **/

                isValid = false;

                if (!string.Empty.Equals(cid))
                {
                    action = string.Empty;
                    actionKey = string.Empty;
                    isValid = true;
                }
                else if (!string.Empty.Equals(action))
                {
                    actionKey = kv.GetOptions(Option.ActionKey, Option.Ak).LogWhenEmpty("Option {Option} is required.".WithArgs(Option.ActionKey));
                    cid = string.Empty;
                    isValid = !string.Empty.Equals(actionKey);
                }

                if (isValid && !string.Empty.Equals(key))
                {
                    response = response.With(new CommandModel {
                        Verb = Verb.CloudWatch,
                        SettingsKey = key,
                        CorrelationId = cid,
                        Action = action,
                        ActionSettingsKey = actionKey
                    });
                }
                break;

            case "s3":
                key = kv.GetOptions(Option.Key, Option.K).LogWhenEmpty("Option {Option} is required.".WithArgs(Option.Key));
                cid = kv.GetOptions(Option.CorrelationId, Option.C);
                action = kv.GetOptions(Option.Action, Option.A);

                var path = kv.GetOptions(Option.Path, Option.P);

                /**
                 * Allowed option combos for S3:
                 * - cid
                 * - action actionkey
                 * - path
                **/

                isValid = false;

                if (!string.Empty.Equals(cid))
                {
                    action = string.Empty;
                    actionKey = string.Empty;
                    path = string.Empty;
                    isValid = true;
                }
                else if (!string.Empty.Equals(action))
                {
                    actionKey = kv.GetOptions(Option.ActionKey, Option.Ak).LogWhenEmpty("Option {Option} is required.".WithArgs(Option.ActionKey));
                    cid = string.Empty;
                    path = string.Empty;
                    isValid = !string.Empty.Equals(actionKey);
                }
                else if (!string.Empty.Equals(path))
                {
                    cid = string.Empty;
                    action = string.Empty;
                    actionKey = string.Empty;
                    isValid = true;
                }

                if (isValid && !string.Empty.Equals(key))
                {
                    response = response.With(new CommandModel {
                        Verb = Verb.S3,
                        SettingsKey = key,
                        CorrelationId = cid,
                        Path = path,
                        Action = action,
                        ActionSettingsKey = actionKey
                    });
                }
                break;

            case "db":
                key = kv.GetOptions(Option.Key, Option.K).LogWhenEmpty("Option {Option} is required.".WithArgs(Option.Key));
                cid = kv.GetOptions(Option.CorrelationId, Option.C);
                action = kv.GetOptions(Option.Action, Option.A);

                var pk = kv.GetOptions(Option.PartitionKey, Option.Pk);
                var sk = kv.GetOptions(Option.SortKey, Option.Sk);

                /**
                 * Allowed option combos for DynamoDB:
                 * - cid
                 * - action actionkey
                 * - pk sk
                 * - pk
                **/

                isValid = false;

                if (!string.Empty.Equals(cid))
                {
                    action = string.Empty;
                    actionKey = string.Empty;
                    pk = string.Empty;
                    sk = string.Empty;
                    isValid = true;
                }
                else if (!string.Empty.Equals(action))
                {
                    actionKey = kv.GetOptions(Option.ActionKey, Option.Ak).LogWhenEmpty("Option {Option} is required.".WithArgs(Option.ActionKey));
                    cid = string.Empty;
                    pk = string.Empty;
                    sk = string.Empty;
                    isValid = !string.Empty.Equals(actionKey);
                }
                else if (!string.Empty.Equals(pk) && !string.Empty.Equals(sk))
                {
                    cid = string.Empty;
                    action = string.Empty;
                    actionKey = string.Empty;
                    isValid = true;
                }
                else if (!string.Empty.Equals(pk))
                {
                    cid = string.Empty;
                    action = string.Empty;
                    actionKey = string.Empty;
                    sk = string.Empty;
                    isValid = true;
                }

                if (isValid && !string.Empty.Equals(key))
                {
                    response = response.With(new CommandModel {
                        Verb = Verb.DynamoDB,
                        SettingsKey = key,
                        CorrelationId = cid,
                        PartitionKey = pk,
                        SortKey = sk,
                        Action = action,
                        ActionSettingsKey = actionKey
                    });
                }
                break;

            case "ld":
                key = kv.GetOptions(Option.Key, Option.K).LogWhenEmpty("Option {Option} is required.".WithArgs(Option.Key));
                action = kv.GetOptions(Option.Action, Option.A).LogWhenEmpty("Option {Option} is required.".WithArgs(Option.Action));

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

        if (!isValid) isValid.LogValue("Arguments aren't valid for verb: [{Verb}].".WithArgs(args[0]));
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
