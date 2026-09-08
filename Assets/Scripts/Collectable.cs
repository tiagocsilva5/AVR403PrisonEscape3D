using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectable : MonoBehaviour
{

    ParticleSystem effect;
    // Start is called before the first frame update
    void Start()
    {
        effect = GetComponentInChildren<ParticleSystem>();
        effect.Pause();
        GetComponent<Animator>().Rebind();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collect");
        StartCoroutine(Collected());
        
    }

    IEnumerator Collected()
    {
        GetComponent<Animator>().SetTrigger("Collected");
        GetComponent<Collider>().enabled = false;
        GetComponent<AudioSource>().Play();
        effect.Play();
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }


}
