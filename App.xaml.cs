using System;
using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;
using System.Net; // Для ServicePointManager

namespace ForgeTales
{
    public partial class App : Application
    {
        public App()
        {
            // Установка режима совместимости IE
            SetBrowserFeatureControl();

            // Игнорирование ошибок SSL
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;
        }

        private static void SetBrowserFeatureControl()
        {
            try
            {
                var appName = Process.GetCurrentProcess().ProcessName + ".exe";
                SetRegistryKey("FEATURE_BROWSER_EMULATION", appName, 11001);
            }
            catch { /* Игнорируем ошибки реестра */ }
        }

        private static void SetRegistryKey(string feature, string appName, uint value)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(
                $@"Software\Microsoft\Internet Explorer\Main\FeatureControl\{feature}"))
            {
                key?.SetValue(appName, value, RegistryValueKind.DWord);
            }
        }
    }
}