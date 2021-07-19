import "./css/main.css";
import * as signalR from "@microsoft/signalr";

console.log("v0");
const video: any = document.getElementById('video');
const button: any = document.getElementById('button');

video.addEventListener('loadedmetadata', function () {
    console.log(`Remote video videoWidth: ${this.videoWidth}px,  videoHeight: ${this.videoHeight}px`);
});

video.onresize = () => {
    console.log(`Remote video size changed to ${video.videoWidth}x${video.videoHeight}`);
    console.warn('RESIZE', video.videoWidth, video.videoHeight);
};

const signalrConnection = new signalR.HubConnectionBuilder()
    .withUrl("/livestream")
    .build();

const webrtcConnection = new RTCPeerConnection({
    iceServers: [
        {
            urls: ['stun:stun.l.google.com:19302']
        }
    ],
});

webrtcConnection.addEventListener("icecandidate", async event => {
    let candidate = event.candidate.toJSON();
    await signalrConnection.send("iceReceived", candidate.candidate, candidate.sdpMid, candidate.sdpMLineIndex);
    console.log("ICE sent: ", candidate);
});

webrtcConnection.addEventListener("iceconnectionstatechange", event => console.log("Ice-Connection:", event));
webrtcConnection.addEventListener("icegatheringstatechange", event => console.log("Ice-Gathering:", event));
webrtcConnection.addEventListener("datachannel", event => console.log("Datachannel:", event));
webrtcConnection.addEventListener("connectionstatechange", event => console.log("Connection:", event));
webrtcConnection.addEventListener("icecandidateerror", event => console.log("Candidate-Error:", event));
webrtcConnection.addEventListener("negotiationneeded", event => console.log("Negotiation:", event));
webrtcConnection.addEventListener("signalingstatechange", event => console.log("Signaling:", event));


webrtcConnection.addEventListener("track", event => {
    console.log("TRACK FOUND: ", event);

    video.srcObject = null;
    video.srcObject = new MediaStream([event.track]);
});

signalrConnection.on("sdpReceived", async (sdp, type) => {
    let message: RTCSessionDescriptionInit = { sdp, type };
    console.log("SDP received: ", message);

    await webrtcConnection.setRemoteDescription(message);
    if (message.type == "offer") {
        let answer = await webrtcConnection.createAnswer();

        await signalrConnection.send("sdpReceived", answer.sdp, answer.type);
        console.log("SDP sent: ", answer);
    }
});

signalrConnection.on("iceReceived", async (candidate, sdpMid, sdpMLineIndex) => {
    let iceCandidate: RTCIceCandidateInit = { candidate, sdpMid, sdpMLineIndex };
    console.log("ICE received: ", iceCandidate);
    await webrtcConnection.addIceCandidate(iceCandidate);
});

signalrConnection.start()
    .catch(err => document.write(err))
    .then(async () => {
        await signalrConnection.send("RequestWebRtcConnection");
        console.log("Connection requested.");

        button.addEventListener('click', async () => {
            await signalrConnection.send("RequestLivestream");
            console.log("Livestream requested.");
        });
        button.disabled = false;
    });