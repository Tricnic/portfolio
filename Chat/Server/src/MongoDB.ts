import {UpdateCallback, ReadCallback, DBRecord, AppConfig, AdminAlertsConfig} from './Interfaces'
import {default as Config} from './Config';

const MongoClient = require('mongodb').MongoClient;
const host = Config.adminAlerts.dbURI;
const dbname = Config.adminAlerts.dbName;

export class Client {
    constructor() {
        MongoClient.connect(host, { useNewUrlParser: true }, (err, client) => {
            if (err !== null) {
                console.log("MongoDB connection error: " + err);
            } else {
                this.connection = client.db(dbname);
                this.collection = this.connection.collection('epicsnails');
                this.IsConnected = true;
                console.log("MongoDB connection established");
            }
        });
    }

    private connection;
    private collection;
    
    public IsConnected: boolean = false;

    Read(id: string, onComplete: ReadCallback) {
        this.collection.find({id: id}).toArray(function (err, results) {
            let doc: DBRecord = {};
            if (err) {
                console.log("MongoDB read error: " + err)
            } else {
                doc = results[0];
            }
            onComplete(err, doc);
        });
    }

    Update(doc: DBRecord, onComplete: UpdateCallback) {
        this.collection.findOneAndReplace({id: doc.id}, doc, {}, function (err, result) {
            onComplete(err, doc);
        });
    }
}
