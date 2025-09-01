using Godot;
using System.Collections.Generic;
using Archistrateia;

namespace Archistrateia
{
    public partial class Main : Control
    {
        [Export]
        public TurnManager TurnManager { get; set; }

        [Export]
        public Label TitleLabel { get; set; }

        [Export]
        public Button StartButton { get; set; }

        private GameManager _gameManager;
        private Button _nextPhaseButton;
        private Node2D _mapContainer;
        private Label _gameStatusLabel;
        private MapRenderer _mapRenderer;
        private Dictionary<TerrainType, Color> _terrainColors;
        private int _currentPlayerIndex = 0;
        
        // Zoom control UI elements
        private HSlider _zoomSlider;
        private Label _zoomLabel;
        
        // Debug functionality
        private Button _debugAdjacentButton;
        private bool _debugAdjacentMode = false;
        private VisualHexTile _lastHoveredTile = null;
        
        // Map dimensions come from centralized configuration
        private static int MAP_WIDTH => MapConfiguration.MAP_WIDTH;
        private static int MAP_HEIGHT => MapConfiguration.MAP_HEIGHT;

        // Scrolling variables
        private Vector2 _scrollOffset = Vector2.Zero;
        private const float SCROLL_SPEED = 300.0f; // pixels per second
        private const float EDGE_SCROLL_THRESHOLD = 50.0f; // pixels from edge to trigger scrolling

        public override void _Ready()
        {
            // Try to find UI elements if references are null
            if (StartButton == null)
            {
                StartButton = GetNodeOrNull<Button>("UI/StartButton");
            }

            if (TitleLabel == null)
            {
                TitleLabel = GetNodeOrNull<Label>("UI/TitleLabel");
            }

            InitializeTerrainColors();
            CreateZoomControls();
        }

        private void InitializeTerrainColors()
        {
            _terrainColors = new Dictionary<TerrainType, Color>
            {
                { TerrainType.Desert, new Color(0.9f, 0.8f, 0.6f) },
                { TerrainType.Hill, new Color(0.6f, 0.5f, 0.3f) },
                { TerrainType.River, new Color(0.3f, 0.6f, 0.9f) },
                { TerrainType.Shoreline, new Color(0.8f, 0.7f, 0.5f) },
                { TerrainType.Lagoon, new Color(0.2f, 0.5f, 0.7f) }
            };
        }

        private void CreateZoomControls()
        {
            // Create a background panel for zoom controls
            var backgroundPanel = new Panel();
            backgroundPanel.Position = new Vector2(GetViewport().GetVisibleRect().Size.X - 150, 10);
            backgroundPanel.Size = new Vector2(130, 60);
            backgroundPanel.ZIndex = 1000; // Ensure zoom controls are always on top
            backgroundPanel.MouseFilter = Control.MouseFilterEnum.Ignore; // Don't block mouse events
            AddChild(backgroundPanel);

            // Create a container for zoom controls
            var zoomContainer = new VBoxContainer();
            zoomContainer.Position = new Vector2(5, 5); // Small margin from panel edges
            zoomContainer.Size = new Vector2(120, 50);
            zoomContainer.CustomMinimumSize = new Vector2(120, 50); // Ensure minimum size
            backgroundPanel.AddChild(zoomContainer);

            // Zoom label
            _zoomLabel = new Label();
            _zoomLabel.Text = "Zoom: 1.0x";
            _zoomLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _zoomLabel.AddThemeFontSizeOverride("font_size", 16);
            _zoomLabel.CustomMinimumSize = new Vector2(120, 20); // Set minimum size
            zoomContainer.AddChild(_zoomLabel);

            // Zoom slider
            _zoomSlider = new HSlider();
            _zoomSlider.MinValue = 0.1f;
            _zoomSlider.MaxValue = 3.0f;
            _zoomSlider.Value = 1.0f;
            _zoomSlider.Step = 0.1f;
            _zoomSlider.CustomMinimumSize = new Vector2(120, 20); // Set minimum size
            _zoomSlider.MouseFilter = Control.MouseFilterEnum.Stop; // Ensure slider receives mouse events
            _zoomSlider.ValueChanged += OnZoomSliderChanged;
            _zoomSlider.GuiInput += OnZoomSliderInput; // Add input event handler
            _zoomSlider.AllowGreater = false; // Ensure value stays within bounds
            _zoomSlider.AllowLesser = false; // Ensure value stays within bounds
            zoomContainer.AddChild(_zoomSlider);
        }

        private void OnZoomSliderChanged(double value)
        {
            HexGridCalculator.SetZoom((float)value);
            RegenerateMapWithCurrentZoom();
            UpdateTitleLabel();
            UpdateZoomLabel();
            UpdateUIPositions(); // Update UI positions when zoom changes
        }
        
