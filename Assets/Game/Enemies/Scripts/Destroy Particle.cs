using System.Collections;
using UnityEngine;

public class DestroyParticle : MonoBehaviour
{
    [SerializeField] private float destroyTime = 1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(destruction());
    }

    private IEnumerator destruction()
    {
        yield return new WaitForSeconds(destroyTime);
        Destroy(this.gameObject);
    }
}
