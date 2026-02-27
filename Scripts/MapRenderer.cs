using Godot;
using System.Collections.Generic;

namespace Archistrateia
{
    public partial class MapRenderer : Node2D
    {
        private enum HoverInfoKind
        {
            None,
            Tile,
            Unit
        }

        public GameManager GameManager { get; private set; }
        private List<VisualUnit> _visualUnits = new List<VisualUnit>();
        private Dictionary<Vector2I, VisualHexTile> _visualTiles = new Dictionary<Vector2I, VisualHexTile>();
        private IMapInteractionController _interactionController = new MapInteractionController();
        private Player _currentPlayer;
        private GamePhase _currentPhase;
        private InformationPanel _informationPanel;
        private TileUnitCoordinator _tileUnitCoordinator;
        private Node2D _mapContainer;
        private const float HOVER_INFO_SHOW_DELAY = 0.0f;
        private const float HOVER_INFO_HIDE_GRACE = 0.08f;
        private HoverInfoKind _pendingHoverKind = HoverInfoKind.None;
        private VisualHexTile _pendingHoverTile;
        private VisualUnit _pendingHoverUnit;
        private float _pendingHoverElapsed = 0.0f;
        private HoverInfoKind _activeHoverKind = HoverInfoKind.None;
        private VisualHexTile _activeHoverTile;
        private VisualUnit _activeHoverUnit;
        private float _hideGraceRemaining = 0.0f;
        private bool _hoverInfoModeEnabled = false;
        private readonly HashSet<Vector2I> _purchasePlacementTiles = new();

        [Signal]
        public delegate void PurchaseTileClickedEventHandler(Vector2I tilePosition);

        public void Initialize(GameManager gameManager, TileUnitCoordinator tileUnitCoordinator = null, Node2D mapContainer = null)
        {
            GameManager = gameManager;
            _tileUnitCoordinator = tileUnitCoordinator ?? new TileUnitCoordinator();
            _mapContainer = mapContainer;
            _interactionController = new MapInteractionController(
                new PlayerInteractionLogic(),
                new MovementCoordinator(GameManager?.MovementSystem));
            ZIndex = 5; // Ensure MapRenderer is above tiles but below units
            CreateInformationPanel();
        }

        private void CreateInformationPanel()
        {
            _informationPanel = new InformationPanel();
            _informationPanel.Name = "InformationPanel";
            _informationPanel.ZIndex = 2000;
            GetTree().CurrentScene.AddChild(_informationPanel);
        }

        public void SetCurrentPlayer(Player player)
        {
            _currentPlayer = player;
        }

        public void SetCurrentPhase(GamePhase phase)
        {
            ApplyPhaseChange(phase);
        }

        public GamePhase GetCurrentPhase()
        {
            return _currentPhase;
        }
        
        public void ClearAllTiles()
        {
            GD.Print("🧹 MapRenderer: Clearing all tile references");
            _visualTiles.Clear();
            
            // Also clear any highlights or selections that reference old tiles
            ClearAllHighlights();
            DeselectAll();
            ClearAllUnitMovementDisplays();
        }
        
        public void ClearAllUnits()
        {
            GD.Print("🧹 MapRenderer: Clearing all visual units");
            
            // Disconnect signals and free visual units
            foreach (var visualUnit in _visualUnits)
            {
                if (visualUnit.IsConnected(VisualUnit.SignalName.UnitClicked, new Callable(this, MethodName.OnUnitClicked)))
                {
                    visualUnit.UnitClicked -= OnUnitClicked;
                }
                if (visualUnit.IsConnected(VisualUnit.SignalName.UnitHovered, new Callable(this, MethodName.OnUnitHovered)))
                {
                    visualUnit.UnitHovered -= OnUnitHovered;
                }
                if (visualUnit.IsConnected(VisualUnit.SignalName.UnitUnhovered, new Callable(this, MethodName.OnUnitUnhovered)))
                {
                    visualUnit.UnitUnhovered -= OnUnitUnhovered;
                }
                visualUnit.QueueFree();
            }
            
            _visualUnits.Clear();
            
            // Clear any unit-related selections
            _interactionController.ClearSelection();
        }

        public Player GetCurrentPlayer()
        {
            return _currentPlayer;
        }

        public List<VisualUnit> GetVisualUnits()
        {
            return _visualUnits;
        }

