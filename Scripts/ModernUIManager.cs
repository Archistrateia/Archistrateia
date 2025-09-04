using Godot;

namespace Archistrateia
{
    public partial class ModernUIManager : Control
    {
        private Panel _topBar;
        private Panel _bottomBar;
        private Panel _leftSidebar;
        private Panel _rightSidebar;
        private Control _gameArea;
        
        private Label _titleLabel;
        private Label _currentTurnLabel;
        private Label _currentPlayerLabel;
        private Label _currentPhaseLabel;
        private Button _nextPhaseButton;
        private Panel _gameStatusPanel;
        private Panel _mapControlsPanel;
        
        private OptionButton _mapTypeSelector;
        private Button _regenerateMapButton;
        private Button _startGameButton;
        private HSlider _zoomSlider;
        private Label _zoomLabel;

        public override void _Ready()
        {
            CreateModernLayout();
        }

        private void CreateModernLayout()
        {
            // Fill the entire parent
            AnchorLeft = 0;
            AnchorTop = 0;
            AnchorRight = 1;
            AnchorBottom = 1;
            OffsetLeft = 0;
            OffsetTop = 0;
            OffsetRight = 0;
            OffsetBottom = 0;
            
            CreateTopBar();
            CreateBottomBar();
            CreateSidebars();
            CreateGameArea();
        }

        private void CreateTopBar()
        {
            _topBar = new Panel();
            _topBar.Name = "TopBar";
            // Top wide preset
            _topBar.AnchorLeft = 0;
            _topBar.AnchorTop = 0;
            _topBar.AnchorRight = 1;
            _topBar.AnchorBottom = 0;
            _topBar.Size = new Vector2(0, 60);
            
            var topBarStyle = new StyleBoxFlat();
            topBarStyle.BgColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
            topBarStyle.BorderColor = new Color(0.2f, 0.2f, 0.3f);
            topBarStyle.BorderWidthBottom = 2;
            _topBar.AddThemeStyleboxOverride("panel", topBarStyle);
            
            AddChild(_topBar);

            var topBarContainer = new HBoxContainer();
            // Full rect preset
            topBarContainer.AnchorLeft = 0;
            topBarContainer.AnchorTop = 0;
            topBarContainer.AnchorRight = 1;
            topBarContainer.AnchorBottom = 1;
            topBarContainer.AddThemeConstantOverride("separation", 20);
            _topBar.AddChild(topBarContainer);

            var leftSection = new HBoxContainer();
            leftSection.AddThemeConstantOverride("separation", 20);
            topBarContainer.AddChild(leftSection);

            _titleLabel = new Label();
            _titleLabel.Text = "Archistrateia";
            _titleLabel.AddThemeFontSizeOverride("font_size", 24);
            _titleLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 1.0f));
            _titleLabel.VerticalAlignment = VerticalAlignment.Center;
            _titleLabel.CustomMinimumSize = new Vector2(200, 0); // Increased width for title
            _titleLabel.HorizontalAlignment = HorizontalAlignment.Center; // Center the text within its container
            leftSection.AddChild(_titleLabel);

            var separator1 = new VSeparator();
            separator1.AddThemeColorOverride("separator", new Color(0.3f, 0.3f, 0.4f));
            leftSection.AddChild(separator1);

