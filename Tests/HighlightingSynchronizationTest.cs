using NUnit.Framework;
using Godot;
using System.Collections.Generic;

[TestFixture]
public class HighlightingSynchronizationTest
{
    [Test]
    public void Should_Immediately_Clear_Visual_Overlays()
    {
        // Test that visual tile overlays are immediately removed
        var visualTile = new VisualHexTile();
        visualTile.Initialize(new Vector2I(0, 0), TerrainType.Shoreline, Colors.Blue, Vector2.Zero);
        
        GD.Print("=== TESTING IMMEDIATE OVERLAY REMOVAL ===");
        
        // Add gray overlay
        visualTile.SetGrayed(true);
        var grayOverlay = visualTile.GetNodeOrNull("GrayOverlay");
        Assert.IsNotNull(grayOverlay, "Gray overlay should be added");
        
        // Remove gray overlay - should be immediate
        visualTile.SetGrayed(false);
        var removedGrayOverlay = visualTile.GetNodeOrNull("GrayOverlay");
        Assert.IsNull(removedGrayOverlay, "Gray overlay should be immediately removed from scene tree");
        
        // Add bright overlay
        visualTile.SetBrightened(true);
        var brightOverlay = visualTile.GetNodeOrNull("BrightOverlay");
        Assert.IsNotNull(brightOverlay, "Bright overlay should be added");
        
        // Remove bright overlay - should be immediate
        visualTile.SetBrightened(false);
        var removedBrightOverlay = visualTile.GetNodeOrNull("BrightOverlay");
        Assert.IsNull(removedBrightOverlay, "Bright overlay should be immediately removed from scene tree");
        
        GD.Print("✅ Visual overlays are immediately removed - highlighting synchronization fixed!");
        
        visualTile.QueueFree();
    }
    
    [Test]
    public void Should_Handle_Multiple_Overlay_State_Changes()
    {
        var visualTile = new VisualHexTile();
        visualTile.Initialize(new Vector2I(1, 1), TerrainType.Desert, Colors.Brown, Vector2.Zero);
        
        GD.Print("=== TESTING MULTIPLE OVERLAY STATE CHANGES ===");
        
        // Rapid state changes - simulating what happens during highlighting updates
        visualTile.SetGrayed(true);
        Assert.IsNotNull(visualTile.GetNodeOrNull("GrayOverlay"), "Should have gray overlay");
        
        visualTile.SetBrightened(true);
        Assert.IsNotNull(visualTile.GetNodeOrNull("BrightOverlay"), "Should have bright overlay");
        
        // Clear all - should remove both immediately
        visualTile.SetGrayed(false);
        visualTile.SetBrightened(false);
        
        Assert.IsNull(visualTile.GetNodeOrNull("GrayOverlay"), "Gray overlay should be gone");
        Assert.IsNull(visualTile.GetNodeOrNull("BrightOverlay"), "Bright overlay should be gone");
        
        GD.Print("✅ Multiple overlay state changes handled correctly!");
        
        visualTile.QueueFree();
    }
} 