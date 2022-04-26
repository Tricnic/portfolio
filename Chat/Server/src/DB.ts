import {Client} from './MongoDB';
import {UpdateCallback, ReadCallback, DBRecord} from './Interfaces'

let client = new Client();

export function Read(id: string, onComplete: ReadCallback) {
    client.Read(id, onComplete);
}

export function Update(record: DBRecord, onComplete: UpdateCallback) {
    client.Update(record, onComplete);
}
