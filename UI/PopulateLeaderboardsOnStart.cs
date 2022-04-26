using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopulateLeaderboardsOnStart : MonoBehaviour
{
    [SerializeField] private List<LeaderboardDisplay> _leaderboardDisplays = default;

    private void Start()
    {
        foreach(var leaderboardDisplay in _leaderboardDisplays)
        {
            leaderboardDisplay.FetchLeaderboard();
        }
    }
}
