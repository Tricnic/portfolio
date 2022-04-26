import * as Interfaces from './Interfaces'
import {default as Connection} from './Connection';

export function socketHandler (socket: Interfaces.Socket) {
    socket.isReady = false;
    let connection = new Connection(socket);
    let oldConnection = Connection.GetConnectionByPlayerId(connection.Player.PlayerId);
    if (oldConnection) {
        oldConnection.socket.emit('OnError', 'disconnect');
        oldConnection.Disconnect();
    }
}
