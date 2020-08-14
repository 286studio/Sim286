using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyInSecond : MonoBehaviour
{
    public float destroyInSecond;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine("Exec");
    }

    IEnumerator Exec()
    {
        yield return new WaitForSeconds(destroyInSecond);
        Destroy(gameObject);
    }
}
