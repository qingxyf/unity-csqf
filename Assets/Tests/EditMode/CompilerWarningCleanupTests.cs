using System.IO;
using NUnit.Framework;

public class CompilerWarningCleanupTests
{
    [Test]
    public void LegacyUnusedSerializedFieldsStayRemoved()
    {
        string bossBulletSource = File.ReadAllText("Assets/Scripts/Combat/BossBulletBehavior.cs");
        string playerControllerSource = File.ReadAllText("Assets/Scripts/Player/PlayerController.cs");

        Assert.That(bossBulletSource, Does.Not.Contain("destroyDistance"));
        Assert.That(playerControllerSource, Does.Not.Contain("shootInterval"));
        Assert.That(playerControllerSource, Does.Not.Contain("canShoot"));
    }
}
