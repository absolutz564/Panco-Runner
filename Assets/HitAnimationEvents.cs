using UnityEngine;

public class HitAnimationEvents : MonoBehaviour
{
    public void OnHitAnimationEnd()
    {
        // avisa o GameController para liberar o movimento
        var gc = FindObjectOfType<GameController>();
        if (gc != null)
        {
            gc.OnHitAnimationEnd();   // método que você cria no GameController
        }
    }
}