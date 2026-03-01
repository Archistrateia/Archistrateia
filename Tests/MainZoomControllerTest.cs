using Godot;
using NUnit.Framework;
using Archistrateia;

[TestFixture]
public class MainZoomControllerTest
{
    [Test]
    public void OnZoomSliderChanged_Should_Update_Viewport_And_UI()
    {
        float zoom = 0f;
        int titleCalls = 0;
        int uiCalls = 0;
        var slider = new HSlider();
        var controller = new MainZoomController(
            () => slider,
            value => zoom = value,
            () => titleCalls++,
            () => uiCalls++);

        controller.OnZoomSliderChanged(1.8);

        Assert.AreEqual(1.8f, zoom, 0.001f);
        Assert.AreEqual(1, titleCalls);
        Assert.AreEqual(1, uiCalls);
    }

    [Test]
    public void OnZoomSliderInput_Should_Apply_Calculated_Value_And_Invoke_Callback()
    {
        var slider = new HSlider
        {
            MinValue = 0.1,
            MaxValue = 3.0,
            Size = new Vector2(100, 20),
            Value = 1.0
        };
        double? appliedValue = null;
        var controller = new MainZoomController(
            () => slider,
            _ => { },
            () => { },
            () => { });

        controller.OnZoomSliderInput(
            new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Left },
            value => appliedValue = value);

        Assert.IsNotNull(appliedValue, "Mouse click should route through fallback zoom handling.");
        Assert.AreEqual(slider.MinValue, slider.Value, 0.001, "Without viewport mouse context, local position defaults to start of slider.");
    }
}
