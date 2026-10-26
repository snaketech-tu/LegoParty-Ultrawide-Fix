using System;
using UnityEngine;

namespace Koala.Minigames.UI
{
    // Real base class is Koala.Network NetworkUIObject.
    public class MinigamePIPGameView : MonoBehaviour
    {
        public MinigamePIPGameView(IntPtr pointer) : base(pointer) { }
        public RenderTexture _renderTexture { get => throw null; set => throw null; }
        public void CreateAndAssignNewRenderTexture() => throw null;
    }
}
