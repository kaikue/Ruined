using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
	private const int SHAKES = 10;
	private const float SHAKE_DISTANCE = 0.2f;
	private const float SHAKE_TIME = 0.02f;

	public void Shake(float intensity = 1)
	{
		StartCoroutine(CrtShake(intensity));
	}

	private IEnumerator CrtShake(float intensity)
	{
		int numShakes = Mathf.RoundToInt(SHAKES * intensity);
		for (int i = 0; i < numShakes; i++)
		{
			float shakeAmount = SHAKE_DISTANCE * intensity;
			Camera.main.transform.localPosition = new Vector3(Random.Range(-shakeAmount, shakeAmount), Random.Range(-shakeAmount, shakeAmount), 0);
			yield return new WaitForSeconds(SHAKE_TIME);
		}
		Camera.main.transform.localPosition = Vector3.zero;
	}

}