            _currentTurnLabel = new Label();
            _currentTurnLabel.Text = "Turn: 1";
            _currentTurnLabel.AddThemeFontSizeOverride("font_size", 16);
            _currentTurnLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.9f));
            _currentTurnLabel.VerticalAlignment = VerticalAlignment.Center;
            _currentTurnLabel.CustomMinimumSize = new Vector2(100, 0); // Increased width for turn
            _currentTurnLabel.HorizontalAlignment = HorizontalAlignment.Left;
            _currentTurnLabel.AddThemeConstantOverride("margin_left", 15); // Left padding
            _currentTurnLabel.AddThemeConstantOverride("margin_right", 15); // Right padding
            leftSection.AddChild(_currentTurnLabel);

            var separator2 = new VSeparator();
            separator2.AddThemeColorOverride("separator", new Color(0.3f, 0.3f, 0.4f));
            leftSection.AddChild(separator2);

            _currentPlayerLabel = new Label();
            _currentPlayerLabel.Text = "Player: -";
            _currentPlayerLabel.AddThemeFontSizeOverride("font_size", 16);
            _currentPlayerLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.9f));
            _currentPlayerLabel.VerticalAlignment = VerticalAlignment.Center;
            _currentPlayerLabel.CustomMinimumSize = new Vector2(150, 0); // Increased width for player
            _currentPlayerLabel.HorizontalAlignment = HorizontalAlignment.Left;
            _currentPlayerLabel.AddThemeConstantOverride("margin_left", 15); // Left padding
            _currentPlayerLabel.AddThemeConstantOverride("margin_right", 15); // Right padding
            leftSection.AddChild(_currentPlayerLabel);

            var separator3 = new VSeparator();
            separator3.AddThemeColorOverride("separator", new Color(0.3f, 0.3f, 0.4f));
            leftSection.AddChild(separator3);

            _currentPhaseLabel = new Label();
            _currentPhaseLabel.Text = "Phase: -";
            _currentPhaseLabel.AddThemeFontSizeOverride("font_size", 16);
            _currentPhaseLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.9f));
            _currentPhaseLabel.VerticalAlignment = VerticalAlignment.Center;
            _currentPhaseLabel.CustomMinimumSize = new Vector2(130, 0); // Increased width for phase
            _currentPhaseLabel.HorizontalAlignment = HorizontalAlignment.Left;
            _currentPhaseLabel.AddThemeConstantOverride("margin_left", 15); // Left padding
            _currentPhaseLabel.AddThemeConstantOverride("margin_right", 15); // Right padding
            leftSection.AddChild(_currentPhaseLabel);

            topBarContainer.AddChild(new Control()); // Spacer

            var rightSection = new HBoxContainer();
            rightSection.AddThemeConstantOverride("separation", 10);
            topBarContainer.AddChild(rightSection);

            _nextPhaseButton = new Button();
            _nextPhaseButton.Text = "Next Phase";
            _nextPhaseButton.AddThemeFontSizeOverride("font_size", 12);
            _nextPhaseButton.CustomMinimumSize = new Vector2(100, 40);
            
            var buttonStyle = new StyleBoxFlat();
            buttonStyle.BgColor = new Color(0.2f, 0.4f, 0.6f);
            buttonStyle.CornerRadiusTopLeft = 4;
            buttonStyle.CornerRadiusTopRight = 4;
            buttonStyle.CornerRadiusBottomLeft = 4;
            buttonStyle.CornerRadiusBottomRight = 4;
            _nextPhaseButton.AddThemeStyleboxOverride("normal", buttonStyle);
            
            var buttonHoverStyle = new StyleBoxFlat();
            buttonHoverStyle.BgColor = new Color(0.25f, 0.45f, 0.65f);
            buttonHoverStyle.CornerRadiusTopLeft = 4;
            buttonHoverStyle.CornerRadiusTopRight = 4;
            buttonHoverStyle.CornerRadiusBottomLeft = 4;
            buttonHoverStyle.CornerRadiusBottomRight = 4;
            _nextPhaseButton.AddThemeStyleboxOverride("hover", buttonHoverStyle);
            
            rightSection.AddChild(_nextPhaseButton);
        }

        private void CreateBottomBar()
        {
            _bottomBar = new Panel();
            _bottomBar.Name = "BottomBar";
            // Bottom wide preset
            _bottomBar.AnchorLeft = 0;
            _bottomBar.AnchorTop = 1;
            _bottomBar.AnchorRight = 1;
            _bottomBar.AnchorBottom = 1;
            _bottomBar.Size = new Vector2(0, 40);
            
            var bottomBarStyle = new StyleBoxFlat();
            bottomBarStyle.BgColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
            bottomBarStyle.BorderColor = new Color(0.2f, 0.2f, 0.3f);
            bottomBarStyle.BorderWidthTop = 2;
            _bottomBar.AddThemeStyleboxOverride("panel", bottomBarStyle);
            
            AddChild(_bottomBar);

            var bottomBarContainer = new HBoxContainer();
            // Full rect preset
            bottomBarContainer.AnchorLeft = 0;
            bottomBarContainer.AnchorTop = 0;
            bottomBarContainer.AnchorRight = 1;
            bottomBarContainer.AnchorBottom = 1;
            bottomBarContainer.AddThemeConstantOverride("separation", 10);
            _bottomBar.AddChild(bottomBarContainer);

            var statusLabel = new Label();
            statusLabel.Text = "Strategic War Simulation";
            statusLabel.AddThemeFontSizeOverride("font_size", 12);
            statusLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.8f));
            statusLabel.VerticalAlignment = VerticalAlignment.Center;
            bottomBarContainer.AddChild(statusLabel);
        }

        private void CreateSidebars()
        {
            CreateRightSidebar();
        }

        private void CreateRightSidebar()
        {
            _rightSidebar = new Panel();
            _rightSidebar.Name = "RightSidebar";
            
            // Use absolute positioning to ensure it works
            var viewportSize = GetViewport().GetVisibleRect().Size;
            _rightSidebar.Position = new Vector2(viewportSize.X - 200, 60);
            _rightSidebar.Size = new Vector2(200, viewportSize.Y - 100); // 60 for top bar + 40 for bottom bar
            
            var sidebarStyle = new StyleBoxFlat();
            sidebarStyle.BgColor = new Color(0.1f, 0.1f, 0.15f, 0.9f);
            sidebarStyle.BorderColor = new Color(0.2f, 0.2f, 0.3f);
            sidebarStyle.BorderWidthLeft = 2;
            _rightSidebar.AddThemeStyleboxOverride("panel", sidebarStyle);
            
            AddChild(_rightSidebar);

            var sidebarContainer = new VBoxContainer();
            // Full rect preset
            sidebarContainer.AnchorLeft = 0;
            sidebarContainer.AnchorTop = 0;
            sidebarContainer.AnchorRight = 1;
            sidebarContainer.AnchorBottom = 1;
            sidebarContainer.AddThemeConstantOverride("separation", 10);
            _rightSidebar.AddChild(sidebarContainer);

            CreateMapControls(sidebarContainer);
            CreateZoomControls(sidebarContainer);
        }

        private void CreateMapControls(VBoxContainer parent)
        {
            _mapControlsPanel = new Panel();
            _mapControlsPanel.CustomMinimumSize = new Vector2(0, 160);
            
            var controlsStyle = new StyleBoxFlat();
            controlsStyle.BgColor = new Color(0.12f, 0.12f, 0.18f, 0.8f);
            controlsStyle.CornerRadiusTopLeft = 6;
            controlsStyle.CornerRadiusTopRight = 6;
            controlsStyle.CornerRadiusBottomLeft = 6;
            controlsStyle.CornerRadiusBottomRight = 6;
            _mapControlsPanel.AddThemeStyleboxOverride("panel", controlsStyle);
            
            parent.AddChild(_mapControlsPanel);

            var controlsContainer = new VBoxContainer();
            controlsContainer.Position = new Vector2(10, 10);
            controlsContainer.Size = new Vector2(180, 140);
            controlsContainer.AddThemeConstantOverride("separation", 8);
            _mapControlsPanel.AddChild(controlsContainer);

            var titleLabel = new Label();
            titleLabel.Text = "MAP CONTROLS";
            titleLabel.AddThemeFontSizeOverride("font_size", 12);
            titleLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.9f, 1.0f));
            controlsContainer.AddChild(titleLabel);

            var mapTypeLabel = new Label();
            mapTypeLabel.Text = "Map Type:";
            mapTypeLabel.AddThemeFontSizeOverride("font_size", 10);
            mapTypeLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.8f));
            controlsContainer.AddChild(mapTypeLabel);

            _mapTypeSelector = new OptionButton();
            _mapTypeSelector.AddItem("Continental");
            _mapTypeSelector.AddItem("Archipelago");
            _mapTypeSelector.AddItem("Highland");
            _mapTypeSelector.AddItem("Desert");
            _mapTypeSelector.AddItem("Wetlands");
            _mapTypeSelector.AddItem("Volcanic");
            _mapTypeSelector.Selected = 0;
            controlsContainer.AddChild(_mapTypeSelector);

            _regenerateMapButton = new Button();
            _regenerateMapButton.Text = "Regenerate Map";
            _regenerateMapButton.AddThemeFontSizeOverride("font_size", 10);
            controlsContainer.AddChild(_regenerateMapButton);

            // Add Start Game button
            _startGameButton = new Button();
            _startGameButton.Text = "Start Game";
            _startGameButton.AddThemeFontSizeOverride("font_size", 14);
            _startGameButton.CustomMinimumSize = new Vector2(0, 35);
            
            var startButtonStyle = new StyleBoxFlat();
            startButtonStyle.BgColor = new Color(0.2f, 0.6f, 0.2f);
            startButtonStyle.CornerRadiusTopLeft = 4;
            startButtonStyle.CornerRadiusTopRight = 4;
            startButtonStyle.CornerRadiusBottomLeft = 4;
            startButtonStyle.CornerRadiusBottomRight = 4;
            _startGameButton.AddThemeStyleboxOverride("normal", startButtonStyle);
            
            var startButtonHoverStyle = new StyleBoxFlat();
            startButtonHoverStyle.BgColor = new Color(0.25f, 0.65f, 0.25f);
            startButtonHoverStyle.CornerRadiusTopLeft = 4;
            startButtonHoverStyle.CornerRadiusTopRight = 4;
            startButtonHoverStyle.CornerRadiusBottomLeft = 4;
            startButtonHoverStyle.CornerRadiusBottomRight = 4;
            _startGameButton.AddThemeStyleboxOverride("hover", startButtonHoverStyle);
            
            controlsContainer.AddChild(_startGameButton);
        }

        private void CreateZoomControls(VBoxContainer parent)
        {
            var zoomPanel = new Panel();
            zoomPanel.CustomMinimumSize = new Vector2(0, 80);
            
            var zoomStyle = new StyleBoxFlat();
            zoomStyle.BgColor = new Color(0.12f, 0.12f, 0.18f, 0.8f);
            zoomStyle.CornerRadiusTopLeft = 6;
            zoomStyle.CornerRadiusTopRight = 6;
            zoomStyle.CornerRadiusBottomLeft = 6;
            zoomStyle.CornerRadiusBottomRight = 6;
            zoomPanel.AddThemeStyleboxOverride("panel", zoomStyle);
            
            parent.AddChild(zoomPanel);

            var zoomContainer = new VBoxContainer();
            zoomContainer.Position = new Vector2(10, 10);
            zoomContainer.Size = new Vector2(180, 60);
            zoomContainer.AddThemeConstantOverride("separation", 5);
            zoomPanel.AddChild(zoomContainer);

            var titleLabel = new Label();
            titleLabel.Text = "ZOOM";
            titleLabel.AddThemeFontSizeOverride("font_size", 12);
            titleLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.9f, 1.0f));
            zoomContainer.AddChild(titleLabel);

            _zoomLabel = new Label();
            _zoomLabel.Text = "100%";
            _zoomLabel.AddThemeFontSizeOverride("font_size", 10);
            _zoomLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.8f));
            zoomContainer.AddChild(_zoomLabel);

            _zoomSlider = new HSlider();
            _zoomSlider.MinValue = 0.5;
            _zoomSlider.MaxValue = 2.0;
            _zoomSlider.Value = 1.0;
            _zoomSlider.Step = 0.1;
            _zoomSlider.AllowGreater = false;
            _zoomSlider.AllowLesser = false;
            zoomContainer.AddChild(_zoomSlider);
        }

        private void CreateGameArea()
        {
            _gameArea = new Control();
            _gameArea.Name = "GameArea";
            // Full rect preset
            _gameArea.AnchorLeft = 0;
            _gameArea.AnchorTop = 0;
            _gameArea.AnchorRight = 1;
            _gameArea.AnchorBottom = 1;
            _gameArea.OffsetTop = 60;
            _gameArea.OffsetBottom = -40;
            _gameArea.OffsetRight = -200;
            _gameArea.MouseFilter = Control.MouseFilterEnum.Ignore;
            AddChild(_gameArea);
        }

        public void UpdatePlayerInfo(string playerName, string phase, int turn = 1)
        {
            if (_currentTurnLabel != null)
                _currentTurnLabel.Text = $"Turn: {turn}";
            if (_currentPlayerLabel != null)
                _currentPlayerLabel.Text = $"Player: {playerName}";
            if (_currentPhaseLabel != null)
                _currentPhaseLabel.Text = $"Phase: {phase}";
        }

        public Button GetNextPhaseButton() => _nextPhaseButton;
        public OptionButton GetMapTypeSelector() => _mapTypeSelector;
        public Button GetRegenerateMapButton() => _regenerateMapButton;
        public Button GetStartGameButton() => _startGameButton;
        public HSlider GetZoomSlider() => _zoomSlider;
        public Label GetZoomLabel() => _zoomLabel;
        public Control GetGameArea() => _gameArea;

        public void HideStartButton()
        {
            if (_startGameButton != null)
            {
                _startGameButton.Visible = false;
            }
        }
    }
}
