using System;
using System.Collections;
using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.SceneManagement;

public class StartupManager : MonoBehaviour
{
    private int _clicks = 0;

    static StartupManager()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    private void Awake()
    {
        ThreadedLogger.Start();

        StartCoroutine(StartupCoroutine());
    }

    private IEnumerator StartupCoroutine()
    {
        yield return null;

        ThreadedLogger.EnqueueMessage($"Starting {Time.realtimeSinceStartup}");

        DontDestroyOnLoad(gameObject);

        StartCoroutine(InitWww(1));

        var sceneToLoad = "SampleScene2";

        ThreadedLogger.EnqueueMessage($"Load {sceneToLoad} Scene, {Time.realtimeSinceStartup}");

        yield return SceneManager.LoadSceneAsync(sceneToLoad);

        OnLoadingShown();
    }

    private void OnLoadingShown()
    {
        ThreadedLogger.EnqueueMessage($"Init second time, {Time.realtimeSinceStartup}");

        StartCoroutine(InitWww(2));
    }

    private IEnumerator InitWww(int index)
    {
        ThreadedLogger.EnqueueMessage($"WWW Init started, {index}, {Time.realtimeSinceStartup}");

        Uri uri = new Uri("https://google.com");
        int finished = 0;

        var _task = new Task(() => 
        {
            Thread.CurrentThread.Name = $"NetworkThread({index})";
            try
            {
                ThreadedLogger.EnqueueMessage($"[{index}] Thread Started!");

                IPAddress[] addresses;

                using (ManualResetEvent mre = new ManualResetEvent(false))
                {
                    IAsyncResult result = System.Net.Dns.BeginGetHostAddresses(uri.Host, (res) => 
                    {
                        ThreadedLogger.EnqueueMessage($"[{index}] DNS Resolved!");
                        mre.Set();
                    }, null);

                    bool success = mre.WaitOne(TimeSpan.FromSeconds(20));

                    if (success)
                    {
                        addresses = System.Net.Dns.EndGetHostAddresses(result);
                    }
                    else
                    {
                        throw new TimeoutException("DNS resolve timed out!");
                    }
                }

                ThreadedLogger.EnqueueMessage($"[{index}] IP Found!");

                Socket client = null;

                foreach (var ipAddress in addresses)
                {
                    var remoteEP = new IPEndPoint(ipAddress, uri.Port);

                    client = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    using (ManualResetEvent mre = new ManualResetEvent(false))
                    {
                        IAsyncResult result = client.BeginConnect(remoteEP, (res) =>
                        {
                            ThreadedLogger.EnqueueMessage($"[{index}] Connected!");
                            mre.Set();
                        }, null);

                        var active = mre.WaitOne(TimeSpan.FromSeconds(20));
                        if (active)
                            client.EndConnect(result);
                        else
                        {
                            try
                            {
                                client.Disconnect(true);
                            }
                            catch
                            { }

                            throw new TimeoutException("Connection timed out!");
                        }
                    }
                }

                ThreadedLogger.EnqueueMessage($"[{index}] TCP Connected!");

                var networkStream = new NetworkStream(client, true);

                ThreadedLogger.EnqueueMessage($"[{index}] Connecting with TLS");

                using var sslStream = new SslStream(networkStream);
                sslStream.AuthenticateAsClient(uri.Host);

                ThreadedLogger.EnqueueMessage($"[{index}] Connected with TLS");

                sslStream.Close();

                ThreadedLogger.EnqueueMessage($"[{index}] Closed!");
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
                ThreadedLogger.EnqueueMessage($"[{index}] Exception: {ex.Message}");
            }

            ThreadedLogger.EnqueueMessage($"[{index}] Finishing!");
            Interlocked.Exchange(ref finished, 1);
        }, TaskCreationOptions.LongRunning);

        _task.ConfigureAwait(false);
        _task.Start();

        while (finished == 0)
            yield return 0;

        OnInitWww(index);
    }

    private void OnInitWww(int tag)
    {
        ThreadedLogger.EnqueueMessage($"WWW Init completed: {tag}, {Time.realtimeSinceStartup}");

        if (tag == 2)
            OnComplete();
    }

    private void OnComplete()
    {
        ThreadedLogger.EnqueueMessage($"OnComplete, {Time.realtimeSinceStartup}");
        StartCoroutine(OnCompleteCoroutine());
    }

    private IEnumerator OnCompleteCoroutine()
    {
        yield return new WaitForSeconds(0.5f);

        ThreadedLogger.EnqueueMessage("App not hang, restarting app for another attempt...");

        RestartAndroidApp();
    }

    private void OnGUI()
    {
        GUI.skin.button.fontSize = 40;

        // Button allows checking if app is not responding (button not clickable)
        if (GUI.Button(new Rect(Screen.width / 2, Screen.height / 2, 300, 300), _clicks.ToString()))
            _clicks++;
    }

    private static void RestartAndroidApp()
    {
        if (Application.isEditor)
            return;

        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            const int kIntent_FLAG_ACTIVITY_CLEAR_TASK = 0x00008000;
            const int kIntent_FLAG_ACTIVITY_NEW_TASK = 0x10000000;

            var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var pm = currentActivity.Call<AndroidJavaObject>("getPackageManager");
            var intent = pm.Call<AndroidJavaObject>("getLaunchIntentForPackage", Application.identifier);

            intent.Call<AndroidJavaObject>("setFlags", kIntent_FLAG_ACTIVITY_NEW_TASK | kIntent_FLAG_ACTIVITY_CLEAR_TASK);
            currentActivity.Call("startActivity", intent);
            currentActivity.Call("finish");
            var process = new AndroidJavaClass("android.os.Process");
            int pid = process.CallStatic<int>("myPid");
            process.CallStatic("killProcess", pid);
        }
    }
}
