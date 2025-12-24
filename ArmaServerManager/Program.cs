using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace ArmaServerManager
{
    public static class Program
    {
        [DllImport("Microsoft.UI.Xaml.dll")]
        private static extern void XamlCheckProcessRequirements();

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                XamlCheckProcessRequirements();

                Application.Start((p) =>
                {
                    var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                    SynchronizationContext.SetSynchronizationContext(context);
                    new App();
                });
            }
            catch (Exception ex)
            {
                LogStartupError(ex);
            }
        }

        private static void LogStartupError(Exception ex)
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_error.txt");
                var message = $"[{DateTime.Now}] LOW-LEVEL CRITICAL ERROR:\n{ex}\nStack Trace:\n{ex.StackTrace}\n\n";
                File.AppendAllText(path, message);
            }
            catch { }
        }
    }
}
