using Microsoft.Extensions.Options;
using Std.Out;
using Std.Out.Models;
using Tests.Std.Out.Config;

namespace Tests.Std.Out
{
    public class StdOutTests
    {
        private readonly IStdOut _std;

        private readonly StorageKey _key;
        private readonly string _cid;

        public StdOutTests()
        {
            _std = DI.Get<IStdOut>();

            var key = DI.Get<IOptions<StorageKeyConfig>>().Value;

            _key = StorageKey.CreateWithEnvironment(key.Application, key.Environment, (key.Namespace, key.Offset.GetValueOrDefault(0)));
            _cid = Guid.NewGuid().ToString();
        }

        [Fact]
        public async Task Store()
        {
            var result = await _std.Store(_key, _cid);

            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task Load()
        {
            var action = $"{typeof(StdOutTests).Namespace}.{nameof(StdOutTests)}.{nameof(Store)}";
            var key = StorageKey.CreateWithEnvironment(_key.Application, _key.Environment, action);

            var result = await _std.Load(key);

            Assert.True(result.IsValid);
        }
    }
}