using L4NdoStreamService.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.MixedReality.WebRTC;
using System;
using System.Collections.Generic;
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

        public async Task RequestWebRtcConnection()
        {
            _logger.LogInformation("Livestream Request incoming.");
            this.DestroyLivestream();

            // Initialize Connection
            PeerConnection connection = new PeerConnection();
            connection.RenegotiationNeeded += () => connection.CreateOffer();
            await connection.InitializeAsync(new PeerConnectionConfiguration
            {
                IceServers = new List<IceServer> {
                    new IceServer{ Urls = { "stun:stun.l.google.com:19302" } }
                }
            });
            _logger.LogInformation("Connection initialized.");

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
            Context.Items.Add("WebRtc", connection);
        }

        public async Task RequestLivestream()
        {
            object temp;
            bool connected = this.Context.Items.TryGetValue("WebRtc", out temp);
            if (connected)
            {
                PeerConnection connection = (PeerConnection)temp;
                Transceiver transceiver = connection.AddTransceiver(MediaKind.Video);
                transceiver.DesiredDirection = Transceiver.Direction.SendOnly;
                LocalVideoTrackInitConfig config = new LocalVideoTrackInitConfig { trackName = "" };
                transceiver.LocalVideoTrack = LocalVideoTrack.CreateFromSource(this._renderer.VideoTrackSource, config);
                _logger.LogInformation(".");
            }
            

        }

        public async Task SdpReceived(string sdp, string type)
        {
            SdpMessage message = new SdpMessage { Content = sdp, Type = type == "offer" ? SdpMessageType.Offer : SdpMessageType.Answer };
            _logger.LogInformation("SDP received: " + type);

            object temp;
            bool connected = this.Context.Items.TryGetValue("WebRtc", out temp);
            if (connected)
            {
                PeerConnection connection = (PeerConnection)temp;

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
                PeerConnection connection = (PeerConnection)temp;
                connection.AddIceCandidate(iceCandidate);
            }
        }

        public void DestroyLivestream()
        {
            object temp;
            bool connected = this.Context.Items.TryGetValue("WebRtc", out temp);
            if(connected)
            {
                PeerConnection connection = (PeerConnection)temp;

                connection.Close();
                connection.Dispose();

                this.Context.Items.Remove("WebRtc");
            }
            _logger.LogInformation("Livestream destroyed.");
        }
    }
}