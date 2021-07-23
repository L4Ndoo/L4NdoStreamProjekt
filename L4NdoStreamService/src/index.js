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
document.getElementById("output").value = "v0.0.3";
function output(line) {
    var output = document.getElementById("output");
    output.value = new Date().toLocaleTimeString() + "\t" + line + "\n" + output.value;
}
var video = document.getElementById('video');
var signalrConnection = new signalR.HubConnectionBuilder()
    .withUrl("/livestream")
    .build();
var webrtcConnection = new RTCPeerConnection({ iceServers: [{ urls: ['stun:stun.l.google.com:19302'] }] });
// Setup handlers on webrtc connection
webrtcConnection.onicecandidate = function (event) { return __awaiter(void 0, void 0, void 0, function () {
    return __generator(this, function (_a) {
        switch (_a.label) {
            case 0:
                if (!event.candidate) return [3 /*break*/, 2];
                return [4 /*yield*/, signalrConnection.send("iceReceived", event.candidate.candidate, event.candidate.sdpMid, event.candidate.sdpMLineIndex)];
            case 1:
                _a.sent();
                output("ICE sent: " + event.candidate);
                _a.label = 2;
            case 2: return [2 /*return*/];
        }
    });
}); };
webrtcConnection.onnegotiationneeded = function (event) { return __awaiter(void 0, void 0, void 0, function () {
    var _a, _b;
    return __generator(this, function (_c) {
        switch (_c.label) {
            case 0:
                output("Renegotiation needed.");
                _b = (_a = webrtcConnection).setLocalDescription;
                return [4 /*yield*/, webrtcConnection.createOffer({ offerToReceiveVideo: true, offerToReceiveAudio: false })];
            case 1: return [4 /*yield*/, _b.apply(_a, [_c.sent()])];
            case 2:
                _c.sent();
                return [4 /*yield*/, signalrConnection.send("sdpReceived", webrtcConnection.localDescription.sdp, webrtcConnection.localDescription.type)];
            case 3:
                _c.sent();
                output("Renegotiation handled.");
                return [2 /*return*/];
        }
    });
}); };
webrtcConnection.ontrack = function (event) {
    output("Track added: " + event.track);
    event.track.onunmute = function (evt) {
        if (video.srcObject) {
            return;
        }
        var stream = new MediaStream();
        stream.addTrack(event.track);
        video.srcObject = stream;
    };
};
webrtcConnection.oniceconnectionstatechange = function (event) { return output("Ice-Connection: " + webrtcConnection.iceConnectionState); };
webrtcConnection.onicegatheringstatechange = function (event) { return output("Ice-Gathering: " + webrtcConnection.iceGatheringState); };
webrtcConnection.ondatachannel = function (event) { return output("Datachannel: " + event); };
webrtcConnection.onconnectionstatechange = function (event) { return output("Connection: " + webrtcConnection.connectionState); };
webrtcConnection.onicecandidateerror = function (event) { return output("Candidate-Error: " + event); };
webrtcConnection.onsignalingstatechange = function (event) { return output("Signaling: " + webrtcConnection.signalingState); };
// Setup handlers on signalr connection
signalrConnection.on("sdpReceived", function (sdp, type) { return __awaiter(void 0, void 0, void 0, function () {
    var message, answer, _a;
    return __generator(this, function (_b) {
        switch (_b.label) {
            case 0:
                message = { sdp: sdp, type: type };
                output("SDP received: " + message);
                return [4 /*yield*/, webrtcConnection.setRemoteDescription(message)];
            case 1:
                _b.sent();
                if (!(webrtcConnection.signalingState == "have-local-pranswer" || webrtcConnection.signalingState == "have-remote-offer")) return [3 /*break*/, 7];
                _b.label = 2;
            case 2:
                _b.trys.push([2, 6, , 7]);
                return [4 /*yield*/, webrtcConnection.createAnswer()];
            case 3:
                answer = _b.sent();
                return [4 /*yield*/, webrtcConnection.setLocalDescription(answer)];
            case 4:
                _b.sent();
                return [4 /*yield*/, signalrConnection.send("sdpReceived", webrtcConnection.localDescription.sdp, webrtcConnection.localDescription.type)];
            case 5:
                _b.sent();
                output("SDP sent: " + webrtcConnection.localDescription);
                return [3 /*break*/, 7];
            case 6:
                _a = _b.sent();
                output("Creating answer skipped.");
                return [3 /*break*/, 7];
            case 7: return [2 /*return*/];
        }
    });
}); });
signalrConnection.on("iceReceived", function (candidate, sdpMid, sdpMLineIndex) { return __awaiter(void 0, void 0, void 0, function () {
    var iceCandidate;
    return __generator(this, function (_a) {
        switch (_a.label) {
            case 0:
                if (!(candidate != null)) return [3 /*break*/, 2];
                iceCandidate = { candidate: candidate, sdpMid: sdpMid, sdpMLineIndex: sdpMLineIndex };
                return [4 /*yield*/, webrtcConnection.addIceCandidate(iceCandidate)];
            case 1:
                _a.sent();
                output("ICE received: " + iceCandidate);
                _a.label = 2;
            case 2: return [2 /*return*/];
        }
    });
}); });
// Connect signalR
signalrConnection.start()
    .catch(function (err) { return document.write(err); })
    .then(function () { return __awaiter(void 0, void 0, void 0, function () {
    return __generator(this, function (_a) {
        // Setup handlers for html interaction
        document.getElementById("connect").onclick = function () { return __awaiter(void 0, void 0, void 0, function () { return __generator(this, function (_a) {
            switch (_a.label) {
                case 0: return [4 /*yield*/, signalrConnection.send("requestConnection")];
                case 1: return [2 /*return*/, _a.sent()];
            }
        }); }); };
        document.getElementById("disconnect").onclick = function () { return __awaiter(void 0, void 0, void 0, function () { return __generator(this, function (_a) {
            switch (_a.label) {
                case 0: return [4 /*yield*/, signalrConnection.send("destroyConnection")];
                case 1: return [2 /*return*/, _a.sent()];
            }
        }); }); };
        document.getElementById("join").onclick = function () { return __awaiter(void 0, void 0, void 0, function () {
            var stream;
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0:
                        stream = document.getElementById("stream").value;
                        return [4 /*yield*/, signalrConnection.send("requestLivestream", stream)];
                    case 1:
                        _a.sent();
                        return [2 /*return*/];
                }
            });
        }); };
        document.getElementById("leave").onclick = function () { return __awaiter(void 0, void 0, void 0, function () {
            var stream;
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0:
                        stream = document.getElementById("stream").value;
                        return [4 /*yield*/, signalrConnection.send("removeLivestream", stream)];
                    case 1:
                        _a.sent();
                        return [2 /*return*/];
                }
            });
        }); };
        document.getElementById("resolution").onclick = function () { return __awaiter(void 0, void 0, void 0, function () {
            var stream, scale;
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0:
                        stream = document.getElementById("stream").value;
                        scale = document.getElementById("scale").value;
                        return [4 /*yield*/, signalrConnection.send("SetResolution", stream, scale)];
                    case 1:
                        _a.sent();
                        return [2 /*return*/];
                }
            });
        }); };
        video.ontimeupdate = function () {
            document.getElementById("info").innerText =
                "Resolution: " + video.videoWidth + "x" + video.videoHeight + "; Rate: " + video.playbackRate + "; Time: " + video.currentTime;
        };
        return [2 /*return*/];
    });
}); });
