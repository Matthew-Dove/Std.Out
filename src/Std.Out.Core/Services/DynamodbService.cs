using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ContainerExpressions.Containers;
using Std.Out.Core.Models;
using Std.Out.Core.Models.Config;
using System.Net;

namespace Std.Out.Core.Services
{
    public interface IDynamodbService
    {
        Task<Response<string[]>> Query(DynamodbSourceModel source, string pk, string sk);
        Task<Response<string[]>> QueryIndex(DynamodbSourceModel source);
        Task<Response<string[]>> QuerySk(string tableName, string partitionKeyName, string partitionKeyValue, string sortKeyName);
        Task<Response<Unit>> Put(string tableName, Dictionary<string, string> attributes, string ttlName, long ttl);
        Task<Response<Either<string, NotFound>>> Get(string tableName, string partitionKeyName, string partitionKeyValue, string sortKeyName, string sortKeyValue);
    }

    public sealed class DynamodbService : IDynamodbService
    {
        private static readonly AmazonDynamoDBClient _client = new AmazonDynamoDBClient();

        public async Task<Response<string[]>> Query(DynamodbSourceModel source, string pk, string sk)
        {
            var response = new Response<string[]>();

            var queryRequest = new QueryRequest
            {
                TableName = source.TableName,
                KeyConditionExpression = "#pk = :pk",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#pk", source.PartitionKeyName }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":pk", new AttributeValue { S = pk } }
                },
                ProjectionExpression = source.Projection.Length == 0 ? null : string.Join(", ", source.Projection),
                Limit = CoreConstants.MaxLimit
            };

            if (sk != string.Empty)
            {
                queryRequest.KeyConditionExpression += " AND #sk = :sk";
                queryRequest.ExpressionAttributeNames.Add("#sk", source.SortKeyName);
                queryRequest.ExpressionAttributeValues.Add(":sk", new AttributeValue { S = sk });
            }

            try
            {
                var queryResponse = await _client.QueryAsync(queryRequest);

                if (queryResponse.HttpStatusCode == HttpStatusCode.OK)
                {
                    var result = queryResponse.Items.Select(Document.FromAttributeMap).Select(x => x.ToJsonPretty()).ToArray();
                    response = response.With(result);
                }
                else
                {
                    queryResponse.LogErrorValue(x => "Querying failed with HTTP code: {HttpCode}, on table: {Table}, with pk: {Pk}.".WithArgs(x.HttpStatusCode, source.TableName, pk));
                }
            }
            catch (Exception ex)
            {
                ex.LogError("Error querying table: {Table}, with pk: {Pk}.".WithArgs(source.TableName, pk));
            }

            return response;
        }
        
        public async Task<Response<string[]>> QueryIndex(DynamodbSourceModel source)
        {
            var response = new Response<string[]>();

            var queryRequest = new QueryRequest
            {
                TableName = source.TableName,
                IndexName = source.IndexName,
                KeyConditionExpression = "#ixpk = :ixpk",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#ixpk", source.IndexPartitionKeyName }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":ixpk", new AttributeValue { S = source.IndexPartitionKeyMask } }
                },
                ProjectionExpression = source.PartitionKeyName + (source.SortKeyName == string.Empty ? string.Empty : ("," + source.SortKeyName)),
                Limit = 1
            };

            if (source.IndexSortKeyName != string.Empty)
            {
                queryRequest.KeyConditionExpression += " AND #ixsk = :ixsk";
                queryRequest.ExpressionAttributeNames.Add("#ixsk", source.IndexSortKeyName);
                queryRequest.ExpressionAttributeValues.Add(":ixsk", new AttributeValue { S = source.IndexSortKeyMask });
            }

            try
            {
                var queryResponse = await _client.QueryAsync(queryRequest);

                if (queryResponse.HttpStatusCode == HttpStatusCode.OK)
                {
                    response = response.With(Array.Empty<string>());
                    if (queryResponse.Items.Count == 1)
                    {
                        var item = queryResponse.Items[0];
                        var pk = item[source.PartitionKeyName].S;
                        var sk = source.SortKeyName == string.Empty ? string.Empty : item[source.SortKeyName].S;
                        response = await Query(source, pk, sk);
                    }
                }
                else
                {
                    queryResponse.LogErrorValue(x => "Querying {Index} failed with HTTP code: {HttpCode}, on table: {Table}, with ix_pk: {IxPk}, ix_sk: {IxSk}.".WithArgs(source.IndexName, x.HttpStatusCode, source.TableName, source.IndexPartitionKeyMask, source.IndexSortKeyMask));
                }
            }
            catch (Exception ex)
            {
                ex.LogError("Error querying {Index} for table: {Table}, with ix_pk: {IxPk}, ix_sk: {IxSk}.".WithArgs(source.IndexName, source.TableName, source.IndexPartitionKeyMask, source.IndexSortKeyMask));
            }