        private void OnZoomSliderInput(InputEvent @event)
        {
            GD.Print($"Zoom slider received input event: {@event.GetType().Name}");
            
            // Handle mouse button events manually if the slider isn't responding
            if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
            {
                GD.Print($"Zoom slider mouse button pressed: {mouseEvent.ButtonIndex}");
                
                // Use Godot's built-in slider value calculation
                var localPos = _zoomSlider.GetLocalMousePosition();
                var sliderWidth = _zoomSlider.Size.X;
                var normalizedPos = localPos.X / sliderWidth;
                
                // Calculate new value using Godot's built-in math functions
                var newValue = _zoomSlider.MinValue + (normalizedPos * (_zoomSlider.MaxValue - _zoomSlider.MinValue));
                newValue = Mathf.Clamp(newValue, _zoomSlider.MinValue, _zoomSlider.MaxValue);
                
                GD.Print($"Calculated new zoom value: {newValue} from mouse position {localPos}");
                
                // Update the slider value and trigger change event
                _zoomSlider.Value = newValue;
                OnZoomSliderChanged(newValue);
            }
        }



        private void UpdateZoomLabel()
        {
            if (_zoomLabel != null)
            {
                _zoomLabel.Text = $"Zoom: {HexGridCalculator.ZoomFactor:F1}x";
            }
        }
        
        private void UpdateUIPositions()
        {
            var viewportSize = GetViewport().GetVisibleRect().Size;
            
            // Update Next Phase button position
            if (_nextPhaseButton != null)
            {
                _nextPhaseButton.Position = new Vector2(10, viewportSize.Y - 50);
            }
            
            // Update Debug Adjacent button position
            if (_debugAdjacentButton != null)
            {
                _debugAdjacentButton.Position = new Vector2(140, viewportSize.Y - 50);
            }
            
            // Update zoom controls position
            if (_zoomSlider != null)
            {
                var zoomPanel = _zoomSlider.GetParent().GetParent() as Panel;
                if (zoomPanel != null)
                {
                    zoomPanel.Position = new Vector2(viewportSize.X - 150, 10);
                }
            }
            
            // Update status panel position
            if (_gameStatusLabel != null)
            {
                var statusPanel = _gameStatusLabel.GetParent() as Panel;
                if (statusPanel != null)
                {
                    statusPanel.Position = new Vector2(10, 10);
                }
            }
        }

        public void OnStartButtonPressed()
        {
            // Try to find StartButton if reference is null
            if (StartButton == null)
            {
                StartButton = GetNodeOrNull<Button>("UI/StartButton");
            }

            if (StartButton != null)
            {
                StartButton.Visible = false;
            }

            // Hide title label when game starts
            if (TitleLabel != null)
            {
                TitleLabel.Visible = false;
            }

            // Create background panel for game status
            var statusBackgroundPanel = new Panel();
            statusBackgroundPanel.Position = new Vector2(10, 10);
            statusBackgroundPanel.Size = new Vector2(400, 40);
            statusBackgroundPanel.ZIndex = 1000; // Ensure UI is always on top
            statusBackgroundPanel.MouseFilter = Control.MouseFilterEnum.Ignore; // Don't block mouse events
            AddChild(statusBackgroundPanel);

            // Create dedicated game status label
            _gameStatusLabel = new Label();
            _gameStatusLabel.Position = new Vector2(5, 5); // Small margin from panel edges
            _gameStatusLabel.Size = new Vector2(390, 30);
            _gameStatusLabel.AddThemeFontSizeOverride("font_size", 24);
            _gameStatusLabel.ZIndex = 1000; // Ensure UI is always on top
            statusBackgroundPanel.AddChild(_gameStatusLabel);

            // Calculate and set optimal zoom based on viewport and grid size
            var viewportSize = GetViewport().GetVisibleRect().Size;
            var optimalZoom = HexGridCalculator.CalculateOptimalZoom(viewportSize, MAP_WIDTH, MAP_HEIGHT);
            HexGridCalculator.SetZoom(optimalZoom);
            
            // Update zoom slider to reflect the optimal zoom
            if (_zoomSlider != null)
            {
                _zoomSlider.Value = optimalZoom;
            }
            
            // Update zoom label to reflect the optimal zoom
            UpdateZoomLabel();

            GenerateMap();
            InitializeGameManager();
            
            // Update UI positions after everything is initialized
            UpdateUIPositions();

            // Create Next Phase button using Godot's viewport system
            _nextPhaseButton = new Button();
            _nextPhaseButton.Text = "Next Phase";
            _nextPhaseButton.Position = new Vector2(10, GetViewport().GetVisibleRect().Size.Y - 50);
            _nextPhaseButton.Size = new Vector2(120, 40); // Set explicit size
            _nextPhaseButton.AddThemeFontSizeOverride("font_size", 20);
            _nextPhaseButton.ZIndex = 1000; // Ensure UI is always on top
            _nextPhaseButton.MouseFilter = Control.MouseFilterEnum.Stop; // Ensure button receives mouse events
            _nextPhaseButton.Pressed += OnNextPhaseButtonPressed;
            // _nextPhaseButton.GuiInput += OnNextPhaseButtonInput; // Add input event handler
            AddChild(_nextPhaseButton);
            
            // Create Debug Adjacent Tiles button
            _debugAdjacentButton = new Button();
            _debugAdjacentButton.Text = "Debug Adjacent";
            _debugAdjacentButton.Position = new Vector2(140, GetViewport().GetVisibleRect().Size.Y - 50);
            _debugAdjacentButton.Size = new Vector2(120, 40); // Set explicit size
            _debugAdjacentButton.AddThemeFontSizeOverride("font_size", 16);
            _debugAdjacentButton.ZIndex = 1000; // Ensure UI is always on top
            _debugAdjacentButton.MouseFilter = Control.MouseFilterEnum.Stop; // Ensure button receives mouse events
            _debugAdjacentButton.Pressed += OnDebugAdjacentButtonPressed;
            AddChild(_debugAdjacentButton);
            
            // Debug UI elements to help troubleshoot
            DebugUIElements();
        }

