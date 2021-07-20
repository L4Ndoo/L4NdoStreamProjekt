import "./css/main.css";
import * as signalR from "@microsoft/signalr";

console.log("v0.0.2");
const video: any = document.getElementById('video');

video.loadedmetadata = () => console.log(`Remote video videoWidth: ${video.videoWidth}px,  videoHeight: ${video.videoHeight}px`);;
video.onresize = () => console.log(`Remote video size changed to ${video.videoWidth}x${video.videoHeight}`);

const signalrConnection = new signalR.HubConnectionBuilder()
    .withUrl("/livestream")
    .build();

const webrtcConnection = new RTCPeerConnection({iceServers: [{ urls: ['stun:stun.l.google.com:19302']}]});

let iceRestart: boolean = false;

webrtcConnection.onicecandidate = async event => {
    if (event.candidate) {
        await signalrConnection.send("iceReceived", event.candidate.candidate, event.candidate.sdpMid, event.candidate.sdpMLineIndex);
        console.log("ICE sent: ", event.candidate);
    }
};
webrtcConnection.onnegotiationneeded = async event => {
    await webrtcConnection.setLocalDescription(await webrtcConnection.createOffer({ offerToReceiveVideo: true, offerToReceiveAudio: false, iceRestart: iceRestart }));
    await signalrConnection.send("sdpReceived", webrtcConnection.localDescription.sdp, webrtcConnection.localDescription.type);
    console.log("Renegotiation handled.");
    iceRestart = false;
}

webrtcConnection.oniceconnectionstatechange = event => {
    if (webrtcConnection.iceConnectionState == "failed") {
        iceRestart = true;
    }
    console.log("Ice-Connection:", webrtcConnection.iceConnectionState);
}
webrtcConnection.onicegatheringstatechange = event => console.log("Ice-Gathering:", webrtcConnection.iceGatheringState);
webrtcConnection.ondatachannel = event => console.log("Datachannel:", event);
webrtcConnection.onconnectionstatechange = event => console.log("Connection:", webrtcConnection.connectionState);
webrtcConnection.onicecandidateerror = event => console.log("Candidate-Error:", event);
webrtcConnection.onsignalingstatechange = event => console.log("Signaling:", webrtcConnection.signalingState);


webrtcConnection.ontrack = event => {
    console.log("TRACK FOUND: ", event.track);
    event.track.onunmute = evt => {
        console.log("TRACK UNMUTED: ", evt.target, event.track);
        if (video.srcObject) { return; }
        let stream: MediaStream = new MediaStream();
        stream.addTrack(event.track);
        video.srcObject = stream;
    }
};

signalrConnection.on("sdpReceived", async (sdp, type) => {
    let message: RTCSessionDescriptionInit = { sdp, type };
    console.log("SDP received: ", message);

    await webrtcConnection.setRemoteDescription(message);

    if (message.type == "offer") {
        await webrtcConnection.setLocalDescription(await webrtcConnection.createAnswer({ offerToReceiveVideo: true, offerToReceiveAudio: false, iceRestart: iceRestart }));

        await signalrConnection.send("sdpReceived", webrtcConnection.localDescription.sdp, webrtcConnection.localDescription.type);
        console.log("SDP sent: ", webrtcConnection.localDescription);
    }
});

signalrConnection.on("iceReceived", async (candidate, sdpMid, sdpMLineIndex) => {
    if (candidate != null) {
        let iceCandidate: RTCIceCandidateInit = { candidate, sdpMid, sdpMLineIndex };

        await webrtcConnection.addIceCandidate(iceCandidate);
        console.log("ICE received: ", iceCandidate);
    }
});

signalrConnection.start()
    .catch(err => document.write(err))
    .then(async () => {
        await signalrConnection.send("RequestLivestream");
        console.log("Livestream requested.");
    });