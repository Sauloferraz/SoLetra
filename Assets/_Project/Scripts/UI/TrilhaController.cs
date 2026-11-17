using UnityEngine;
using UnityEngine.SceneManagement;

public class TrilhaController : MonoBehaviour
{
    public void VoltarAoMenu()
    {
        SceneManager.LoadScene("Menu");
    }
}