        private void GenerateMap()
        {
            if (_mapContainer != null)
            {
                _mapContainer.QueueFree();
            }

            // Reset scroll offset when generating new map
            _scrollOffset = Vector2.Zero;
            HexGridCalculator.SetScrollOffset(_scrollOffset);

            _mapContainer = new Node2D();
            _mapContainer.Name = "MapContainer";
            _mapContainer.ZIndex = 0; // Ensure map is below UI elements
            AddChild(_mapContainer);

            int tilesCreated = 0;
            for (int x = 0; x < MAP_WIDTH; x++)
            {
                for (int y = 0; y < MAP_HEIGHT; y++)
                {
                    var terrainType = GetRandomTerrainType();
                    var worldPosition = HexGridCalculator.CalculateHexPositionCentered(x, y, GetViewport().GetVisibleRect().Size, MAP_WIDTH, MAP_HEIGHT);
                    var gridPosition = new Vector2I(x, y);
                    
                    var visualTile = new VisualHexTile();
                    visualTile.Initialize(gridPosition, terrainType, _terrainColors[terrainType], worldPosition);
                    _mapContainer.AddChild(visualTile);
                    
                    tilesCreated++;
                }
            }

            GD.Print($"Generated hex map with {tilesCreated} tiles");
        }

        private void RegenerateMapWithCurrentZoom()
        {
            if (_mapContainer != null)
            {
                // Update all existing tiles' visual components
                foreach (Node child in _mapContainer.GetChildren())
                {
                    if (child is VisualHexTile visualTile)
                    {
                        // Update the tile's position with new zoom and scroll offset
                        var worldPosition = HexGridCalculator.CalculateHexPositionCentered(
                            visualTile.GridPosition.X, 
                            visualTile.GridPosition.Y, 
                            GetViewport().GetVisibleRect().Size, 
                            MAP_WIDTH, 
                            MAP_HEIGHT
                        );
                        visualTile.Position = worldPosition;
                        
                        // Update the visual components (hex shape, outline, collision)
                        visualTile.UpdateVisualComponents();
                    }
                }
                
                // Update visual units' positions and visual components
                if (_mapRenderer != null)
                {
                    foreach (var visualUnit in _mapRenderer.GetVisualUnits())
                    {
                        // Find the logical tile this unit occupies
                        var logicalTile = FindLogicalTileWithUnit(visualUnit.LogicalUnit);
                        if (logicalTile != null)
                        {
                            // Calculate new position with current zoom and scroll offset
                            var newWorldPosition = HexGridCalculator.CalculateHexPositionCentered(
                                logicalTile.Position.X, 
                                logicalTile.Position.Y, 
                                GetViewport().GetVisibleRect().Size, 
                                MAP_WIDTH, 
                                MAP_HEIGHT
                            );
                            
                            // Update unit position and visual components
                            visualUnit.UpdatePosition(newWorldPosition);
                            visualUnit.UpdateVisualComponents();
                        }
                    }
                }
            }
        }

        private static TerrainType GetRandomTerrainType()
        {
            var terrainTypes = System.Enum.GetValues<TerrainType>();
            var randomType = terrainTypes[GD.RandRange(0, terrainTypes.Length - 1)];
            return randomType;
        }

