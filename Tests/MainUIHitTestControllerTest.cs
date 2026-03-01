using Godot;
using NUnit.Framework;
using Archistrateia;

[TestFixture]
public class MainUIHitTestControllerTest
{
    [Test]
    public void IsMouseOverUIControls_Should_Return_True_When_Mouse_Is_Over_BuyButton()
    {
        var buyButton = new Button
        {
            GlobalPosition = new Vector2(100, 100),
            Size = new Vector2(80, 30)
        };
        var controller = CreateController(
            getPurchaseBuyButton: () => buyButton);

        bool overUi = controller.IsMouseOverUIControls(new Vector2(110, 110));

        Assert.IsTrue(overUi);
    }

    [Test]
    public void IsMouseOverGameArea_Should_Be_Inverse_Of_UI_Check()
    {
        var nextPhaseButton = new Button
        {
            GlobalPosition = new Vector2(20, 20),
            Size = new Vector2(100, 40)
        };
        var controller = CreateController(
            getNextPhaseButton: () => nextPhaseButton);

        Assert.IsFalse(controller.IsMouseOverGameArea(new Vector2(30, 30)));
        Assert.IsTrue(controller.IsMouseOverGameArea(new Vector2(400, 400)));
    }

    [Test]
    public void IsMouseWithinGameArea_Should_Fallback_True_When_Area_Is_Missing()
    {
        var controller = CreateController();

        Assert.IsTrue(controller.IsMouseWithinGameArea(new Vector2(-100, -100)));
    }

    [Test]
    public void IsMouseWithinGameArea_Should_Use_GameArea_Bounds_When_Available()
    {
        var gameArea = new Panel
        {
            GlobalPosition = new Vector2(10, 10),
            Size = new Vector2(100, 60)
        };
        var controller = CreateController(
            getGameArea: () => gameArea);

        Assert.IsTrue(controller.IsMouseWithinGameArea(new Vector2(20, 20)));
        Assert.IsFalse(controller.IsMouseWithinGameArea(new Vector2(200, 200)));
    }

    private static MainUIHitTestController CreateController(
        System.Func<HSlider> getZoomSlider = null,
        System.Func<Button> getNextPhaseButton = null,
        System.Func<Button> getDebugAdjacentButton = null,
        System.Func<OptionButton> getPurchaseUnitSelector = null,
        System.Func<Button> getPurchaseBuyButton = null,
        System.Func<Button> getPurchaseCancelButton = null,
        System.Func<Control> getGameArea = null)
    {
        return new MainUIHitTestController(
            getZoomSlider ?? (() => null),
            getNextPhaseButton ?? (() => null),
            getDebugAdjacentButton ?? (() => null),
            getPurchaseUnitSelector ?? (() => null),
            getPurchaseBuyButton ?? (() => null),
            getPurchaseCancelButton ?? (() => null),
            getGameArea ?? (() => null));
    }
}