        public VisualUnit CreateVisualUnit(Unit logicalUnit, Vector2 position, Color color)
        {
            var visualUnit = new VisualUnit();
            
            // Add units to the same container as tiles to ensure consistent coordinate system
            var mapContainer = GetMapContainer();
            if (mapContainer != null)
            {
                mapContainer.AddChild(visualUnit);
                GD.Print($"🔧 CONTAINER: Adding unit to map container");
            }
            else
            {
                AddChild(visualUnit);
                GD.Print($"⚠️ CONTAINER: No map container found, adding to MapRenderer");
            }
            
            visualUnit.Initialize(logicalUnit, position, color);
            visualUnit.SetMapRenderer(this); // Set the MapRenderer reference
            visualUnit.UnitClicked += OnUnitClicked;
            visualUnit.UnitHovered += OnUnitHovered;
            visualUnit.UnitUnhovered += OnUnitUnhovered;
            
            _visualUnits.Add(visualUnit);
            
            // Find where this unit is logically positioned
            var logicalPosition = FindUnitPosition(logicalUnit);
            GD.Print($"🔥 UNIT CREATE Debug: Unit({logicalUnit.Name}) LogicalGrid({logicalPosition?.X ?? -1},{logicalPosition?.Y ?? -1}) VisualWorld({position.X:F1},{position.Y:F1})");
            
            return visualUnit;
        }
        
        private Node2D GetMapContainer()
        {
            return _mapContainer;
        }

        public void OnUnitClicked(VisualUnit clickedUnit)
        {
            var gridPos = FindUnitPosition(clickedUnit.LogicalUnit) ?? new Vector2I(-1, -1);
            GD.Print($"🖱️ UNIT CLICK Debug: Unit({clickedUnit.LogicalUnit.Name}) at Grid({gridPos.X},{gridPos.Y}) World({clickedUnit.Position.X:F1},{clickedUnit.Position.Y:F1})");
            
            GD.Print($"🎯 MapRenderer: Unit {clickedUnit.LogicalUnit.Name} clicked!");
            GD.Print($"   Current Player: {_currentPlayer?.Name ?? "NULL"}");
            GD.Print($"   Current Phase: {_currentPhase}");
            GD.Print($"   Unit Owner: {GetUnitOwner(clickedUnit.LogicalUnit)?.Name ?? "UNKNOWN"}");
            
            var selectionResult = _interactionController.HandleUnitClicked(_currentPlayer, _currentPhase, clickedUnit.LogicalUnit);
            if (selectionResult.WasSelected)
            {
                GD.Print($"✅ Successfully selected {clickedUnit.LogicalUnit.Name}");
                UpdateVisualSelection();
                ShowValidMovementDestinations(selectionResult.SelectedUnit);
            }
            else
            {
                GD.Print($"❌ Cannot select {clickedUnit.LogicalUnit.Name} - invalid selection");
            }
        }

        private Player GetUnitOwner(Unit unit)
        {
            if (GameManager?.Players != null)
            {
                foreach (var player in GameManager.Players)
                {
                    if (player.Units.Contains(unit))
                    {
                        return player;
                    }
                }
            }
            return null;
        }

        private void UpdateVisualSelection()
        {
            var selectedUnit = _interactionController.GetSelectedUnit();
            
            // Update visual state for all units
            foreach (var visualUnit in _visualUnits)
            {
                var isSelected = visualUnit.LogicalUnit == selectedUnit;
                visualUnit.SetSelected(isSelected);
            }
        }

        public static void UpdateVisualUnitPosition(VisualUnit visualUnit, Vector2 newPosition)
        {
            visualUnit.UpdatePosition(newPosition);
        }

        public void RemoveVisualUnit(VisualUnit visualUnit)
        {
            if (_visualUnits.Remove(visualUnit))
            {
                if (visualUnit.IsConnected(VisualUnit.SignalName.UnitClicked, new Callable(this, MethodName.OnUnitClicked)))
                {
                    visualUnit.UnitClicked -= OnUnitClicked;
                }
                if (visualUnit.IsConnected(VisualUnit.SignalName.UnitHovered, new Callable(this, MethodName.OnUnitHovered)))
                {
                    visualUnit.UnitHovered -= OnUnitHovered;
                }
                if (visualUnit.IsConnected(VisualUnit.SignalName.UnitUnhovered, new Callable(this, MethodName.OnUnitUnhovered)))
                {
                    visualUnit.UnitUnhovered -= OnUnitUnhovered;
                }
                visualUnit.QueueFree();
            }
        }

        public Unit GetSelectedUnit()
        {
            return _interactionController.GetSelectedUnit();
        }

