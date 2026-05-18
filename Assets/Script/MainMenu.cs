using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public Button starterButton;

    void Start()
    {
        starterButton.onClick.AddListener(KeStarter);
    }

    void KeStarter()
    {
        SceneManager.LoadScene("Starter"); 
    }
}
