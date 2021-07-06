using L4NdoStreamService.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.MixedReality.WebRTC;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace L4NdoStreamService.Entities
{
    public class LiveStreamHub : Hub
    {
        private WebRtcRenderer _renderer;
        private ILogger<LiveStreamHub> _logger;

        public LiveStreamHub(WebRtcRenderer renderer, ILogger<LiveStreamHub> logger)
        {
            this._renderer = renderer;
            this._logger = logger;
        }

        public async Task RequestLivestream()
        {
            _logger.LogInformation("Livestream Request incoming.");
            this.DestroyLivestream();

            // Initialize Connection
            PeerConnection connection = new PeerConnection();
            connection.RenegotiationNeeded += () => connection.CreateOffer();
            await connection.InitializeAsync();
            _logger.LogInformation("Connection initialized.");

            // Add Video-Stream
            Transceiver transceiver = connection.AddTransceiver(MediaKind.Video);
            transceiver.DesiredDirection = Transceiver.Direction.SendOnly;
            LocalVideoTrackInitConfig config = new LocalVideoTrackInitConfig { trackName = "" };
            transceiver.LocalVideoTrack = LocalVideoTrack.CreateFromSource(this._renderer.VideoTrackSource, config);

            // Add Audio-Stream
            //Transceiver transceiver = connection.AddTransceiver(MediaKind.Audio);
            //transceiver.DesiredDirection = Transceiver.Direction.SendReceive;
            //LocalAudioTrackInitConfig config = new LocalAudioTrackInitConfig { trackName = "audiostream" };
            //transceiver.LocalAudioTrack = LocalAudioTrack.CreateFromSource(await DeviceAudioTrackSource.CreateAsync(), config);
            //_logger.LogInformation($"Videotrack added: {transceiver.StreamIDs.Aggregate("", (r, i) => r + i)}");

            // Create Offer
            var caller = Clients.Caller;
            connection.LocalSdpReadytoSend += async message =>
            {
                await caller.SendAsync("SdpReceived", message.Content, message.Type.ToString().ToLower());
                _logger.LogInformation("SDP sent: " + message);
            };
            connection.IceCandidateReadytoSend += async candidate =>
            {
                await caller.SendAsync("IceReceived", candidate.Content, candidate.SdpMid, candidate.SdpMlineIndex);
                _logger.LogInformation("ICE sent: " + candidate);
            };

            connection.CreateOffer();
            _logger.LogInformation("Offer created.");

            // Store webrtc object
            Context.Items.Add("WebRtc", transceiver);
        }

        public async Task SdpReceived(string sdp, string type)
        {
            SdpMessage message = new SdpMessage { Content = sdp, Type = type == "offer" ? SdpMessageType.Offer : SdpMessageType.Answer };
            _logger.LogInformation("SDP received: " + type);

            object temp;
            bool connected = this.Context.Items.TryGetValue("WebRtc", out temp);
            if (connected)
            {
                Transceiver transceiver = (Transceiver)temp;
                PeerConnection connection = transceiver.PeerConnection;

                await connection.SetRemoteDescriptionAsync(message);
                if (message.Type == SdpMessageType.Offer) { connection.CreateAnswer(); }
            }
        }

        public void IceReceived(string candidate, string sdpMid, int sdpMLineIndex)
        {
            IceCandidate iceCandidate = new IceCandidate { Content = candidate, SdpMid = sdpMid, SdpMlineIndex = sdpMLineIndex };
            _logger.LogInformation("ICE received: " + candidate);
            object temp;
            bool connected = this.Context.Items.TryGetValue("WebRtc", out temp);
            if (connected)
            {
                Transceiver transceiver = (Transceiver)temp;
                PeerConnection connection = transceiver.PeerConnection;

                connection.AddIceCandidate(iceCandidate);
            }
        }

        public void DestroyLivestream()
        {
            object temp;
            bool connected = this.Context.Items.TryGetValue("WebRtc", out temp);
            if(connected)
            {
                Transceiver transceiver = (Transceiver)temp;

                transceiver?.PeerConnection?.Close();
                transceiver?.PeerConnection?.Dispose();

                this.Context.Items.Remove("WebRtc");
            }
            _logger.LogInformation("Livestream destroyed.");
        }
    }
}