        private void InitializeGameManager()
        {
            _gameManager = new GameManager();
            AddChild(_gameManager);
            
            // Use CallDeferred to connect TurnManager after GameManager's _Ready is called
            CallDeferred(MethodName.ConnectTurnManager);
        }

        private void ConnectTurnManager()
        {
            // Use the GameManager's TurnManager instead of the exported one
            TurnManager = _gameManager.TurnManager;
            
            // Now initialize MapRenderer with proper TurnManager
            InitializeMapRenderer();
        }

        private void InitializeMapRenderer()
        {
            _mapRenderer = new MapRenderer();
            _mapRenderer.Name = "MapRenderer";
            AddChild(_mapRenderer);
            _mapRenderer.Initialize(_gameManager);
            
            // Connect MapRenderer to GameManager for phase change notifications
            _gameManager.SetMapRenderer(_mapRenderer);
            
            // Set initial player and phase
            if (_gameManager.Players.Count > 0)
            {
                _mapRenderer.SetCurrentPlayer(_gameManager.Players[_currentPlayerIndex]);
            }
            
            // TurnManager is now guaranteed to be available
            _mapRenderer.SetCurrentPhase(TurnManager.CurrentPhase);
            
            // Ensure all tiles know the current phase state
            _mapRenderer.UpdateTileOccupationStatus();
            
            // Update the title label with the correct initial phase
            UpdateTitleLabel();
            
            // Register all visual tiles with the MapRenderer
            RegisterVisualTilesWithMapRenderer();
            
            CreateVisualUnitsForPlayers();
            
            // Update initial tile occupation status
            _mapRenderer.UpdateTileOccupationStatus();
        }

        private void RegisterVisualTilesWithMapRenderer()
        {
            if (_mapContainer == null || _mapRenderer == null) return;
            
            foreach (Node child in _mapContainer.GetChildren())
            {
                if (child is VisualHexTile visualTile)
                {
                    _mapRenderer.AddVisualTile(visualTile);
                }
            }
        }

        private void CreateVisualUnitsForPlayers()
        {
            if (_gameManager == null || _mapRenderer == null) return;

            foreach (var player in _gameManager.Players)
            {
                var playerColor = player.Name == "Pharaoh" ? new Color(0.8f, 0.2f, 0.2f) : new Color(0.2f, 0.2f, 0.8f);
                
                foreach (var unit in player.Units)
                {
                    var logicalTile = FindLogicalTileWithUnit(unit);
                    if (logicalTile != null)
                    {
                        var worldPosition = HexGridCalculator.CalculateHexPositionCentered(
                            logicalTile.Position.X, 
                            logicalTile.Position.Y, 
                            GetViewport().GetVisibleRect().Size, 
                            MAP_WIDTH, 
                            MAP_HEIGHT
                        );
                        
                        _mapRenderer.CreateVisualUnit(unit, worldPosition, playerColor);
                    }
                }
            }
        }

        private HexTile FindLogicalTileWithUnit(Unit unit)
        {
            foreach (var kvp in _gameManager.GameMap)
            {
                if (kvp.Value.OccupyingUnit == unit)
                {
                    return kvp.Value;
                }
            }
            return null;
        }

        private void OnNextPhaseButtonPressed()
        {
            if (TurnManager != null)
            {
                TurnManager.AdvancePhase();
                
                // Handle phase-specific actions with GameManager
                if (_gameManager != null)
                {
                    HandlePhaseChange(TurnManager.CurrentPhase);
                }
                
                // Update MapRenderer with new phase
                if (_mapRenderer != null)
                {
                    _mapRenderer.OnPhaseChanged(TurnManager.CurrentPhase);
                }
            }

            UpdateTitleLabel();
        }

        private void HandlePhaseChange(GamePhase phase)
        {
            // Update MapRenderer with current phase
            if (_mapRenderer != null)
            {
                _mapRenderer.SetCurrentPhase(phase);
            }

            switch (phase)
            {
                case GamePhase.Earn:
                    GD.Print("=== EARN PHASE: Processing city income ===");
                    _gameManager.ProcessEarnPhase();
                    // Switch to next player at start of new turn
                    SwitchToNextPlayer();
                    break;
                case GamePhase.Purchase:
                    GD.Print("=== PURCHASE PHASE: Players can buy units ===");
                    break;
                case GamePhase.Move:
                    GD.Print("=== MOVE PHASE: Units can move ===");
                    foreach (var player in _gameManager.Players)
                    {
                        player.ResetUnitMovement();
                    }
                    // Deselect any units when entering move phase
                    if (_mapRenderer != null)
                    {
                        _mapRenderer.DeselectAll();
                    }
                    break;
                case GamePhase.Combat:
                    GD.Print("=== COMBAT PHASE: Combat resolution ===");
                    break;
            }
        }

