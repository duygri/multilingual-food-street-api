using System.Reflection;
using Microsoft.AspNetCore.RateLimiting;
using NarrationApp.Server.Controllers;

namespace NarrationApp.Server.Tests.Controllers;

public sealed class RateLimitingMetadataTests
{
    [Theory]
    [InlineData(typeof(AuthController), nameof(AuthController.RegisterAsync), "auth")]
    [InlineData(typeof(AuthController), nameof(AuthController.RegisterOwnerAsync), "auth")]
    [InlineData(typeof(AuthController), nameof(AuthController.LoginAsync), "auth")]
    [InlineData(typeof(AuthController), nameof(AuthController.LoginTouristAsync), "auth")]
    [InlineData(typeof(AuthController), nameof(AuthController.ChangePasswordAsync), "auth")]
    [InlineData(typeof(PoisController), nameof(PoisController.CreateAsync), "content-mutation")]
    [InlineData(typeof(PoisController), nameof(PoisController.UpdateAsync), "content-mutation")]
    [InlineData(typeof(PoisController), nameof(PoisController.DeleteAsync), "content-mutation")]
    [InlineData(typeof(AudioController), nameof(AudioController.UploadAsync), "content-mutation")]
    [InlineData(typeof(AudioController), nameof(AudioController.GenerateTtsAsync), "generation")]
    [InlineData(typeof(AudioController), nameof(AudioController.UpdateAsync), "content-mutation")]
    [InlineData(typeof(AudioController), nameof(AudioController.DeleteAsync), "content-mutation")]
    [InlineData(typeof(TranslationsController), nameof(TranslationsController.UpsertAsync), "content-mutation")]
    [InlineData(typeof(TranslationsController), nameof(TranslationsController.AutoTranslateAsync), "generation")]
    [InlineData(typeof(ModerationRequestsController), nameof(ModerationRequestsController.CreateAsync), "content-mutation")]
    [InlineData(typeof(AdminController), nameof(AdminController.ApproveAsync), "content-mutation")]
    [InlineData(typeof(AdminController), nameof(AdminController.RejectAsync), "content-mutation")]
    [InlineData(typeof(AdminController), nameof(AdminController.UpdateUserRoleAsync), "content-mutation")]
    public void Critical_mutation_actions_declare_expected_rate_limit_policy(Type controllerType, string methodName, string expectedPolicy)
    {
        var method = controllerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(method);

        var attribute = method!.GetCustomAttribute<EnableRateLimitingAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(expectedPolicy, attribute!.PolicyName);
    }
}
