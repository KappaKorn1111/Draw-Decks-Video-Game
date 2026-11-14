using UnityEngine;

namespace DrawDecks.Client.Bootstrap
{
    public sealed class DrawDecksBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Application.runInBackground = true;
            Debug.Log("Draw Decks client foundation initialized.");
        }
    }
}
