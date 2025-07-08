using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System;

public class RunWebServer
{
    private const string BuildFolder = "Builds/WebBuild";
    private const string ServerJs = "server.js";
    private const string PackageJson = "package.json";
    private const string NodeModules = "node_modules";
    private const string ExpressFolder = "node_modules/express";
    private const string IndexHtml = "index.html";

    [MenuItem("Tools/Run Web Build")]
    public static void RunServer()
    {
        string projectPath = Directory.GetCurrentDirectory();
        string buildPath = Path.Combine(projectPath, BuildFolder);

        // Find node
        string nodePath = FindExecutable("node");
        if (string.IsNullOrEmpty(nodePath))
        {
            UnityEngine.Debug.LogError("Node.js is not installed or not in PATH. Please install Node.js from https://nodejs.org/");
            return;
        }
        // Derive npm path from node path
        string npmPath = nodePath.Replace("node", "npm");
        
        // 2. Check build folder
        if (!Directory.Exists(buildPath))
        {
            UnityEngine.Debug.LogError($"WebGL build folder not found at: {buildPath}. Please build your WebGL project first.");
            return;
        }

        // Inject Firebase into index.html
        string indexHtmlPath = Path.Combine(buildPath, IndexHtml);
        if (!File.Exists(indexHtmlPath))
        {
            UnityEngine.Debug.LogError($"index.html not found at: {indexHtmlPath}. Please build your WebGL project first.");
            return;
        }
        InjectFirebaseToIndexHtml(indexHtmlPath);

        // 3. Check server.js
        string serverJsPath = Path.Combine(buildPath, ServerJs);
        if (!File.Exists(serverJsPath))
        {
            File.WriteAllText(serverJsPath, GetServerJsContent());
            UnityEngine.Debug.Log("Created server.js in build folder.");
        }

        // 4. Check package.json
        string packageJsonPath = Path.Combine(buildPath, PackageJson);
        if (!File.Exists(packageJsonPath))
        {
            if (!RunShellCommand(npmPath, "init -y", buildPath))
            {
                UnityEngine.Debug.LogError("Failed to run 'npm init -y'. Please run it manually in the build folder.");
                return;
            }
            UnityEngine.Debug.Log("Created package.json.");
        }

        // 5. Check express
        string expressPath = Path.Combine(buildPath, ExpressFolder);
        if (!Directory.Exists(expressPath))
        {
            if (!RunShellCommand(npmPath, "install express", buildPath))
            {
                UnityEngine.Debug.LogError("Failed to install express. Please run 'npm install express' manually in the build folder.");
                return;
            }
            UnityEngine.Debug.Log("Installed express.");
        }

        // 6. Start server
        if (!IsServerRunning())
        {
            RunShellCommand(nodePath, ServerJs, buildPath, true); // true = run in background
            UnityEngine.Debug.Log("Started Brotli server.");
        }
        else
        {
            UnityEngine.Debug.Log("Brotli server already running.");
        }

        // 7. Open browser
        Application.OpenURL("http://localhost:8080");
    }