        public void DeselectAll()
        {
            _interactionController.ClearSelection();
            UpdateVisualSelection();
            ClearAllHighlights();
            ClearAllUnitMovementDisplays();
        }

        public void AddVisualTile(VisualHexTile visualTile)
        {
            _visualTiles[visualTile.GridPosition] = visualTile;
            visualTile.TileClicked += OnTileClicked;
            visualTile.TileHovered += OnTileHovered;
            visualTile.TileUnhovered += OnTileUnhovered;
        }

        private void OnTileClicked(VisualHexTile clickedTile)
        {
            GD.Print($"🖱️ CLICK Debug: Tile clicked at Grid({clickedTile.GridPosition.X},{clickedTile.GridPosition.Y}) World({clickedTile.Position.X:F1},{clickedTile.Position.Y:F1})");

            var result = _interactionController.HandleTileClicked(
                _currentPhase,
                clickedTile.GridPosition,
                GameManager?.GameMap,
                FindUnitPosition);

            if (result.Kind == TileInteractionKind.PurchaseTileSelected)
            {
                EmitSignal(SignalName.PurchaseTileClicked, clickedTile.GridPosition);
                return;
            }

            if (result.Kind == TileInteractionKind.Ignored)
            {
                GD.Print($"   ❌ Tile click ignored during {_currentPhase}");
                return;
            }

            if (result.Kind == TileInteractionKind.DeselectRequired)
            {
                GD.Print($"❌ Move failed: {result.ErrorMessage} - Deselecting unit");
                DeselectAll();
                return;
            }

            GD.Print($"✅ MOVEMENT SUCCESS: Unit moved to Grid({result.NewPosition.X},{result.NewPosition.Y})");

            var visualUnit = FindVisualUnit(result.SelectedUnit);
            if (visualUnit != null && _visualTiles.TryGetValue(result.NewPosition, out var targetTile))
            {
                var newWorldPosition = targetTile.Position;
                visualUnit.UpdatePosition(newWorldPosition);
                visualUnit.RefreshMovementDisplay();
            }

            UpdateTileOccupationStatus();

            if (result.SelectedUnit.CurrentMovementPoints > 0)
            {
                ShowValidMovementDestinations(result.SelectedUnit);
            }
            else
            {
                DeselectAll();
            }
        }

        private void OnTileHovered(VisualHexTile hoveredTile)
        {
            if (!_hoverInfoModeEnabled) return;
            SetPendingHover(hoveredTile);
        }

        private void OnTileUnhovered(VisualHexTile unhoveredTile)
        {
            if (!_hoverInfoModeEnabled) return;
            if (_pendingHoverKind == HoverInfoKind.Tile && _pendingHoverTile == unhoveredTile)
            {
                ClearPendingHover();
                BeginHideGrace();
            }

            if (_activeHoverKind == HoverInfoKind.Tile && _activeHoverTile == unhoveredTile)
            {
                BeginHideGrace();
            }
        }

        private void OnUnitHovered(VisualUnit hoveredUnit)
        {
            if (!_hoverInfoModeEnabled) return;
            SetPendingHover(hoveredUnit);
        }

        private void OnUnitUnhovered(VisualUnit unhoveredUnit)
        {
            if (!_hoverInfoModeEnabled) return;
            if (_pendingHoverKind == HoverInfoKind.Unit && _pendingHoverUnit == unhoveredUnit)
            {
                ClearPendingHover();
                BeginHideGrace();
            }

            if (_activeHoverKind == HoverInfoKind.Unit && _activeHoverUnit == unhoveredUnit)
            {
                BeginHideGrace();
            }
        }

        public override void _Process(double delta)
        {
            if (_informationPanel == null)
            {
                return;
            }

            if (!_hoverInfoModeEnabled)
            {
                ClearPendingHover();
                _hideGraceRemaining = 0.0f;
                HideInformationPanel();
                return;
            }

            var mousePos = GetViewport().GetMousePosition();
            if (!ShouldAllowHoverInfo(mousePos))
            {
                ClearPendingHover();
                _hideGraceRemaining = 0.0f;
                HideInformationPanel();
                return;
            }

            // Keep tooltip anchored near the cursor while hovered.
            if (_activeHoverKind != HoverInfoKind.None && _informationPanel.Visible)
            {
                _informationPanel.UpdatePosition(mousePos);
            }

            if (_pendingHoverKind != HoverInfoKind.None)
            {
                _pendingHoverElapsed += (float)delta;
                if (_pendingHoverElapsed >= HOVER_INFO_SHOW_DELAY)
                {
                    ShowPendingHoverInfo();
                    _hideGraceRemaining = 0.0f;
                }
            }

            if (_hideGraceRemaining > 0.0f)
            {
                _hideGraceRemaining -= (float)delta;
                if (_hideGraceRemaining <= 0.0f && _pendingHoverKind == HoverInfoKind.None)
                {
                    HideInformationPanel();
                }
            }
        }