        private void SwitchToNextPlayer()
        {
            if (_gameManager?.Players.Count > 0)
            {
                _currentPlayerIndex = (_currentPlayerIndex + 1) % _gameManager.Players.Count;
                var currentPlayer = _gameManager.Players[_currentPlayerIndex];
                
                GD.Print($"Switched to player: {currentPlayer.Name}");
                
                // Update MapRenderer with new current player
                if (_mapRenderer != null)
                {
                    _mapRenderer.SetCurrentPlayer(currentPlayer);
                }
            }
        }

        private void UpdateTitleLabel()
        {
            // During gameplay, update the game status label instead of title label
            if (_gameStatusLabel != null && TurnManager != null)
            {
                var currentPlayerName = "Unknown";
                if (_gameManager?.Players.Count > 0 && _currentPlayerIndex < _gameManager.Players.Count)
                {
                    currentPlayerName = _gameManager.Players[_currentPlayerIndex].Name;
                }
                
                var newText = $"Turn {TurnManager.CurrentTurn} - {TurnManager.CurrentPhase} - {currentPlayerName}";
                _gameStatusLabel.Text = newText;
            }
            // On title screen, update the title label
            else if (TitleLabel != null && TurnManager != null)
            {
                var newText = $"Turn {TurnManager.CurrentTurn} - {TurnManager.CurrentPhase}";
                TitleLabel.Text = newText;
            }
        }

        public override void _Input(InputEvent @event)
        {
            // Handle debug adjacent hover functionality
            if (_debugAdjacentMode && @event is InputEventMouseMotion)
            {
                var mousePosition = GetViewport().GetMousePosition();
                HandleDebugAdjacentHover(mousePosition);
            }
            
            // Handle zoom controls
            if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
            {
                if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
                {
                    HexGridCalculator.ZoomIn();
                    _zoomSlider.Value = HexGridCalculator.ZoomFactor;
                    RegenerateMapWithCurrentZoom();
                    UpdateTitleLabel();
                    UpdateZoomLabel();
                    GetViewport().SetInputAsHandled();
                }
                else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
                {
                    HexGridCalculator.ZoomOut();
                    _zoomSlider.Value = HexGridCalculator.ZoomFactor;
                    RegenerateMapWithCurrentZoom();
                    UpdateTitleLabel();
                    UpdateZoomLabel();
                    GetViewport().SetInputAsHandled();
                }
                else if (mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    // Debug mouse clicks to see if UI controls are being detected
                    var mousePosition = GetViewport().GetMousePosition();
                    DebugMousePosition(mousePosition);
                    
                    // Don't mark left clicks as handled to allow UI controls to receive them
                }
            }
            
            // Handle two-finger scroll for Mac trackpad
            if (@event is InputEventPanGesture panGesture)
            {
                HandleTwoFingerScroll(panGesture);
                GetViewport().SetInputAsHandled();
            }
            
            // Don't handle other input events to allow UI controls to receive them
        }

        public override void _Process(double delta)
        {
            HandleEdgeScrolling(delta);
        }

        private void HandleEdgeScrolling(double delta)
        {
            var mousePosition = GetViewport().GetMousePosition();
            var viewportSize = GetViewport().GetVisibleRect().Size;
            
            // Only activate scrolling if the grid extends beyond the viewport
            if (!HexGridCalculator.IsScrollingNeeded(viewportSize, MAP_WIDTH, MAP_HEIGHT))
            {
                return;
            }
            
            // Check if mouse is hovering over UI controls - if so, don't scroll
            if (IsMouseOverUIControls(mousePosition))
            {
                return;
            }
            
            var scrollDelta = Vector2.Zero;
            
            // Check if mouse is near edges
            if (mousePosition.X < EDGE_SCROLL_THRESHOLD)
            {
                scrollDelta.X = -SCROLL_SPEED * (float)delta;
            }
            else if (mousePosition.X > viewportSize.X - EDGE_SCROLL_THRESHOLD)
            {
                scrollDelta.X = SCROLL_SPEED * (float)delta;
            }
            
            if (mousePosition.Y < EDGE_SCROLL_THRESHOLD)
            {
                scrollDelta.Y = -SCROLL_SPEED * (float)delta;
            }
            else if (mousePosition.Y > viewportSize.Y - EDGE_SCROLL_THRESHOLD)
            {
                scrollDelta.Y = SCROLL_SPEED * (float)delta;
            }
            
            // Apply scroll delta if any
            if (scrollDelta != Vector2.Zero)
            {
                _scrollOffset += scrollDelta;
                
                // Calculate dynamic scroll bounds based on current zoom and viewport
                var currentViewportSize = GetViewport().GetVisibleRect().Size;
                var scrollBounds = HexGridCalculator.CalculateScrollBounds(currentViewportSize, MAP_WIDTH, MAP_HEIGHT);
                
                // Clamp scroll offset to keep grid on screen
                _scrollOffset.X = Mathf.Clamp(_scrollOffset.X, -scrollBounds.X, scrollBounds.X);
                _scrollOffset.Y = Mathf.Clamp(_scrollOffset.Y, -scrollBounds.Y, scrollBounds.Y);
                
                // Update HexGridCalculator scroll offset
                HexGridCalculator.SetScrollOffset(_scrollOffset);
                
                // Regenerate map with new scroll offset
                RegenerateMapWithCurrentZoom();
            }
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                if (HandlePhaseInput(keyEvent)) return;
                if (HandleZoomInput(keyEvent)) return;
                HandleScrollInput(keyEvent);
            }
        }

