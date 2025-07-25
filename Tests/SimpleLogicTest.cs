using NUnit.Framework;
using Godot;

[TestFixture]
public class SimpleLogicTest
{
    [Test]
    public void Should_Create_Nakhtu_Unit()
    {
        var nakhtu = new Nakhtu();
        
        Assert.IsNotNull(nakhtu, "Nakhtu should not be null");
        Assert.AreEqual("Nakhtu", nakhtu.Name, "Name should be Nakhtu");
        Assert.AreEqual(3, nakhtu.Attack, "Attack should be 3");
        Assert.AreEqual(2, nakhtu.Defense, "Defense should be 2");
    }

    [Test]
    public void Should_Create_Vector2()
    {
        var position = new Vector2(100, 100);
        
        Assert.AreEqual(100, position.X);
        Assert.AreEqual(100, position.Y);
    }

    [Test]
    public void Should_Create_Color()
    {
        var color = new Color(1.0f, 0.0f, 0.0f);
        
        Assert.AreEqual(1.0f, color.R, 0.01f);
        Assert.AreEqual(0.0f, color.G, 0.01f);
        Assert.AreEqual(0.0f, color.B, 0.01f);
    }

    [Test]
    public void Should_Create_UnitDisplayData()
    {
        var nakhtu = new Nakhtu();
        var position = new Vector2(100, 100);
        var color = new Color(1.0f, 0.0f, 0.0f);
        
        var displayData = new UnitDisplayData(nakhtu, position, color);
        
        Assert.IsNotNull(displayData);
        Assert.AreEqual(nakhtu, displayData.LogicalUnit);
    }
} 