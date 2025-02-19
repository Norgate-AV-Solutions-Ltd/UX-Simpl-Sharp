﻿using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;

namespace UXLib.UI
{
    public class UIViewController : IDisposable, IUIVisibleObject
    {
        public UIViewController(UIController uiController, UIViewBase view)
        {
            UIController = uiController;
            View = view;
            View.VisibilityChange += View_VisibilityChange;
        }

        public UIViewController(UIViewController ownerViewController, UIViewBase view)
            : this(ownerViewController.UIController, view)
        {
            Owner = ownerViewController;
        }

        public UIViewController(UIViewController ownerViewController)
            : this(ownerViewController.UIController, ownerViewController.View) { }

        public UIViewBase View { get; protected set; }
        public event UIViewControllerEventHandler VisibilityChange;
        public UIController UIController { get; protected set; }
        public UIViewController Owner { get; protected set; }
        public BasicTriList Device
        {
            get
            {
                return UIController.Device;
            }
        }

        void View_VisibilityChange(UIViewBase sender, UIViewVisibilityEventArgs args)
        {
            if (args.EventType == eViewEventType.DidShow)
                OnShow();
            else if (args.EventType == eViewEventType.DidHide)
                OnHide();
            else if (args.EventType == eViewEventType.WillShow)
            {
                try
                {
                    WillShow();
                }
                catch(Exception e)
                {
                    ErrorLog.Exception(string.Format("Exception in UIViewController.WillShow(), {0}", e.Message), e);
                }
            }
            else if (args.EventType == eViewEventType.WillHide)
                WillHide();
        }

        public string Title
        {
            get
            {
                return View.Title;
            }
            set
            {
                View.Title = value;
            }
        }

        public virtual void Show()
        {
            View.Show();
        }

        public virtual void Hide()
        {
            View.Hide();
        }

        public bool Visible
        {
            get
            {
                return View.Visible;
            }
            set
            {
                View.Visible = value;
            }
        }

        protected virtual void OnShow()
        {
            if (VisibilityChange != null)
                VisibilityChange(this, new UIViewVisibilityEventArgs(eViewEventType.DidShow));
        }

        protected virtual void OnHide()
        {
            if (VisibilityChange != null)
                VisibilityChange(this, new UIViewVisibilityEventArgs(eViewEventType.DidHide));
        }

        protected virtual void WillShow()
        {
            if (Owner != null)
                Owner.VisibilityChange += new UIViewControllerEventHandler(Owner_VisibilityChange);

            if (VisibilityChange != null)
                VisibilityChange(this, new UIViewVisibilityEventArgs(eViewEventType.WillShow));
        }

        protected virtual void WillHide()
        {
            if(Owner != null)
                Owner.VisibilityChange -= new UIViewControllerEventHandler(Owner_VisibilityChange);
            if (VisibilityChange != null)
                VisibilityChange(this, new UIViewVisibilityEventArgs(eViewEventType.WillHide));
        }

        public uint VisibleJoinNumber
        {
            get
            {
                return View.VisibleJoinNumber;
            }
        }

        void Owner_VisibilityChange(UIViewController sender, UIViewVisibilityEventArgs args)
        {
            if (args.EventType == eViewEventType.WillHide && View.Visible)
                View.Hide();
        }
        
        /// <summary>
        /// Unregister from any sig changes and dispose of resources
        /// </summary>
        public void Dispose()
        {
            if (!Disposed)
            {
                // Dispose of unmanaged resources.
                Dispose(true);
                // Suppress finalization
                CrestronEnvironment.GC.SuppressFinalize(this);
            }
        }
        
        bool disposed = false;

        public bool Disposed
        {
            get
            {
                return disposed;
            }
        }

        /// <summary>
        /// Override this to free resources
        /// </summary>
        /// <param name="disposing">true is Dispose() has been called</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here.
                //

                View.VisibilityChange -= new UIViewBaseVisibitlityEventHandler(View_VisibilityChange);
            }

            // Free any unmanaged objects here.
            //

            disposed = true;
        }
    }

    public delegate void UIViewControllerEventHandler(UIViewController sender, UIViewVisibilityEventArgs args);
}