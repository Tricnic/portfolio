import * as Interfaces from './Interfaces';
import * as Room from './Room';
import {default as Connection} from './Connection';
import * as Ninja from './Ninja';
import Player from './Player';
import {default as Config} from './Config';

export function SetupConnectionHooks(connection: Connection) {
    connection.socket.on('J', OnJoinRoom.bind(connection));
    connection.socket.on('L', OnLeaveRoom.bind(connection));
    connection.socket.on('M', OnPublicMessage.bind(connection));
    connection.socket.on('PM', OnPrivateMessage.bind(connection));
    connection.socket.on('U', OnUpdateStatus.bind(connection));
}

export function OnDisconnect(connection: Connection) {
    if (connection.Player.rooms)
        connection.Player.rooms.forEach(room => OnLeaveRoom.call(connection, {r: room.Id}));
}

// 'this' refers to an instance of Connection
function OnJoinRoom(request: Interfaces.JoinRoomRequest) {
    let roomId = request.r;
    if (Room.IsValidRoom(roomId) && Room.RoomExists(roomId)) {
        Room.GetRoom(roomId)
            .AddPlayer(this.Player);
    }
    else {
        this.Player.socket.emit('OnJoinedRoom', {error: 'Unknown room '+roomId+'. Are you lost?'});
    }
}

// 'this' refers to an instance of Connection
function OnLeaveRoom(request: Interfaces.LeaveRoomRequest) {
    let roomId = request.r;
    if (Room.IsValidRoom(roomId) && Room.RoomExists(roomId)) {
        Room.GetRoom(roomId)
            .RemovePlayer(this.Player);
    }
    else {
        this.Player.socket.emit('OnLeftRoom', {error: 'Unknown room '+roomId+'. Are you lost?'});
    }
}

// 'this' refers to an instance of Connection
function OnPublicMessage(message: Interfaces.PublicMessageRequest) {
    console.log("Received message for room", message.r, "from", this.Player.Nickname,":", message);
    let roomId = message.r;
    if (Room.RoomExists(roomId)) {
        Room.GetRoom(roomId)
            .BroadcastMessage(this.Player, message);
    }
    else {
        this.Player.socket.emit('M', {error: 'Unknown room '+roomId+'. Are you lost?'});
    }
}

// 'this' refers to an instance of Connection
function OnPrivateMessage(message: Interfaces.PrivateMessageRequest) {
    console.log("Received private message from", this.Player.Nickname,":", message);
    let otherConn = Connection.GetConnectionByPlayerId(message.i);
    if (otherConn) {
        let crumb: Interfaces.PrivateMessageCrumb = {
            i: this.Player.PlayerId,
            m: message.m,
            sn: otherConn.Player.Nickname
        };
    
        if (!Config.useNinja) {
            EmitPrivateMessage(this.Player, otherConn, crumb);
        }
        else {
            let info = {
                room: "private",
                text: message.m,
                player: this.Player.PlayerId,
                player_display_name: this.Player.Nickname
            };
            Ninja.MakeRequest(info, result => {
                if (result.response) {
                    EmitPrivateMessage(this.Player, otherConn, crumb);
                }
            });
        }
        }
    else {
        this.Player.socket.emit('M', {error: 'Cannot find that player. Are you lost?'});
        console.log("Tried to send private message to player ", message.i, " but that player isn't connected");
        return;
    }
}

function OnUpdateStatus(request: Interfaces.UpdateStatusRequest) {
    this.Player.SetStatus(request.s);
}

function EmitPrivateMessage(player: Player, otherConn: Connection, crumb: Interfaces.PrivateMessageCrumb) {
    player.socket.emit('PM', crumb);
    crumb.sn = player.Nickname;
    console.log("Sending message from", player.PlayerId, "to", otherConn.Player.PlayerId, "(socket id:", otherConn.socket.id, "):", crumb);
    player.socket.to(otherConn.socket.id).emit('PM', crumb);
}
