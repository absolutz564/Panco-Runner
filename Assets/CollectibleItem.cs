using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    public float rotateSpeed = 90f;

    void Update()
    {
        // só pra dar um visual de rodando
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameController gc = FindObjectOfType<GameController>();
            if (gc != null)
                gc.OnItemCollected();

            Animator anim = GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetTrigger("Collect");
                Destroy(gameObject.transform.parent.gameObject, 0.5f); // espera a animação
            }
            else
            {
                Destroy(gameObject.transform.parent.gameObject);
            }
        }
    }
}