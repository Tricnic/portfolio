import * as Interfaces from './Interfaces';
import {default as Player} from './Player';
import * as Ninja from './Ninja';

let useNinja : boolean = !process.env.USE_NINJA || process.env.USE_NINJA !== "0";

export class Room {
    constructor(roomId: string) {
        this.Id = roomId;
        this.PlayerList = [];
    }

    public Id: string;
    public PlayerList: Player[];

    AddPlayer(player: Player) {
        if (this.PlayerIsInRoom(player)) {
            return;
        }

        if (!IsValidRoom(this.Id) || !RoomExists(this.Id)) {
            player.socket.emit('OnJoinedRoom', {error: 'Unknown room '+this.Id+'. Are you lost?'});
            return;
        }

        if (this.PlayerIsInRoom(player)) {
            player.socket.emit('OnJoinedRoom', {error: 'Cannot join room '+this.Id+'. You are already in this room'});
            return;
        }

        console.log(player.PlayerId, " joining room ", this.Id);
        player.socket.join(this.Id, () => {
            this.PlayerList.push(player);
            player.rooms.push(this);
            player.socket.emit('OnJoinedRoom', this.ToCrumb());
            let crumb : Interfaces.PlayerJoinedRoom = {
                r: this.Id,
                p: player.ToCrumb()
            };
            player.socket.to(this.Id).emit('A', crumb);
            console.log("Player(#", player.PlayerId, ")", player.Nickname, "has entered", this.Id, "room");
        });
    }

    RemovePlayer(player: Player) {
        if (this.PlayerIsInRoom(player)) {
            player.RemoveRoomFromList(this);
            let crumb: Interfaces.PlayerLeftRoom = {
                i: player.PlayerId,
                r: this.Id
            }
            player.socket.to(this.Id).emit('R', crumb);
            player.socket.leave(this.Id, () => {
                player.socket.emit('OnLeftRoom', {r: this.Id});
                console.log("Player", player.Nickname, "is now in rooms:", player.socket.rooms);
            });
        }

        let index = this.PlayerList.indexOf(player);
        if (index > -1) {
            this.PlayerList.splice(index, 1);
            console.log("Player(#", player.PlayerId, ")", player.Nickname, "has left", this.Id, "room");
            if (this.PlayerList.length == 0 && IsGroupRoom(this.Id)) {
                delete roomList.groupRooms[this.Id];
            }
        }
    }

    PlayerIsInRoom(player: Player) {
        return this.PlayerList.indexOf(player) > -1;
    }

    BroadcastPlayerUpdate(player: Player) {
        let crumb : Interfaces.PlayerUpdated = {
            r: this.Id,
            p : player.ToCrumb()
        };
        player.socket.emit('U', crumb);
        player.socket.to(this.Id).emit('U', crumb);
    }

    BroadcastMessage(player: Player, message: Interfaces.PublicMessageRequest) {
        if (!this.PlayerIsInRoom(player)) {
            player.socket.emit('M', {error: 'Unknown room '+this.Id+'. Are you lost?'});
            console.log("Player(#", player.PlayerId, ")", player.Nickname, "tried to send message to", this.Id, "but s/he is not in the room");
            return;
        }
        
        let crumb: Interfaces.PublicMessageCrumb = {
            i: player.PlayerId,
            m: message.m,
            r: message.r,
            a: player.isAdmin
        };

        if (!useNinja) {
            this.EmitPublicMessage(player, this.Id, crumb);
        }
        else {
            let info = {
                room: this.Id,
                text: message.m,
                player: player.PlayerId,
                player_display_name: player.Nickname
            };
            Ninja.MakeRequest(info, result => {
                console.log("sift result: ", result);
                if (result.response) {
                    this.EmitPublicMessage(player, this.Id, crumb);
                }
                else {
                    console.log("Ninja! ", message, result);
                }
            });
        }
    }

    EmitPublicMessage(player: Player, roomId: string, crumb: Interfaces.PublicMessageCrumb) {
        player.socket.emit('M', crumb);
        player.socket.to(roomId).emit('M', crumb)
    }

    ToCrumb() : Interfaces.RoomCrumb {
        let crumb = {
            r: this.Id,
            p: []
        };

        this.PlayerList.forEach(player => crumb.p.push(player.ToCrumb()));

        return crumb;
    }
}

let roomList = {
    general: new Room('general'),
    groupRooms: {}
};

function IsGroupRoom(roomId: string): boolean {
    return roomList.groupRooms.hasOwnProperty(roomId) && roomList.groupRooms[roomId].constructor == Room;
}

function IsSystemRoom(roomId: string): boolean {
    return roomList.hasOwnProperty(roomId) && roomList[roomId].constructor === Room;
}

export function IsValidRoom(roomId: string): boolean {
    return true; //TODO: Flush this out
}

export function RoomExists(roomId: string): boolean {
    return IsSystemRoom(roomId) || IsGroupRoom(roomId);
}

export function GetRoom(roomId: string) : Room {
    let room: Room = roomList[roomId];
    if (!room) {
        room = roomList.groupRooms[roomId];
    }
    return room;
}

export function AddGroupRoom(roomId: string) {
    roomList.groupRooms[roomId] = new Room(roomId);
}

export default roomList;