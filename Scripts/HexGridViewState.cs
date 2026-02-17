using Godot;

namespace Archistrateia
{
    public sealed class HexGridViewState
    {
        private float _zoomFactor = 1.0f;
        private Vector2 _scrollOffset = Vector2.Zero;

        public float ZoomFactor
        {
            get => _zoomFactor;
            set => _zoomFactor = Mathf.Clamp(value, 0.1f, 3.0f);
        }

        public Vector2 ScrollOffset
        {
            get => _scrollOffset;
            set => _scrollOffset = value;
        }
    }
}
