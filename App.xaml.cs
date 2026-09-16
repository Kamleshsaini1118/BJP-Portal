using System;
using System.Windows;

namespace StockPortalApp
{
    public partial class App : Application
    {
        // public App()
        // {
        //     DispatcherUnhandledException += App_DispatcherUnhandledException;
        //     AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        // }

        // private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        // {
        //     MessageBox.Show($"An error occurred:\n\n{e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}",
        //         "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //     e.Handled = true;
        // }

        // private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        // {
        //     if (e.ExceptionObject is Exception ex)
        //     {
        //         MessageBox.Show($"A critical error occurred:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
        //             "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //     }
        // }
    }
}