        private void SetPendingHover(VisualHexTile tile)
        {
            if (_pendingHoverKind == HoverInfoKind.Tile && _pendingHoverTile == tile)
            {
                return;
            }

            _pendingHoverKind = HoverInfoKind.Tile;
            _pendingHoverTile = tile;
            _pendingHoverUnit = null;
            _pendingHoverElapsed = 0.0f;
            _hideGraceRemaining = 0.0f;
            ShowPendingHoverInfo();
        }

        private void SetPendingHover(VisualUnit unit)
        {
            if (_pendingHoverKind == HoverInfoKind.Unit && _pendingHoverUnit == unit)
            {
                return;
            }

            _pendingHoverKind = HoverInfoKind.Unit;
            _pendingHoverUnit = unit;
            _pendingHoverTile = null;
            _pendingHoverElapsed = 0.0f;
            _hideGraceRemaining = 0.0f;
            ShowPendingHoverInfo();
        }

        private void ClearPendingHover()
        {
            _pendingHoverKind = HoverInfoKind.None;
            _pendingHoverTile = null;
            _pendingHoverUnit = null;
            _pendingHoverElapsed = 0.0f;
        }

        private void BeginHideGrace()
        {
            _hideGraceRemaining = HOVER_INFO_HIDE_GRACE;
        }

        private void ShowPendingHoverInfo()
        {
            var gameMap = GameManager?.GameMap;
            if (gameMap == null)
            {
                ClearPendingHover();
                HideInformationPanel();
                return;
            }

            var mousePos = GetViewport().GetMousePosition();

            if (_pendingHoverKind == HoverInfoKind.Unit && _pendingHoverUnit != null)
            {
                var unitPos = FindUnitPosition(_pendingHoverUnit.LogicalUnit);
                if (unitPos != null && gameMap.ContainsKey(unitPos.Value))
                {
                    var tile = gameMap[unitPos.Value];
                    _informationPanel.ShowUnitInfo(_pendingHoverUnit.LogicalUnit, tile.TerrainType, tile.MovementCost, mousePos);
                    _activeHoverKind = HoverInfoKind.Unit;
                    _activeHoverUnit = _pendingHoverUnit;
                    _activeHoverTile = null;
                    ClearPendingHover();
                    return;
                }
            }

            if (_pendingHoverKind == HoverInfoKind.Tile && _pendingHoverTile != null && gameMap.ContainsKey(_pendingHoverTile.GridPosition))
            {
                var tile = gameMap[_pendingHoverTile.GridPosition];
                _informationPanel.ShowTerrainInfo(tile.TerrainType, tile.MovementCost, mousePos);
                _activeHoverKind = HoverInfoKind.Tile;
                _activeHoverTile = _pendingHoverTile;
                _activeHoverUnit = null;
                ClearPendingHover();
                return;
            }

            ClearPendingHover();
            HideInformationPanel();
        }

        private void HideInformationPanel()
        {
            _activeHoverKind = HoverInfoKind.None;
            _activeHoverTile = null;
            _activeHoverUnit = null;
            _informationPanel?.Hide();
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseMotion mouseMotion)
            {
                UpdateHoverTargetFromMouse(mouseMotion.GlobalPosition);
            }
        }

        private bool ShouldAllowHoverInfo(Vector2 mousePosition)
        {
            if (!_hoverInfoModeEnabled)
            {
                return false;
            }

            var mainScene = GetTree().CurrentScene as Main;
            if (mainScene == null)
            {
                return true;
            }

            return mainScene.IsMouseWithinGameArea(mousePosition) &&
                   !mainScene.IsMouseOverUIControls(mousePosition);
        }

        private void UpdateHoverTargetFromMouse(Vector2 mousePosition)
        {
            if (!ShouldAllowHoverInfo(mousePosition))
            {
                ClearPendingHover();
                BeginHideGrace();
                return;
            }

            var hoveredUnit = GetHoveredUnitAt(mousePosition);
            if (hoveredUnit != null)
            {
                SetPendingHover(hoveredUnit);
                return;
            }

            var hoveredTile = GetHoveredTileAt(mousePosition);
            if (hoveredTile != null)
            {
                SetPendingHover(hoveredTile);
                return;
            }

            ClearPendingHover();
            BeginHideGrace();
        }

