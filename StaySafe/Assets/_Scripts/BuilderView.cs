using UnityEngine;
using UnityEngine.UI;

public class BuilderView : View
{
    [SerializeField] private Button buildBtn;

    private void OnDisable()
    {
        buildBtn.onClick.RemoveAllListeners();
    }
    public void Connect(Builder builder)
    {
        buildBtn.onClick.AddListener(() => builder.Build());
    }

    private void Update()
    {
        buildBtn.onClick.GetPersistentEventCount();
    }
}
