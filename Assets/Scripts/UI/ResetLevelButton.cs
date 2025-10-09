using UnityEngine;
using UnityEngine.SceneManagement;

public class ResetLevelButton : MonoBehaviour
{
	public void OnClickReset()
	{
		Time.timeScale = 1f;
		Scene active = SceneManager.GetActiveScene();
		SceneManager.LoadScene(active.buildIndex);
	}
}


