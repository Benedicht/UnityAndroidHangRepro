
namespace Test
{

	using System;
	using System.Collections;

	using UnityEngine.Networking;


	using UnityEngine;

	/// <summary>
	/// NEVER called. Should be present in the build to get app hang.
	/// </summary>
	[DisallowMultipleComponent]
	public class DummyUnityWebRequest : MonoBehaviour
	{
		public bool started = false;
		public bool isRunning = false;
		
		
		private const int DefaultTimeoutSeconds = 10;


		private static readonly WaitForEndOfFrame CachedEndOfFrame = new WaitForEndOfFrame();
		private static bool requestInProgress;


		public float interval = 5;
		
		private Uri cachedUri;

		private float timeElapsed;
		private bool updateAfterPause;
		private double lastOnlineSecondsUtc;

		

		private void Update()
		{
			if (!started || !isRunning) return;

			if (interval > 0)
			{
				if (updateAfterPause)
				{
					updateAfterPause = false;
					return;
				}

				timeElapsed += Time.unscaledDeltaTime;
				if (timeElapsed >= interval * 60)
				{
					timeElapsed = 0;
					StartCoroutine(SendRequest());
				}
			}
		}
		
		public static IEnumerator SendRequest()
		{
			yield return SendRequest(new Uri("https://www.google.com"));
		}

		public static IEnumerator SendRequest(Uri uri)
		{
			if (requestInProgress)
			{
				yield return CachedEndOfFrame;
			}

			requestInProgress = true;
			

			using (var wr = GetWebRequest(uri))
			{
				yield return wr.SendWebRequest();
			}

			requestInProgress = false;
		}


		private static UnityWebRequest GetWebRequest(Uri uri, bool isHead = true)
		{

			var request = new UnityWebRequest(uri, isHead ? UnityWebRequest.kHttpVerbHEAD : UnityWebRequest.kHttpVerbGET)
			{
				useHttpContinue = false,
				timeout =  DefaultTimeoutSeconds,
				
				certificateHandler = null
				
			};
			
			return request;
		}

	}
}
