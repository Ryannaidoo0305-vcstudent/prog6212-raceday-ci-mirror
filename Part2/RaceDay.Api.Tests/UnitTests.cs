using RaceDay.Api.Controllers;
using RaceDay.Api.Security;

namespace RaceDay.Api.Tests;

// Plain unit tests of the business rules, without starting the API.
public class UnitTests
{
    [Fact]
    public void Hash_IsBCrypt_AndVerifies()
    {
        var hash = PasswordHashing.Hash("RaceDay@2026");

        Assert.StartsWith("$2a$11$", hash);
        Assert.True(PasswordHashing.Verify("RaceDay@2026", hash));
        Assert.False(PasswordHashing.Verify("wrong", hash));
    }

    [Fact]
    public void Hash_SamePasswordTwice_GivesDifferentHashes()
    {
        Assert.NotEqual(PasswordHashing.Hash("RaceDay@2026"), PasswordHashing.Hash("RaceDay@2026"));
    }

    [Fact]
    public void Part1SeedHash_MatchesDemoPassword()
    {
        Assert.True(PasswordHashing.Verify("RaceDay@2026", "$2a$11$wj6inT9j12Da9caXJQflmeMsmucCODikYIMnxXSBMGERv1jumu7Cu"));
    }

    [Theory]
    [InlineData(2001, 7, 25, 2027, 4, 25, 25)]   // birthday later in the year
    [InlineData(2001, 4, 25, 2027, 4, 25, 26)]   // birthday on race day
    [InlineData(2000, 2, 29, 2027, 2, 28, 27)]   // leap-year birthday counts on 28 Feb
    public void AgeOn_RaceDay(int by, int bm, int bd, int ey, int em, int ed, int expected)
    {
        var age = EnrolmentsController.AgeOn(new DateOnly(by, bm, bd), new DateTime(ey, em, ed));

        Assert.Equal(expected, age);
    }
}
