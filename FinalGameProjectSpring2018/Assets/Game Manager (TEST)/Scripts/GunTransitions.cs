﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunTransitions : MonoBehaviour {
	public Animator animator;
    public GameObject scopeOverlay;
    public GameObject weaponCamera;
    public GameObject crossHair;
    public Camera playerCam; //for referenced scopping
    public float scopeFOV = 15f; //field of view to be zoomed in
    private float normalFOV; //original fov
  
	private bool Scoped=false;
	public bool Reloading = false;
    public bool isRifle = false;
    public bool isShotgun= false;
    public bool isShooting = false;

	void Update () {
        if (Input.GetMouseButtonDown(1))
   
        {
            
                Scoped = !Scoped;
                animator.SetBool("isScoped", Scoped);
                if (Scoped)
                    StartCoroutine(OnScoped());
                else
                    OnUnscoped();

            
        }

		if (Input.GetKeyDown(KeyCode.R)) {
			Reloading = !Reloading;
			animator.SetBool ("reloading", Reloading);
			StartCoroutine (ReloadWait ());
		}

        if (Input.GetMouseButtonDown(0) && gameObject.GetComponentInParent<Camera>().gameObject.GetComponentInParent<Player>().currentGun == "shotgun") {
            isShotgun = !isShotgun;
            animator.SetBool("isShotgun", isShotgun);
            StartCoroutine(Shotgun_RecoilTime());

        }

        if ((Input.GetMouseButton(0) && gameObject.GetComponentInParent<Camera>().gameObject.GetComponentInParent<Player>().currentGun == "rifle" )|| (Input.GetMouseButton(0)&& gameObject.GetComponentInParent<Camera>().gameObject.GetComponentInParent<Player>().currentGun == "tommygun"))
        {
           // isRifle = !isRifle;
           // animator.SetBool("isRifle", isRifle);
           // StartCoroutine(Rifle_RecoilTime());

        }




    }
    IEnumerator Rifle_RecoilTime()
    {
        yield return new WaitForSeconds(0.3f);
        isRifle = false;
        animator.SetBool("isRifle", isRifle);
    }

    IEnumerator Shotgun_RecoilTime()
    {
        yield return new WaitForSeconds(0.2f);
        isShotgun = false;
        animator.SetBool("isShotgun", isShotgun);
    }

    IEnumerator ReloadWait(){
		yield return new WaitForSeconds (1.2f);
		Reloading = false;
		animator.SetBool ("reloading", Reloading);
	}

    IEnumerator OnScoped() {

        yield return new WaitForSeconds(0.15f);
        scopeOverlay.SetActive(true);
        weaponCamera.SetActive(false);
        crossHair.SetActive(false);

        normalFOV = playerCam.fieldOfView;
        playerCam.fieldOfView = scopeFOV;
        
    }

    void OnUnscoped() {
        scopeOverlay.SetActive(false);
        weaponCamera.SetActive(true);
        crossHair.SetActive(true);

        playerCam.fieldOfView = normalFOV;
    }
}
