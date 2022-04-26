using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MarkDecorator : MapObjectBaseClass
{
    public override System.Type ControlledType => typeof(Mark);

    protected override MapObjectDecorator CreateObject(MapObject DataObject)
    {
        Mark MarkData = (Mark)DataObject;
        var Type = TryGetMarkType(MarkData);
        GameObject MarkObject = new GameObject();
        MarkObject.tag = "MapObject";
        MarkObject.layer = 5;
        MarkObject.transform.SetParent(Map.MapCanvas);
        MapObjectDecorator Decorator = new MapObjectDecorator(MarkData, MarkObject);
        return Decorator;
    }

    public override void RefreshSceneView(MapObjectDecorator PickedMark)
    {
        var MarkObj = PickedMark.ObjectOnScene;
        if (MarkObj == null) return;
        Mark Data = PickedMark.DataReference as Mark;
        var Type = TryGetMarkType(Data);
        if (MarkObj.GetComponent<RectTransform>() == null) MarkObj.AddComponent<RectTransform>();
        if (MarkObj.GetComponent<Image>() == null) MarkObj.AddComponent<Image>();
        MarkObj.GetComponent<Image>().sprite = Type.SpriteReference;
    }

    public override void RefreshTransform(MapObjectDecorator RefreshedObject)
    {
        if (RefreshedObject.ObjectOnScene == null) return;
        Mark Data = RefreshedObject.DataReference as Mark;
        var Type = TryGetMarkType(Data);
        RectTransform Rect = RefreshedObject.ObjectOnScene.GetComponent<RectTransform>();
        Rect.position = (Vector3)MapScaler.GetPositionInWorld(Data.PosOnCanvas) + Vector3.forward * Rect.position.z;
        Rect.eulerAngles = Vector3.forward * 90 * Data.RotationQuarter;
        Rect.sizeDelta = MapScaler.SheetScale * (Type.DefaultSize) * Data.ScaleOnCanvas;
    }

    static MarkType TryGetMarkType(Mark mark)
    {
        foreach (MarkCategory Category in Map.Reference.MarksCategories)
        {
            if (Category.CategoryName == mark.CategoryName)
            {
                foreach (var Type in Category.MarkTypes)
                {
                    if (Type.TypeName == mark.TypeName)
                    {
                        return Type;
                    }
                }
            }
        }
        return null;
    }
}


[System.Serializable]
class MarkController : ControllerCategory
{
    public override System.Type ControlledType => typeof(Mark);
    Mark KnowedMark;
    MarkCategory PreviousPickedCategory;
    [Space(40), SerializeField] Dropdown MarkTypesDropdown;
    [SerializeField] Slider MarkScaleSlider;
    [SerializeField] Button RotateMarkButton;

    string PickedTypeName;
    int PickedRotationQuarter;
    float PickedScale;

    public override void InitCustomLogic()
    {
        ApplyScaleLogic();
        ApplyRotationLogic();
    }

    public override void HideAllMenus()
    {
        base.HideAllMenus();
        PreviousPickedCategory = null;
    }

    protected override void SavePickedObjectData()
    {
        Mark CurrentMark = (Mark)Decorator.DataReference;
        foreach (var Category in Map.Reference.MarksCategories)
        {
            if (Category.CategoryName == CurrentMark.CategoryName)
            {
                PreviousPickedCategory = Category;
                break;
            }
        }
        KnowedMark = new Mark()
        {
            PosOnCanvas = CurrentMark.PosOnCanvas,
            RotationQuarter = CurrentMark.RotationQuarter,
            ScaleOnCanvas = CurrentMark.ScaleOnCanvas,
            CategoryName = CurrentMark.CategoryName,
            TypeName = CurrentMark.TypeName
        };
        PickedTypeName = CurrentMark.TypeName;
        PickedRotationQuarter = CurrentMark.RotationQuarter;
        PickedScale = CurrentMark.ScaleOnCanvas;
        
    }

    protected override void PickExistedAdditional()
    {
        RefreshTypesMenu();
    }

