using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunarADRemover
{
    internal class Program
    {
        static void Main()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string baseDir = Path.Combine(localAppData, @"Programs\Lunar Client\resources");

            string asarPath = Path.Combine(baseDir, "app.asar");
            string shimPath = Path.Combine(baseDir, "nnnnnnnnnnnnnnnnnnnnnnnn.js");

            string patchName = "nnnnnnnnnnnnnnnnnnnnnnnn.js";

            string searchString = "\"main\":\"dist-electron/electron/main.js\"";
            string replaceString = $"\"main\":\"../{patchName}\"";

            string searchStringSpaced = "\"main\": \"dist-electron/electron/main.js\"";
            string replaceStringSpaced = $"\"main\": \"../{patchName}\"";

            try
            {
                if (!File.Exists(asarPath))
                {
                    Console.WriteLine("[-] Error: app.asar not found at " + asarPath);
                    return;
                }

                Console.WriteLine("[*] Patching...");
                string content = File.ReadAllText(asarPath, Encoding.Default);

                if (content.Contains(searchString) || content.Contains(searchStringSpaced))
                {
                    Console.WriteLine("[+] Found target string. Patching...");
                    content = content.Replace(searchString, replaceString);
                    content = content.Replace(searchStringSpaced, replaceStringSpaced);

                    File.WriteAllText(asarPath, content, Encoding.Default);
                    Console.WriteLine("[+] app.asar patched successfully.");
                }
                else if (content.Contains(patchName))
                {
                    Console.WriteLine("[!] already patched! Exiting...");
                    return;
                }


                Console.WriteLine("[*] Creating preload file...");
                string jsContent = @"const { app, BrowserWindow } = require('electron');
const path = require('path');

app.on('browser-window-created', (event, window) => {
    window.webContents.openDevTools({ mode: 'detach' });

    window.webContents.on('did-finish-load', () => {
        const windowTitle = window.getTitle();

        window.webContents.executeJavaScript(`
            (function() {
                const removeAds = () => {
                    const plusIcon = document.querySelector('img[alt=""Lunar Plus""]');
                    if (plusIcon) {
                        const adContainer = plusIcon.parentElement.parentElement.parentElement;
                        if (adContainer) {
                            adContainer.remove();
                            return true;
                        }
                    }
                    return false;
                };

                removeAds();
                const observer = new MutationObserver(() => { removeAds(); });
                observer.observe(document.body, { childList: true, subtree: true });
            })();
        `).catch(err => console.log('Injection failed:', err));
    });
});

console.log('[*] Loading original main...');
try {
    require('./app.asar/dist-electron/electron/main.js');
} catch (error) {
    console.error('[-] Failed to load original main:', error);
}";

                File.WriteAllText(shimPath, jsContent);
                Console.WriteLine("[+] preload file created at: " + shimPath);
                Console.WriteLine("\n[!] Done. Restart Lunar Client to see changes.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] An error occurred: " + ex.Message);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
