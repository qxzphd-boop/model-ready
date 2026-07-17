using ModelReady.Core;

namespace ModelReady.Core.Tests;

public sealed class ProductInfoTests
{
    [Fact]
    public void Product_identity_is_stable()
    {
        Assert.Equal("ModelReady", ProductInfo.Name);
        Assert.Equal("0.1.0-dev", ProductInfo.Version);
    }
}
