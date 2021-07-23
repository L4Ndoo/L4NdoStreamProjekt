import "./css/main.css";
import * as signalR from "@microsoft/signalr";

(document.getElementById("output") as HTMLTextAreaElement).value = "v0.0.3";
function output(line: string): void {
    let output: HTMLTextAreaElement = (document.getElementById("output") as HTMLTextAreaElement);
    output.value = new Date().toLocaleTimeString() + "\t" + line + "\n" + output.value;
}

const video: HTMLVideoElement = document.getElementById('video') as HTMLVideoElement;
const signalrConnection = new signalR.HubConnectionBuilder()
    .withUrl("/livestream")
    .build();
const webrtcConnection = new RTCPeerConnection({ iceServers: [{ urls: ['stun:stun.l.google.com:19302']}]});

// Setup handlers on webrtc connection
webrtcConnection.onicecandidate = async event => {
    if (event.candidate) {
        await signalrConnection.send("iceReceived", event.candidate.candidate, event.candidate.sdpMid, event.candidate.sdpMLineIndex);
        output("ICE sent: " + event.candidate);
    }
};
webrtcConnection.onnegotiationneeded = async event => {
    output("Renegotiation needed.");
    await webrtcConnection.setLocalDescription(await webrtcConnection.createOffer({ offerToReceiveVideo: true, offerToReceiveAudio: false }));
    await signalrConnection.send("sdpReceived", webrtcConnection.localDescription.sdp, webrtcConnection.localDescription.type);
    output("Renegotiation handled.");
};
webrtcConnection.ontrack = event => {
    output("Track added: " + event.track);
    event.track.onunmute = evt => {
        if (video.srcObject) { return; }
        let stream: MediaStream = new MediaStream();
        stream.addTrack(event.track);
        video.srcObject = stream;
    }
};
webrtcConnection.oniceconnectionstatechange = event => output("Ice-Connection: " + webrtcConnection.iceConnectionState);
webrtcConnection.onicegatheringstatechange = event => output("Ice-Gathering: " + webrtcConnection.iceGatheringState);
webrtcConnection.ondatachannel = event => output("Datachannel: " + event);
webrtcConnection.onconnectionstatechange = event => output("Connection: " + webrtcConnection.connectionState);
webrtcConnection.onicecandidateerror = event => output("Candidate-Error: " + event);
webrtcConnection.onsignalingstatechange = event => output("Signaling: " + webrtcConnection.signalingState);

// Setup handlers on signalr connection
signalrConnection.on("sdpReceived", async (sdp, type) => {
    let message: RTCSessionDescriptionInit = { sdp, type };
    output("SDP received: " + message);

    await webrtcConnection.setRemoteDescription(message);
    if (webrtcConnection.signalingState == "have-local-pranswer" || webrtcConnection.signalingState == "have-remote-offer") {
        try {
            let answer = await webrtcConnection.createAnswer();
            await webrtcConnection.setLocalDescription(answer);

            await signalrConnection.send("sdpReceived", webrtcConnection.localDescription.sdp, webrtcConnection.localDescription.type);
            output("SDP sent: " + webrtcConnection.localDescription);
        }
        catch {
            output("Creating answer skipped.");
        }
    }
});
signalrConnection.on("iceReceived", async (candidate, sdpMid, sdpMLineIndex) => {
    if (candidate != null) {
        let iceCandidate: RTCIceCandidateInit = { candidate, sdpMid, sdpMLineIndex };

        await webrtcConnection.addIceCandidate(iceCandidate);
        output("ICE received: " + iceCandidate);
    }
});

// Connect signalR
signalrConnection.start()
    .catch(err => document.write(err))
    .then(async () => {
        // Setup handlers for html interaction
        document.getElementById("connect").onclick = async () => await signalrConnection.send("requestConnection");
        document.getElementById("disconnect").onclick = async () => await signalrConnection.send("destroyConnection");
        document.getElementById("join").onclick = async () => {
            let stream: string = (document.getElementById("stream") as HTMLInputElement).value;
            await signalrConnection.send("requestLivestream", stream);
        };
        document.getElementById("leave").onclick = async () => {
            let stream: string = (document.getElementById("stream") as HTMLInputElement).value;
            await signalrConnection.send("removeLivestream", stream);
        };
        document.getElementById("resolution").onclick = async () => {
            let stream: string = (document.getElementById("stream") as HTMLInputElement).value;
            let scale: string = (document.getElementById("scale") as HTMLInputElement).value;
            await signalrConnection.send("SetResolution", stream, scale);
        }
        video.ontimeupdate = () => {
            (document.getElementById("info") as HTMLSpanElement).innerText =
                `Resolution: ${video.videoWidth}x${video.videoHeight}; Rate: ${video.playbackRate}; Time: ${video.currentTime}`;
        }
    });