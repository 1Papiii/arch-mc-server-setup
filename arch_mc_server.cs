using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Main
{
    class Program
    {
        const string SERVER_DIR = "opt/minecraft";
        const string SERVICE_NAME = "minecraft";
        const string SESSIONS = "mc-server";
        const string MANIFEST = "https://launchermeta.mojang.com/mc/game/version_manifest.json";
        const string PAPER_API = "https://api.papermc.io/v2/projects/paper";
        const string PLAYIT = "https://github.com/playit-cloud/playit-agent/releases/download/playit-agent-linux-amd64.tar.gz";
        const string packages = "jre-opnjdk-headless curl wget tmux htop python3";
        // --- ADD THESE BELOW YOUR CONST DECLARATIONS ---
        static string XMS = "1";
        static string XMX = "4";
        static string VERSION_ID = "";
        static string JAR_URL = "";
        static string SERVER_TYPE = "";
        static bool HAS_SYSTEMD = false;
        static bool HAS_PLAYIT = false;

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Clear();

            string[] lines = new[]
            {
                "████████████████████████████████████████████████████████████████",
                "█  _  _ _ _  _ ____ ____ ____ ____ ____ ____                   █",
                "█ |\\/| | |\\ | |___ |    |__/ |__| |___ |                       █",
                "█ |  | | | \\| |___ |___ |  \\ |  | |    |                       █",
                "█                                                              █",
                "█  ____  ____ ____ _  _ ____ ____    ____ ____ ____ _  _ ___   █",
                "█ [__   |___ |__/ |  | |___ |__/    [__  |___ |__/ |  | |__]   █",
                "█ ___]  |___ |  \\/  \\/  |___ |  \\    ___] |___ |  \\/  \\/  |    █",
                "█                                                              █",
                "█  ▸ Fresh Arch Linux  →  Fully running Minecraft server       █",
                "█  ▸ Vanilla or Paper  ·  Any version  ·  playit.gg tunnel     █",
                "█  ▸ tmux dashboard  ·  On-login auto-launch                   █",
                "█  ▸ github.com/cpu-gpu-ram                                    █",
                "█  ▸ github.com/1Papiii                                        █",
                "████████████████████████████████████████████████████████████████"
            };

            Console.ResetColor();

            foreach (var line in lines)
                Console.WriteLine(line);

            Console.WriteLine();

            Console.WriteLine("Minecraft End User License Agreement");
            Console.WriteLine("Before the server can run, you must agree to Mojang's EULA.");
            Console.WriteLine("https://www.minecraft.net/en-us/eula");
            Console.WriteLine("You can:");
            Console.WriteLine("     • Run this server for personal, non-commercial use.");
            Console.WriteLine("     • Cannot charge players money beyond Mojang's guidelines.");
            Console.WriteLine("     • Cannot redistribute the server software itself.");
            Console.WriteLine("     • Mojang's Terms of Service apply at all times.");
            Console.WriteLine();

            Console.WriteLine("Do you agree to the Minecraft EULA? (y/n)");
            string usrIn = Console.ReadLine() ?? string.Empty;

            if (string.Equals(usrIn, "y", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("EULA accepted. Starting package install...");

                string installCommand = $"pacman -S --noconfirm {packages}";
                ProcessStartInfo installPsi = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{installCommand}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(installPsi))
                {
                    if (process != null)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();

                        process.WaitForExit();

                        Console.WriteLine("--- Installation Output ---");
                        Console.WriteLine(output);

                        if (process.ExitCode != 0)
                        {
                            Console.WriteLine($"--- Error (Exit Code: {process.ExitCode}) ---");
                            Console.WriteLine(error);
                        }
                    }
                }

                Console.WriteLine("Let's allocate some RAM now.");
                string memoryCommand = "grep MemTotal /proc/meminfo";

                ProcessStartInfo memoryPsi = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{memoryCommand}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(memoryPsi))
                {
                    if (process != null)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                        {
                            Console.WriteLine($"Raw Memory: {output.Trim()}");

                            Match match = Regex.Match(output, @"\d+");
                            if (match.Success)
                            {
                                long memoryInKb = long.Parse(match.Value);
                                double memoryInGb = memoryInKb / 1024.0 / 1024.0;

                                Console.WriteLine("\n--- Parsed System Memory ---");
                                Console.WriteLine($"Total RAM: {memoryInGb:F2} GB");
                                Console.WriteLine("How much do you want to allocate? (GB)");

                                string memInput = Console.ReadLine() ?? string.Empty;
                                if (double.TryParse(memInput, out double serverMem))
                                {
                                    Console.WriteLine($"Allocating {serverMem:F2} GB");
                                    
                                    // Set RAM based on user input
                                    XMX = ((int)Math.Max(1, serverMem)).ToString();
                                    XMS = "1";

                                    Console.WriteLine("\n[1] Vanilla  -  The official Mojang server");
                                    Console.WriteLine("[2] Paper    -  A faster Minecraft fork with plugin support");
                                    Console.WriteLine("Which type? [1 or 2]:");
                                    
                                    string typeChoice = Console.ReadLine()?.Trim() ?? "";

                                    if (typeChoice == "1")
                                    {
                                        SERVER_TYPE = "Vanilla";
                                        PickVanillaVersion();
                                    }
                                    else
                                    {
                                        SERVER_TYPE = "Paper";
                                        PickPaperVersion();
                                    }

                                    SetupServerDir();
                                    MaybeCreateSystemdService();
                                    SetupPlayit();
                                    SetupTmuxDashboard();
                                    PrintSummary();
                                }

                                
                                else
                                {
                                    Console.WriteLine($"Invalid allocation value: {memInput}");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Failed to retrieve memory. Error: {error}");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("EULA not accepted. Exiting...");
                return;
            }
        }

        // ====================================================================
        // HELPER METHODS (Paste below Main method)
        // ====================================================================

        static string FetchJson(string url)
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Arch-MC-Setup-CSharp");
            return client.GetStringAsync(url).GetAwaiter().GetResult();
        }

        static void RunBashCommand(string command)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using Process process = Process.Start(psi);
            process?.WaitForExit();
        }

        static void DownloadFile(string url, string path)
        {
            Console.WriteLine($"Downloading to {path}...");
            using HttpClient client = new HttpClient();
            var response = client.GetAsync(url).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            using FileStream fs = new FileStream(path, FileMode.Create);
            response.Content.CopyToAsync(fs).GetAwaiter().GetResult();
            Console.WriteLine($"Saved -> {path}");
        }

        static void PickVanillaVersion()
        {
            Console.WriteLine("Fetching version list from Mojang ...");
            string json = FetchJson(MANIFEST);
            using JsonDocument doc = JsonDocument.Parse(json);
            
            string latestRelease = doc.RootElement.GetProperty("latest").GetProperty("release").GetString();
            string latestSnapshot = doc.RootElement.GetProperty("latest").GetProperty("snapshot").GetString();

            var releases = doc.RootElement.GetProperty("versions").EnumerateArray()
                .Where(v => v.GetProperty("type").GetString() == "release")
                .Take(10).ToList();

            Console.WriteLine("\n  Recent releases:\n");
            for (int i = 0; i < releases.Count; i++)
            {
                string vid = releases[i].GetProperty("id").GetString();
                string marker = (vid == latestRelease) ? "  <- latest" : "";
                Console.WriteLine($"  [{i + 1,2}]  {vid}{marker}");
            }
            Console.WriteLine($"  [ s]  {latestSnapshot}  (snapshot)");
            Console.WriteLine($"  [ c]  Enter a custom version ID\n");

            Console.WriteLine("Pick a version:");
            string choice = Console.ReadLine()?.Trim().ToLower() ?? "";

            if (choice == "s")
            {
                VERSION_ID = latestSnapshot;
                string snapUrl = doc.RootElement.GetProperty("versions").EnumerateArray()
                    .First(v => v.GetProperty("id").GetString() == latestSnapshot).GetProperty("url").GetString();
                using JsonDocument snapDoc = JsonDocument.Parse(FetchJson(snapUrl));
                JAR_URL = snapDoc.RootElement.GetProperty("downloads").GetProperty("server").GetProperty("url").GetString();
            }
            else if (choice == "c")
            {
                Console.WriteLine("Enter version ID (e.g. 1.20.4):");
                VERSION_ID = Console.ReadLine()?.Trim() ?? "";
                var customUrlNode = doc.RootElement.GetProperty("versions").EnumerateArray()
                    .FirstOrDefault(v => v.GetProperty("id").GetString() == VERSION_ID);

                if (customUrlNode.ValueKind == JsonValueKind.Undefined)
                {
                    Console.WriteLine($"[FATAL] Version '{VERSION_ID}' not found.");
                    Environment.Exit(1);
                }
                using JsonDocument customDoc = JsonDocument.Parse(FetchJson(customUrlNode.GetProperty("url").GetString()));
                JAR_URL = customDoc.RootElement.GetProperty("downloads").GetProperty("server").GetProperty("url").GetString();
            }
            else if (int.TryParse(choice, out int idx) && idx > 0 && idx <= releases.Count)
            {
                VERSION_ID = releases[idx - 1].GetProperty("id").GetString();
                string url = releases[idx - 1].GetProperty("url").GetString();
                using JsonDocument vDoc = JsonDocument.Parse(FetchJson(url));
                JAR_URL = vDoc.RootElement.GetProperty("downloads").GetProperty("server").GetProperty("url").GetString();
            }

            Console.WriteLine($"Selected Vanilla {VERSION_ID}");
        }

        static void PickPaperVersion()
        {
            Console.WriteLine("Fetching Paper version list ...");
            using JsonDocument doc = JsonDocument.Parse(FetchJson(PAPER_API));
            
            var allVersions = doc.RootElement.GetProperty("versions").EnumerateArray().Select(v => v.GetString()).ToList();
            var versions = allVersions.Skip(Math.Max(0, allVersions.Count - 10)).ToList();

            Console.WriteLine("\n  Available Paper versions:\n");
            for (int i = versions.Count - 1, disp = 1; i >= 0; i--, disp++)
            {
                string marker = (i == versions.Count - 1) ? "  <- latest" : "";
                Console.WriteLine($"  [{disp,2}]  {versions[i]}{marker}");
            }
            Console.WriteLine("  [ c]  Enter a custom version\n");

            Console.WriteLine("Pick a version:");
            string choice = Console.ReadLine()?.Trim().ToLower() ?? "";

            if (choice == "c")
            {
                Console.WriteLine("Enter Paper version (e.g. 1.20.4):");
                VERSION_ID = Console.ReadLine()?.Trim() ?? "";
            }
            else if (int.TryParse(choice, out int idx) && idx > 0 && idx <= versions.Count)
            {
                VERSION_ID = versions[versions.Count - idx];
            }

            Console.WriteLine($"Fetching latest build for Paper {VERSION_ID} ...");
            using JsonDocument vDoc = JsonDocument.Parse(FetchJson($"{PAPER_API}/versions/{VERSION_ID}"));
            int latestBuild = vDoc.RootElement.GetProperty("builds").EnumerateArray().Last().GetInt32();

            using JsonDocument bDoc = JsonDocument.Parse(FetchJson($"{PAPER_API}/versions/{VERSION_ID}/builds/{latestBuild}"));
            string jarName = bDoc.RootElement.GetProperty("downloads").GetProperty("application").GetProperty("name").GetString();
            
            JAR_URL = $"{PAPER_API}/versions/{VERSION_ID}/builds/{latestBuild}/downloads/{jarName}";
            Console.WriteLine($"Selected Paper {VERSION_ID} build #{latestBuild}");
        }

        static void SetupServerDir()
        {
            string absoluteDir = $"/{SERVER_DIR}";
            Console.WriteLine($"\nSetting up server directory: {absoluteDir}");
            
            if (Directory.Exists(absoluteDir))
            {
                Console.WriteLine($"{absoluteDir} already exists. Overwrite? (y/n)");
                if (Console.ReadLine()?.Trim().ToLower() != "y")
                {
                    Console.WriteLine("Aborted - will not overwrite.");
                    Environment.Exit(1);
                }
                Directory.Delete(absoluteDir, true);
            }
            Directory.CreateDirectory(absoluteDir);

            DownloadFile(JAR_URL, Path.Combine(absoluteDir, "server.jar"));
            File.WriteAllText(Path.Combine(absoluteDir, "eula.txt"), "eula=true\n");

            string serverProps = @"server-port=25565
gamemode=survival
difficulty=normal
max-players=20
motd=King Koonta MC - powered by princebread
online-mode=true
spawn-protection=16
view-distance=10";
            File.WriteAllText(Path.Combine(absoluteDir, "server.properties"), serverProps);

            string startSh = $@"#!/usr/bin/env bash
cd ""{absoluteDir}""
exec java -Xms{XMS}G -Xmx{XMX}G \
    -XX:+UseG1GC \
    -XX:G1HeapRegionSize=4M \
    -XX:+UnlockExperimentalVMOptions \
    -XX:+ParallelRefProcEnabled \
    -XX:+AlwaysPreTouch \
    -jar server.jar --nogui";
            
            string startPath = Path.Combine(absoluteDir, "start.sh");
            File.WriteAllText(startPath, startSh);
            RunBashCommand($"chmod +x {startPath}");
            Console.WriteLine("start.sh written.");
        }

        static void MaybeCreateSystemdService()
        {
            Console.WriteLine("\nCreate a systemd service for Minecraft? (y/n)");
            if (Console.ReadLine()?.Trim().ToLower() != "y") return;

            string absoluteDir = $"/{SERVER_DIR}";
            RunBashCommand($"id -u minecraft &>/dev/null || useradd -r -m -d {absoluteDir} -s /bin/bash minecraft");
            RunBashCommand($"chown -R minecraft:minecraft {absoluteDir}");

            string svcInfo = $@"[Unit]
Description=Minecraft Server (King Koonta)
After=network.target

[Service]
User=minecraft
WorkingDirectory={absoluteDir}
ExecStart={absoluteDir}/start.sh
ExecStop=/bin/kill -s INT $MAINPID
Restart=on-failure
RestartSec=5s
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target";

            File.WriteAllText($"/etc/systemd/system/{SERVICE_NAME}.service", svcInfo);
            RunBashCommand("systemctl daemon-reload");

            Console.WriteLine("Enable server to start automatically at boot? (y/n)");
            if (Console.ReadLine()?.Trim().ToLower() == "y")
            {
                RunBashCommand($"systemctl enable {SERVICE_NAME}");
                Console.WriteLine("Service enabled at boot.");
            }
            HAS_SYSTEMD = true;
        }

        static void SetupPlayit()
        {
            Console.WriteLine("\nInstall playit.gg? (y/n)");
            if (Console.ReadLine()?.Trim().ToLower() != "y") return;

            string playitBin = "/usr/local/bin/playit";
            try
            {
                DownloadFile(PLAYIT, playitBin);
                RunBashCommand($"chmod +x {playitBin}");
            }
            catch
            {
                Console.WriteLine("Direct download failed. Trying AUR...");
                RunBashCommand("yay -S --noconfirm playit-bin || paru -S --noconfirm playit-bin");
            }

            string user = Environment.GetEnvironmentVariable("SUDO_USER");
            if (string.IsNullOrEmpty(user)) user = "root";
            string home = user == "root" ? "/root" : $"/home/{user}";

            string wrapperStr = @"#!/usr/bin/env bash
LOGFILE=""/tmp/playit-live.log""
wait_for_net() {
    until ping -c1 -W2 1.1.1.1 &>/dev/null; do sleep 5; done
}
while true; do
    wait_for_net
    /usr/local/bin/playit 2>&1 | tee ""$LOGFILE""
    sleep 10
done";
            File.WriteAllText("/usr/local/bin/playit-wrapper", wrapperStr);
            RunBashCommand("chmod +x /usr/local/bin/playit-wrapper");

            string svcsPlayit = $@"[Unit]
Description=playit.gg tunnel agent
After=network-online.target
Wants=network-online.target

[Service]
User={user}
Type=simple
ExecStart=/usr/local/bin/playit-wrapper
Restart=always
RestartSec=10s
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target";

            File.WriteAllText("/etc/systemd/system/playit.service", svcsPlayit);
            RunBashCommand("systemctl daemon-reload");

            Console.WriteLine("Enable playit to start automatically at boot? (y/n)");
            if (Console.ReadLine()?.Trim().ToLower() == "y")
                RunBashCommand("systemctl enable playit");

            string cfgDir = $"{home}/.config/playit";
            if (File.Exists($"{cfgDir}/config.toml") || File.Exists($"{cfgDir}/playit.toml"))
            {
                Console.WriteLine("Existing playit config found.");
            }
            else
            {
                Console.WriteLine("\nFIRST-TIME CLAIM: Watch the bottom tmux pane for the claim URL!");
            }
            HAS_PLAYIT = true;
        }

        static void SetupTmuxDashboard()
        {
            Console.WriteLine("\nSet up the tmux dashboard with on-login auto-launch? (y/n)");
            if (Console.ReadLine()?.Trim().ToLower() != "y") return;

            string user = Environment.GetEnvironmentVariable("SUDO_USER");
            if (string.IsNullOrEmpty(user)) user = "root";
            string home = user == "root" ? "/root" : $"/home/{user}";
            string launchScript = $"{home}/.mc_dashboard.sh";

            string mcCmd = HAS_SYSTEMD ? $"journalctl -u {SERVICE_NAME} -f --no-pager" : $"bash /{SERVER_DIR}/start.sh";
            string playitCmd = HAS_PLAYIT ? "bash /usr/local/bin/playit-wrapper" : "bash";

            string scriptStr = $@"#!/usr/bin/env bash
SESSION=""{SESSIONS}""
if tmux has-session -t ""$SESSION"" 2>/dev/null; then
    tmux attach-session -t ""$SESSION""
    exit 0
fi

COLS=$(tput cols)
ROWS=$(tput lines)

tmux new-session -d -s ""$SESSION"" -x ""$COLS"" -y ""$ROWS""
tmux rename-window -t ""$SESSION:0"" ""mc-dashboard""
tmux send-keys -t ""$SESSION:0.0"" ""{mcCmd}"" Enter

tmux split-window -t ""$SESSION:0.0"" -h -p 40
tmux send-keys -t ""$SESSION:0.1"" ""htop"" Enter

tmux select-pane -t ""$SESSION:0.0""
tmux split-window -t ""$SESSION:0.0"" -v -p 30
tmux send-keys -t ""$SESSION:0.2"" ""{playitCmd}"" Enter

tmux select-pane -t ""$SESSION:0.0""
tmux attach-session -t ""$SESSION""";

            File.WriteAllText(launchScript, scriptStr);
            RunBashCommand($"chmod +x {launchScript}");
            RunBashCommand($"chown {user}:{user} {launchScript}");

            string profilePath = $"{home}/.bash_profile";
            string hookStr = $@"
if [[ -z ""${{TMUX:-}}"" ]] && [[ ""$(tty)"" =~ /dev/tty[0-9] ]]; then
    bash ""{launchScript}""
fi";
            
            if (!File.Exists(profilePath) || !File.ReadAllText(profilePath).Contains("mc_dashboard"))
            {
                File.AppendAllText(profilePath, hookStr);
                RunBashCommand($"chown {user}:{user} {profilePath}");
            }
            Console.WriteLine("Tmux dashboard setup complete.");
        }

        static void PrintSummary()
        {
            Console.WriteLine("\n████████████████████████████████████████████████████████████████");
            Console.WriteLine("█  ALL DONE                                                    █");
            Console.WriteLine("████████████████████████████████████████████████████████████████");
            Console.WriteLine($"\n  Server:        {SERVER_TYPE} {VERSION_ID}");
            Console.WriteLine($"  RAM:           Xms={XMS}G  Xmx={XMX}G");
            Console.WriteLine($"  Directory:     /{SERVER_DIR}");
            Console.WriteLine("\n  Dashboard:     Log out and back in to open tmux automatically");
            Console.WriteLine("                 Or run: bash ~/.mc_dashboard.sh");
            Console.WriteLine("\n████████████████████████████████████████████████████████████████\n");
        }
        
    }
}