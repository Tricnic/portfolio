import * as PlayFab from 'playfab-sdk';
import {PlayFabAuthenticateCallback, PlayFabLoginCallback, PlayFabAuthenticateResult, GetPhotonAuthenticationTokenCallback} from './Interfaces';

let PlayFabServer = PlayFab.PlayFabServer;
let PlayFabClient = PlayFab.PlayFabClient;

PlayFabServer.settings.titleId = process.env.PLAYFAB_TITLEID;
PlayFabServer.settings.developerSecretKey = process.env.PLAYFAB_SECRET;
PlayFabClient.settings.titleId = process.env.PLAYFAB_TITLEID;
let photonAppId = process.env.PHOTON_APP_ID;

PlayFabServer.GetTitleData({
    Keys: ["Test"]
}, function (error, result) {
    if (error) {
        console.log("ERROR CONNECTING TO PLAYFAB: " + error);
        return;
    }
    console.log('CONNECTED TO PLAYFAB:', PlayFabServer.settings.titleId);
});

export function LoginSession(ticket: string, callback: PlayFabAuthenticateCallback) {
    if (ticket) {
        AuthenticateSessionTicket(ticket, callback);
    }
}

function AuthenticateSessionTicket(ticket: string, callback: PlayFabAuthenticateCallback) {
    var request = {
        SessionTicket: ticket
    };
    PlayFabServer.AuthenticateSessionTicket(request, function (error, result) {
        if (error) {
            console.log(error);
            return;
        }
        var player: PlayFabAuthenticateResult = {
            PlayerId: result.data.UserInfo.PlayFabId,
            Nickname: result.data.UserInfo.TitleInfo.DisplayName,
            isAdmin: false
        };
        GetUserReadOnlyData(player, callback);
    });
}

function GetUserReadOnlyData(player: PlayFabAuthenticateResult, callback: PlayFabAuthenticateCallback) {
    var request = {
        PlayFabId: player.PlayerId
    };
    PlayFabServer.GetUserReadOnlyData(request, function (error, result) {
        if (error) {
            console.log(error);
            return;
        }
        if (result.data.hasOwnProperty('Data')) {
            var data = result.data.Data;
            if (data.hasOwnProperty('isAdmin')) {
                if (data.isAdmin.Value == 'true') {
                    player.isAdmin = true;
                }
            }
        }
        callback(null, player);
    });
}

export function LoginWithEmailAddress(email: string, password: string, callback: PlayFabLoginCallback) : void {
    let request = {
        TitleId: PlayFabServer.settings.titleId,
        Email: email,
        Password: password,
        InfoRequestParameters: {
            GetPlayerProfile: true,
            GetCharacterInventories: false,
            GetCharacterList: false,
            GetPlayerStatistics: false,
            GetTitleData: false,
            GetUserAccountInfo: false,
            GetUserData: false,
            GetUserInventory: false,
            GetUserReadOnlyData: false,
            GetUserVirtualCurrency: false
        }
    };

    console.log("Sending LoginWithEmailAddress request: " + JSON.stringify(request));
    PlayFabClient.LoginWithEmailAddress(request, function (error, response) {
        // console.log("LoginWithEmailAddress response: ", JSON.stringify(response.data, null, 4));
        if (error) {
            callback(error, null);
            return;
        }
        callback(error, response ? response.data : null);
    });
}

export function GetPhotonAuthenticationToken(callback : GetPhotonAuthenticationTokenCallback) : void {
    let request = { PhotonApplicationId: photonAppId };

    PlayFabClient.GetPhotonAuthenticationToken(request, function (error, response) {
        // console.log("GetPhotonAuthenticationToken error: " + JSON.stringify(error));
        // console.log("GetPhotonAuthenticationToken response: " + JSON.stringify(response.data));
        if (error) {
            callback(error, null);
            return;
        }
        callback(error, response ? response.data : null);
    });
}
