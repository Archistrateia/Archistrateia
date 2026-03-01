using Godot;
using System;

namespace Archistrateia
{
    public sealed class MainInputPolicyController
    {
        private readonly Func<InputEventKey, bool> _handleViewportInput;
        private readonly Action _advancePhaseWithSideEffects;

        public MainInputPolicyController(
            Func<InputEventKey, bool> handleViewportInput,
            Action advancePhaseWithSideEffects)
        {
            _handleViewportInput = handleViewportInput ?? throw new ArgumentNullException(nameof(handleViewportInput));
            _advancePhaseWithSideEffects = advancePhaseWithSideEffects ?? throw new ArgumentNullException(nameof(advancePhaseWithSideEffects));
        }

        public bool HandlePhaseInput(InputEventKey keyEvent)
        {
            if (keyEvent.Keycode != Key.Space)
            {
                return false;
            }

            _advancePhaseWithSideEffects();
            return true;
        }

        public bool HandleViewportInput(InputEventKey keyEvent)
        {
            return _handleViewportInput(keyEvent);
        }
    }
}