    private static void InjectFirebaseToIndexHtml(string indexHtmlPath)
    {
        string firebaseBlock = @"
    <!-- Firebase SDKs -->
    <script src='https://www.gstatic.com/firebasejs/10.12.2/firebase-app-compat.js'></script>
    <script src='https://www.gstatic.com/firebasejs/10.12.2/firebase-analytics-compat.js'></script>
    <script src='https://www.gstatic.com/firebasejs/10.12.2/firebase-database-compat.js'></script>
    <script src='https://www.gstatic.com/firebasejs/10.12.2/firebase-auth-compat.js'></script>
    <script>
      const firebaseConfig = {
        apiKey: 'AIzaSyCbolaU8c4Pd7tHwHn9p_6mSDwoZagZi8Q',
        authDomain: 'unityhighscoresgroup3.firebaseapp.com',
        databaseURL: 'https://unityhighscoresgroup3-default-rtdb.firebaseio.com',
        projectId: 'unityhighscoresgroup3',
        storageBucket: 'unityhighscoresgroup3.firebasestorage.app',
        messagingSenderId: '1081610006617',
        appId: '1:1081610006617:web:1af29cc63619bf3d60c148',
        measurementId: 'G-FJSKVHB7TE'
      };
      let firebaseInitialized = false;
      window.InitializeFirebase = function() {
        console.log('[FirebaseBridge] InitializeFirebase called');
        try {
          if (!firebaseInitialized) {
            firebase.initializeApp(firebaseConfig);
            firebase.analytics();
            firebaseInitialized = true;
          }
          firebase.auth().signInAnonymously()
            .then(() => {
              if (typeof window.unityInstance !== 'undefined') {
                console.log('Sending message to Unity...');
                window.unityInstance.SendMessage('FirebaseBridge', 'OnFirebaseInitialized', ""success"");
              }
            })
            .catch((e) => {
              console.error('[FirebaseBridge] Anonymous sign-in failed:', e);
              if (typeof window.unityInstance !== 'undefined') {
                console.log('Sending message to Unity...');
                window.unityInstance.SendMessage('FirebaseBridge', 'OnFirebaseInitialized', String('error:' + (e.message || 'Unknown error')));
              }
            });
        } catch (e) {
          console.error('[FirebaseBridge] InitializeFirebase error:', e);
          if (typeof window.unityInstance !== 'undefined') {
            console.log('Sending message to Unity...');
            window.unityInstance.SendMessage('FirebaseBridge', 'OnFirebaseInitialized', String(e.message || 'Unknown error'));
          }
        }
      };
      window.SaveScoreToFirebaseJS = function(nickname, score, finishTime, callbackSuccess, callbackError) {
        console.log('[FirebaseBridge] SaveScoreToFirebaseJS called with', nickname, score, finishTime);
        try {
          firebase.database().ref('highscores/' + nickname).set({
            Score: score,
            FinishTime: finishTime
          }, function(error) {
            if (error) {
              console.error('[FirebaseBridge] SaveScoreToFirebaseJS error:', error);
              if (typeof window.unityInstance !== 'undefined' && typeof callbackError === 'string') {
                console.log('Sending message to Unity...');
                window.unityInstance.SendMessage('FirebaseBridge', callbackError, String(error.message || 'Unknown error'));
              }
            } else {
              console.log('[FirebaseBridge] SaveScoreToFirebaseJS success');
              if (typeof window.unityInstance !== 'undefined' && typeof callbackSuccess === 'string') {
                console.log('Sending message to Unity...');
                window.unityInstance.SendMessage('FirebaseBridge', callbackSuccess, ""OK"");
              }
            }
          });
        } catch (e) {
          console.error('[FirebaseBridge] SaveScoreToFirebaseJS exception:', e);
          if (typeof window.unityInstance !== 'undefined' && typeof callbackError === 'string') {
            console.log('Sending message to Unity...');
            window.unityInstance.SendMessage('FirebaseBridge', callbackError, String(e.message || 'Unknown error'));
          }
        }
      };
      window.GetLeaderboardFromFirebaseJS = function(callbackSuccess, callbackError) {
        console.log('[FirebaseBridge] GetLeaderboardFromFirebaseJS called');
        try {
          firebase.database().ref('highscores').orderByChild('Score').limitToLast(10).once('value')
            .then(function(snapshot) {
              var data = [];
              snapshot.forEach(function(child) {
                var val = child.val();
                data.push({
                  Nickname: child.key,
                  Score: val.Score,
                  FinishTime: val.FinishTime
                });
              });
              data.sort((a, b) => b.score - a.score);
              console.log('[FirebaseBridge] GetLeaderboardFromFirebaseJS success:', data);
              console.log('Sending message:', callbackSuccess, JSON.stringify(data));
              if (typeof window.unityInstance !== 'undefined') {
                console.log('Sending message to Unity...');
                window.unityInstance.SendMessage('FirebaseBridge', callbackSuccess, JSON.stringify(data) || '');
              }
            })
            .catch(function(error) {
              console.error('[FirebaseBridge] GetLeaderboardFromFirebaseJS error:', error);
              if (typeof window.unityInstance !== 'undefined' && typeof callbackError === 'string') {
                console.log('Sending message to Unity...');
                window.unityInstance.SendMessage('FirebaseBridge', callbackError, String(error.message || 'Unknown error'));
              }
            });
        } catch (e) {
          console.error('[FirebaseBridge] GetLeaderboardFromFirebaseJS exception:', e);
          if (typeof window.unityInstance !== 'undefined' && typeof callbackError === 'string') {
            console.log('Sending message to Unity...');
            window.unityInstance.SendMessage('FirebaseBridge', callbackError, String(e.message || 'Unknown error'));
          }
        }
      };
    </script>
";

        string html = File.ReadAllText(indexHtmlPath);
        if (html.Contains("<!-- Firebase SDKs -->"))
        {
            UnityEngine.Debug.Log("Firebase SDK block already present in index.html, skipping injection.");
            return;
        }
        int headClose = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        if (headClose >= 0)
        {
            html = html.Insert(headClose, "\n" + firebaseBlock + "\n");
            File.WriteAllText(indexHtmlPath, html);
            UnityEngine.Debug.Log("Injected Firebase SDK and interop JS into index.html");
        }
        else
        {
            UnityEngine.Debug.LogError("Could not find </head> in index.html to inject Firebase block.");
        }
    }