    void RefreshTypesMenu()
    {
        if (PreviousPickedCategory == null) return;
        List<Dropdown.OptionData> NewData = new List<Dropdown.OptionData>();
        int KnownValue = 0;
        for (int i=0; i < PreviousPickedCategory.MarkTypes.Count; i++)
        {
            Dropdown.OptionData NewOption = new Dropdown.OptionData(PreviousPickedCategory.MarkTypes[i].SpriteReference);
            NewData.Add(NewOption);
            if (KnowedMark!= null && KnowedMark.TypeName == PreviousPickedCategory.MarkTypes[i].TypeName)
            {
                KnownValue = i;
            }
        }
        MarkTypesDropdown.options = NewData;
        MarkTypesDropdown.onValueChanged.RemoveAllListeners();
        MarkTypesDropdown.value = KnownValue;
        MarkTypesDropdown.onValueChanged.AddListener((int PickedType) => 
            ApplyTypeOfMark(PreviousPickedCategory.MarkTypes[PickedType].TypeName));
        
    }

    void ApplyTypeOfMark(string NewTypeName)
    {
        PickedTypeName = NewTypeName;
        if (!isObjectExist()) return;
        (Decorator.DataReference as Mark).TypeName = PickedTypeName;
        RefreshDecoratorView();
        RefreshDecoratorTransform();
    }

    void ApplyScaleLogic()
    {
        MarkScaleSlider.onValueChanged.RemoveAllListeners();
        if (KnowedMark!= null)
        {
            MarkScaleSlider.value = KnowedMark.ScaleOnCanvas;
        }
        MarkScaleSlider.onValueChanged.AddListener(ApplyScaleOfMark);
    }

    void ApplyScaleOfMark(float newScale)
    {
        if (!isObjectExist()) return; 
        (Decorator.DataReference as Mark).ScaleOnCanvas = newScale;
        RefreshDecoratorTransform();
    }

    void ApplyRotationLogic()
    {
        RotateMarkButton.onClick.RemoveAllListeners();
        RotateMarkButton.onClick.AddListener(RotateMark);
    }

    void RotateMark()
    {
        if (!isObjectExist()) return; 
        if ((Decorator.DataReference as Mark).RotationQuarter == 3) (Decorator.DataReference as Mark).RotationQuarter = 0;
        else (Decorator.DataReference as Mark).RotationQuarter++;
        RefreshDecoratorTransform();
    }

    public bool IsCategoryAlreadyPicked(MarkCategory NewCategory)
    {
        return PreviousPickedCategory == NewCategory;
    }

    public void PickNewCategory(MarkCategory NewCategory)
    {
        if (PreviousPickedCategory == NewCategory)
        {
            PreviousPickedCategory = null;
            base.HideAllMenus();
            return;
        }
        base.ShowButtonsMenu();
        PreviousPickedCategory = NewCategory;
        PickedTypeName = PreviousPickedCategory.MarkTypes[0].TypeName;
        if (isObjectExist())
        {
            if (WorkMode == WorkModes.Modify && KnowedMark != null)
            {
                Mark CurrentMark = Decorator.DataReference as Mark;
                CurrentMark.PosOnCanvas = MapScaler.GetPositionForSaving(KnowedMark.PosOnCanvas);
                CurrentMark.RotationQuarter = KnowedMark.RotationQuarter;
                CurrentMark.ScaleOnCanvas = KnowedMark.ScaleOnCanvas;
                CurrentMark.TypeName = KnowedMark.TypeName;
                RefreshDecoratorView();
                CurrentMark = null;
            }
            if (WorkMode == WorkModes.CreateNew)
            {
                DeleteDecorator();
            }
        }
        RefreshTypesMenu();
    }

    public override void ApplyUserControl()
    {
        if (!isObjectExist()) return;
        if (Input.GetKeyDown(KeyCode.Q))
        {
            RotateMark();
        }
        if (!base.IsPositionSaved)
        {
            if (LastMousePosition != (Vector2)Input.mousePosition)
            {
                LastMousePosition = (Vector2)Input.mousePosition;
                (Decorator.DataReference as Mark).PosOnCanvas = MapScaler.GetPositionForSaving(UserInput.GetMousePoint());
                RefreshDecoratorTransform();
            }
            if (Input.GetMouseButtonDown(0))
            {
                if (ParentController.IsMouseClickedEditor()) return;
                IsPositionSaved = true;
            }
        }
    }

    protected override async System.Threading.Tasks.Task<MapObject> CreateNewObject() 
    => new Mark(PreviousPickedCategory.CategoryName, PickedTypeName, MarkScaleSlider.value);
}