import {AppConfig} from './Interfaces';

let config: AppConfig = {
    useNinja: process.env.USE_NINJA === "true",
    useChat: process.env.USE_CHAT === "true",
    useMatchmaking: process.env.USE_MATCHMAKING === "true",
    playFab: {
        titleId: process.env.PLAYFAB_TITLEID,
        secret: process.env.PLAYFAB_SECRET,
        username: process.env.PLAYFAB_SERVER_USERNAME,
        password: process.env.PLAYFAB_SERVER_PASSWORD
    },
    communitySift: {
        accountId: process.env.NINJA_ACCOUNTID,
        channel: process.env.NINJA_CHANNEL,
        secret: process.env.NINJA_SECRET
    },
    adminAlerts: {
        password: process.env.ADMIN_PASSWORD,
        dbURI: process.env.MONGODB_URI,
        dbName: process.env.MONGODB_NAME
    }
};

export default config;
