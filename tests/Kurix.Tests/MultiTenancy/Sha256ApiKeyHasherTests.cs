using Kurix.Infrastructure.MultiTenancy;

namespace Kurix.Tests.MultiTenancy;

public class Sha256ApiKeyHasherTests
{
    private readonly Sha256ApiKeyHasher _hasher = new();

    [Fact]
    public void Hash_Is_Deterministic()
    {
        const string key = "kx_some-api-key";
        Assert.Equal(_hasher.Hash(key), _hasher.Hash(key));
    }

    [Fact]
    public void Hash_Differs_For_Different_Keys()
    {
        Assert.NotEqual(_hasher.Hash("kx_a"), _hasher.Hash("kx_b"));
    }

    [Fact]
    public void Verify_Returns_True_For_Matching_Key()
    {
        const string key = "kx_secret";
        var hash = _hasher.Hash(key);
        Assert.True(_hasher.Verify(key, hash));
    }

    [Fact]
    public void Verify_Returns_False_For_Wrong_Key()
    {
        var hash = _hasher.Hash("kx_secret");
        Assert.False(_hasher.Verify("kx_other", hash));
    }

    [Fact]
    public void GenerateApiKey_Produces_Unique_Prefixed_Keys()
    {
        var a = _hasher.GenerateApiKey();
        var b = _hasher.GenerateApiKey();

        Assert.StartsWith("kx_", a);
        Assert.StartsWith("kx_", b);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Generated_Key_RoundTrips_Through_Hash_And_Verify()
    {
        var key = _hasher.GenerateApiKey();
        var hash = _hasher.Hash(key);
        Assert.True(_hasher.Verify(key, hash));
    }
}