        private bool HandlePhaseInput(InputEventKey keyEvent)
        {
            if (keyEvent.Keycode == Key.Space)
            {
                if (TurnManager != null)
                {
                    TurnManager.AdvancePhase();
                    UpdateTitleLabel();
                }
                return true;
            }
            return false;
        }

        private bool HandleZoomInput(InputEventKey keyEvent)
        {
            bool handled = false;
            
            if (keyEvent.Keycode == Key.Equal || keyEvent.Keycode == Key.KpAdd)
            {
                HexGridCalculator.ZoomIn();
                handled = true;
            }
            else if (keyEvent.Keycode == Key.Minus || keyEvent.Keycode == Key.KpSubtract)
            {
                HexGridCalculator.ZoomOut();
                handled = true;
            }
            else if (keyEvent.Keycode == Key.Key0)
            {
                HexGridCalculator.SetZoom(1.0f);
                handled = true;
            }

            if (handled)
            {
                UpdateZoomUI();
                GetViewport().SetInputAsHandled();
            }

            return handled;
        }

        private bool HandleScrollInput(InputEventKey keyEvent)
        {
            var viewportSize = GetViewport().GetVisibleRect().Size;
            
            // Handle Home key regardless of whether scrolling is needed
            if (keyEvent.Keycode == Key.Home)
            {
                _scrollOffset = Vector2.Zero;
                HexGridCalculator.SetScrollOffset(_scrollOffset);
                RegenerateMapWithCurrentZoom();
                GetViewport().SetInputAsHandled();
                return true;
            }
            
            // Only allow directional scrolling if the grid extends beyond the viewport
            if (!HexGridCalculator.IsScrollingNeeded(viewportSize, MAP_WIDTH, MAP_HEIGHT))
            {
                return false;
            }
            
            var scrollDelta = Vector2.Zero;
            const float ScrollStep = 50.0f;

            if (keyEvent.Keycode == Key.W || keyEvent.Keycode == Key.Up)
            {
                scrollDelta.Y = -ScrollStep;
            }
            else if (keyEvent.Keycode == Key.S || keyEvent.Keycode == Key.Down)
            {
                scrollDelta.Y = ScrollStep;
            }
            else if (keyEvent.Keycode == Key.A || keyEvent.Keycode == Key.Left)
            {
                scrollDelta.X = -ScrollStep;
            }
            else if (keyEvent.Keycode == Key.D || keyEvent.Keycode == Key.Right)
            {
                scrollDelta.X = ScrollStep;
            }

            if (scrollDelta != Vector2.Zero)
            {
                ApplyScrollDelta(scrollDelta);
                GetViewport().SetInputAsHandled();
                return true;
            }

            return false;
        }

        private void UpdateZoomUI()
        {
            _zoomSlider.Value = HexGridCalculator.ZoomFactor;
            RegenerateMapWithCurrentZoom();
            UpdateTitleLabel();
            UpdateZoomLabel();
        }

        private void ApplyScrollDelta(Vector2 scrollDelta)
        {
            _scrollOffset += scrollDelta;
            var scrollBounds = HexGridCalculator.CalculateScrollBounds(GetViewport().GetVisibleRect().Size, MAP_WIDTH, MAP_HEIGHT);
            _scrollOffset.X = Mathf.Clamp(_scrollOffset.X, -scrollBounds.X, scrollBounds.X);
            _scrollOffset.Y = Mathf.Clamp(_scrollOffset.Y, -scrollBounds.Y, scrollBounds.Y);
            HexGridCalculator.SetScrollOffset(_scrollOffset);
            RegenerateMapWithCurrentZoom();
        }
        
