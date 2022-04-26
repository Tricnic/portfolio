import {AppConfig, CommunitySiftConfig} from './Interfaces';
import {default as Config} from './Config';

/** BEGIN CUSTOMIZATION **/

// Your account ID.
// If your Sift Ninja URL is "https://my-cool-site.siftninja.com", then your account ID is "my-cool-site";
let ACCOUNT_ID = Config.communitySift.accountId;

// Your account's API key. 
// Click on your name/email address in the upper-right corner of Sift Ninja to see it.
let API_KEY = Config.communitySift.secret;

// Your Sift Ninja channel name as it appears in the Sift Ninja navigation bar
let CHANNEL_NAME = Config.communitySift.channel;

/** END CUSTOMIZATION **/

// Import the HTTPS library
let https = require("https");

// Authorization is done through the "Authorization" HTTP header
// The value is in the form of ACCOUNT_ID:API_KEY, converted to Base64.
let authKey = new Buffer(ACCOUNT_ID + ":" + API_KEY).toString("base64");

// Define the HTTPS request options, including method, hostname, path, and headers.
let options = {
    "method": "POST",
    "hostname": ACCOUNT_ID + ".siftninja.com",
    "path": "/api/v1/channel/" + CHANNEL_NAME + "/sifted_data",
    "headers": {
        "authorization": "Basic " + authKey,
        "content-type": "application/json"
    }
};

export function MakeRequest(request, callback) {
    if (request == null) {
        request = {};
    }
    var requestBody = JSON.stringify(request);

    var postReq = https.request(options, function (res) {

        // Array to buffer the data we'll be getting back.
        var chunks = [];

        // When we receive some data, push it to a chunks array.
        // When we've received all of the data (signifed by the "end" event below)
        // we can process it however we need to.
        res.on("data", function (chunk) {
            chunks.push(chunk);
        });

        // This indicates we've received the last of the data.
        // It is at this point you'd act upon the response data.
        res.on("end", function () {
            // No callback, stop processing
            if (callback == null) {
                return;
            }

            // Merge all of the chunks into a single string.
            var body = Buffer.concat(chunks);

            // Convert body a usable object.
            body = JSON.parse(body.toString());

            // Do something with "body"
            callback(body);

        });
    });

    postReq.write(requestBody);
    postReq.end();
}

// TEST IT!
MakeRequest({
    text: "This is a test message"
}, function (result) {
    if (result.response) {
        console.log('CONNECTED TO NINJA');
    }
});

