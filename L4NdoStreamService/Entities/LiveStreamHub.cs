using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.MixedReality.WebRTC;
using System.Collections.Generic;
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
            this._logger.LogInformation("Livestream Request incoming.");
            this.DestroyLivestream();

            // Initialize Connection
            PeerConnection connection = new PeerConnection();
            await connection.InitializeAsync(new PeerConnectionConfiguration{
                IceServers = new List<IceServer>{
                    new IceServer{ Urls = new List<string>{ "stun:stun.l.google.com:19302" } }
                }
            });
            WebRtc webRtc = new WebRtc(connection);
            this._logger.LogInformation("Connection initialized.");

            // Add Videotrack
            Transceiver transceiver = connection.AddTransceiver(MediaKind.Video);
            transceiver.DesiredDirection = Transceiver.Direction.SendOnly;
            LocalVideoTrackInitConfig config = new LocalVideoTrackInitConfig { trackName = "stream" };
            transceiver.LocalVideoTrack = LocalVideoTrack.CreateFromSource(this._renderer.VideoTrackSource, config);
            this._logger.LogInformation("Videotrack added.");

            // Setup handlers
            var caller = Clients.Caller;
            connection.Connected += () => this._logger.LogInformation("Connected");
            connection.VideoTrackAdded += track => this._logger.LogInformation("Videotrack added: " + track.Name);
            connection.IceGatheringStateChanged += args => this._logger.LogInformation($"IceGathering: {args}");
            connection.IceStateChanged += args => this._logger.LogInformation($"IceState: {args}");
            connection.RenegotiationNeeded += () =>
            {
                webRtc.MakingOffer = connection.CreateOffer();
                this._logger.LogInformation("Renegotiation needed.");
            };
            connection.LocalSdpReadytoSend += async message =>
            {
                webRtc.MakingOffer = message.Type == SdpMessageType.Offer;
                await caller.SendAsync("SdpReceived", message.Content, message.Type.ToString().ToLower());
                webRtc.MakingOffer = false;
                this._logger.LogInformation("SDP sent: " + message);
            };
            connection.IceCandidateReadytoSend += async candidate =>
            {
                await caller.SendAsync("IceReceived", candidate.Content, candidate.SdpMid, candidate.SdpMlineIndex);
                this._logger.LogInformation("ICE sent: " + candidate);
            };

            // Store connection to process incoming messages and create offer
            Context.Items.Add("WebRtc", webRtc);
            connection.CreateOffer();
            this._logger.LogInformation("Offer created.");
        }

        public async Task SdpReceived(string sdp, string type)
        {
            SdpMessage message = new SdpMessage { Content = sdp, Type = type == "offer" ? SdpMessageType.Offer : SdpMessageType.Answer };
            this._logger.LogInformation("SDP received: " + type);

            if (this.Context.Items.TryGetValue("WebRtc", out object temp))
            {
                WebRtc webRtc = (WebRtc)temp;
                if(message.Type == SdpMessageType.Offer && webRtc.MakingOffer) { return; }

                await webRtc.Connection.SetRemoteDescriptionAsync(message);
                if (message.Type == SdpMessageType.Offer) { webRtc.Connection.CreateAnswer(); }
            }
        }

        public void IceReceived(string candidate, string sdpMid, int sdpMLineIndex)
        {
            if (candidate != null && this.Context.Items.TryGetValue("WebRtc", out object temp))
            {
                this._logger.LogInformation("ICE received: " + candidate);
                IceCandidate iceCandidate = new IceCandidate { Content = candidate, SdpMid = sdpMid, SdpMlineIndex = sdpMLineIndex };

                WebRtc webRtc = (WebRtc)temp;
                webRtc.Connection.AddIceCandidate(iceCandidate);
            }
        }

        public void DestroyLivestream()
        {
            if(this.Context.Items.TryGetValue("WebRtc", out object temp))
            {
                WebRtc webRtc = (WebRtc)temp;

                webRtc.Connection.Close();
                webRtc.Connection.Dispose();

                this.Context.Items.Remove("WebRtc");
            }
            this._logger.LogInformation("Livestream destroyed.");
        }
    }
}