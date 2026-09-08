using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomGem : MonoBehaviour
{
    public GameObject[] Gems;


    // Start is called before the first frame update
    void Awake()
    {
        int index = Random.Range(0, Gems.Length);
        GameObject prefab = Instantiate(Gems[index]);

        prefab.transform.parent = transform;
        prefab.transform.localPosition = Vector3.zero;

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