        private void HandleTwoFingerScroll(InputEventPanGesture panGesture)
        {
            var viewportSize = GetViewport().GetVisibleRect().Size;
            
            // Only allow scrolling if the grid extends beyond the viewport
            if (!HexGridCalculator.IsScrollingNeeded(viewportSize, MAP_WIDTH, MAP_HEIGHT))
            {
                return;
            }
            
            // Check if mouse is hovering over UI controls - if so, don't scroll
            var mousePosition = GetViewport().GetMousePosition();
            if (IsMouseOverUIControls(mousePosition))
            {
                return;
            }
            
            // Convert pan gesture delta to scroll delta
            // Pan gesture delta is typically smaller, so we scale it up for responsive scrolling
            const float PAN_SCROLL_MULTIPLIER = 4.0f;
            var scrollDelta = new Vector2(
                panGesture.Delta.X * PAN_SCROLL_MULTIPLIER,
                panGesture.Delta.Y * PAN_SCROLL_MULTIPLIER
            );
            
            // Apply the scroll delta
            ApplyScrollDelta(scrollDelta);
        }
        
        public bool IsMouseOverUIControls(Vector2 mousePosition)
        {
            // Check if mouse is over zoom controls (top-right panel)
            if (_zoomSlider != null)
            {
                var zoomPanel = _zoomSlider.GetParent().GetParent() as Panel;
                if (zoomPanel != null)
                {
                    var panelRect = new Rect2(zoomPanel.GlobalPosition, zoomPanel.Size);
                    if (panelRect.HasPoint(mousePosition))
                    {
                        return true;
                    }
                }
            }
            
            // Check if mouse is over Next Phase button
            if (_nextPhaseButton != null)
            {
                var buttonRect = new Rect2(_nextPhaseButton.GlobalPosition, _nextPhaseButton.Size);
                if (buttonRect.HasPoint(mousePosition))
                {
                    return true;
                }
            }
            
            // Check if mouse is over Debug Adjacent button
            if (_debugAdjacentButton != null)
            {
                var buttonRect = new Rect2(_debugAdjacentButton.GlobalPosition, _debugAdjacentButton.Size);
                if (buttonRect.HasPoint(mousePosition))
                {
                    return true;
                }
            }
            
            // Check if mouse is over game status panel
            if (_gameStatusLabel != null)
            {
                var statusPanel = _gameStatusLabel.GetParent() as Panel;
                if (statusPanel != null)
                {
                    var panelRect = new Rect2(statusPanel.GlobalPosition, statusPanel.Size);
                    if (panelRect.HasPoint(mousePosition))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        // Enhanced debug method to help troubleshoot UI issues with mouse coordinates
        private void DebugMousePosition(Vector2 mousePosition)
        {
            GD.Print($"=== DEBUGGING MOUSE POSITION: {mousePosition} ===");
            
            if (_nextPhaseButton != null)
            {
                var buttonRect = new Rect2(_nextPhaseButton.GlobalPosition, _nextPhaseButton.Size);
                bool overButton = buttonRect.HasPoint(mousePosition);
                GD.Print($"Next Phase Button: GlobalPos={_nextPhaseButton.GlobalPosition}, Size={_nextPhaseButton.Size}, Over={overButton}");
            }
            
            if (_zoomSlider != null)
            {
                var zoomPanel = _zoomSlider.GetParent().GetParent() as Panel;
                if (zoomPanel != null)
                {
                    var panelRect = new Rect2(zoomPanel.GlobalPosition, zoomPanel.Size);
                    bool overZoom = panelRect.HasPoint(mousePosition);
                    GD.Print($"Zoom Panel: GlobalPos={zoomPanel.GlobalPosition}, Size={zoomPanel.Size}, Over={overZoom}");
                }
            }
            
            if (_gameStatusLabel != null)
            {
                var statusPanel = _gameStatusLabel.GetParent() as Panel;
                if (statusPanel != null)
                {
                    var panelRect = new Rect2(statusPanel.GlobalPosition, statusPanel.Size);
                    bool overStatus = panelRect.HasPoint(mousePosition);
                    GD.Print($"Status Panel: GlobalPos={statusPanel.GlobalPosition}, Size={statusPanel.Size}, Over={overStatus}");
                }
            }
        }
        
        // Debug method to help troubleshoot UI issues
        private void DebugUIElements()
        {
            GD.Print("=== DEBUGGING UI ELEMENTS ===");
            
            if (_nextPhaseButton != null)
            {
                GD.Print($"Next Phase Button: Position={_nextPhaseButton.GlobalPosition}, Size={_nextPhaseButton.Size}, Visible={_nextPhaseButton.Visible}");
            }
            else
            {
                GD.Print("Next Phase Button: NULL");
            }
            
            if (_zoomSlider != null)
            {
                var zoomPanel = _zoomSlider.GetParent().GetParent() as Panel;
                if (zoomPanel != null)
                {
                    GD.Print($"Zoom Panel: Position={zoomPanel.GlobalPosition}, Size={zoomPanel.Size}, Visible={zoomPanel.Visible}");
                }
                GD.Print($"Zoom Slider: Position={_zoomSlider.GlobalPosition}, Size={_zoomSlider.Size}, Visible={_zoomSlider.Visible}");
            }
            else
            {
                GD.Print("Zoom Slider: NULL");
            }
            
            if (_gameStatusLabel != null)
            {
                var statusPanel = _gameStatusLabel.GetParent() as Panel;
                if (statusPanel != null)
                {
                    GD.Print($"Status Panel: Position={statusPanel.GlobalPosition}, Size={statusPanel.Size}, Visible={statusPanel.Visible}");
                }
            }
            else
            {
                GD.Print("Game Status Label: NULL");
            }
        }
        
        private void OnDebugAdjacentButtonPressed()
        {
            _debugAdjacentMode = !_debugAdjacentMode;
            _debugAdjacentButton.Text = _debugAdjacentMode ? "Debug Adjacent: ON" : "Debug Adjacent";
            
            if (!_debugAdjacentMode)
            {
                // Clear any existing debug highlights
                ClearDebugHighlights();
            }
            
            GD.Print($"Debug adjacent mode: {(_debugAdjacentMode ? "ENABLED" : "DISABLED")}");
        }
        
        private void ClearDebugHighlights()
        {
            if (_mapContainer == null) return;
            
            foreach (Node child in _mapContainer.GetChildren())
            {
                if (child is VisualHexTile visualTile)
                {
                    visualTile.SetHighlight(false);
                }
            }
            
            _lastHoveredTile = null;
        }
        
        private void HandleDebugAdjacentHover(Vector2 mousePosition)
        {
            if (!_debugAdjacentMode || _mapContainer == null) return;
            
            // Find the tile under the mouse
            VisualHexTile hoveredTile = null;
            foreach (Node child in _mapContainer.GetChildren())
            {
                if (child is VisualHexTile visualTile)
                {
                    var localPos = visualTile.ToLocal(mousePosition);
                    if (IsPointInHexagon(localPos, visualTile))
                    {
                        hoveredTile = visualTile;
                        break;
                    }
                }
            }
            
            // If we're hovering over a different tile, update highlights
            if (hoveredTile != _lastHoveredTile)
            {
                // Clear previous highlights
                ClearDebugHighlights();
                
                if (hoveredTile != null)
                {
                    // Highlight the hovered tile
                    hoveredTile.SetHighlight(true, new Color(1.0f, 1.0f, 0.0f, 0.5f));
                    
                    // Get adjacent positions using the actual method
                    var adjacentPositions = MovementValidationLogic.GetAdjacentPositions(hoveredTile.GridPosition);
                    
                    GD.Print($"Hovering over tile at {hoveredTile.GridPosition}, adjacent tiles: {string.Join(", ", adjacentPositions)}");
                    
                    // Highlight adjacent tiles
                    foreach (var adjacentPos in adjacentPositions)
                    {
                        var adjacentTile = FindVisualTileAtPosition(adjacentPos);
                        if (adjacentTile != null)
                        {
                            adjacentTile.SetHighlight(true, new Color(0.0f, 1.0f, 0.0f, 0.5f));
                        }
                    }
                    
                    _lastHoveredTile = hoveredTile;
                }
            }
        }
        
        private bool IsPointInHexagon(Vector2 point, VisualHexTile tile)
        {
            // Get the exact hex vertices used for rendering
            var vertices = HexGridCalculator.CreateHexagonVertices();
            
            // Ray casting algorithm for point-in-polygon
            bool inside = false;
            int j = vertices.Length - 1;
            
            for (int i = 0; i < vertices.Length; i++)
            {
                if (((vertices[i].Y > point.Y) != (vertices[j].Y > point.Y)) &&
                    (point.X < (vertices[j].X - vertices[i].X) * (point.Y - vertices[i].Y) / (vertices[j].Y - vertices[i].Y) + vertices[i].X))
                {
                    inside = !inside;
                }
                j = i;
            }
            
            return inside;
        }
        
        private VisualHexTile FindVisualTileAtPosition(Vector2I position)
        {
            if (_mapContainer == null) return null;
            
            foreach (Node child in _mapContainer.GetChildren())
            {
                if (child is VisualHexTile visualTile && visualTile.GridPosition == position)
                {
                    return visualTile;
                }
            }
            
            return null;
        }
    }
}
