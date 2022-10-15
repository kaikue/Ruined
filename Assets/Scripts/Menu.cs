using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public UnityEvent startEvent;
    public GameObject loadingScreenPrefab;

	private void Update()
	{
		if (Input.GetButtonDown("Start"))
		{
            startEvent.Invoke();
		}
	}

	public void LoadStart()
    {
        Persistent p = FindObjectOfType<Persistent>();
        if (p != null)
        {
            SceneManager.sceneLoaded -= p.OnSceneLoaded;
            Destroy(p.gameObject);
        }
        SceneManager.LoadScene(0);
    }

    public void LoadGame()
    {
        SceneManager.LoadScene(1);
        Instantiate(loadingScreenPrefab);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
