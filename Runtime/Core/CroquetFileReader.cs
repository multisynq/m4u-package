using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

// this is currently only needed in a WebGL deployment, where we have no way to carry out
// a synchronous fetch of an arbitrary file from the deployment site.  Its only use so far
// is in fetching the JS tools record (from which we learn the package version string).

[AddComponentMenu("Croquet/CroquetFileReader")]
public class CroquetFileReader : MonoBehaviour {
    public void Awake() {
        CroquetBuilder.FileReaderIsReady(this);
    }

    public void FetchFile(string url, Action<string> callback) {
        StartCoroutine(GetRequest(url, callback));
    }

    IEnumerator GetRequest(string url, Action<string> callback)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                callback(www.downloadHandler.text);
            }
            else
            {
                Debug.Log("Error: " + www.error);
                callback("");
            }
        } // The using block ensures www.Dispose() is called when this block is exited
    }
}
