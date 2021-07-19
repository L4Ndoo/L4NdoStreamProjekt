"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __generator = (this && this.__generator) || function (thisArg, body) {
    var _ = { label: 0, sent: function() { if (t[0] & 1) throw t[1]; return t[1]; }, trys: [], ops: [] }, f, y, t, g;
    return g = { next: verb(0), "throw": verb(1), "return": verb(2) }, typeof Symbol === "function" && (g[Symbol.iterator] = function() { return this; }), g;
    function verb(n) { return function (v) { return step([n, v]); }; }
    function step(op) {
        if (f) throw new TypeError("Generator is already executing.");
        while (_) try {
            if (f = 1, y && (t = op[0] & 2 ? y["return"] : op[0] ? y["throw"] || ((t = y["return"]) && t.call(y), 0) : y.next) && !(t = t.call(y, op[1])).done) return t;
            if (y = 0, t) op = [op[0] & 2, t.value];
            switch (op[0]) {
                case 0: case 1: t = op; break;
                case 4: _.label++; return { value: op[1], done: false };
                case 5: _.label++; y = op[1]; op = [0]; continue;
                case 7: op = _.ops.pop(); _.trys.pop(); continue;
                default:
                    if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) { _ = 0; continue; }
                    if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) { _.label = op[1]; break; }
                    if (op[0] === 6 && _.label < t[1]) { _.label = t[1]; t = op; break; }
                    if (t && _.label < t[2]) { _.label = t[2]; _.ops.push(op); break; }
                    if (t[2]) _.ops.pop();
                    _.trys.pop(); continue;
            }
            op = body.call(thisArg, _);
        } catch (e) { op = [6, e]; y = 0; } finally { f = t = 0; }
        if (op[0] & 5) throw op[1]; return { value: op[0] ? op[1] : void 0, done: true };
    }
};
Object.defineProperty(exports, "__esModule", { value: true });
require("./css/main.css");
var signalR = require("@microsoft/signalr");
console.log("v0");
var video = document.getElementById('video');
var button = document.getElementById('button');
video.addEventListener('loadedmetadata', function () {
    console.log("Remote video videoWidth: " + this.videoWidth + "px,  videoHeight: " + this.videoHeight + "px");
});
video.onresize = function () {
    console.log("Remote video size changed to " + video.videoWidth + "x" + video.videoHeight);
    console.warn('RESIZE', video.videoWidth, video.videoHeight);
};
var signalrConnection = new signalR.HubConnectionBuilder()
    .withUrl("/livestream")
    .build();
var webrtcConnection = new RTCPeerConnection({
    iceServers: [
        {
            urls: ['stun:stun.l.google.com:19302']
        }
    ],
});
webrtcConnection.addEventListener("icecandidate", function (event) { return __awaiter(void 0, void 0, void 0, function () {
    var candidate;
    return __generator(this, function (_a) {
        switch (_a.label) {
            case 0:
                candidate = event.candidate.toJSON();
                return [4 /*yield*/, signalrConnection.send("iceReceived", candidate.candidate, candidate.sdpMid, candidate.sdpMLineIndex)];
            case 1:
                _a.sent();
                console.log("ICE sent: ", candidate);
                return [2 /*return*/];
        }
    });
}); });
webrtcConnection.addEventListener("iceconnectionstatechange", function (event) { return console.log("Ice-Connection:", event); });
webrtcConnection.addEventListener("icegatheringstatechange", function (event) { return console.log("Ice-Gathering:", event); });
webrtcConnection.addEventListener("datachannel", function (event) { return console.log("Datachannel:", event); });
webrtcConnection.addEventListener("connectionstatechange", function (event) { return console.log("Connection:", event); });
webrtcConnection.addEventListener("icecandidateerror", function (event) { return console.log("Candidate-Error:", event); });
webrtcConnection.addEventListener("negotiationneeded", function (event) { return console.log("Negotiation:", event); });
webrtcConnection.addEventListener("signalingstatechange", function (event) { return console.log("Signaling:", event); });
webrtcConnection.addEventListener("track", function (event) {
    console.log("TRACK FOUND: ", event);
    video.srcObject = null;
    video.srcObject = new MediaStream([event.track]);
});
signalrConnection.on("sdpReceived", function (sdp, type) { return __awaiter(void 0, void 0, void 0, function () {
    var message, answer;
    return __generator(this, function (_a) {
        switch (_a.label) {
            case 0:
                message = { sdp: sdp, type: type };
                console.log("SDP received: ", message);
                return [4 /*yield*/, webrtcConnection.setRemoteDescription(message)];
            case 1:
                _a.sent();
                if (!(message.type == "offer")) return [3 /*break*/, 4];
                return [4 /*yield*/, webrtcConnection.createAnswer()];
            case 2:
                answer = _a.sent();
                return [4 /*yield*/, signalrConnection.send("sdpReceived", answer.sdp, answer.type)];
            case 3:
                _a.sent();
                console.log("SDP sent: ", answer);
                _a.label = 4;
            case 4: return [2 /*return*/];
        }
    });
}); });
signalrConnection.on("iceReceived", function (candidate, sdpMid, sdpMLineIndex) { return __awaiter(void 0, void 0, void 0, function () {
    var iceCandidate;
    return __generator(this, function (_a) {
        switch (_a.label) {
            case 0:
                iceCandidate = { candidate: candidate, sdpMid: sdpMid, sdpMLineIndex: sdpMLineIndex };
                console.log("ICE received: ", iceCandidate);
                return [4 /*yield*/, webrtcConnection.addIceCandidate(iceCandidate)];
            case 1:
                _a.sent();
                return [2 /*return*/];
        }
    });
}); });
signalrConnection.start()
    .catch(function (err) { return document.write(err); })
    .then(function () { return __awaiter(void 0, void 0, void 0, function () {
    return __generator(this, function (_a) {
        switch (_a.label) {
            case 0: return [4 /*yield*/, signalrConnection.send("RequestWebRtcConnection")];
            case 1:
                _a.sent();
                console.log("Connection requested.");
                button.addEventListener('click', function () { return __awaiter(void 0, void 0, void 0, function () {
                    return __generator(this, function (_a) {
                        switch (_a.label) {
                            case 0: return [4 /*yield*/, signalrConnection.send("RequestLivestream")];
                            case 1:
                                _a.sent();
                                console.log("Livestream requested.");
                                return [2 /*return*/];
                        }
                    });
                }); });
                button.disabled = false;
                return [2 /*return*/];
        }
    });
}); });
