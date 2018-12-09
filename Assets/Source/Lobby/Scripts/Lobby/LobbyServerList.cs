using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using System.Collections;
using System.Collections.Generic;

namespace Prototype.NetworkLobby
{
    public class LobbyServerList : MonoBehaviour
    {
        public LobbyManager lobbyManager;

        public RectTransform serverListRect;
        public GameObject serverEntryPrefab;
        public GameObject noServerFound;

        protected int currentPage = 0;
        protected int previousPage = 0;

        static Color OddServerColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        static Color EvenServerColor = new Color(.94f, .94f, .94f, 1.0f);

        void OnEnable()
        {
            currentPage = 0;
            previousPage = 0;

            foreach (Transform t in serverListRect)
                Destroy(t.gameObject);

            noServerFound.SetActive(false);

            StartCoroutine(PeriodicalPageRefresh());
        }

		public void OnGUIMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matches)
		{
			if (matches.Count == 0)
			{
                if (currentPage == 0)
                {
                    noServerFound.SetActive(true);

                    foreach (Transform t in serverListRect)
                    {
                        Destroy(t.gameObject);
                    }
                }

                currentPage = previousPage;
               
                return;
            }

            noServerFound.SetActive(false);
            foreach (Transform t in serverListRect)
                Destroy(t.gameObject);

			for (int i = 0; i < matches.Count; ++i)
			{
				if(!matches[i].name.StartsWith(GameController.Instance.Scene))
				{
					continue;
				}

                GameObject o = Instantiate(serverEntryPrefab) as GameObject;
                Debug.Log("?");

				o.GetComponent<LobbyServerEntry>().Populate(matches[i], lobbyManager, (i % 2 == 0) ? OddServerColor : EvenServerColor);

				o.transform.SetParent(serverListRect, false);
            }
        }

        public void ChangePage(int dir)
        {
            int newPage = Mathf.Max(0, currentPage + dir);

            //if we have no server currently displayed, need we need to refresh page0 first instead of trying to fetch any other page
            if (noServerFound.activeSelf)
                newPage = 0;

            RequestPage(newPage);
        }

        IEnumerator PeriodicalPageRefresh()
        {
            while (this.enabled)
            {
                RequestPage(0);
                yield return new WaitForSeconds(5.0f);
            }
        }

        public void RequestPage(int page)
        {
            previousPage = currentPage;
            currentPage = page;
            
            if(lobbyManager.matchMaker == null)
            {
                lobbyManager.StartMatchMaker();
                lobbyManager.backDelegate = lobbyManager.SimpleBackClbk;
            }

			lobbyManager.matchMaker.ListMatches(page, 6, "", true, 0, 0, OnGUIMatchList);
		}
    }
}