using Godot;
using NUnit.Framework;
using System.Linq;

[TestFixture]
public partial class MapTypeConfigurationTest : Node
{
    [Test]
    public void Should_Have_Configuration_For_All_Map_Types()
    {
        var allMapTypes = System.Enum.GetValues<MapType>();
        
        foreach (var mapType in allMapTypes)
        {
            var config = MapTypeConfiguration.GetConfig(mapType);
            Assert.IsNotNull(config, $"Configuration missing for map type: {mapType}");
            Assert.IsNotEmpty(config.Name, $"Name is empty for map type: {mapType}");
            Assert.IsNotEmpty(config.Description, $"Description is empty for map type: {mapType}");
        }
    }
    
    [Test]
    public void Should_Have_Unique_Names_For_All_Map_Types()
    {
        var allMapTypes = System.Enum.GetValues<MapType>();
        var names = allMapTypes.Select(mt => MapTypeConfiguration.GetConfig(mt).Name).ToList();
        
        var duplicates = names.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key);
        Assert.IsEmpty(duplicates, $"Duplicate map type names found: {string.Join(", ", duplicates)}");
    }
    
    [Test]
    public void Should_Have_Valid_Parameter_Ranges()
    {
        var allMapTypes = System.Enum.GetValues<MapType>();
        
        foreach (var mapType in allMapTypes)
        {
            var config = MapTypeConfiguration.GetConfig(mapType);
            
            Assert.That(config.NoiseFrequency, Is.GreaterThan(0).And.LessThan(1), 
                $"Invalid noise frequency for {mapType}: {config.NoiseFrequency}");
            
            Assert.That(config.ElevationMultiplier, Is.GreaterThan(0).And.LessThan(5), 
                $"Invalid elevation multiplier for {mapType}: {config.ElevationMultiplier}");
            
            Assert.That(config.WaterFlowIntensity, Is.GreaterThan(0).And.LessThan(5), 
                $"Invalid water flow intensity for {mapType}: {config.WaterFlowIntensity}");
            
            Assert.That(config.RiverGenerationRate, Is.GreaterThanOrEqualTo(0).And.LessThanOrEqualTo(1), 
                $"Invalid river generation rate for {mapType}: {config.RiverGenerationRate}");
            
            Assert.That(config.SeaLevelAdjustment, Is.GreaterThanOrEqualTo(-20).And.LessThanOrEqualTo(20), 
                $"Invalid sea level adjustment for {mapType}: {config.SeaLevelAdjustment}");
        }
    }
    
    [Test]
    public void Should_Have_Terrain_Bias_For_All_Configurations()
    {
        var allMapTypes = System.Enum.GetValues<MapType>();
        
        foreach (var mapType in allMapTypes)
        {
            var config = MapTypeConfiguration.GetConfig(mapType);
            Assert.IsNotNull(config.TerrainBias, $"Terrain bias is null for {mapType}");
            Assert.That(config.TerrainBias.Count, Is.GreaterThan(0), $"No terrain bias defined for {mapType}");
            
            foreach (var bias in config.TerrainBias.Values)
            {
                Assert.That(bias, Is.GreaterThan(0), $"Invalid terrain bias value for {mapType}: {bias}");
            }
        }
    }
    
    [Test]
    public void Continental_Should_Have_Balanced_Configuration()
    {
        var config = MapTypeConfiguration.GetConfig(MapType.Continental);
        
        Assert.AreEqual("Continental", config.Name);
        Assert.AreEqual(0, config.SeaLevelAdjustment);
        Assert.That(config.TerrainBias[TerrainType.Grassland], Is.GreaterThan(config.TerrainBias[TerrainType.Desert]));
    }
    
    [Test]
    public void Archipelago_Should_Favor_Water_And_Coastal_Terrain()
    {
        var config = MapTypeConfiguration.GetConfig(MapType.Archipelago);
        
        Assert.AreEqual("Archipelago", config.Name);
        Assert.That(config.SeaLevelAdjustment, Is.GreaterThan(0), "Archipelago should have higher sea level");
        Assert.That(config.TerrainBias[TerrainType.Water], Is.GreaterThan(1.5f), "Archipelago should favor water");
        Assert.That(config.TerrainBias[TerrainType.Shoreline], Is.GreaterThan(1.5f), "Archipelago should favor shoreline");
    }
    
    [Test]
    public void Highland_Should_Favor_Mountains_And_Hills()
    {
        var config = MapTypeConfiguration.GetConfig(MapType.Highland);
        
        Assert.AreEqual("Highland", config.Name);
        Assert.That(config.ElevationMultiplier, Is.GreaterThan(1.0f), "Highland should have higher elevation multiplier");
        Assert.That(config.TerrainBias[TerrainType.Mountain], Is.GreaterThan(1.5f), "Highland should favor mountains");
        Assert.That(config.TerrainBias[TerrainType.Hill], Is.GreaterThan(1.0f), "Highland should favor hills");
    }
    
    [Test]
    public void Desert_Should_Minimize_Water_And_Favor_Desert_Terrain()
    {
        var config = MapTypeConfiguration.GetConfig(MapType.Desert);
        
        Assert.AreEqual("Desert", config.Name);
        Assert.That(config.WaterFlowIntensity, Is.LessThan(0.5f), "Desert should have low water flow");
        Assert.That(config.RiverGenerationRate, Is.LessThan(0.2f), "Desert should have few rivers");
        Assert.That(config.TerrainBias[TerrainType.Desert], Is.GreaterThan(2.0f), "Desert should heavily favor desert terrain");
    }
    
    [Test]
    public void Wetlands_Should_Favor_Water_Features()
    {
        var config = MapTypeConfiguration.GetConfig(MapType.Wetlands);
        
        Assert.AreEqual("Wetlands", config.Name);
        Assert.That(config.WaterFlowIntensity, Is.GreaterThan(1.5f), "Wetlands should have high water flow");
        Assert.That(config.RiverGenerationRate, Is.GreaterThan(0.5f), "Wetlands should have many rivers");
        Assert.That(config.TerrainBias[TerrainType.River], Is.GreaterThan(1.5f), "Wetlands should favor rivers");
    }
    
    [Test]
    public void Volcanic_Should_Have_Extreme_Elevation_Changes()
    {
        var config = MapTypeConfiguration.GetConfig(MapType.Volcanic);
        
        Assert.AreEqual("Volcanic", config.Name);
        Assert.That(config.ElevationMultiplier, Is.GreaterThan(1.5f), "Volcanic should have high elevation multiplier");
        Assert.That(config.NoiseFrequency, Is.GreaterThan(0.1f), "Volcanic should have high noise frequency for dramatic terrain");
    }
    
    [Test]
    public void Should_Return_Default_Configuration_For_Invalid_Map_Type()
    {
        var invalidMapType = (MapType)999;
        var config = MapTypeConfiguration.GetConfig(invalidMapType);
        
        Assert.IsNotNull(config);
        Assert.AreEqual("Continental", config.Name, "Should return Continental as default");
    }
}
