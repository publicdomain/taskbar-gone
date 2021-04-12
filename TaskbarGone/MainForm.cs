// <copyright file="MainForm.cs" company="PUblicDomain.com">
//     CC0 1.0 Universal (CC0 1.0) - Public Domain Dedication
//     https://creativecommons.org/publicdomain/zero/1.0/legalcode
// </copyright>

namespace TaskbarGone
{
    // Directives
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using System.Xml.Serialization;
    using Microsoft.Win32;
    using PublicDomain;

    /// <summary>
    /// Description of MainForm.
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>
        /// Gets or sets the associated icon.
        /// </summary>
        /// <value>The associated icon.</value>
        private Icon associatedIcon = null;

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
        /// Sends the message.
        /// </summary>
        /// <returns>The message.</returns>
        /// <param name="hWnd">H window.</param>
        /// <param name="msg">Message.</param>
        /// <param name="wParam">W parameter.</param>
        /// <param name="lParam">L parameter.</param>
        [DllImport("User32.dll")]
        public static extern Int64 SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// The wm paint.
        /// </summary>
        private const int WM_PAINT = 0x000F;

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
        /// The settings data.
        /// </summary>
        private SettingsData settingsData = new SettingsData();

        /// <summary>
        /// The settings data path.
        /// </summary>
        private string settingsDataPath = $"{Application.ProductName}-SettingsData.txt";

        /// <summary>
        /// Initializes a new instance of the <see cref="T:TaskbarGone.MainForm"/> class.
        /// </summary>
        public MainForm()
        {
            try
            {
                // The InitializeComponent() call is required for Windows Forms designer support.
                this.InitializeComponent();

                /* Set settings */

                // Set current directory
                Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

                // Check for settings file
                if (!File.Exists(this.settingsDataPath))
                {
                    // Create new settings file
                    this.SaveSettingsFile(this.settingsDataPath, this.settingsData);
                }

                // Load settings from disk
                this.settingsData = this.LoadSettingsFile(this.settingsDataPath);

                // Set GUI
                this.alwaysOnTopToolStripMenuItem.Checked = this.settingsData.AlwaysOnTop;
                this.startOnLoginToolStripMenuItem.Checked = this.settingsData.StartOnLogin;
                this.startMinimizedToolStripMenuItem.Checked = this.settingsData.StartMinimized;
                this.hideInTrayToolStripMenuItem.Checked = this.settingsData.HideInTray;
                this.enableHotkeysToolStripMenuItem.Checked = this.settingsData.EnableHotkeys;

                // Set topmost
                this.TopMost = this.settingsData.AlwaysOnTop;

                // Set hotkey native form
                //this.hotkeyNativeForm = new HotkeyNativeForm(this);

                /* Set icons */

                // Set associated icon from exe file
                this.associatedIcon = Icon.ExtractAssociatedIcon(typeof(MainForm).GetTypeInfo().Assembly.Location);

                // Set public domain weekly tool strip menu item image
                this.moreReleasesPublicDomainGiftcomToolStripMenuItem.Image = this.associatedIcon.ToBitmap();

                // Set taskbar icon
                this.mainNotifyIcon.Icon = this.Icon;
            }
            catch (Exception ex)
            {
                // Advise user
                MessageBox.Show($"Error when initializing the program.{Environment.NewLine}{Environment.NewLine}Message:{Environment.NewLine}{ex.Message}", "Initialization error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    this.enableDisableButton.PerformClick();
                }
            }
            catch
            {
                // TODO Advise user
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// Handle the enable disable button click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        public void OnEnableDisableButtonClick(object sender, EventArgs e)
        {
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

            // Act om button text
            if (this.enableDisableButton.Text.StartsWith("&E", StringComparison.InvariantCulture))
            {
                // Hide
                SetWindowPos(this.taskbarHandle, IntPtr.Zero, 0, this.taskbarHandleRect.Top * 2, 0, 0, SetWindowPosFlags.SWP_HIDEWINDOW);
                SetWindowPos(this.startButtonHandle, IntPtr.Zero, 0, this.startButtonHandleRect.Top * 2, 0, 0, SetWindowPosFlags.SWP_HIDEWINDOW);

                // Update button text
                this.enableDisableButton.Text = "&Disable";
            }
            else
            {
                // Show 
                SetWindowPos(this.taskbarHandle, IntPtr.Zero, this.taskbarHandleRect.Left, this.taskbarHandleRect.Top, 0, 0, SetWindowPosFlags.SWP_SHOWWINDOW);
                SetWindowPos(this.startButtonHandle, IntPtr.Zero, this.startButtonHandleRect.Left, this.startButtonHandleRect.Top, 0, 0, SetWindowPosFlags.SWP_SHOWWINDOW);
                SendMessage(this.startButtonHandle, WM_PAINT, IntPtr.Zero, IntPtr.Zero);

                // Update button text
                this.enableDisableButton.Text = "&Enable";
            }
        }

        /// <summary>
        /// Handle the exit tool strip menu item1 click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnExitToolStripMenuItem1Click(object sender, EventArgs e)
        {
            // Close program
            this.Close();
        }

        /// <summary>
        /// Handle the options tool strip menu item drop down item clicked event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnOptionsToolStripMenuItemDropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            // Set tool strip menu item
            ToolStripMenuItem toolStripMenuItem = (ToolStripMenuItem)e.ClickedItem;

            // Toggle checked
            toolStripMenuItem.Checked = !toolStripMenuItem.Checked;

            // Set topmost
            this.TopMost = this.alwaysOnTopToolStripMenuItem.Checked;

            // Check if must perform any hotkey action
            if (toolStripMenuItem.Name == this.enableHotkeysToolStripMenuItem.Name)
            {
                // Act on checked
                if (this.enableHotkeysToolStripMenuItem.Checked)
                {
                    // Register
                    this.RegisterHotkeys();
                }
                else
                {
                    // Unregister
                    this.UnregisterHotkeys();
                }
            }
            // Start on login
            else if (toolStripMenuItem.Name == this.startOnLoginToolStripMenuItem.Name)
            {
                // Set settings data
                this.settingsData.StartOnLogin = this.enableHotkeysToolStripMenuItem.Checked;

                // Process startup registry action
                this.ProcessRunAtStartupRegistry();
            }
        }

        /// <summary>
        /// Processes the run at startup registry action.
        /// </summary>
        private void ProcessRunAtStartupRegistry()
        {
            // Open registry key
            using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                // Check for run at startup in settings data
                if (this.settingsData.StartOnLogin)
                {
                    // Add app value
                    registryKey.SetValue(Application.ProductName, $"\"{Application.ExecutablePath}\"");
                }
                else
                {
                    // Check for app value
                    if (registryKey.GetValue(Application.ProductName) != null)
                    {
                        // Erase app value
                        registryKey.DeleteValue(Application.ProductName, false);
                    }
                }
            }
        }

