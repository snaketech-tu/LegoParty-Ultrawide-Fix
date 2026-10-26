using System;

namespace UnityEngine.EventSystems
{
    public class UIBehaviour : MonoBehaviour
    {
        public UIBehaviour(IntPtr pointer) : base(pointer) { }
    }
}

namespace UnityEngine.UI
{
    public class AspectRatioFitter : EventSystems.UIBehaviour
    {
        public AspectRatioFitter(IntPtr pointer) : base(pointer) { }
        public float aspectRatio { get => throw null; set => throw null; }
    }

    // The real chain is Image : MaskableGraphic : Graphic : UIBehaviour; only Behaviour.enabled is used.
    public class Image : EventSystems.UIBehaviour
    {
        public Image(IntPtr pointer) : base(pointer) { }
    }
}
