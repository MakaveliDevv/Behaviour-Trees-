using TMPro;
using UnityEngine;

public class GuardStateUI : MonoBehaviour
{
    [SerializeField] private GuardEnemy guard;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] Camera playerCamera;

  private void LateUpdate()
    {
        stateText.text = guard.CurrentState.ToString();

        Transform uiTransform = stateText.transform.parent != null ? stateText.transform.parent : stateText.transform;
        uiTransform.LookAt(playerCamera.transform);
        uiTransform.Rotate(0f, 180f, 0f);
    }
}