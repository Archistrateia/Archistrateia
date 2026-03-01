using Godot;
using NUnit.Framework;
using Archistrateia;

[TestFixture]
public class MainMapSetupControllerTest
{
    [Test]
    public void GenerateMap_Should_Update_MapContainer_And_ZoomUI()
    {
        Node2D currentContainer = null;
        float sliderValue = 0f;
        int zoomLabelCalls = 0;
        var generatedContainer = new Node2D();
        var controller = CreateController(
            isGameStarted: () => false,
            getCurrentMapType: () => MapType.Continental,
            setCurrentMapType: _ => { },
            getMapContainer: () => currentContainer,
            setMapContainer: container => currentContainer = container,
            generatePreviewMap: (_, _) => generatedContainer,
            getZoomFactor: () => 1.7f,
            setZoomSliderValue: zoom => sliderValue = zoom,
            updateZoomLabel: () => zoomLabelCalls++,
            setMapTypeDescription: _ => { });

        controller.GenerateMap();

        Assert.AreSame(generatedContainer, currentContainer);
        Assert.AreEqual(1.7f, sliderValue, 0.001f);
        Assert.AreEqual(1, zoomLabelCalls);
    }

    [Test]
    public void OnMapTypeSelected_Should_Update_Type_Description_And_Regenerate_When_Not_Started()
    {
        MapType selectedType = MapType.Continental;
        string description = string.Empty;
        int generateCalls = 0;
        var controller = CreateController(
            isGameStarted: () => false,
            getCurrentMapType: () => selectedType,
            setCurrentMapType: mapType => selectedType = mapType,
            getMapContainer: () => null,
            setMapContainer: _ => { },
            generatePreviewMap: (_, _) =>
            {
                generateCalls++;
                return new Node2D();
            },
            getZoomFactor: () => 1.0f,
            setZoomSliderValue: _ => { },
            updateZoomLabel: () => { },
            setMapTypeDescription: text => description = text);

        controller.OnMapTypeSelected((long)MapType.Desert);

        Assert.AreEqual(MapType.Desert, selectedType);
        Assert.AreEqual(MapTypeConfiguration.GetConfig(MapType.Desert).Description, description);
        Assert.AreEqual(1, generateCalls);
    }

    [Test]
    public void OnRegenerateMapPressed_Should_Not_Regenerate_When_Game_Has_Started()
    {
        int generateCalls = 0;
        var controller = CreateController(
            isGameStarted: () => true,
            getCurrentMapType: () => MapType.Continental,
            setCurrentMapType: _ => { },
            getMapContainer: () => null,
            setMapContainer: _ => { },
            generatePreviewMap: (_, _) =>
            {
                generateCalls++;
                return new Node2D();
            },
            getZoomFactor: () => 1.0f,
            setZoomSliderValue: _ => { },
            updateZoomLabel: () => { },
            setMapTypeDescription: _ => { });

        controller.OnRegenerateMapPressed();

        Assert.AreEqual(0, generateCalls);
    }

    private static MainMapSetupController CreateController(
        System.Func<bool> isGameStarted,
        System.Func<MapType> getCurrentMapType,
        System.Action<MapType> setCurrentMapType,
        System.Func<Node2D> getMapContainer,
        System.Action<Node2D> setMapContainer,
        System.Func<Node2D, MapType, Node2D> generatePreviewMap,
        System.Func<float> getZoomFactor,
        System.Action<float> setZoomSliderValue,
        System.Action updateZoomLabel,
        System.Action<string> setMapTypeDescription)
    {
        return new MainMapSetupController(
            isGameStarted,
            getCurrentMapType,
            setCurrentMapType,
            getMapContainer,
            setMapContainer,
            generatePreviewMap,
            getZoomFactor,
            setZoomSliderValue,
            updateZoomLabel,
            setMapTypeDescription);
    }
}
