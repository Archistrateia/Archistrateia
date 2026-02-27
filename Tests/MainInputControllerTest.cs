using Godot;
using NUnit.Framework;
using Archistrateia;

[TestFixture]
public class MainInputControllerTest
{
    [Test]
    public void HandleInput_Should_Forward_DebugHover_When_Mode_Is_Enabled()
    {
        var viewport = new FakeViewportInputController();
        Vector2? hoverPosition = null;
        var controller = CreateController(
            viewport,
            mousePosition: new Vector2(42, 24),
            isDebugAdjacentModeEnabled: () => true,
            handleDebugAdjacentHover: position => hoverPosition = position);

        controller.HandleInput(new InputEventMouseMotion());

        Assert.AreEqual(new Vector2(42, 24), hoverPosition);
    }

    [Test]
    public void HandleInput_Should_Delegate_MouseWheel_To_Viewport_When_Not_Over_UI()
    {
        var viewport = new FakeViewportInputController { HandleMouseInputResult = true };
        int handledCount = 0;
        var controller = CreateController(
            viewport,
            markInputHandled: () => handledCount++);

        controller.HandleInput(new InputEventMouseButton
        {
            Pressed = true,
            ButtonIndex = MouseButton.WheelUp
        });

        Assert.AreEqual(1, viewport.MouseInputCalls);
        Assert.AreEqual(1, handledCount);
    }

    [Test]
    public void HandleInput_Should_Not_Delegate_MouseWheel_When_Mouse_Is_Over_UI()
    {
        var viewport = new FakeViewportInputController { HandleMouseInputResult = true };
        var controller = CreateController(
            viewport,
            isMouseOverUiControls: _ => true);

        controller.HandleInput(new InputEventMouseButton
        {
            Pressed = true,
            ButtonIndex = MouseButton.WheelDown
        });

        Assert.AreEqual(0, viewport.MouseInputCalls);
    }

    [Test]
    public void HandleInput_Should_Forward_Pan_Gesture_And_Mark_Handled()
    {
        var viewport = new FakeViewportInputController();
        int handledCount = 0;
        var controller = CreateController(
            viewport,
            markInputHandled: () => handledCount++);

        controller.HandleInput(new InputEventPanGesture
        {
            Delta = new Vector2(2, 3),
            Position = new Vector2(10, 20)
        });

        Assert.AreEqual(1, viewport.PanGestureCalls);
        Assert.AreEqual(1, handledCount);
    }

    [Test]
    public void HandleUnhandledInput_Should_Stop_After_First_Handler_Returns_True()
    {
        var controller = CreateController(new FakeViewportInputController());
        int debugCalls = 0;
        int phaseCalls = 0;
        int hoverCalls = 0;

        controller.HandleUnhandledInput(
            new InputEventKey { Pressed = true, Keycode = Key.F3 },
            _ => { debugCalls++; return true; },
            _ => { phaseCalls++; return true; },
            _ => { hoverCalls++; return true; },
            _ => false,
            _ => false);

        Assert.AreEqual(1, debugCalls);
        Assert.AreEqual(0, phaseCalls);
        Assert.AreEqual(0, hoverCalls);
    }

    [Test]
    public void HandleProcess_Should_Update_Viewport_And_Edge_Scrolling()
    {
        var viewport = new FakeViewportInputController { IsScrollingNeededResult = true };
        var controller = CreateController(
            viewport,
            mousePosition: new Vector2(100, 200),
            getGameAreaSize: () => new Vector2(800, 600),
            getGameGridRect: () => new Rect2(0, 60, 800, 540),
            isMouseOverUiControls: _ => false);

        controller.HandleProcess(0.25);

        Assert.AreEqual(1, viewport.UpdateCalls);
        Assert.AreEqual(1, viewport.EdgeScrollCalls);
        Assert.AreEqual(new Vector2(100, 200), viewport.LastEdgeScrollMousePosition);
    }

    private static MainInputController CreateController(
        IViewportInputController viewport,
        Vector2? mousePosition = null,
        System.Func<Vector2> getGameAreaSize = null,
        System.Func<Rect2> getGameGridRect = null,
        System.Func<Vector2, bool> isMouseOverUiControls = null,
        System.Func<Vector2, bool> isMouseOverGameArea = null,
        System.Action<Vector2> debugMousePosition = null,
        System.Func<bool> isDebugAdjacentModeEnabled = null,
        System.Action<Vector2> handleDebugAdjacentHover = null,
        System.Action markInputHandled = null)
    {
        return new MainInputController(
            viewport,
            null,
            () => mousePosition ?? Vector2.Zero,
            getGameAreaSize ?? (() => new Vector2(1200, 800)),
            getGameGridRect ?? (() => new Rect2(0, 60, 1200, 740)),
            isMouseOverUiControls ?? (_ => false),
            isMouseOverGameArea ?? (_ => true),
            debugMousePosition ?? (_ => { }),
            isDebugAdjacentModeEnabled ?? (() => false),
            handleDebugAdjacentHover ?? (_ => { }),
            markInputHandled ?? (() => { }));
    }

    private sealed class FakeViewportInputController : IViewportInputController
    {
        public bool HandleMouseInputResult { get; set; }
        public bool IsScrollingNeededResult { get; set; }
        public int MouseInputCalls { get; private set; }
        public int PanGestureCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public int EdgeScrollCalls { get; private set; }
        public Vector2 LastEdgeScrollMousePosition { get; private set; }
        public float EdgeScrollThreshold => 50.0f;

        public bool HandleMouseInput(InputEventMouseButton mouseEvent, Vector2 gameAreaSize)
        {
            MouseInputCalls++;
            return HandleMouseInputResult;
        }

        public void HandlePanGesture(InputEventPanGesture panGesture, Vector2 gameAreaSize, System.Func<Vector2, bool> isMouseOverUIControls, System.Func<Vector2, bool> isMouseOverGameArea = null)
        {
            PanGestureCalls++;
        }

        public void Update(double delta)
        {
            UpdateCalls++;
        }

        public bool IsScrollingNeeded(Vector2 gameAreaSize)
        {
            return IsScrollingNeededResult;
        }

        public void HandleEdgeScrolling(Vector2 mousePosition, Rect2 gameGridRect, Vector2 gameAreaSize, bool isOverUIControls, double delta)
        {
            EdgeScrollCalls++;
            LastEdgeScrollMousePosition = mousePosition;
        }

        public bool HandleKeyboardInput(InputEventKey keyEvent, Vector2 gameAreaSize)
        {
            return false;
        }

        public void ApplyScrollDelta(Vector2 scrollDelta, Vector2 gameAreaSize)
        {
        }

        public void SetZoom(float zoomFactor)
        {
        }
    }
}