            return response;
        }

        public async Task<Response<string[]>> QuerySk(string tableName, string partitionKeyName, string partitionKeyValue, string sortKeyName)
        {
            var response = new Response<string[]>();

            var queryRequest = new QueryRequest
            {
                TableName = tableName,
                KeyConditionExpression = "#pk = :pk",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#pk", partitionKeyName }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":pk", new AttributeValue { S = partitionKeyValue } }
                },
                ProjectionExpression = sortKeyName,
                Limit = CoreConstants.MaxLimit
            };

            try
            {
                var queryResponse = await _client.QueryAsync(queryRequest);

                if (queryResponse.HttpStatusCode == HttpStatusCode.OK)
                {
                    var result = queryResponse.Items.Select(x => x[sortKeyName].S).ToArray();
                    response = response.With(result);
                }
                else
                {
                    queryResponse.LogErrorValue(x => "Querying failed with HTTP code: {HttpCode}, on table: {Table}, with pk: {Pk}.".WithArgs(x.HttpStatusCode, tableName, partitionKeyValue));
                }
            }
            catch (Exception ex)
            {
                ex.LogError("Error querying table: {Table}, with pk: {Pk}.".WithArgs(tableName, partitionKeyValue));
            }

            return response;
        }

        public async Task<Response<Unit>> Put(string tableName, Dictionary<string, string> attributes, string ttlName, long ttl)
        {
            var response = new Response<Unit>();

            var item = attributes.Select(x => new KeyValuePair<string, AttributeValue>(x.Key, new AttributeValue { S = x.Value }));
            var putRequest = new PutItemRequest
            {
                TableName = tableName,
                Item = new Dictionary<string, AttributeValue>(item)
            };
            if (ttl > 0) putRequest.Item[ttlName] = new AttributeValue { N = ttl.ToString() };

            try
            {
                var putResponse = await _client.PutItemAsync(putRequest);

                if (putResponse.HttpStatusCode == HttpStatusCode.OK)
                {
                    response = response.With(Unit.Instance);
                }
                else
                {
                    putResponse.LogErrorValue(x => "Put failed with HTTP code: {HttpCode}, on table: {Table}.".WithArgs(x.HttpStatusCode, tableName));
                }
            }
            catch (Exception ex)
            {
                ex.LogError("Error attempting a Put to the table: {Table}.".WithArgs(tableName));
            }

            return response;
        }

        public async Task<Response<Either<string, NotFound>>> Get(
            string tableName,
            string partitionKeyName, string partitionKeyValue,
            string sortKeyName, string sortKeyValue
            )
        {
            var response = new Response<Either<string, NotFound>>();

            var getRequest = new GetItemRequest
            {
                TableName = tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { partitionKeyName, new AttributeValue { S = partitionKeyValue } }
                }
            };

            if (sortKeyName != string.Empty) getRequest.Key.Add(sortKeyName, new AttributeValue { S = sortKeyValue });

            try
            {
                var getResponse = await _client.GetItemAsync(getRequest);

                if (getResponse.HttpStatusCode == HttpStatusCode.OK)
                {
                    if (getResponse.Item.Count == 0) response = response.With(new NotFound());
                    else
                    {
                        var json = Document.FromAttributeMap(getResponse.Item).ToJson();
                        response = response.With(json);
                    }
                }
                else
                {
                    getResponse.LogErrorValue(x => "Get failed with HTTP code: {HttpCode}, on table: {Table}.".WithArgs(x.HttpStatusCode, tableName));
                }
            }
            catch (Exception ex)
            {
                ex.LogError("Error attempting a Get from the table: {Table}.".WithArgs(tableName));
            }

            return response;
        }
    }
}
