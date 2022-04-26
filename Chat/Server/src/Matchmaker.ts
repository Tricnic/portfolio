import * as Interfaces from './Interfaces'
import {default as Player} from './Player';
import { v4 as UUID} from 'uuid';
import * as loadBalancer from './LoadbalancingClient';
import {default as Connection} from './Connection';

let logPrefix = "[Matchmaker] ";
function log(message: string) {
    console.log(logPrefix + message);
}

let matchmakingQueue: Player[] = [];
let waitingForRoomlistUpdate: boolean = false;
let lastNewRoomName: string;
let playersAddedToNewRoomSoFar: number = 0;

export function SetupConnectionHooks(connection: Connection) {
    connection.socket.on('EM', OnEnterMatchmaking.bind(connection));
    connection.socket.on('LM', OnLeaveMatchmaking.bind(connection));
}

export function OnDisconnect(connection: Connection) {
    matchmaker.LeaveMatchmaking(connection.Player, {});
}

// 'this' refers to an instance of Connection
function OnEnterMatchmaking(request: Interfaces.JoinMatchmakingRequest) {
    matchmaker.EnterMatchmaking(this.Player, request);
}

// 'this' refers to an instance of Connection
function OnLeaveMatchmaking(request: Interfaces.LeaveMatchmakingRequest) {
    matchmaker.LeaveMatchmaking(this.Player, request);
}

class Matchmaker {
    constructor() {
        loadBalancer.SubscribeToMatchRoomUpdates(OnRoomListUpdated);
    }

    EnterMatchmaking(player: Player, request: Interfaces.JoinMatchmakingRequest): void {
        log('Player ' + player.Nickname + ' joined matchmaking');
        if (matchmakingQueue.indexOf(player) < 0) {
            if (request.r === true) {
                matchmakingQueue.unshift(player);
            }
            else {
                matchmakingQueue.push(player);
            }
            this.MakeMatch();
        }
    }

    LeaveMatchmaking(player: Player, request: Interfaces.LeaveMatchmakingRequest) {
        log('Player ' + player.Nickname + ' left matchmaking');
        let index = matchmakingQueue.indexOf(player);
        if (index > -1) {
            matchmakingQueue.splice(index, 1);
        }
        this.MakeMatch();
    }

    MakeMatch(): void {
        log('Running matchmaking for ' + matchmakingQueue.length + ' players');
        if (matchmakingQueue.length == 0) {
            return;
        }

        FindOpenRoom(loadBalancer.GetMatchRooms());
    }
}

function OnRoomListUpdated(rooms: Photon.LoadBalancing.RoomInfo[]) {
    waitingForRoomlistUpdate = false;
    lastNewRoomName = "";
    playersAddedToNewRoomSoFar = 0;
    FindOpenRoom(rooms);
}

function FindOpenRoom(rooms: Photon.LoadBalancing.RoomInfo[]) {
    MatchExisting(rooms);
    MatchNew();
}

function MatchExisting(rooms: Photon.LoadBalancing.RoomInfo[]) {
    log('Trying to match ' + matchmakingQueue.length + ' players into ' + rooms.length + ' rooms');
    rooms.forEach(room => {
        let openSlots = room.maxPlayers - room.playerCount;
        if (openSlots > 0) {
            let count: number = 0;
            let maxPlayers = openSlots < matchmakingQueue.length ? openSlots : matchmakingQueue.length;
            for (let i = 0; i < maxPlayers; i++) {
                EmitJoinMatchResponse(matchmakingQueue[i], {r: room.name, c: false, s: true});
                count++;
            }
            log('Matched ' + count + ' players into existing room ' + room.name);
            matchmakingQueue.splice(0, count);
        }
    });
}

function MatchNew() {
    if (matchmakingQueue.length > 1 || (matchmakingQueue.length > 0 && waitingForRoomlistUpdate && playersAddedToNewRoomSoFar < 8)) {
        let matchName: string;
        let openSlots: number = 8;
        if (waitingForRoomlistUpdate && lastNewRoomName && playersAddedToNewRoomSoFar < 8) {
            log(playersAddedToNewRoomSoFar+' already in room '+lastNewRoomName);
            matchName = lastNewRoomName;
            openSlots = 8 - playersAddedToNewRoomSoFar;
        }
        else {
            matchName = 'Game' + Math.floor(Math.random() * 900 + 100) + UUID();
            lastNewRoomName = "";
        }
        let count = matchmakingQueue.length < openSlots ? matchmakingQueue.length : openSlots;

        for (let i = 0; i < count; i++) {
            EmitJoinMatchResponse(matchmakingQueue[i], {r: matchName, c: true, s: true});
        }
        matchmakingQueue.splice(0, count);
        log('Matched ' + count + ' players into new room ' + matchName);
        playersAddedToNewRoomSoFar += count;

        if (playersAddedToNewRoomSoFar < 8) {
            log((8 - playersAddedToNewRoomSoFar)+' slots are open in room '+matchName);
            waitingForRoomlistUpdate = true;
            lastNewRoomName = matchName;
        }
        else {
            log('No slots left in room '+matchName);
            waitingForRoomlistUpdate = false;
            lastNewRoomName = "";
            playersAddedToNewRoomSoFar = 0;
        }

        MatchNew();
    }
}

function EmitJoinMatchResponse(player: Player, response: Interfaces.JoinMatchResponse) {
    player.socket.emit('JM', response);
}

let matchmaker = new Matchmaker();
export default matchmaker;
