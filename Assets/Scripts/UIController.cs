using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIController : MonoBehaviour
{
    System.Type CurrentControlledType;
    [SerializeField] GameObject CategoryButtonReference;
    [SerializeField] UpperMenu upperMenu;

    [SerializeField] MarkController MarkControl;
    [SerializeField] StraightLinesController LinesControl;
    [SerializeField] CurvedLinesControlled CurvedControl;
    [SerializeField] AreasController AreaControl;
    [SerializeField] OuterImageController OuterImageControl;
    ControllerCategory[] Categories => new ControllerCategory[] {MarkControl, LinesControl, CurvedControl , AreaControl, OuterImageControl};

    float DeselectTimer;
    MaskableGraphic LastPickedTypeButton;
    GameObject LastPickedItemOnScene;
    [SerializeField] Material HighlightedObjectMaterial;

    public RectTransform MapCanvas;    
    Camera cam;

    void Start()
    {
        upperMenu.Init();
        foreach (var Category in Categories)
        {
            Category.SetNewParent(this);
            Category.Init();
        }
        CreateLeftMenu();
    }

    void CreateLeftMenu()
    {
        CreateStraightLineButton();
        CreateCurvedLineButton();
        CreateAreaButton();
        CreateOuterImagesButton();
        CreateCategoryButtons();
    }

    void CreateButton(Sprite ButtonSprite, System.Action OnButtonClick)
    {
        GameObject Button = Instantiate(CategoryButtonReference);
        Button.transform.SetParent(CategoryButtonReference.transform.parent);
        Button.gameObject.SetActive(true);
        Button.transform.GetChild(0).GetComponent<Image>().sprite = ButtonSprite;
        Button.transform.GetChild(0).GetComponent<Image>().preserveAspect = true;
        Button.transform.GetComponent<Button>().onClick.AddListener(OnButtonClick.Invoke);
        MaskableGraphic ButtonUI = Button.GetComponent<MaskableGraphic>();
        Button.transform.GetComponent<Button>().onClick.AddListener(() => HighLightTypeCategory(ButtonUI));
    }

    void CreateStraightLineButton()
    {
        CreateButton(Map.Reference.Lines.ButtonIcon, PickStraightLineMode);
    }

    void PickStraightLineMode()
    {
        ChangeWorkType(typeof(StraightLine));

        if (CurrentControlledType == typeof(StraightLine))
        {
            LinesControl.ShowButtonsMenu();
        }
    }

    void CreateCurvedLineButton()
    {
        CreateButton(Map.Reference.CurvedLines.ButtonIcon, PickCurvedLineMode);
    }

    void PickCurvedLineMode()
    {
        ChangeWorkType(typeof(CurvedLine));
        if (CurrentControlledType == typeof(CurvedLine))
        {
            CurvedControl.ShowButtonsMenu();
        }
    }

    void CreateAreaButton()
    {
        CreateButton(Map.Reference.Areas.ButtonIcon,PickAreaMode);
    }

    void PickAreaMode()
    {
        ChangeWorkType(typeof(Area));
        if (CurrentControlledType == typeof(Area))
        {
            AreaControl.ShowButtonsMenu();
        }
    }

    void CreateOuterImagesButton()
    {
        CreateButton(Map.Reference.outerImages.ButtonIcon, PickOuterImageMode);
    }

    void PickOuterImageMode()
    {
        ChangeWorkType(typeof(OuterImage));
        if (CurrentControlledType == typeof(OuterImage))
        {
            OuterImageControl.ShowButtonsMenu();
        }
    }

    void CreateCategoryButtons()
    {
        foreach(var Category in Map.Reference.MarksCategories)
        {
            CreateButton(Category.CategoryIcon, ()=> PickMarkMode(Category));
        }
    }

    void PickMarkMode(MarkCategory PickedCategory)
    {
        if (MarkControl.IsCategoryAlreadyPicked(PickedCategory))
        {
            ChangeWorkType(null);
            return;
        }
        else
        {
            if (CurrentControlledType != typeof(Mark))
            {
                ChangeWorkType(typeof(Mark));
            }
            MarkControl.ShowButtonsMenu();
            MarkControl.PickNewCategory(PickedCategory);
        }
    }

    public void ChangeWorkType(System.Type NewControlledType)
    {
        foreach(var Category in Categories)
        {
            if (Category.ControlledType == CurrentControlledType)
            {
                Category.HideAllMenus();
                break;
            }
        }
        if (CurrentControlledType == NewControlledType)
        {
            CurrentControlledType = null;
        }
        else
        {
            CurrentControlledType = NewControlledType;
        }
    }

    void HighLightTypeCategory(MaskableGraphic PickedButton)
    {
        if (LastPickedTypeButton != null) LastPickedTypeButton.color = Color.white;
        if (LastPickedTypeButton == PickedButton || PickedButton == null)
        {
            LastPickedTypeButton = null;
            return;
        }
        else
        {
            LastPickedTypeButton = PickedButton;
            LastPickedTypeButton.color = Color.yellow;
        }
    }


    public void Update()
    {
        ProcessSelectTimer();
        if (CurrentControlledType == null)
        {
            ApplyEditMode();
        }
        else 
        {
            foreach(var cat in Categories)
            {
                if (cat.ControlledType == CurrentControlledType)
                {
                    cat.ApplyUserControl();
                }
            }
        }
    }

    void ApplyEditMode()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        var Results = GetCanvasRaycastResults(MapCanvas.GetComponent<Canvas>());
        
        for (int i = 0; i < Results.Count; i++)
        {
            if (Results[i].gameObject.CompareTag("MapObject"))
            {
                if (LastPickedItemOnScene != Results[i].gameObject)
                {
                    DeselectPrevious();
                    SelectNewItem(Results[i].gameObject);
                }
                else
                {
                    ModifyLastPickedObject();
                }
                return;
            }
        }

        if (cam == null) cam = Camera.main;
        RaycastHit2D hit = Physics2D.Raycast(cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        if (hit.collider!= null && hit.collider.CompareTag("MapObject"))
        {
            if (LastPickedItemOnScene != hit.collider.gameObject)
            {
                DeselectPrevious();
                SelectNewItem(hit.collider.gameObject);
            }
            else 
            {
                ModifyLastPickedObject();
            }
            return;
        }
        DeselectPrevious();
    }

    void DeselectPrevious()
    {
        if (LastPickedItemOnScene == null) return;
        var Maskable = LastPickedItemOnScene.GetComponent<MaskableGraphic>();
        if (Maskable!= null)
        {
            Maskable.material = null;
        }
        LastPickedItemOnScene = null;
    }

    void SelectNewItem(GameObject Item)
    {
        LastPickedItemOnScene = Item;
        var Maskable = LastPickedItemOnScene.GetComponent<MaskableGraphic>();
        if (Maskable != null)
        {
            Maskable.material = HighlightedObjectMaterial;
        }
        DeselectTimer = 0.5f;
    }

    void ProcessSelectTimer()
    {
        if (LastPickedItemOnScene == null) return;
        DeselectTimer -= Time.deltaTime;
        if (DeselectTimer<0) DeselectPrevious();
    }

    void ModifyLastPickedObject()
    {
        MapObjectDecorator Container = Map.GetContainerOfObject(LastPickedItemOnScene);
        if (Container != null)
        {
            foreach (var Category in Categories)
            {
                if (Container.DataReference.GetType() == Category.ControlledType)
                {
                    Category.PickExistedObject(Container);
                    CurrentControlledType = Category.ControlledType;
                    break;
                }
            }
        }
        DeselectPrevious();
    }

    public List<RaycastResult> GetCanvasRaycastResults(Canvas Target)
    {
        PointerEventData m_PointerEventData = new PointerEventData(EventSystem.current);
        if (cam == null) cam = Camera.main;
        m_PointerEventData.position = Input.mousePosition;
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        Target.GetComponent<GraphicRaycaster>().Raycast(m_PointerEventData, raycastResults);
        return raycastResults;
    }

    public List<RaycastResult> GetAllEditorRaycastResults()
    {
        List<RaycastResult> results = new List<RaycastResult>();
        var Canvases = gameObject.GetComponentsInChildren<Canvas>();
        for (int i = 0; i < Canvases.Length; i++)
        {
            results.AddRange(GetCanvasRaycastResults(Canvases[i]));
        }
        return results;
    }

    public bool IsMouseClickedEditor()
    {
        var results = GetAllEditorRaycastResults();
        if (results.Count == 0) return false;
        for (int i = 0; i < Mathf.Clamp(results.Count,0,4); i++){
            Transform ChildOfEditor = results[i].gameObject.transform;
            while (ChildOfEditor.parent!= null)
            {
                ChildOfEditor = ChildOfEditor.parent;
            }
            if (ChildOfEditor.gameObject == gameObject) return true;
        }
        return false;
    }
}