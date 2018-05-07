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

class TwitchPlaysActive
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
}

[RequireComponent(typeof(KMService))]
[RequireComponent(typeof(KMGameInfo))]
class Modes : MonoBehaviour
{
    private List<Bomb> Bombs = null;
    private List<TimerComponent> Timers = new List<TimerComponent>();
    private float normalRate = 0;
    private void Awake()
    {
        StartCoroutine(TwitchPlaysActive.Refresh());
        GetComponent<KMGameInfo>().OnStateChange += delegate (KMGameInfo.State state)
            {
                if (state == KMGameInfo.State.Gameplay && !TwitchPlaysActive.Installed())
                {
                    StartCoroutine(CheckForBomb());
                }
                else
                {
                    StopCoroutine(CheckForBomb());
                }
                if (TwitchPlaysActive.Installed())
                {
                    Debug.LogFormat("[Modes] Conflict detected, disabling Modes {0}", TwitchPlaysActive._gameObject.name);
                }
            };
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
}

class ModesSettings
{
    public float StartTime = 30 * 60;
    public bool ZenActive = true;
    public bool TimeActive = false;
}