﻿using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;

namespace UXLib.UI
{
    public class UISmartObjectList : UISmartObject
    {
        public UISmartObjectList(SmartObject smartObject, ListData listData, BoolInputSig enableJoin, BoolInputSig visibleJoin)
            : base(smartObject, enableJoin, visibleJoin)
        {
            this.Data = listData;
            this.Data.DataChange += new ListDataChangeEventHandler(Data_DataChange);
            try
            {
                for (uint item = 1; item <= this.MaxNumberOfItems; item++)
                {
                    UISmartObjectButton listButton = new UISmartObjectButton(this,
                        item, this.DeviceSmartObject,
                        string.Format("Item {0} Pressed", item),
                        string.Format("Item {0} Selected", item),
                        string.Format("Set Item {0} Text", item),
                        string.Format("Set Item {0} Icon Serial", item),
                        string.Format("Item {0} Enabled", item),
                        string.Format("Item {0} Visible", item)
                        );
                    this.AddButton(listButton);
                }

                this.NumberOfItems = 0;
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error constructing UISmartObjectList with KeyName: {0}", e.Message);
            }
        }

        protected ListData Data { get; set; }
        protected BoolInputSig LoadingSubPageOverlay;

        public void ScrollToItem(ushort item)
        {
            this.DeviceSmartObject.UShortInput["Scroll To Item"].UShortValue = item;
        }

        public bool IsMoving
        {
            get
            {
                if (this.DeviceSmartObject.BooleanOutput["Is Moving"] != null)
                    return this.DeviceSmartObject.BooleanOutput["Is Moving"].BoolValue;
                return false;
            }
        }

        protected virtual void Data_DataChange(ListData listData, ListDataChangeEventArgs args)
        {
            if (args.EventType == eListDataChangeEventType.IsStartingToLoad)
            {
                this.Buttons[1].Title = "Loading...";
                this.Buttons[1].Icon = UIMediaIcons.Blank;
                this.NumberOfItems = 1;
                if (LoadingSubPageOverlay != null)
                    LoadingSubPageOverlay.BoolValue = true;
            }
            else if (args.EventType == eListDataChangeEventType.HasCleared)
            {
                for (uint item = 1; item <= this.NumberOfItems; item++)
                {
                    this.Buttons[item].Feedback = false;
                }
                this.NumberOfItems = 0;
            }
            else if (args.EventType == eListDataChangeEventType.HasLoaded)
            {
                ushort listSize;

                if (listData.Count > this.MaxNumberOfItems)
                {
                    listSize = this.MaxNumberOfItems;
                }
                else
                {
                    listSize = (ushort)listData.Count;
                }

                this.NumberOfItems = listSize;

                for (uint item = 1; item <= listSize; item++)
                {
                    int listDataIndex = (int)item - 1;
                    this.Buttons[item].Title = listData[listDataIndex].Title;
                    this.Buttons[item].Icon = listData[listDataIndex].Icon;
                    this.Buttons[item].LinkedObject = listData[listDataIndex].DataObject;
                    this.Buttons[item].Enabled = listData[listDataIndex].Enabled;
                    this.Buttons[item].Feedback = listData[listDataIndex].IsSelected;
                }

                if (LoadingSubPageOverlay != null)
                    LoadingSubPageOverlay.BoolValue = false;
            }
            else if (args.EventType == eListDataChangeEventType.ItemSelectionHasChanged)
            {
                for (uint item = 1; item <= this.NumberOfItems; item++)
                {
                    int listDataIndex = (int)item - 1;
                    this.Buttons[item].Feedback = listData[listDataIndex].IsSelected;
                }
            }
        }

        public object LinkedObjectForButton(uint buttonIndex)
        {
            return this.Buttons[buttonIndex].LinkedObject;
        }

        public void LoadingSubPageOverlayAssign(BoolInputSig loadingSubPageOverlaySig)
        {
            this.LoadingSubPageOverlay = loadingSubPageOverlaySig;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                this.Data.DataChange -= new ListDataChangeEventHandler(Data_DataChange);
            base.Dispose(disposing);
        }
    }
}