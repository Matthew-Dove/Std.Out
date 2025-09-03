using ContainerExpressions.Containers;
using Microsoft.Extensions.DependencyInjection;
using Std.Out.Services;

namespace Tests.Std.Out
{
    public sealed class StdCliTests : IDisposable
    {
        // CloudWatch.
        private static readonly string[] CW_CID = "cw --key appname --cid c6b8c804-34cb-4cf7-b762-d24b644831e9".Split(" ");
        private static readonly string[] CW_ATN = "cw --key appname --action Tests.Std.Out.StdOutTests.Store --actionkey appname".Split(" ");

        // Simple Storage Service.
        private static readonly string[] S3_CID = "s3 --key appname --cid c6b8c804-34cb-4cf7-b762-d24b644831e9".Split(" ");
        private static readonly string[] S3_ATN = "s3 --key appname --action Tests.Std.Out.StdOutTests.Store --actionkey appname".Split(" ");
        private static readonly string[] S3_PTH = "s3 --key appname --path assets/plaintext/c6b8c804-34cb-4cf7-b762-d24b644831e9".Split(" ");

        // DynamoDB.
        private static readonly string[] DB_CID = "db --key appname --cid c6b8c804-34cb-4cf7-b762-d24b644831e9".Split(" ");
        private static readonly string[] DB_ATN = "db --key appname --action Tests.Std.Out.StdOutTests.Store --actionkey appname".Split(" ");
        private static readonly string[] DB_PK = "db --key appname -pk pk_value".Split(" ");
        private static readonly string[] DB_PK_SK = "db --key appname -pk pk_value -sk sk_value".Split(" ");

        // Load.
        private static readonly string[] LD_ATN = "ld --key appname --action Tests.Std.Out.StdOutTests.Store".Split(" ");

        // Query.
        private static readonly string[] QY = "qy --key appname".Split(" ");

        private readonly IServiceScope _scope;
        private readonly IStdCli _std;

        public StdCliTests()
        {
            _scope = DEI.GetScope();
            _std = _scope.Get<IStdCli>();
        }

        public void Dispose() => _scope.Dispose();

        [Fact]
        public async Task CloudWatch_CorrelationId()
        {
            var result = await _std.Execute(CW_CID);

            Assert.True(result.IsValid);
            Assert.True(result.Value.Match( badRequest => false, display => true));
        }

        [Fact]
        public async Task CloudWatch_Action()
        {
            var result = await _std.Execute(CW_ATN);

            Assert.True(result.IsValid);
            Assert.True(result.Value.Match(badRequest => false, display => true));
        }

        [Fact]
        public async Task SimpleStorageService_CorrelationId()
        {
            var result = await _std.Execute(S3_CID);

            Assert.True(result.IsValid);
            Assert.True(result.Value.Match(badRequest => false, display => true));
        }

        [Fact]
        public async Task SimpleStorageService_Action()
        {
            var result = await _std.Execute(S3_ATN);

            Assert.True(result.IsValid);
            Assert.True(result.Value.Match(badRequest => false, display => true));
        }

        [Fact]
        public async Task SimpleStorageService_Path()
        {
            var result = await _std.Execute(S3_PTH);

            Assert.True(result.IsValid);
            Assert.True(result.Value.Match(badRequest => false, display => true));
        }

        [Fact]
        public async Task DynamoDB_CorrelationId()
        {
            var result = await _std.Execute(DB_CID);

            Assert.True(result.IsValid);
            Assert.True(result.Value.Match(badRequest => false, display => true));
        }

        [Fact]
        public async Task DynamoDB_Action()
        {
            var result = await _std.Execute(DB_ATN);

            Assert.True(result.IsValid);
            Assert.True(result.Value.Match(badRequest => false, display => true));
        }

        [Fact]
        public async Task DynamoDB_PartitionKey()
        {
            var result = await _std.Execute(DB_PK);

            Assert.True(result.IsValid);
            Assert.True(result.Value.Match(badRequest => false, display => true));
        }

        [Fact]
        public async Task DynamoDB_PartitionKey_SortKey()
        {
            var result = await _std.Execute(DB_PK_SK);

            Assert.True(result.IsValid);
            Assert.True(result.Value.Match(badRequest => false, display => true));
        }

        [Fact]
        public async Task Load_Action()
        {
            var result = await _std.Execute(LD_ATN);

            Assert.True(result.IsValid);
            Assert.True(result.Value.Match(badRequest => false, display => true));
        }

        [Fact]
        public async Task Query()
        {
            var result = await _std.Execute(QY);

            Assert.True(result.IsValid);
            Assert.True(result.Value.Match(badRequest => false, display => true));
        }
    }
}
