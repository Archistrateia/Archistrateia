using Godot;
using System;

namespace Archistrateia
{
    public sealed class MainUIBootstrapController
    {
        public sealed class MainUIBootstrapResult
        {
            public ModernUIManager UIManager { get; init; }
            public Archistrateia.Debug.DebugScrollOverlay DebugScrollOverlay { get; init; }
            public Button NextPhaseButton { get; init; }
            public OptionButton MapTypeSelector { get; init; }
            public Button RegenerateMapButton { get; init; }
            public Button StartButton { get; init; }
            public HSlider ZoomSlider { get; init; }
            public Label ZoomLabel { get; init; }
            public OptionButton PurchaseUnitSelector { get; init; }
            public Label PurchaseUnitDetailsLabel { get; init; }
            public Label PurchaseGoldLabel { get; init; }
            public Label PurchaseStatusLabel { get; init; }
            public Button PurchaseBuyButton { get; init; }
            public Button PurchaseCancelButton { get; init; }
        }

        public MainUIBootstrapResult CreateAndAttachUI(Node host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            var uiManager = new ModernUIManager
            {
                Name = "ModernUIManager"
            };
            host.AddChild(uiManager);

            var debugScrollOverlay = new Archistrateia.Debug.DebugScrollOverlay
            {
                Name = "DebugScrollOverlay"
            };
            host.AddChild(debugScrollOverlay);

            return new MainUIBootstrapResult
            {
                UIManager = uiManager,
                DebugScrollOverlay = debugScrollOverlay,
                NextPhaseButton = uiManager.GetNextPhaseButton(),
                MapTypeSelector = uiManager.GetMapTypeSelector(),
                RegenerateMapButton = uiManager.GetRegenerateMapButton(),
                StartButton = uiManager.GetStartGameButton(),
                ZoomSlider = uiManager.GetZoomSlider(),
                ZoomLabel = uiManager.GetZoomLabel(),
                PurchaseUnitSelector = uiManager.GetPurchaseUnitSelector(),
                PurchaseUnitDetailsLabel = uiManager.GetPurchaseUnitDetailsLabel(),
                PurchaseGoldLabel = uiManager.GetPurchaseGoldLabel(),
                PurchaseStatusLabel = uiManager.GetPurchaseStatusLabel(),
                PurchaseBuyButton = uiManager.GetPurchaseBuyButton(),
                PurchaseCancelButton = uiManager.GetPurchaseCancelButton()
            };
        }

        public void ConnectUISignals(
            MainUIBootstrapResult ui,
            Action onNextPhasePressed,
            Action<long> onMapTypeSelected,
            Action onRegenerateMapPressed,
            Action<double> onZoomChanged,
            Action onStartPressed,
            Action populatePurchaseSelector,
            Action<long> onPurchaseUnitSelected,
            Action onPurchaseBuyPressed,
            Action onPurchaseCancelPressed)
        {
            if (ui == null)
            {
                throw new ArgumentNullException(nameof(ui));
            }

            if (ui.NextPhaseButton != null)
            {
                ui.NextPhaseButton.Pressed += onNextPhasePressed;
            }

            if (ui.MapTypeSelector != null)
            {
                ui.MapTypeSelector.ItemSelected += index => onMapTypeSelected?.Invoke(index);
            }

            if (ui.RegenerateMapButton != null)
            {
                ui.RegenerateMapButton.Pressed += onRegenerateMapPressed;
            }

            if (ui.ZoomSlider != null)
            {
                ui.ZoomSlider.ValueChanged += value => onZoomChanged?.Invoke(value);
            }

            if (ui.StartButton != null)
            {
                ui.StartButton.Pressed += onStartPressed;
            }

            if (ui.PurchaseUnitSelector != null)
            {
                populatePurchaseSelector?.Invoke();
                ui.PurchaseUnitSelector.ItemSelected += index => onPurchaseUnitSelected?.Invoke(index);
            }

            if (ui.PurchaseBuyButton != null)
            {
                ui.PurchaseBuyButton.Pressed += onPurchaseBuyPressed;
            }

            if (ui.PurchaseCancelButton != null)
            {
                ui.PurchaseCancelButton.Pressed += onPurchaseCancelPressed;
            }
        }
    }
}
