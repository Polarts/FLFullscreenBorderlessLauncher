using System;
using System.Windows;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Interop;

namespace FLBorderless
{
    /// <summary>
    /// AppControl.xaml 
    /// from https://www.codeproject.com/Tips/673701/Hosting-EXE-Applications-in-a-WPF-Window-Applicati
    /// </summary>
    public partial class AppControl : UserControl, IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct HWND__ {
    
            /// int
            public int unused;
        }

        public event EventHandler ChildProcessExited;

        public AppControl()
        {
            InitializeComponent();
            this.SizeChanged += new SizeChangedEventHandler(OnSizeChanged);
            this.Loaded += new RoutedEventHandler(OnVisibleChanged);
            this.SizeChanged += new SizeChangedEventHandler(OnResize);
        }

        ~AppControl()
        {
            this.Dispose();
        }

        /// <summary>
        /// Track if the application has been created
        /// </summary>
        private bool _iscreated = false;
        
        /// <summary>
        /// Track if the control is disposed
        /// </summary>
        private bool _isdisposed = false;

        /// <summary>
        /// Handle to the application Window
        /// </summary>
        IntPtr _appWin;

        private Process _childp;

        /// <summary>
        /// The name of the exe to launch
        /// </summary>
        private string exeName = "";

        public string ExeName
        {
            get
            {
                return exeName;
            }
            set
            {
                exeName = value;				
            }
        }


        [DllImport("user32.dll", EntryPoint="GetWindowThreadProcessId",  SetLastError=true,
             CharSet=CharSet.Unicode, ExactSpelling=true,
             CallingConvention=CallingConvention.StdCall)]
        private static extern long GetWindowThreadProcessId(long hWnd, long lpdwProcessId); 
            
        [DllImport("user32.dll", SetLastError=true)]
        private static extern IntPtr FindWindow (string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError=true)]
        private static extern long SetParent (IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", EntryPoint="GetWindowLongA", SetLastError=true)]
        private static extern long GetWindowLong (IntPtr hwnd, int nIndex);

        [DllImport("user32.dll", EntryPoint="SetWindowLongA", SetLastError=true)]
        public static extern int SetWindowLongA([In()] IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError=true)]
        private static extern long SetWindowPos(IntPtr hwnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint wFlags);
        
        [DllImport("user32.dll", SetLastError=true)]
        private static extern bool MoveWindow(IntPtr hwnd, int x, int y, int cx, int cy, bool repaint);

        private const int SWP_NOOWNERZORDER = 0x200;
        private const int SWP_NOREDRAW = 0x8;
        private const int SWP_NOZORDER = 0x4;
        private const int SWP_SHOWWINDOW = 0x0040;
        private const int WS_EX_MDICHILD = 0x40;
        private const int SWP_FRAMECHANGED = 0x20;
        private const int SWP_NOACTIVATE = 0x10;
        private const int SWP_ASYNCWINDOWPOS = 0x4000;
        private const int SWP_NOMOVE = 0x2;
        private const int SWP_NOSIZE = 0x1;
        private const int GWL_STYLE = (-16);
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_CHILD = 0x40000000;

        
        /// <summary>
        /// Force redraw of control when size changes
        /// </summary>
        /// <param name="e">Not used</param>
        protected void OnSizeChanged(object s, SizeChangedEventArgs e)
        {
            this.InvalidateVisual();
        }


        /// <summary>
        /// Create control when visibility changes
        /// </summary>
        /// <param name="e">Not used</param>
        protected void OnVisibleChanged(object s, RoutedEventArgs e)
        {
            // If control needs to be initialized/created
            if (_iscreated == false)
            {

                // Mark that control is created
                _iscreated = true;

                // Initialize handle value to invalid
                _appWin = IntPtr.Zero;

                try
                {
                    var procInfo = new ProcessStartInfo(this.exeName, "-w");
                    procInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(this.exeName);
                    // Start the process
                    _childp = Process.Start(procInfo);

                    // Wait for process to be created and enter idle condition
                    _childp.WaitForInputIdle();

                    // Should detect when the process exits. Doesn't work with FL.
                    // TODO: Find another way!
                    _childp.Exited += OnChildProcessExited;

                    // Get the main handle
                    _appWin = _childp.MainWindowHandle;
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.Message + "Error");
                }

                // Put it into this form
                var helper = new WindowInteropHelper(Window.GetWindow(this.AppContainer));
                SetParent(_appWin, helper.Handle);

                // Remove border and whatnot
                SetWindowLongA(_appWin, GWL_STYLE, WS_VISIBLE);

                // Move the window to overlay it on this window
                Point relativePoint = PointToScreen(new Point(0, 0));
                MoveWindow(_appWin, (int)relativePoint.X, (int)relativePoint.Y, (int)this.ActualWidth, (int)this.ActualHeight, true);

            }
        }

        private void OnChildProcessExited(object sender, EventArgs e)
        {
            ChildProcessExited?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Update display of the executable
        /// </summary>
        /// <param name="e">Not used</param>
        protected void OnResize(object s, SizeChangedEventArgs e)
        {
            if (this._appWin != IntPtr.Zero)
            {
                Point relativePoint = PointToScreen(new Point(0, 0));
                MoveWindow(_appWin, (int)relativePoint.X, (int)relativePoint.Y, (int)this.ActualWidth, (int)this.ActualHeight, true);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isdisposed)
            {
                if (disposing)
                {
                    if (_iscreated && _appWin != IntPtr.Zero && !_childp.HasExited)
                    {
                        // Stop the application
                        _childp.Kill();

                        // Clear internal handle
                        _appWin = IntPtr.Zero;
                    }
                }
                _isdisposed = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void RemoveBorderAndWhatnot() 
        {
            SetWindowLongA(_appWin, GWL_STYLE, WS_VISIBLE);
        }
    }
}
