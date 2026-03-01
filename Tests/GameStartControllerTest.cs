using NUnit.Framework;
using System.Collections.Generic;
using Archistrateia;

[TestFixture]
public class GameStartControllerTest
{
    [Test]
    public void StartGame_Should_Run_Expected_Startup_Sequence()
    {
        float zoom = 1.4f;
        var order = new List<string>();
        var sliderValues = new List<float>();
        var controller = new GameStartController(
            hideStartButton: () => order.Add("hide-start"),
            hideTitleLabel: () => order.Add("hide-title"),
            hideMapGenerationControls: () => order.Add("hide-map-controls"),
            getCurrentZoom: () => zoom,
            setZoomSliderValue: value =>
            {
                sliderValues.Add(value);
                order.Add("set-slider");
            },
            updateZoomLabel: () => order.Add("update-zoom-label"),
            initializeGameManager: () =>
            {
                order.Add("initialize-game-manager");
                zoom = 2.0f;
            },
            regenerateMapWithCurrentZoom: () => order.Add("regenerate-map"),
            updateUIPositions: () => order.Add("update-ui-positions"),
            initializeDebugTools: () => order.Add("init-debug-tools"));

        controller.StartGame();

        Assert.IsTrue(controller.IsGameStarted);
        Assert.AreEqual(
            new[]
            {
                "hide-start",
                "hide-title",
                "hide-map-controls",
                "set-slider",
                "update-zoom-label",
                "initialize-game-manager",
                "set-slider",
                "update-zoom-label",
                "regenerate-map",
                "update-ui-positions",
                "init-debug-tools"
            },
            order);
        Assert.AreEqual(new[] { 1.4f, 2.0f }, sliderValues);
    }

    [Test]
    public void StartGame_Should_Be_Idempotent_After_First_Start()
    {
        int initializeCalls = 0;
        var controller = new GameStartController(
            hideStartButton: () => { },
            hideTitleLabel: () => { },
            hideMapGenerationControls: () => { },
            getCurrentZoom: () => 1.0f,
            setZoomSliderValue: _ => { },
            updateZoomLabel: () => { },
            initializeGameManager: () => initializeCalls++,
            regenerateMapWithCurrentZoom: () => { },
            updateUIPositions: () => { },
            initializeDebugTools: () => { });

        controller.StartGame();
        controller.StartGame();

        Assert.AreEqual(1, initializeCalls, "Second start should no-op once game is already started.");
    }
}
