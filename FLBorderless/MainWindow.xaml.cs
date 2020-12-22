using System;
using System.Windows;
using System.Windows.Threading;

namespace FLBorderless
{
    public partial class MainWindow : Window
    {
        DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };

        public MainWindow()
        {
            InitializeComponent();
            // That's my FL install dir, if you want to try insert yours here...
            // TODO: Make this just "Freelancer.exe" to allow running from the game's folder
            appControl.ExeName = "D:\\Freelated\\Freelancer\\EXE\\Freelancer.exe";
            this.Unloaded += new RoutedEventHandler((s, e) => { appControl.Dispose(); });

            // I'm currently unsure how to properly remove the inner window borders, 
            // so I try and guess the loading time with a timer...
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            appControl.RemoveBorderAndWhatnot();
            timer.Stop();
        }

        private void appControl_ChildProcessExited(object sender, EventArgs e)
        {
            // Doesn't work! 
            // TODO: Find another way!
            Application.Current.Shutdown();
        }
    }
}
