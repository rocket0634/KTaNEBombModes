using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public enum BombMode
{
    Normal,
    Time,
    //VS,
    Zen
}

/*class TwitchPlaysActive
{
    public static GameObject _gameObject;

    private static IDictionary<string, object> Properties
    {
        get
        {
            return _gameObject == null
                ? null
                : _gameObject.GetComponent<IDictionary<string, object>>();
        }
    }

    public static IEnumerator Refresh()
    {
        for (var i = 0; i < 4 && _gameObject == null; i++)
        {
            _gameObject = GameObject.Find("TwitchPlays_Info");
            yield return null;
        }
    }

    public static bool Installed()
    {
        return _gameObject != null;
    }
}*/

[RequireComponent(typeof(KMService))]
[RequireComponent(typeof(KMGameInfo))]
class Modes : MonoBehaviour
{
    private List<Bomb> Bombs = null;
    private List<TimerComponent> Timers = new List<TimerComponent>();
    private float normalRate = 0;
    private bool TwitchPlaysActive = false;
    private void Awake()
    {
        GetComponent<KMGameInfo>().OnStateChange += delegate (KMGameInfo.State state)
        {
            Debug.LogFormat("[Modes] Updating services...");
            StartCoroutine(UpdateServices());
            if (state == KMGameInfo.State.Gameplay && !TwitchPlaysActive)
            {
                StartCoroutine(CheckForBomb());
            }
            else if (TwitchPlaysActive)
            {
                StopCoroutine(CheckForBomb());
            }
        };
    }

    private void GetModServices()
    {
        KMService[] modServices = FindObjectsOfType<KMService>();
        List<string> temp = new List<string>();
        foreach (KMService modService in modServices)
        {
            if (modService.name.StartsWith("TwitchPlaysService")) temp.Add("TwitchPlaysService");
        }
        if (temp.Contains("TwitchPlaysService"))
        {
            TwitchPlaysActive = true;
            Debug.LogFormat("[Modes] Conflict Detected");
        }
        else TwitchPlaysActive = false;
    }

    private IEnumerator CheckForBomb()
    {
        yield return new WaitUntil(() => (SceneManager.Instance.GameplayState.Bombs != null && SceneManager.Instance.GameplayState.Bombs.Count > 0));
        Bombs = SceneManager.Instance.GameplayState.Bombs;

        foreach (Bomb bomb in Bombs)
        {
            if (bomb.GetTimer() == null || bomb.GetTimer().GetRate() < 0) continue;
            normalRate = -bomb.GetTimer().GetRate();
            bomb.GetTimer().text.color = Color.blue;
            bomb.GetTimer().SetRateModifier(normalRate);
            bomb.GetTimer().SetTimeRemaing(1);
            bomb.NumStrikesToLose += 1;
            foreach (BombComponent bombComponent in bomb.BombComponents)
            {
                bombComponent.OnStrike += delegate{ CheckForStrikes(bomb); return false; };
            }
        }
        
    }

    private void CheckForStrikes(Bomb bomb)
    {
        bomb.GetTimer().SetRateModifier(normalRate);
        bomb.NumStrikesToLose += 1;
    }

    private IEnumerator UpdateServices()
    {
        GetModServices();
        yield return null;
    }
}

class ModesSettings
{
    public float StartTime = 30 * 60;
    public bool ZenActive = true;
    public bool TimeActive = false;
}