    private static string FindExecutable(string name)
    {
        // Try direct
        if (IsExecutableAvailable(name)) return name;

        // Try common locations
        string[] commonPaths = {
            "/opt/homebrew/bin/" + name,
            "/usr/local/bin/" + name,
            "/usr/bin/" + name
        };
        foreach (var path in commonPaths)
        {
            if (IsExecutableAvailable(path)) return path;
        }
        return null;
    }

    private static bool IsExecutableAvailable(string path)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = path,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = new Process { StartInfo = psi };
            process.Start();
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                UnityEngine.Debug.LogError($"Failed to run {path} --version. Exit code: {process.ExitCode}. Stdout: {stdout} Stderr: {stderr}");
            }
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsServerRunning()
    {
        // Try to connect to localhost:8080
        try
        {
            var client = new System.Net.Sockets.TcpClient("localhost", 8080);
            client.Close();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool RunShellCommand(string command, string args, string workingDir, bool background = false)
    {
        return RunShellCommand(command, args, workingDir, background, out _);
    }

    private static bool RunShellCommand(string command, string args, string workingDir, bool background, out string output)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = !background,
                RedirectStandardError = !background,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            // Patch PATH for Homebrew and common locations
            string path = Environment.GetEnvironmentVariable("PATH") ?? "";
            path = "/opt/homebrew/bin:/usr/local/bin:" + path;
            psi.Environment["PATH"] = path;

            var process = new Process { StartInfo = psi };
            process.Start();
            output = "";
            if (!background)
            {
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            return true;
        }
        catch (Exception e)
        {
            output = e.Message;
            return false;
        }
    }

    private static string GetServerJsContent()
    {
        return @"const express = require('express');
const path = require('path');
const app = express();
const buildPath = __dirname;

const IDLE_TIMEOUT = 0.1 * 60 * 1000;
let lastRequestTime = Date.now();
let idleTimer = null;

function resetIdleTimer() {
  lastRequestTime = Date.now();
  if (idleTimer) clearTimeout(idleTimer);
  idleTimer = setTimeout(() => {
    console.log('No requests received for 5 minutes. Shutting down server.');
    process.exit(0);
  }, IDLE_TIMEOUT);
}

// Reset timer on every request
app.use((req, res, next) => {
  resetIdleTimer();
  next();
});

// Serve Brotli files with correct header
app.get(/^.*\.br$/, (req, res, next) => {
  res.set('Content-Encoding', 'br');
  // Set correct Content-Type for Unity files
  if (req.url.endsWith('.js.br')) res.type('application/javascript');
  if (req.url.endsWith('.wasm.br')) res.type('application/wasm');
  if (req.url.endsWith('.data.br')) res.type('application/octet-stream');
  if (req.url.endsWith('.symbols.json.br')) res.type('application/json');
  next();
});

app.use(express.static(buildPath));

app.listen(8080, () => {
  console.log('Server running at http://localhost:8080');
  resetIdleTimer();
});
";
    }
} 