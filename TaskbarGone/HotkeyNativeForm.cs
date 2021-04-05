using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TaskbarGone
{
    /// <summary>
    /// Hotkey native form.
    /// </summary>
    public class HotkeyNativeForm : NativeWindow, IDisposable
    {
        /// <summary>
        /// The taskbar handle.
        /// </summary>
        private IntPtr taskbarHandle;

        /// <summary>
        /// The taskbar handle rect.
        /// </summary>
        private RECT taskbarHandleRect;

        /// <summary>
        /// The start button handle.
        /// </summary>
        private IntPtr startButtonHandle;

        /// <summary>
        /// The start button handle rect.
        /// </summary>
        private RECT startButtonHandleRect;

        /// <summary>
        /// The wm hotkey.
        /// </summary>
        private const int WM_HOTKEY = 0x312;

        /// <summary>
        /// The main form.
        /// </summary>
        private MainForm mainForm = null;

        /// <summary>
        /// Rect.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        /// <summary>
        /// Sets the window position.
        /// </summary>
        /// <returns><c>true</c>, if window position was set, <c>false</c> otherwise.</returns>
        /// <param name="hWnd">H window.</param>
        /// <param name="hWndInsertAfter">H window insert after.</param>
        /// <param name="X">X.</param>
        /// <param name="Y">Y.</param>
        /// <param name="cx">Cx.</param>
        /// <param name="cy">Cy.</param>
        /// <param name="uFlags">U flags.</param>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        /// <summary>
        /// Finds the window.
        /// </summary>
        /// <returns>The window.</returns>
        /// <param name="lpClassName">Lp class name.</param>
        /// <param name="lpWindowName">Lp window name.</param>
        [DllImport("user32", EntryPoint = "FindWindowA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        /// <summary>
        /// Finds the window ex.
        /// </summary>
        /// <returns>The window ex.</returns>
        /// <param name="parentHwnd">Parent hwnd.</param>
        /// <param name="childAfterHwnd">Child after hwnd.</param>
        /// <param name="className">Class name.</param>
        /// <param name="windowText">Window text.</param>
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr parentHwnd, IntPtr childAfterHwnd, IntPtr className, string windowText);

        /// <summary>
        /// Gets the window rect.
        /// </summary>
        /// <returns><c>true</c>, if window rect was gotten, <c>false</c> otherwise.</returns>
        /// <param name="hWnd">H window.</param>
        /// <param name="lpRect">Lp rect.</param>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        /// <summary>
        /// Set window position flags.
        /// </summary>
        [Flags]
        private enum SetWindowPosFlags : uint
        {
            SWP_ASYNCWINDOWPOS = 0x4000,
            SWP_DEFERERASE = 0x2000,
            SWP_DRAWFRAME = 0x0020,
            SWP_FRAMECHANGED = 0x0020,
            SWP_HIDEWINDOW = 0x0080,
            SWP_NOACTIVATE = 0x0010,
            SWP_NOCOPYBITS = 0x0100,
            SWP_NOMOVE = 0x0002,
            SWP_NOOWNERZORDER = 0x0200,
            SWP_NOREDRAW = 0x0008,
            SWP_NOREPOSITION = 0x0200,
            SWP_NOSENDCHANGING = 0x0400,
            SWP_NOSIZE = 0x0001,
            SWP_NOZORDER = 0x0004,
            SWP_SHOWWINDOW = 0x0040,
        }

        /// <summary>
        /// Registers the hot key.
        /// </summary>
        /// <returns><c>true</c>, if hot key was registered, <c>false</c> otherwise.</returns>
        /// <param name="hWnd">H window.</param>
        /// <param name="id">Identifier.</param>
        /// <param name="fsModifiers">Fs modifiers.</param>
        /// <param name="vk">Vk.</param>
        [DllImport("User32")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        /// <summary>
        /// Unregisters the hot key.
        /// </summary>
        /// <returns><c>true</c>, if hot key was unregistered, <c>false</c> otherwise.</returns>
        /// <param name="hWnd">H window.</param>
        /// <param name="id">Identifier.</param>
        [DllImport("User32")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        /// <summary>
        /// The mod shift.
        /// </summary>
        public const int MOD_SHIFT = 0x4;

        /// <summary>
        /// The mod control.
        /// </summary>
        public const int MOD_CONTROL = 0x2;

        /// <summary>
        /// The mod alternate.
        /// </summary>
        public const int MOD_ALT = 0x1;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:TaskbarGone.HotkeyNativeForm"/> class.
        /// </summary>
        public HotkeyNativeForm(MainForm mainForm)
        {
            // Create the handle
            CreateHandle(new CreateParams());

            // Set the main form
            this.mainForm = mainForm;

            // TODO Set handles [Set in another function]
            this.taskbarHandle = FindWindow("Shell_traywnd", string.Empty);
            this.startButtonHandle = FindWindowEx(IntPtr.Zero, IntPtr.Zero, (IntPtr)0xC017, null);

            // TODO Set initial RECTs [Can be improved i.e. on start]
            if (this.taskbarHandleRect.Equals(default(RECT)))
            {
                // Set taskbar rect
                GetWindowRect(this.taskbarHandle, ref this.taskbarHandleRect);

                // Set start button rect
                GetWindowRect(this.startButtonHandle, ref this.startButtonHandleRect);
            }
        }

        /// <summary>
        /// Window procedure.
        /// </summary>
        /// <param name="m">M.</param>
        protected override void WndProc(ref Message m)
        {
            try
            {
                // Check for hotkey message and there are hotkeys registered
                if (m.Msg == WM_HOTKEY)
                {
                    // Toggle enabled/disabled state via hotkey
                    this.mainForm.enableDisableButton.PerformClick();
                }
            }
            catch
            {
                // TODO Advise user
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// Hides the taskbar.
        /// </summary>
        public void HideTaskbar()
        {
            SetWindowPos(this.taskbarHandle, IntPtr.Zero, 0, this.taskbarHandleRect.Top * 2, 0, 0, SetWindowPosFlags.SWP_HIDEWINDOW);
            SetWindowPos(this.startButtonHandle, IntPtr.Zero, 0, this.startButtonHandleRect.Top * 2, 0, 0, SetWindowPosFlags.SWP_HIDEWINDOW);
        }

        /// <summary>
        /// Shows the taskbar.
        /// </summary>
        public void ShowTaskbar()
        {
            SetWindowPos(this.taskbarHandle, IntPtr.Zero, this.taskbarHandleRect.Left, this.taskbarHandleRect.Top, 0, 0, SetWindowPosFlags.SWP_SHOWWINDOW);
            SetWindowPos(this.startButtonHandle, IntPtr.Zero, this.startButtonHandleRect.Left, this.startButtonHandleRect.Top, 0, 0, SetWindowPosFlags.SWP_SHOWWINDOW);
        }

        /// <summary>
        /// Registers the hotkeys.
        /// </summary>
        public void RegisterHotkeys()
        {
            // Register ALT + SHIFT + S
            RegisterHotKey(this.Handle, 0, MOD_ALT + MOD_SHIFT, Convert.ToInt16(Keys.S));
        }

        /// <summary>
        /// Unregisters the hotkeys.
        /// </summary>
        public void UnregisterHotkeys()
        {
            // Unregister ALT + SHIFT + S
            UnregisterHotKey(this.Handle, 0);
        }

        /// <summary>
        /// Releases all resource used by the <see cref="T:TaskbarGone.HotkeyNativeForm"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="T:TaskbarGone.HotkeyNativeForm"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="T:TaskbarGone.HotkeyNativeForm"/> in an unusable state.
        /// After calling <see cref="Dispose"/>, you must release all references to the
        /// <see cref="T:TaskbarGone.HotkeyNativeForm"/> so the garbage collector can reclaim the memory that the
        /// <see cref="T:TaskbarGone.HotkeyNativeForm"/> was occupying.</remarks>
        public void Dispose()
        {
            DestroyHandle();
        }
    }
}
