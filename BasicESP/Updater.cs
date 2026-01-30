using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;

namespace BoneESP
{
    internal static class Updater
    {
        public static async Task<bool> CheckAndRunUpdateAsync(string owner, string repo)
        {
            try
            {
                string baseDir = AppContext.BaseDirectory;
                string localVersionFile = Path.Combine(baseDir, "version.txt");
                if (!File.Exists(localVersionFile)) File.WriteAllText(localVersionFile, "0.0.0");
                string localVersion = File.ReadAllText(localVersionFile).Trim();

                using HttpClient client = new HttpClient();
                string remoteVersionUrl = $"https://raw.githubusercontent.com/{owner}/{repo}/main/version.txt";
                string remoteVersion = (await client.GetStringAsync(remoteVersionUrl)).Trim();

                if (string.Equals(remoteVersion, localVersion, StringComparison.OrdinalIgnoreCase)) return false;

                string downloadUrl = $"https://github.com/{owner}/{repo}/releases/latest/download/BasicESP.exe";
                string tempFile = Path.Combine(Path.GetTempPath(), $"BasicESP_update_{remoteVersion}.exe");

                using (var resp = await client.GetAsync(downloadUrl))
                {
                    resp.EnsureSuccessStatusCode();
                    using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await resp.Content.CopyToAsync(fs);
                    }
                }

                var psi = new ProcessStartInfo(tempFile)
                {
                    UseShellExecute = true
                };
                Process.Start(psi);

                File.WriteAllText(localVersionFile, remoteVersion);

                return true;
            }
            catch
            {
                return false;
            }
         }
     }
 }
