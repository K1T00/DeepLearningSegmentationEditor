using AnnotationTool.Core.Models;

namespace AnnotationTool.Core.Interaction
{
    /// <summary>
    /// Central authority for user interaction mode.
    /// Exactly one mode is active at any time.
    /// Supports temporary overrides (e.g. space-bar pan).
    /// </summary>
    public sealed class InteractionModeController
    {
        private InteractionMode current;
        private InteractionMode? temporaryOverride;

        public InteractionModeController()
        {
            current = InteractionMode.None;
        }

        /// <summary>
        /// The effective interaction mode (override > base).
        /// </summary>
        public InteractionMode ActiveMode
        {
            get { return temporaryOverride ?? current; }
        }

        /// <summary>
        /// Sets the base interaction mode (toolbar buttons, etc.).
        /// Clears any temporary override.
        /// </summary>
        public void SetMode(InteractionMode mode)
        {
            current = mode;
            temporaryOverride = null;
        }

        /// <summary>
        /// Temporarily overrides the current mode.
        /// Example: holding Space switches to Pan.
        /// </summary>
        public void PushTemporaryMode(InteractionMode mode)
        {
            if (!temporaryOverride.HasValue)
                temporaryOverride = mode;
        }

        /// <summary>
        /// Removes the temporary override, restoring base mode.
        /// </summary>
        public void PopTemporaryMode()
        {
            temporaryOverride = null;
        }

        /// <summary>
        /// Convenience helpers.
        /// </summary>
        public bool Is(InteractionMode mode)
        {
            return ActiveMode == mode;
        }

        public override string ToString()
        {
            return ActiveMode.ToString();
        }
    }
}
