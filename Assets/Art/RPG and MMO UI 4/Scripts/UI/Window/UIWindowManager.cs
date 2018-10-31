using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Invector.vCharacterController;

namespace UnityEngine.UI
{
	public class UIWindowManager : MonoBehaviour {

		void Update()
		{
			// Check for escape key press
			if (Input.GetKeyDown(KeyCode.Escape))
			{
                bool EligibleForShow = true;
				
				// Get the windows list
				List<UIWindow> windows = UIWindow.GetWindows();
				
				// Loop through the windows and hide if required
				foreach (UIWindow window in windows)
				{
					// Check if the window has escape key action
					if (window.escapeKeyAction != UIWindow.EscapeKeyAction.None)
					{
						// Check if the window should be hidden on escape
						if (window.IsOpen && (window.escapeKeyAction == UIWindow.EscapeKeyAction.Hide || window.escapeKeyAction == UIWindow.EscapeKeyAction.Toggle || (window.escapeKeyAction == UIWindow.EscapeKeyAction.HideIfFocused && window.IsFocused)))
						{
							// Hide the window
							window.Hide();
							
							// Dont allow a window to be shown after a window has been closed
							EligibleForShow = false;
						}
					}
				}

                if(!EligibleForShow)
                {
                    //Hide cursor
                    var ctrl = FindObjectOfType<vThirdPersonInput>();
                    if (ctrl)
                    {
                        ctrl.ShowCursor(ctrl.showCursorOnStart);
                        ctrl.LockCursor(ctrl.unlockCursorOnStart);
                        ctrl.SetLockBasicInput(false);
                        ctrl.SetLockCameraInput(false);
                    }
                }
				
				// If we didnt hide any windows with this key press check if we should show a window
				if (EligibleForShow)
				{
					// Loop through the windows again and show if required
					foreach (UIWindow window in windows)
					{
						// Check if the window has escape key action toggle and is not shown
						if (!window.IsOpen && window.escapeKeyAction == UIWindow.EscapeKeyAction.Toggle)
						{
							// Show the window
							window.Show();

                            //Show cursor
                            var ctrl = FindObjectOfType<vThirdPersonInput>();
                            if(ctrl)
                            {
                                ctrl.ShowCursor(true);
                                ctrl.LockCursor(true);
                                ctrl.SetLockBasicInput(true);
                                ctrl.SetLockCameraInput(true);
                            }
						}
					}
				}
			}
        }
	}
}