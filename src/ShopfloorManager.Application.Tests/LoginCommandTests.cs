using ShopfloorManager.Application.Auth;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Domain.Entities;
using Xunit;

namespace ShopfloorManager.Application.Tests;

file class FakeHasher : IPasswordHasher
{
    public string Hash(string password) => password;
    public bool Verify(string password, string hash) => password == hash;
}

file class FakeJwt : IJwtTokenService
{
    public string GenerateToken(User user) => "fake-token";
}

public class LoginCommandTests
{
    [Theory]
    [InlineData("qc1")]
    [InlineData("QC1")]
    [InlineData("Qc1")]
    public async Task Login_is_case_insensitive_on_username(string typedLogin)
    {
        var db = TestDbContextFactory.Create();
        db.Users.Add(new User { UserLogin = "qc1", PasswordHash = "secret", Name = "QC One", IsActive = true });
        await db.SaveChangesAsync();
        var handler = new LoginCommandHandler(db, new FakeHasher(), new FakeJwt());

        var result = await handler.Handle(new LoginCommand(typedLogin, "secret"), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
