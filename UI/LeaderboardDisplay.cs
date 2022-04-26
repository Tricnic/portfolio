using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using BSCore;
using PlayFab.ClientModels;

public class LeaderboardDisplay : MonoBehaviour
{
    [Inject] private LeaderboardManager _leaderboardManager = default;
    [Inject] private UserManager _userManager = default;

    [SerializeField] private StatisticKey _leaderboardStat = default;
    [SerializeField] private SmartPool _smartPool = default;
    [SerializeField] private LeaderboardPlate _localPlayerPlate = default;

    public void FetchLeaderboard()
    {
        _leaderboardManager.GetLeaderboard(_leaderboardStat, 0, 100, OnLeaderboardFetched, (e) =>
        {
            Debug.LogError($"[LeaderboardDisplay] Failed to fetch leaderboard {_leaderboardStat}. Reason {e}");
        });
        _leaderboardManager.GetLeaderboardAroundLocalPlayer(_leaderboardStat, 1, OnLocalPlayerLeaderboardFetched, (e) => 
        {
            Debug.LogError($"[LeaderboardDisplay] Failed to fetch leaderboard for local player {_leaderboardStat}. Reason {e}");
        });
    }

    private void OnLeaderboardFetched(LeaderboardData leaderboardData)
    {
        Debug.Log($"[LeaderboardDisplay] Got leaderboard for stat {_leaderboardStat}. With {leaderboardData.Leaderboard.Count} entries");
        PopulateLeaderboardList(leaderboardData.Leaderboard);
    }

    private void OnLocalPlayerLeaderboardFetched(LeaderboardData leaderboardData)
    {
        Debug.Log($"[LeaderboardDisplay] Got leaderboard for stat {_leaderboardStat}. With {leaderboardData.Leaderboard.Count} entries");
        if(leaderboardData.Leaderboard.Count <= 0 || leaderboardData.Leaderboard[0].StatValue <= 0)
        {
            Debug.Log($"[LeaderboardDisplay] Got leaderboard for local player but list is empty");
            _localPlayerPlate.PopulateNoStat(_userManager.CurrentUser.DisplayName, true);
            return;
        }

        _localPlayerPlate.Populate(leaderboardData.Leaderboard[0], true);
    }

    private void PopulateLeaderboardList(List<PlayerLeaderboardEntry> leaderboardEntries)
    {
        _smartPool.DespawnAllItems();

        foreach(var entry in leaderboardEntries)
        {
            var go = _smartPool.SpawnItem();
            var leaderboardPlate = go.GetComponent<LeaderboardPlate>();
            if(leaderboardPlate == null)
            {
                Debug.LogError($"[LeaderboardDisplay] Failed to get plate from prefab");
                continue;
            }
            var isLocalPlayer = _userManager.CurrentUser.Id == entry.PlayFabId;
            leaderboardPlate.Populate(entry, isLocalPlayer);
        }
    }
}
