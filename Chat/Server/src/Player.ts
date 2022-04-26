import * as Interfaces from './Interfaces';
import {Room} from './Room';

interface Position {
    x: number,
    y: number
}

interface StatusTypes {
    inLobby: number,
    inMatchmaking: number,
    inMatch: number
}

export default class Player {
    constructor(socket: Interfaces.Socket) {
        this.socket = socket;
        this.Position = {x: 0, y: 0};
    }

    public socket: Interfaces.Socket;
    public PlayerId: string;
    public Nickname: string;
    public isAdmin: boolean;
    public Position: Position;
    public rooms : Room[];
    public Status : number;

    SetInfo(result: Interfaces.PlayFabAuthenticateResult) {
        this.PlayerId = result.PlayerId;
        this.Nickname = result.Nickname;
        this.isAdmin = result.isAdmin;
    }

    SetStatus(status: number) {
        console.log("Updating player", this.Nickname, "status to", status);
        this.Status = status;
        this.rooms.forEach(r => {
            r.BroadcastPlayerUpdate(this);
        });
    }

    SetPosition(x: number, y: number) {
        this.Position.x = x;
        this.Position.y = y;
    }

    RemoveRoomFromList(room: Room) {
        let index = this.rooms.indexOf(room);
        if (index > -1) {
            this.rooms.splice(index, 1);
        }
    }

    ToCrumb() : Interfaces.PlayerCrumb {
        return {
            i: this.PlayerId,
            n: this.Nickname,
            a: this.isAdmin,
            s: this.Status
        };
    }
}
