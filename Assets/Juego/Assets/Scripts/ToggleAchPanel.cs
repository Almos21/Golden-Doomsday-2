using UnityEngine;

public class ToggleAchPanel : MonoBehaviour
{
    public GameObject panel;

    public KeyCode togglekey = KeyCode.I;

    void Update()
    {
        if (Input.GetKeyDown(togglekey))
        {
            panel.SetActive(!panel.activeSelf);
        }
    }
}
