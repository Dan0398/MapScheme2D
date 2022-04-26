using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public abstract class ControllerCategory
    {
        protected enum WorkModes
        {
            Hidden,
            WaitForInput,
            CreateNew,
            Modify
        }
        protected WorkModes WorkMode;
        protected UIController ParentController;
        protected Vector2 LastMousePosition;
        [SerializeField] protected GameObject MenuParent;
        [SerializeField] protected GameObject AddButton, ApplyButton, ModifyButton, DeleteButton, OptionsParent;
        public abstract System.Type ControlledType {get;}
        protected MapObjectDecorator Decorator;
        protected bool IsPositionSaved = true;
        protected bool isClickValid;
        bool IsModifyAnimationInvoked;
        
        public void SetNewParent(UIController NewParent)
        {
            ParentController = NewParent;
        }

        public void Init()
        {
            MenuParent.SetActive(true);
            ApplyControlingButtons();
            WorkMode = WorkModes.Hidden;
            InitCustomLogic();
            HideAllMenus();
        }

        public abstract void InitCustomLogic();

        public void PickExistedObject(MapObjectDecorator PickedObject)
        {
            Decorator = PickedObject as MapObjectDecorator;
            SavePickedObjectData();
            WorkMode = WorkModes.Modify;
            InitCustomLogic();
            ShowButtonsMenu();
            OptionsParent.SetActive(true);
            PickExistedAdditional();
        }

        protected abstract void SavePickedObjectData();

        protected virtual void PickExistedAdditional(){}

        void ApplyControlingButtons()
        {
            AddButton.GetComponent<Button>().onClick.AddListener(PickAddButton);
            ApplyButton.GetComponent<Button>().onClick.AddListener(PickApplyButton);
            ModifyButton.GetComponent<Button>().onClick.AddListener(PickModifyButton);
            DeleteButton.GetComponent<Button>().onClick.AddListener(PickDeleteButton);
            AddButton.GetComponent<Button>().onClick.AddListener(UpdateButtonStatus);
            ApplyButton.GetComponent<Button>().onClick.AddListener(UpdateButtonStatus);
            ModifyButton.GetComponent<Button>().onClick.AddListener(UpdateButtonStatus);
            DeleteButton.GetComponent<Button>().onClick.AddListener(UpdateButtonStatus);
        }

        public virtual void ShowButtonsMenu()
        {
            MenuParent.SetActive(true);
            UpdateButtonStatus();
        }

        public virtual void HideAllMenus()
        {
            OptionsParent.SetActive(false);
            MenuParent.SetActive(false);
        }

        protected async void UpdateButtonStatus()
        {
            await Task.Yield();
            bool isExist = isObjectExist();
            AddButton.SetActive(!isExist);
            ApplyButton.SetActive(isExist);
            ModifyButton.SetActive(isExist);
            DeleteButton.SetActive(isExist);
        }

        public abstract void ApplyUserControl();

        protected async void PickAddButton()
        {
            if (isObjectExist()) return;
            MapObject NewObject = await CreateNewObject();
            if (NewObject == null) return;
            Decorator = Map.ActualDecorator.CreateObjWithDecorator(NewObject);
            WorkMode = WorkModes.CreateNew;
            ShowButtonsMenu();
            OptionsParent.SetActive(true);
            IsPositionSaved = true;
            PickAddAdditional();
            PickModifyButton();
        }

        protected virtual void PickAddAdditional() { }

        protected abstract Task<MapObject> CreateNewObject();

        protected void PickApplyButton()
        {
            if (!isObjectExist()) return;
            Decorator = null;
            OptionsParent.SetActive(false);
            PickApplyAdditional();
            ReturnIfModify();
        }

        protected virtual void PickApplyAdditional() { }

        protected void PickModifyButton()
        {
            if (!isObjectExist()) return;
            IsPositionSaved = !IsPositionSaved;
            if (!IsPositionSaved) AnimateModifyColor();
            PickModifyAdditional();
        }

        async void AnimateModifyColor()
        {
            if (IsModifyAnimationInvoked) return;
            IsModifyAnimationInvoked = true;
            MaskableGraphic Target = ModifyButton.GetComponentInChildren<MaskableGraphic>();
            Color OldColor = Target.color;
            Color HighLightedColor = Color.Lerp(OldColor, Color.white, 0.7f);
            bool isPainted = true;
            while(!IsPositionSaved && Application.isPlaying)
            {
                Target.color = isPainted? HighLightedColor:OldColor;
                isPainted = !isPainted;
                await Task.Delay(500);
            }
            if (!Application.isPlaying) return;
            Target.color = OldColor;
            IsModifyAnimationInvoked = false;
        }

        protected virtual void PickModifyAdditional() { }

        protected void PickDeleteButton()
        {
            if (!isObjectExist()) return;
            DeleteDecorator();
            IsPositionSaved = true;
            OptionsParent.SetActive(false);
            PickDeleteAdditoinal();
            ReturnIfModify(); 
        }

        protected virtual void PickDeleteAdditoinal() { }

        protected bool isObjectExist()
        {
            return (Decorator != null && Decorator.ObjectOnScene != null);
        }

        protected void ReturnIfModify()
        {
            if (WorkMode == WorkModes.Modify)
            {
                ParentController.ChangeWorkType(null);
            }
        }

        protected void RefreshDecoratorView()
        {
            if (!isObjectExist()) return;
            Map.ActualDecorator.RefreshView(Decorator);
        }

        protected void RefreshDecoratorTransform()
        {
            if (!isObjectExist()) return;
            Map.ActualDecorator.RefreshTransform(Decorator);
        }

        protected void DeleteDecorator()
        {
            if (!isObjectExist()) return;
            Map.ActualDecorator.DeleteObjectByDecorator(Decorator);
            Decorator = null;
        }
    }