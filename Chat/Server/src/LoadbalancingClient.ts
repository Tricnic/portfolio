import * as PlayFab from './Playfab';
import * as Interfaces from './Interfaces';
import {default as Config} from './Config';

let Photon = require('./lib/photon/Photon-Javascript_SDK');
let LBC = Photon.LoadBalancing.LoadBalancingClient;
let photonToken : string;
let serverUserDisplayName: string;
let matchRoomList: Photon.LoadBalancing.RoomInfo[] = [];
let lobbyRoomList: Photon.LoadBalancing.RoomInfo[] = [];
let matchRoomUpdateListeners: Interfaces.RoomListUpdateCallback[] = [];

if (Config.useMatchmaking) {
    console.log("Setting up Photon connection with App Version: " + Config.photon.appVersion);
    let lbc = new LBC(Photon.ConnectionProtocol.Ws, Config.photon.appId, Config.photon.appVersion);

    lbc.onStateChange = function (state) {
    	console.info("State:", LBC.StateToName(state));
    	switch (state) {
    		case LBC.State.JoinedLobby:
                console.log("Matchmaker connected to Photon and ready");
                RaiseRoomsUpdated()
    			break;
    		default:    
    			break;
    	}        
    };    

    lbc.onEvent = function (code, data) {
    	console.info("Event:", code, data);
    	lbc.raiseEvent(code, data);
    };

    lbc.onOperationResponse = function (errorCode, errorMsg, code, content) {
    	console.info("op resp:", errorCode, errorMsg, code, content);
    };

    lbc.onAppStats = function (errorCode: number, errorMsg: string, stats) {
        console.log("[onAppStats] ErrorCode: " + errorCode);
        console.log("[onAppStats] ErrorMsg: " + errorMsg);
        console.log("[onAppStats] Stats: " + JSON.stringify(stats));
    };

    lbc.onError = function (errorCode: number, errorMsg: string) {
        console.log("[onError] ErrorCode: " + errorCode);
        console.log("[onError] ErrorMsg: " + errorMsg);
    };

    lbc.onRoomList = function (rooms: Photon.LoadBalancing.RoomInfo[]) {
        SortRoomList(rooms);
    };

    lbc.onRoomListUpdate = function (rooms: Photon.LoadBalancing.RoomInfo[], roomsUpdated: Photon.LoadBalancing.RoomInfo[], roomsAdded: Photon.LoadBalancing.RoomInfo[], roomsRemoved: Photon.LoadBalancing.RoomInfo[]) {
        SortRoomList(rooms);
    };

    console.log("Logging into PlayFab with server user: " + Config.photon.email);

    PlayFab.LoginWithEmailAddress(Config.photon.email, Config.photon.password, OnPlayFabLogin);

    function SortRoomList(rooms: Photon.LoadBalancing.RoomInfo[]) {
        matchRoomList = [];
        lobbyRoomList = [];
        let roomNameList: string[] = [];
        rooms.forEach(room => {
            roomNameList.push(room.name);
            if (room._customProperties && room._customProperties["LobbyRoom"] === true) {
                lobbyRoomList.push(room);
            }    
            else {
                matchRoomList.push(room);
            }    
        });
        console.log("[onRoomListUpdate] Room list updated: ", roomNameList);
        RaiseRoomsUpdated();
    };

    function RaiseRoomsUpdated() {
        matchRoomUpdateListeners.forEach(callback => {
            callback(matchRoomList);
        });    
    }

    function OnPlayFabLogin(error: any, result: PlayFabClientModels.LoginResult) {
        if (error) {
            console.log("Error logging into PlayFab: " + JSON.stringify(error));
            return;
        }
        
        serverUserDisplayName = result.PlayFabId;
        // serverUserDisplayName = result.InfoResultPayload.PlayerProfile.DisplayName;
        console.log("Logged into PlayFab, fetching Photon token for user: " + result.PlayFabId);//. Result: " + JSON.stringify(result));
        PlayFab.GetPhotonAuthenticationToken(OnPhotonTokenFetched);
    }

    function OnPhotonTokenFetched(error: any, response: PlayFabClientModels.GetPhotonAuthenticationTokenResult) {
        if (error) {
            console.log("Error fetching photon token:"+error);
            return;
        }

        photonToken = response.PhotonCustomAuthenticationToken;
        console.log("Photon token fetched: " + photonToken);
        lbc.setCustomAuthentication("token="+photonToken+"&username="+serverUserDisplayName);
        lbc.setUserId(serverUserDisplayName);
        lbc.connect({
            keepMasterConnection: true,
            lobbyStats: true
        });
    }
}

export function GetMatchRooms(): Photon.LoadBalancing.RoomInfo[] {
    return matchRoomList;
}

export function SubscribeToMatchRoomUpdates(callback: Interfaces.RoomListUpdateCallback) {
    matchRoomUpdateListeners.push(callback);
}
