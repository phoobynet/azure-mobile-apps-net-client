﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Xamarin.Auth._MobileServices;
#if __UNIFIED__
using Foundation;
using UIKit;
using NSAction = System.Action;
#else
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace Microsoft.WindowsAzure.MobileServices
{
    internal class MobileServiceUIAuthentication : MobileServicePKCEAuthentication
    {
        private readonly RectangleF rect;
        private readonly object view;

        public MobileServiceUIAuthentication(RectangleF rect, object view, MobileServiceClient client, string providerName, string uriScheme, IDictionary<string, string> parameters)
            : base(client, providerName, uriScheme, parameters)
        {
            this.rect = rect;
            this.view = view;
        }

        internal static WebAuthenticator CurrentAuthenticator;

        protected override Task<string> GetAuthorizationCodeAsync()
        {
            var tcs = new TaskCompletionSource<string>();

            CurrentAuthenticator = new WebRedirectAuthenticator(LoginUri, CallbackUri)
            {
                IsUsingNativeUI = ObjCRuntime.Class.GetHandle("SFSafariViewController") != IntPtr.Zero,
                //ShowUIErrors = false,
                ClearCookiesBeforeLogin = false
            };

            UIViewController c = CurrentAuthenticator.GetUI();

            UIViewController controller = null;
            UIPopoverController popover = null;

            CurrentAuthenticator.Error += (o, e) =>
            {
                NSAction completed = () =>
                {
                    Exception ex = e.Exception ?? new Exception(e.Message);
                    tcs.TrySetException(ex);
                };

                if (controller != null)
                {
                    controller.DismissViewController(true, completed);
                }

                if (popover != null)
                {
                    popover.Dismiss(true);
                    completed();
                }
                CurrentAuthenticator = null;
            };

            CurrentAuthenticator.Completed += (o, e) =>
            {
                NSAction completed = () =>
                {
                    if (!e.IsAuthenticated)
                    {
                        tcs.TrySetException(new InvalidOperationException("Authentication was cancelled by the user."));
                    }
                    else
                    {
                        tcs.TrySetResult(e.Account.Properties["authorization_code"]);
                    }
                };

                if (controller != null)
                {
                    controller.DismissViewController(true, completed);
                }

                if (popover != null)
                {
                    popover.Dismiss(true);
                    completed();
                }
                CurrentAuthenticator = null;
            };

            controller = view as UIViewController;
            if (controller != null)
            {
                controller.PresentViewController(c, true, null);
            }
            else
            {
                UIView v = view as UIView;
                UIBarButtonItem barButton = view as UIBarButtonItem;

                popover = new UIPopoverController(c);

                if (barButton != null)
                {
                    popover.PresentFromBarButtonItem(barButton, UIPopoverArrowDirection.Any, true);
                }
                else
                {
                    popover.PresentFromRect(rect, v, UIPopoverArrowDirection.Any, true);
                }
            }

            return tcs.Task;
        }
    }
}
