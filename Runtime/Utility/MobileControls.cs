using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class MobileControls : MonoBehaviour
{
    public bool isMobile = true;
    public void toggleControls(string state){
        Debug.Log("Toggling controls");
        gameObject.SetActive(state == "true" ? true : false);
        isMobile = state == "true" ? true : false;
    }
    
}