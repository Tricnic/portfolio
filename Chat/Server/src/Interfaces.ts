// System interfaces
export interface AppConfig {
    useNinja: boolean,
    useChat: boolean,
    useMatchmaking: boolean,
    playFab?: PlayFabConfig,
    communitySift?: CommunitySiftConfig,
    adminAlerts?: AdminAlertsConfig,
    photon?: PhotonConfig
}

export interface PlayFabConfig {
    titleId: string,
    secret: string,
    username: string,
    password: string
}

export interface CommunitySiftConfig {
    accountId: string,
    channel: string,
    secret: string
}

export interface AdminAlertsConfig {
    password: string,
    dbURI: string,
    dbName: string
}

export interface PhotonConfig {
    email: string,
    password: string,
    appId: string,
    appVersion: string
}

export interface LoginSessionRequest {
    SessionTicket: string
}

export interface SocketOnMethod {
    (data: any): void
}

export interface SocketOnJoinMethod {
    (): void
}

export interface SocketOnLeaveMethod {
    (): void
}

export interface SocketBroadcast {
    to(roomId: string): Socket
}

export interface Socket {
    id: string,
    isReady: boolean,
    broadcast: SocketBroadcast,
    rooms: object,
    emit(index: string, msg: any): null,
    disconnect(something: boolean): null,
    on(index: string, callback: SocketOnMethod),
    join(roomId: string, callback: SocketOnJoinMethod),
    leave(roomId: string, callback: SocketOnLeaveMethod),
    to(roomId: string): Socket
}

export interface PlayFabAuthenticateResult {
    PlayerId: string,
    Nickname: string,
    isAdmin: boolean
}

export interface PlayFabAuthenticateCallback {
    (error: any, result: PlayFabAuthenticateResult): void
}

export interface PlayFabLoginCallback {
    (error: any, result: PlayFabClientModels.LoginResult): void
}

export interface GetPhotonAuthenticationTokenResult {
    PhotonCustomAuthenticationToken: string
}

export interface GetPhotonAuthenticationTokenCallback {
    (error: any, response: PlayFabClientModels.GetPhotonAuthenticationTokenResult): void
}
// end

// Player interfaces
export interface PlayerCrumb {
    i: string,  // The PlayerId of the player
    n: string,  // The Nickname of the player
    a: boolean, // Whether the player is an admin or not
    s: number   // The status of the player (lobby, matchmaking, match)
}
// end

// Room interfaces
export interface JoinRoomRequest {
    r: string   // The id of the room to join
}

export interface LeaveRoomRequest {
    r: string   // The id of the room to leave
}

export interface UpdateStatusRequest {
    s: number   // The new status (lobby, matchmaking, match)
}

export interface RoomCrumb {
    r: string,          // The id of the room joined
    p: PlayerCrumb[]    // The list of players currently in the room
}

export interface LeftRoom {
    r: string   // The id of the room left
}

export interface PlayerJoinedRoom {
    r: string,      // The id of the room joined
    p: PlayerCrumb  // The player that joined
}

export interface PlayerLeftRoom {
    r: string,  // The room left
    i: string   // The id of the player that left
}

export interface PlayerUpdated {
    r: string,     // the ID of the room the player is in
    p: PlayerCrumb // the new player crumb
}
// end

// Message interfaces
export interface PublicMessageRequest {
	r: string,  // The room to send the message to
    m: string,  // The message to send
    o: number   // The index of the orientation the message was sent from
}

export interface PrivateMessageRequest {
	i: string,  // The id of the Player to send the message to
	m: string   // The message
}

export interface PublicMessageCrumb {
	i: string,  // The id of the sender
	m: string,  // The message sent
	r: string,  // The room the message was sent to
    a: boolean  // Whether the sender is an admin
}

export interface PrivateMessageCrumb {
    i: string,  // The id of the player that sent the message
    m: string,  // The message sent
    sn: string  // The Nickname of the Player that sent the message
}
// end

// Database interfaces
export type UpdateCallback = (err: string, record: DBRecord) => any;
export type ReadCallback = (err: string, record: DBRecord) => any;
export interface DBRecord {
    id?: string
}
export interface DBClient {
    Read(id: string, onComplete: ReadCallback);
    Update(doc: DBRecord, onComplete: UpdateCallback);
}
// end

// Matchmaking interface
export interface JoinMatchmakingRequest {
    r: boolean
}

export interface JoinMatchResponse {
    r: string,      // The Photon room name to join
    c: boolean,     // True to have player create Photon room
    s: boolean,     // True if the player successfully joined matchmaking
    e?: string      // An error message if s is false
}

export interface LeaveMatchmakingRequest { }

export interface LeaveMatchmakingResponse {
    s: boolean,     // True if the player successfully left matchmaking
    e?: string      // An error message if s is false
}

export interface RoomListUpdateCallback {
    (rooms: Photon.LoadBalancing.RoomInfo[]): void
}
// end