        /// <summary>
        /// Handle the more releases public domain giftcom tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnMoreReleasesPublicDomainGiftcomToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Open gift website
            Process.Start("https://publicdomaingift.com");
        }

        /// <summary>
        /// Handle the original thread donation codercom tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnOriginalThreadDonationCodercomToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Open original thread @ DonationCoder
            Process.Start("https://www.donationcoder.com/forum/index.php?topic=51180.0");
        }

        /// <summary>
        /// Handle the source code githubcom tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnSourceCodeGithubcomToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Open GitHub repository
            Process.Start("https://github.com/publicdomain/taskbar-gone");
        }

        /// <summary>
        /// Handles the minimize tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnMinimizeToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Send to system tray
            this.SendToSystemTray();
        }

        /// <summary>
        /// Handles the main form form closing event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnMainFormFormClosing(object sender, FormClosingEventArgs e)
        {
            /* Save settings */

            // Set options
            this.settingsData.AlwaysOnTop = this.alwaysOnTopToolStripMenuItem.Checked;
            this.settingsData.StartOnLogin = this.startOnLoginToolStripMenuItem.Checked;
            this.settingsData.StartMinimized = this.startMinimizedToolStripMenuItem.Checked;
            this.settingsData.HideInTray = this.hideInTrayToolStripMenuItem.Checked;
            this.settingsData.EnableHotkeys = this.enableHotkeysToolStripMenuItem.Checked;

            // Save to disk
            this.SaveSettingsFile(this.settingsDataPath, this.settingsData);
        }

        /// <summary>
        /// Sends the program to the system tray.
        /// </summary>
        private void SendToSystemTray()
        {
            // Check for hide in tray
            if (this.hideInTrayToolStripMenuItem.Checked)
            {
                // Hide main form
                this.Hide();

                // Remove from task bar
                this.ShowInTaskbar = false;
            }

            // Minimize
            this.WindowState = FormWindowState.Minimized;

            // Show notify icon 
            this.mainNotifyIcon.Visible = true;
        }

        /// <summary>
        /// Restores the window back from system tray to the foreground.
        /// </summary>
        private void RestoreFromSystemTray()
        {
            // Make form visible again
            this.Show();

            // Restore in task bar
            this.ShowInTaskbar = true;

            // Return window back to normal
            this.WindowState = FormWindowState.Normal;

            // Hide system tray icon
            this.mainNotifyIcon.Visible = false;
        }

        /// <summary>
        /// Handles the main notify icon mouse click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnMainNotifyIconMouseClick(object sender, MouseEventArgs e)
        {
            // Check for left click
            if (e.Button == MouseButtons.Left)
            {
                // Restore window 
                this.RestoreFromSystemTray();
            }
        }

        /// <summary>
        /// Handles the show tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnShowToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Restore window 
            this.RestoreFromSystemTray();
        }

        /// <summary>
        /// Hndles the hide tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnHideToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Click enable/disable button
            this.enableDisableButton.PerformClick();
        }

        /// <summary>
        /// Registers the hotkeys.
        /// </summary>
        public void RegisterHotkeys()
        {
            // Register CTRL + SHIFT + 9
            RegisterHotKey(this.Handle, 0, MOD_CONTROL + MOD_SHIFT, Convert.ToInt16(Keys.D9));
        }

        /// <summary>
        /// Unregisters the hotkeys.
        /// </summary>
        public void UnregisterHotkeys()
        {
            // Unregister CTRL + SHIFT + 9
            UnregisterHotKey(this.Handle, 0);
        }

        /// <summary>
        /// Loads the settings file.
        /// </summary>
        /// <returns>The settings file.</returns>
        /// <param name="settingsFilePath">Settings file path.</param>
        private SettingsData LoadSettingsFile(string settingsFilePath)
        {
            // Use file stream
            using (FileStream fileStream = File.OpenRead(settingsFilePath))
            {
                // Set xml serialzer
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(SettingsData));

                // Return populated settings data
                return xmlSerializer.Deserialize(fileStream) as SettingsData;
            }
        }

        /// <summary>
        /// Saves the settings file.
        /// </summary>
        /// <param name="settingsFilePath">Settings file path.</param>
        /// <param name="settingsDataParam">Settings data parameter.</param>
        private void SaveSettingsFile(string settingsFilePath, SettingsData settingsDataParam)
        {
            try
            {
                // Use stream writer
                using (StreamWriter streamWriter = new StreamWriter(settingsFilePath, false))
                {
                    // Set xml serialzer
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(SettingsData));

                    // Serialize settings data
                    xmlSerializer.Serialize(streamWriter, settingsDataParam);
                }
            }
            catch (Exception exception)
            {
                // Advise user
                MessageBox.Show($"Error saving settings file.{Environment.NewLine}{Environment.NewLine}Message:{Environment.NewLine}{exception.Message}", "File error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the about tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnAboutToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Set license text
            var licenseText = $"CC0 1.0 Universal (CC0 1.0) - Public Domain Dedication{Environment.NewLine}" +
                $"https://creativecommons.org/publicdomain/zero/1.0/legalcode{Environment.NewLine}{Environment.NewLine}" +
                $"Libraries and icons have separate licenses.{Environment.NewLine}{Environment.NewLine}" +
                $"Row hide icon by Clker-Free-Vector-Images - Pixabay License{Environment.NewLine}" +
                $"https://pixabay.com/vectors/row-hide-sign-symbol-27262/{Environment.NewLine}{Environment.NewLine}" +
                $"Patreon icon used according to published brand guidelines{Environment.NewLine}" +
                $"https://www.patreon.com/brand{Environment.NewLine}{Environment.NewLine}" +
                $"GitHub mark icon used according to published logos and usage guidelines{Environment.NewLine}" +
                $"https://github.com/logos{Environment.NewLine}{Environment.NewLine}" +
                $"DonationCoder icon used with permission{Environment.NewLine}" +
                $"https://www.donationcoder.com/forum/index.php?topic=48718{Environment.NewLine}{Environment.NewLine}" +
                $"PublicDomain icon is based on the following source images:{Environment.NewLine}{Environment.NewLine}" +
                $"Bitcoin by GDJ - Pixabay License{Environment.NewLine}" +
                $"https://pixabay.com/vectors/bitcoin-digital-currency-4130319/{Environment.NewLine}{Environment.NewLine}" +
                $"Letter P by ArtsyBee - Pixabay License{Environment.NewLine}" +
                $"https://pixabay.com/illustrations/p-glamour-gold-lights-2790632/{Environment.NewLine}{Environment.NewLine}" +
                $"Letter D by ArtsyBee - Pixabay License{Environment.NewLine}" +
                $"https://pixabay.com/illustrations/d-glamour-gold-lights-2790573/{Environment.NewLine}{Environment.NewLine}";

            // Prepend sponsors
            licenseText = $"RELEASE SPONSORS:{Environment.NewLine}{Environment.NewLine}" +
            "* Jesse Reichler{Environment.NewLine}{Environment.NewLine}" +
            "=========={Environment.NewLine}{Environment.NewLine}"
            + licenseText;

            // Set title
            string programTitle = typeof(MainForm).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;

            // Set version for generating semantic version 
            Version version = typeof(MainForm).GetTypeInfo().Assembly.GetName().Version;

            // Set about form
            var aboutForm = new AboutForm(
                $"About {programTitle}",
                $"{programTitle} {version.Major}.{version.Minor}.{version.Build}",
                $"Made for: SyRaza{Environment.NewLine}DonationCoder.com{Environment.NewLine}Day #102, Week #15 @ April 12, 2021",
                licenseText,
                this.Icon.ToBitmap())
            {
                // Set about form icon
                Icon = this.associatedIcon,

                // Set always on top
                TopMost = this.TopMost
            };

            // Show about form
            aboutForm.ShowDialog();
        }

        /// <summary>
        /// Handles the main form load event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnMainFormLoad(object sender, EventArgs e)
        {
            // Enable hotkeys
            if (this.settingsData.EnableHotkeys)
            {
                // Register hotkeys
                this.RegisterHotkeys();
            }

            // Start minimized
            if (this.settingsData.StartMinimized)
            {
                // Minimize to tray
                this.SendToSystemTray();
            }
        }
    }
}
