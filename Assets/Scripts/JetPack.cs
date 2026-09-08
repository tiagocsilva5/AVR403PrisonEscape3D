using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JetPack : MonoBehaviour
{

    public float duration = 5f;

    public Transform attach;
    public Transform itemChild;

    Animator animator;
    Collider sphereCollider;

    Vector3 origPosition;

    public GameObject FT1, FT2;
    bool isActive;

    GameObject player;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        sphereCollider = GetComponent<Collider>();
        origPosition = transform.position;
        player = GameObject.FindGameObjectWithTag("Player");

        attach = FindDeepChild(player.transform, "mixamorig:Spine2");

    }

    // Update is called once per frame
    void Update()
    {
        if(isActive)
        {

            if (Input.GetKeyDown(KeyCode.Space))
                EnableBlasters(true);
            if (Input.GetKeyUp(KeyCode.Space))
                EnableBlasters(false);

        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
            StartCoroutine(PowerUp());
    }

    IEnumerator PowerUp()
    {

        ActivateJetpack(true);
        EnableBlasters(false);

        yield return new WaitForSeconds(duration);

        ActivateJetpack(false);

        yield return null;


    }

    void EnableBlasters(bool enable)
    {
        foreach(ParticleSystem PS in GetComponentsInChildren<ParticleSystem>())
        {
            var emission = PS.emission;
            emission.enabled = enable;

        }
        /*
        foreach (ParticleSystem PS in FT2.GetComponentsInChildren<ParticleSystem>())
        {
            var emission = PS.emission;
            emission.enabled = enable;

        }*/

    }

    void ActivateJetpack(bool  enable)
    {
        isActive = enable;

        

        animator.enabled = !enable;
        sphereCollider.enabled = !enable;

        //reset child
        itemChild.localPosition = Vector3.zero;
        itemChild.localRotation = Quaternion.identity;

        player.GetComponent<PlayerController>().jetPack = enable;
        player.GetComponentInChildren<Animator>().SetBool("isFlying", enable);

        FT1.SetActive(enable);
        FT2.SetActive(enable);

        //Set orientation of jetpack
        if(enable)
        { 
            transform.SetParent(attach, true);
            transform.localPosition = new Vector3(0, -1.361f, -0.141f);

        }
        else
        {
            transform.SetParent(null);
            transform.position = origPosition;
            itemChild.localPosition = Vector3.zero;
        }
        transform.localRotation = Quaternion.identity;
    }


    Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
}
