using Amazon.CloudWatchLogs.Model;
using Amazon.CloudWatchLogs;
using Std.Out.Core.Models.Config;
using ContainerExpressions.Containers;
using System.Net;
using System.Text;

namespace Std.Out.Core.Services
{
    public sealed class LogGroup : Alias<string> { public LogGroup(string value) : base(value) { } }

    public interface ICloudWatchService
    {
        Task<Response<(LogGroup, string[])[]>> Query(CloudWatchSourceModel source, string correlationId);
    }

    public sealed class CloudWatchService : ICloudWatchService
    {
        private static readonly AmazonCloudWatchLogsClient _client = new AmazonCloudWatchLogsClient();

        public async Task<Response<(LogGroup, string[])[]>> Query(CloudWatchSourceModel source, string correlationId)
        {
            var queryRequest = BuildQuery(source, correlationId);
            var response = await QueryLogs(queryRequest, source);
            return response;
        }

        private static StartQueryRequest BuildQuery(CloudWatchSourceModel source, string correlationId)
        {
            var endTime = DateTimeOffset.UtcNow;
            var startTime = endTime.AddHours(-source.RelativeHours);

            var fields = string.Join(", ", source.Fields.Select(x => x.Replace("'", "")));
            if (!fields.Contains("@log")) fields += ", @log";

            var isPresent = string.Empty;
            if (source.IsPresentFieldName != string.Empty)
            {
                isPresent = $"ispresent({source.IsPresentFieldName.Replace("'", "")})";
            }

            var filterPredicates = string.Empty;
            if (source.Filters.Length > 0)
            {
                filterPredicates = string.Join(" and ", source.Filters.Select(x => $"{x.Field.Replace("'", "")} = '{x.Value.Replace("'", "")}'"));
            }

            var correlationPredicate = string.Empty;
            if (source.CorrelationIdFieldName != string.Empty && correlationId != string.Empty)
            {
                correlationPredicate = $"{source.CorrelationIdFieldName.Replace("'", "")} = '{correlationId.Replace("'", "")}'";
            }

            var filter = string.Empty;
            if (isPresent != string.Empty || filterPredicates != string.Empty || correlationPredicate != string.Empty)
            {
                var and1 = isPresent != string.Empty && filterPredicates != string.Empty ? " and " : string.Empty;
                var and2 = (correlationPredicate != string.Empty && (isPresent != string.Empty || filterPredicates != string.Empty)) ? " and " : string.Empty;

                filter = $"""

                    | filter {isPresent}{and1}{filterPredicates}{and2}{correlationPredicate}
                    """;
            }

            var query = $"""
                fields {fields}{filter}
                | sort @timestamp asc
                """;

            return new StartQueryRequest
            {
                LogGroupNames = new List<string>(source.LogGroups),
                StartTime = startTime.ToUnixTimeMilliseconds(),
                EndTime = endTime.ToUnixTimeMilliseconds(),
                Limit = source.Limit,
                QueryString = query
            };
        }

        private static async Task<Response<(LogGroup, string[])[]>> QueryLogs(StartQueryRequest queryRequest, CloudWatchSourceModel source)
        {
            var response = new Response<(LogGroup, string[])[]>();

            try
            {
                var queryResponse = await _client.StartQueryAsync(queryRequest);
                var queryPoll = new GetQueryResultsRequest { QueryId = queryResponse.QueryId };
                GetQueryResultsResponse queryResults;
                await Task.Delay(500);

                do
                {
                    queryResults = await _client.GetQueryResultsAsync(queryPoll);
                    await Task.Delay(150);
                } while (
                    queryResults.HttpStatusCode == HttpStatusCode.OK && (
                    queryResults.Status == QueryStatus.Running || queryResults.Status == QueryStatus.Scheduled
                ));

                if (queryResults.Status == QueryStatus.Complete)
                {
                    var groupedResults = queryResults.Results.GroupBy(ExtractLogGroupName);
                    var result = new List<(LogGroup, string[])>();

                    foreach (var group in groupedResults)
                    {
                        var logs = new List<string>();
                        foreach (var value in group)
                        {
                            var sb = new StringBuilder();
                            foreach (var field in source.Fields)
                            {
                                var fieldValue = value.Find(x => x.Field == field).Value ?? string.Empty;
                                sb.Append("[").Append(fieldValue).Append("]").Append(" ");
                            }
                            sb.Remove(sb.Length - 1, 1);
                            var log = sb.ToString();
                            logs.Add(log);
                        }
                        result.Add((new LogGroup(group.Key), logs.ToArray()));
                    }

                    response = response.With(result.ToArray());
                }
                else
                {
                    queryResults.LogErrorValue(x => "Query failed with HTTP code: {HttpCode}, status: {Status}.".WithArgs(x.HttpStatusCode, x.Status));
                }
            }
            catch (Exception ex)
            {
                ex.LogError("Error executing query.");
            }

            return response;
        }

        private static string ExtractLogGroupName(List<ResultField> fields)
        {
            var logGroup = fields.Find(x => x.Field == "@log").Value;
            return logGroup.Substring(logGroup.LastIndexOf('/') + 1).ToLowerInvariant();
        }
    }
}
