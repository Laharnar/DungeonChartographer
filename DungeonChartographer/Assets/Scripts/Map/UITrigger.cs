using UnityEngine;

public class UITrigger : MonoBehaviour
{

    [SerializeField] string uiKey = "empty";

    public void OnUITrigger()
    {
        UIManager.GetUI(uiKey).Run("End turn");
    }
}