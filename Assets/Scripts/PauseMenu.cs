using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    static GameObject Instance;
    [SerializeField] Color ActiveButtonColor;
    [SerializeField] GameObject ContinueButton, SaveButton, SaveAsButton, ExportToImageButton;
    GameObject[] TurnableButtons => new GameObject[]{ ContinueButton, SaveAsButton, SaveButton , ExportToImageButton };
    [SerializeField] Image BackGround;
    [SerializeField] Transform MenuWindow;

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = gameObject;
        }
        ApplyStatusOfButtons();
        UserInput.Deactivate();
        AnimateWindow(true);
    }

    void ApplyStatusOfButtons()
    {
        if (TurnableButtons == null || TurnableButtons.Length == 0) return;
        bool isMapActive = false;
        for (int i=0;i< TurnableButtons.Length; i++)
        {
            if (TurnableButtons[i] == SaveButton) isMapActive = Map.IsMapExist() && Map.IOSystem.IsLastPathKnown();
            else isMapActive = Map.IsMapExist();
            TurnableButtons[i].GetComponent<MaskableGraphic>().color = isMapActive ? ActiveButtonColor : Color.gray;
            TurnableButtons[i].GetComponent<Button>().enabled = isMapActive;
        }
    }

    async void AnimateWindow(bool isStarted)
    {
        if (isStarted) Instance.SetActive(true);
        RectTransform MenuRect =  MenuWindow.GetComponent<RectTransform>();
        MaskableGraphic[] maskableGraphic = MenuWindow.GetComponentsInChildren<MaskableGraphic>();
        Color[] TransparentColors = new Color[maskableGraphic.Length];
        Color[] NormalColors = new Color[maskableGraphic.Length];
        for (int i=0; i < TransparentColors.Length; i++)
        {
            TransparentColors[i] = maskableGraphic[i].color;
            NormalColors[i] = maskableGraphic[i].color;
            NormalColors[i] = new Color(NormalColors[i].r, NormalColors[i].g, NormalColors[i].b, 1);
            TransparentColors[i] = new Color(TransparentColors[i].r, TransparentColors[i].g, TransparentColors[i].b, 0);
        }
        float Lerp = 0;
        for (int i=0; i <= 90; i += 5)
        {
            Lerp = Mathf.Sin(i * Mathf.Deg2Rad);
            if (!isStarted) Lerp = 1 - Lerp;
            BackGround.color = Color.Lerp(Color.clear, Color.black * 0.7f, Lerp);
            for (int z = 0; z< maskableGraphic.Length; z++)
            {
                maskableGraphic[z].color = Color.Lerp(TransparentColors[z], NormalColors[z], Lerp);
            }
            MenuRect.anchoredPosition = 50 * Vector2.down * (1-Lerp);
            await System.Threading.Tasks.Task.Delay(10);
        }
        if (!isStarted) Instance.SetActive(false);
    }

    public static void PickPause()
    {
        Instance.SetActive(true);
    }

    public void PickContinue()
    {
        CloseMenu();
    }

    public void PickCreateNew()
    {
        Map.CreateNewMap();
        CloseMenu();
    }

    public async void PickSaveMap()
    {
        if (await Map.IOSystem.TrySaveToKnownPath())
        {
            CloseMenu();
        }
    }

    public async void PickSaveAsMap()
    {
        if (await Map.IOSystem.TrySaveAsNewMap())
        {
            CloseMenu();
        }
    }

    public async void PickLoadMap()
    {
        if (await Map.IOSystem.TryLoadMap())
        {
            Map.ChangeSheet(0);
            CloseMenu();
        }
    }

    public void PickExportButton()
    {
        Map.Screenshoter.RenderAsNewImage();
    }

    public void PickQuit()
    {
        Application.Quit();
    }

    void CloseMenu()
    {
        UserInput.Activate();
        AnimateWindow(false);
    }
}
