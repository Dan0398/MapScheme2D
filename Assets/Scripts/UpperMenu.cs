using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;


[System.Serializable]
class UpperMenu
{
    //Для анимации основного меню
    [SerializeField] RectTransform UnwrapButton;
    [SerializeField] Image MenuMask;
    [SerializeField] Text CurrentSheetText;
    bool isMenuUnwrapped = false;

    //Для работы со списком страниц карт
    [SerializeField] Button PrevSheetButton, NextSheetButton, CurrentSheetButton;
    [SerializeField] Image ListOfSheetsMask;
    [SerializeField] GameObject ListSheetReference;
    List<GameObject> GeneratedSheetsList;

    [SerializeField] Slider MapScaleSlider;
    [SerializeField] Button SnapToggle;

    bool isListOfSheetsUnwrapped = false;

    bool InTransition = false;

    public void Init()
    {
        UnwrapButton.GetComponent<Button>().onClick.AddListener(() => SwitchMenuUnwrap());
        PrevSheetButton.onClick.AddListener(() => TryChangeSheet(Map.ActualSheet - 1));
        NextSheetButton.onClick.AddListener(() => TryChangeSheet(Map.ActualSheet + 1));
        CurrentSheetButton.onClick.AddListener(() => SwitchListOfSheetsUnwrap());
        PrepareSnaps();
        MapScaleSlider.onValueChanged.AddListener(ChangeSheetScale);
        Map.onSheetChanged += RefreshUI;
    }

    void PrepareSnaps()
    {
        MaskableGraphic SnapButton = SnapToggle.GetComponent<MaskableGraphic>();
        SnapButton.color = UserInput.IsUsingRound() ? Color.yellow: Color.white;
        SnapToggle.onClick.AddListener(()=>
        {
            UserInput.SwitchUseRound();
            SnapButton.color = UserInput.IsUsingRound() ? Color.yellow: Color.white;
        });
    }

    void RefreshUI()
    {
        MapScaleSlider.value = Map.MapData.MapSheets[Map.ActualSheet].Scale;
        CurrentSheetText.text = (Map.ActualSheet+1).ToString();
    }

    void ChangeSheetScale(float NewScale)
    {
        Map.MapData.MapSheets[Map.ActualSheet].Scale = NewScale;
        MapScaler.SheetScale = NewScale;
    }

    void SwitchMenuUnwrap()
    {
        if (InTransition) return;
        isMenuUnwrapped = !isMenuUnwrapped;
        PlayUnwrapMenuAnimation();
    }

    async void PlayUnwrapMenuAnimation()
    {
        if (isMenuUnwrapped) MenuMask.gameObject.SetActive(true);
        InTransition = true;
        float Lerp = 0;
        for (int i = 0; i <= 90; i += 5)
        {
            Lerp = Mathf.Sin(i * Mathf.Deg2Rad);
            if (isMenuUnwrapped) Lerp = 1 - Lerp;
            UnwrapButton.anchorMin = new Vector2(UnwrapButton.anchorMin.x, Lerp);
            UnwrapButton.anchorMax = new Vector2(UnwrapButton.anchorMax.x, Lerp);
            MenuMask.fillAmount = 1-Lerp;
            await Task.Delay(20);
        }
        if (!isMenuUnwrapped) MenuMask.gameObject.SetActive(false);
        InTransition = false;
    }

    void SwitchListOfSheetsUnwrap()
    {
        if (InTransition) return;
        isListOfSheetsUnwrapped = !isListOfSheetsUnwrapped;
        if (isListOfSheetsUnwrapped) RegenerateList();
        PlayUnwrapListOfSheetsAnimation();
    }

    void RegenerateList()
    {
        TryRemoveOldSheetButtons();
        CreateNewSheetsList();
    }

    void TryRemoveOldSheetButtons()
    {
        if (GeneratedSheetsList == null || GeneratedSheetsList.Count == 0) return;
        for (int i= GeneratedSheetsList.Count - 1; i >= 0; i--)
        {
            if (GeneratedSheetsList[i] == null) continue;
            GeneratedSheetsList[i].transform.GetChild(0).GetComponent<Button>().onClick.RemoveAllListeners();
            GeneratedSheetsList[i].transform.GetChild(1).GetComponent<Button>().onClick.RemoveAllListeners();
        GameObject.Destroy(GeneratedSheetsList[i]);
        }
    }

    void CreateNewSheetsList()
    {
        if (GeneratedSheetsList == null) GeneratedSheetsList = new List<GameObject>();
        for (int i=0; i < Map.MapData.MapSheets.Count; i++)
        {
            int SheetNumber = i;
            GameObject NewLine = GameObject.Instantiate(ListSheetReference);
            NewLine.transform.SetParent(ListSheetReference.transform.parent);
            NewLine.SetActive(true);
            NewLine.GetComponentInChildren<Text>().text = (SheetNumber+1).ToString();
            NewLine.transform.GetChild(0).GetComponent<MaskableGraphic>().color = (SheetNumber == Map.ActualSheet ? Color.yellow : Color.white);
            NewLine.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => TryChangeSheet(SheetNumber));
            NewLine.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(() => TryDeleteSheet(SheetNumber));
            GeneratedSheetsList.Add(NewLine);
        }
    }

    async void PlayUnwrapListOfSheetsAnimation()
    {
        if (isListOfSheetsUnwrapped) ListOfSheetsMask.gameObject.SetActive(true);
        InTransition = true;
        float Lerp = 0;
        for (int i=0; i <= 90; i += 10)
        {
            Lerp = Mathf.Sin(i * Mathf.Deg2Rad);
            if (!isListOfSheetsUnwrapped) Lerp = 1 - Lerp;
            ListOfSheetsMask.fillAmount = Lerp;
            await Task.Delay(10);
        }
        if (!isListOfSheetsUnwrapped) ListOfSheetsMask.gameObject.SetActive(false);
        InTransition = false;
    }

    public void TryChangeSheet(int NewSheet)
    {
        if (NewSheet < 0 || NewSheet == Map.ActualSheet) return;
        if (isListOfSheetsUnwrapped) SwitchListOfSheetsUnwrap();
        RefreshUI();
        Map.ChangeSheet(NewSheet);
    }

    public async void TryDeleteSheet(int SheetUnderDelete)
    {
        await Task.Yield();
        Map.DeleteSheet(SheetUnderDelete);
        await Task.Yield();
        RefreshUI();
        RegenerateList();
    }
}