        private VisualUnit GetHoveredUnitAt(Vector2 mousePosition)
        {
            // Prefer units over tiles, matching typical turn-based UX.
            for (int i = _visualUnits.Count - 1; i >= 0; i--)
            {
                var unit = _visualUnits[i];
                if (unit != null && unit.IsInsideTree() && unit.ContainsGlobalPoint(mousePosition))
                {
                    return unit;
                }
            }

            return null;
        }

        public bool IsHoverInfoModeEnabled()
        {
            return _hoverInfoModeEnabled;
        }

        public void ToggleHoverInfoMode()
        {
            _hoverInfoModeEnabled = !_hoverInfoModeEnabled;
            ClearPendingHover();
            _hideGraceRemaining = 0.0f;
            if (_hoverInfoModeEnabled)
            {
                if (IsInsideTree() && GetViewport() != null)
                {
                    UpdateHoverTargetFromMouse(GetViewport().GetMousePosition());
                }
            }
            else
            {
                HideInformationPanel();
            }
        }

        private VisualHexTile GetHoveredTileAt(Vector2 mousePosition)
        {
            foreach (var tile in _visualTiles.Values)
            {
                if (tile != null && tile.IsInsideTree() && tile.ContainsGlobalPoint(mousePosition))
                {
                    return tile;
                }
            }

            return null;
        }

        private Vector2I? FindUnitPosition(Unit unit)
        {
            return _tileUnitCoordinator?.FindUnitPosition(unit, GameManager.GameMap);
        }

        private VisualUnit FindVisualUnit(Unit unit)
        {
            return _tileUnitCoordinator?.FindVisualUnit(unit, _visualUnits);
        }

        private void ClearAllHighlights()
        {
            foreach (var tile in _visualTiles.Values)
            {
                tile.SetHighlight(false);
                tile.SetGrayed(false);
                tile.SetBrightened(false);
                tile.SetOccupied(false);
                tile.SetUnavailable(false);
            }
        }

        private void ShowValidMovementDestinations(Unit selectedUnit)
        {
            if (selectedUnit == null) return;
            
            // Clear any existing highlights first
            ClearAllHighlights();
            
            var unitPosition = FindUnitPosition(selectedUnit);
            if (unitPosition == null) 
            {
                return;
            }

            var validDestinations = _interactionController.GetValidMovementDestinations(
                selectedUnit,
                unitPosition.Value,
                GameManager.GameMap);
            
            // Only apply highlighting if there are valid destinations
            if (validDestinations.Count > 0)
            {
                // Mark all tiles as unavailable first for maximum contrast
                foreach (var tile in _visualTiles.Values)
                {
                    tile.SetUnavailable(true);
                }
                
                // Mark occupied tiles as clearly unavailable with red overlay (only during movement phase)
                foreach (var kvp in GameManager.GameMap)
                {
                    if (kvp.Value.IsOccupied() && _visualTiles.ContainsKey(kvp.Key))
                    {
                        _visualTiles[kvp.Key].SetOccupied(true);
                    }
                }
                
                // Brighten valid destination tiles for maximum contrast
                foreach (var destination in validDestinations)
                {
                    if (_visualTiles.ContainsKey(destination))
                    {
                        var visualTile = _visualTiles[destination];
                        GD.Print($"🌟 HIGHLIGHT: Marking Grid({destination.X},{destination.Y}) as valid destination");
                        GD.Print($"   Visual tile at World({visualTile.Position.X:F1},{visualTile.Position.Y:F1})");
                        visualTile.SetUnavailable(false);  // Remove unavailable overlay
                        visualTile.SetBrightened(true);    // Add bright overlay
                    }
                }
            }
        }

        public void OnPhaseChanged(GamePhase newPhase)
        {
            ApplyPhaseChange(newPhase);
        }

