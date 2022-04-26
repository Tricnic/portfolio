import * as Interfaces from './Interfaces'
import {default as Player} from './Player';
import * as PlayFab from './Playfab';
import * as Matchmaker from './Matchmaker';
import * as Chat from './Chat';
import {default as Config} from './Config';

let useChat: boolean = Config.useChat;
let useMatchmaking: boolean = Config.useMatchmaking;

interface ConnectionMap {[key: string]: Connection}
let connections: ConnectionMap = {};
export function GetConnectionByPlayerId(playerId: string): Connection {
    return connections[playerId];
}

export default class Connection {
    static GetConnectionByPlayerId(playerId: string) {
        return GetConnectionByPlayerId(playerId);
    }

    public name: string;
    public socket: Interfaces.Socket;
    public Player: Player;

    constructor(socket: Interfaces.Socket) {
        this.name = "Connection!";
        this.socket = socket;
        this.Player = new Player(this.socket);
        this.Player.rooms = [];
        this.socket.emit('OnConnect', {TitleId: Config.playFab.titleId});
        this.socket.on('disconnect', this.Disconnect.bind(this));
        this.socket.on('LoginSession', this.LoginSession.bind(this));

        if (useChat) {
            Chat.SetupConnectionHooks(this);
        }

        if (useMatchmaking) {
            Matchmaker.SetupConnectionHooks(this);
        }
    }

    Disconnect() {
        this.socket.disconnect(true);
        if (useChat) {
            Chat.OnDisconnect(this);
        }
        if (useMatchmaking) {
            Matchmaker.OnDisconnect(this);
        }
        if (connections[this.Player.PlayerId] === this) {
            delete connections[this.Player.PlayerId];
        }
    }

    LoginSession(data: Interfaces.LoginSessionRequest) {
        if (!data || !data.SessionTicket) {
            console.log("Error logging user in. No session ticket supplied");
            return;
        }

        let ticket = data.SessionTicket;
        PlayFab.LoginSession(ticket, this.OnPlayFabLoginSessionComplete.bind(this));
    }

    OnPlayFabLoginSessionComplete(error, result: Interfaces.PlayFabAuthenticateResult) {
        if (result) {
            this.Player.SetInfo(result);
            this.socket.emit('LoggedIn', this.Player.ToCrumb());
            this.socket.isReady = true;
            connections[this.Player.PlayerId] = this;
            console.log("PlayFab login complete: ", this.Player.ToCrumb());
        }
        else if (error) {
            console.log("Error logging into PlayFab:", error);
        }
        else {
            console.log("Unknown error logging into PlayFab");
        }
    }
}
