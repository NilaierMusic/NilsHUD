using NUnit.Framework;
using UnityEngine;
using System.Reflection;

[TestFixture]
public class EmoteHUDManagerTests
{
    private GameObject playerObject;
    private PlayerControllerB playerController;
    private Image healthBarImage;
    private Image redHealthBarImage;

    [SetUp]
    public void SetUp()
    {
        playerObject = new GameObject();
        playerController = playerObject.AddComponent<PlayerControllerB>();
        healthBarImage = new GameObject().AddComponent<Image>();
        redHealthBarImage = new GameObject().AddComponent<Image>();

        // Set initial values for playerController
        playerController.health = 100f;
        playerController.isPlayerDead = false;
        playerController.currentSuitID = 1;

        // Set initial values for healthBarImage and redHealthBarImage
        healthBarImage.fillAmount = 1f;
        redHealthBarImage.fillAmount = 1f;

        // Use reflection to set private fields in EmoteHUDManager
        var emoteHUDManagerType = typeof(EmoteHUDManager);
        var playerField = emoteHUDManagerType.GetField("player", BindingFlags.NonPublic | BindingFlags.Static);
        var healthBarImageField = emoteHUDManagerType.GetField("healthBarImage", BindingFlags.NonPublic | BindingFlags.Static);
        var redHealthBarImageField = emoteHUDManagerType.GetField("redHealthBarImage", BindingFlags.NonPublic | BindingFlags.Static);

        playerField.SetValue(null, playerController);
        healthBarImageField.SetValue(null, healthBarImage);
        redHealthBarImageField.SetValue(null, redHealthBarImage);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(healthBarImage.gameObject);
        Object.DestroyImmediate(redHealthBarImage.gameObject);
    }

    [Test]
    public void TestUpdateHUD_HealthBarFillAmount()
    {
        // Arrange
        playerController.health = 50f;

        // Act
        typeof(EmoteHUDManager).GetMethod("UpdateHUD", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);

        // Assert
        Assert.AreEqual(0.5f, healthBarImage.fillAmount);
        Assert.AreEqual(0.5f, redHealthBarImage.fillAmount);
    }

    [Test]
    public void TestUpdateHUD_PlayerDeath()
    {
        // Arrange
        playerController.isPlayerDead = true;

        // Act
        typeof(EmoteHUDManager).GetMethod("UpdateHUD", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);

        // Assert
        Assert.AreEqual(0f, healthBarImage.fillAmount);
        Assert.AreEqual(0f, redHealthBarImage.fillAmount);
    }

    [Test]
    public void TestUpdateHUD_SuitColor()
    {
        // Arrange
        playerController.currentSuitID = 2;

        // Act
        typeof(EmoteHUDManager).GetMethod("UpdateHUD", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);

        // Assert
        // Add assertions to verify the health overlay color updates based on suit ID
        var healthOverlayColor = healthBarImage.color;
        var expectedColor = UnlockableSuitPatch.SuitColorCache.GetSuitColor(playerController.currentSuitID, null); // Assuming null for material
        Assert.AreEqual(expectedColor, healthOverlayColor);
    }
}