        private void ApplyPhaseChange(GamePhase newPhase)
        {
            // Capture the old phase before changing it
            var oldPhase = _currentPhase;
            
            GD.Print($"🔄 MapRenderer.OnPhaseChanged: {oldPhase} → {newPhase}");
            
            // Check if we're leaving the movement phase
            bool isLeavingMovementPhase = (oldPhase == GamePhase.Move && newPhase != GamePhase.Move);
            GD.Print($"   Leaving movement phase: {isLeavingMovementPhase}");
            
            // Update movement phase state for all tiles
            bool isMovementPhase = (newPhase == GamePhase.Move);
            foreach (var tile in _visualTiles.Values)
            {
                tile.SetMovementPhase(isMovementPhase);
            }
            
            // Clear all movement-related displays when moving out of movement phase
            if (isLeavingMovementPhase)
            {
                GD.Print("   🧹 Clearing movement displays and highlights");
                ClearAllHighlights();
                DeselectAll();
                ClearAllUnitMovementDisplays();
            }

            if (_currentPhase == GamePhase.Purchase && newPhase != GamePhase.Purchase)
            {
                _purchasePlacementTiles.Clear();
                ClearAllHighlights();
            }
            
            _currentPhase = newPhase;
            
            // Update all visual units to reflect the new phase
            UpdateAllVisualUnitsForPhase(newPhase, oldPhase);
        }
        
        private void UpdateAllVisualUnitsForPhase(GamePhase newPhase, GamePhase oldPhase)
        {
            GD.Print($"   📊 UpdateAllVisualUnitsForPhase: {oldPhase} → {newPhase}");
            
            if (oldPhase == GamePhase.Move && newPhase != GamePhase.Move)
            {
                GD.Print("   🎯 ENDING movement phase - resetting all MPs to 0");
                
                // End of movement phase: Set all MPs to 0
                if (GameManager?.Players != null)
                {
                    int totalUnits = 0;
                    foreach (var player in GameManager.Players)
                    {
                        foreach (var unit in player.Units)
                        {
                            unit.CurrentMovementPoints = 0;
                            totalUnits++;
                        }
                    }
                    GD.Print($"   ✅ Reset {totalUnits} units to 0 MP");
                }
                else
                {
                    GD.Print("   ❌ GameManager or Players is null!");
                }
                
                // Refresh all MP displays to reflect the new MP values
                foreach (var visualUnit in _visualUnits)
                {
                    visualUnit.RefreshMovementDisplay();
                }
            }
            else if (oldPhase != GamePhase.Move && newPhase == GamePhase.Move)
            {
                GD.Print("   🎯 STARTING movement phase - restoring all MPs to full");
                
                // Start of movement phase: Reset all MPs to full
                if (GameManager?.Players != null)
                {
                    int totalUnits = 0;
                    foreach (var player in GameManager.Players)
                    {
                        foreach (var unit in player.Units)
                        {
                            player.ResetUnitMovement();
                            totalUnits++;
                        }
                    }
                    GD.Print($"   ✅ Restored {totalUnits} units to full MP");
                }
                else
                {
                    GD.Print("   ❌ GameManager or Players is null!");
                }
                
                // Refresh all MP displays to reflect the new MP values
                foreach (var visualUnit in _visualUnits)
                {
                    visualUnit.RefreshMovementDisplay();
                }
            }
            else
            {
                GD.Print($"   ⏭️  Phase change doesn't affect movement: {oldPhase} → {newPhase}");
            }
        }

        private void ClearAllUnitMovementDisplays()
        {
            // Clear movement displays from all visual units
            foreach (var visualUnit in _visualUnits)
            {
                var movementDisplay = visualUnit.GetNodeOrNull("MovementDisplay");
                if (movementDisplay != null)
                {
                    movementDisplay.QueueFree();
                }
            }
        }

        public void UpdateTileOccupationStatus()
        {
            // Delegate to centralized tile-unit coordinator
            _tileUnitCoordinator?.SynchronizeTileOccupation(GameManager.GameMap, _visualTiles);
        }

        public void ShowPurchasePlacementTiles(IReadOnlyList<Vector2I> tiles)
        {
            _purchasePlacementTiles.Clear();
            ClearAllHighlights();

            if (tiles == null || tiles.Count == 0)
            {
                return;
            }

            foreach (var tile in _visualTiles.Values)
            {
                tile.SetUnavailable(true);
            }

            foreach (var position in tiles)
            {
                if (_visualTiles.TryGetValue(position, out var tile))
                {
                    _purchasePlacementTiles.Add(position);
                    tile.SetUnavailable(false);
                    tile.SetHighlight(true, new Color(1.0f, 0.78f, 0.2f, 0.65f));
                }
            }
        }

        public void ClearPurchasePlacementTiles()
        {
            _purchasePlacementTiles.Clear();
            ClearAllHighlights();
        }

        public bool IsPurchasePlacementTile(Vector2I position)
        {
            return _purchasePlacementTiles.Contains(position);
        }
     }
} 
