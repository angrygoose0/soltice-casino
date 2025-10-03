using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;

namespace BalloonFX
{

public class BalloonFXSceneSelect : MonoBehaviour
{
	private bool GUIHide01 = false;
	private bool GUIHide02 = false;
	
	//Scenes
	
    public void LoadSceneBalloon01()	{ SceneManager.LoadScene("BalloonFX01");		}
	public void LoadSceneBalloon02()	{ SceneManager.LoadScene("BalloonFX02"); 		}
	public void LoadSceneBalloon03()	{ SceneManager.LoadScene("BalloonFX03"); 		}
	public void LoadSceneBalloon04()	{ SceneManager.LoadScene("BalloonFX04"); 		}
	
	 void Update()
	{
		CheckKeyCode(KeyCode.J, ref GUIHide01, "SceneCanvas");
		CheckKeyCode(KeyCode.K, ref GUIHide02, "ExtraCanvas");
	}

	void CheckKeyCode(KeyCode keyCode, ref bool guiHide, string canvasName)
	{
		if (Input.GetKeyDown(keyCode))
		{
			guiHide = !guiHide;
			GameObject canvasObject = GameObject.Find(canvasName);
			if (canvasObject != null)
			{
				Canvas canvasComponent = canvasObject.GetComponent<Canvas>();
				if (canvasComponent != null)
				{
					canvasComponent.enabled = !guiHide;
				}
			}
		}
	}
}

}