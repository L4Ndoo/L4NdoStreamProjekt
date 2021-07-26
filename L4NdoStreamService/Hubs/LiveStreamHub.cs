using L4NdoStreamService.Entities;
using L4NdoStreamService.Entities.FrameSource;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.MixedReality.WebRTC;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace L4NdoStreamService.Hubs
{
    public class LiveStreamHub : Hub
    {
        private readonly ILogger<LiveStreamHub> _logger;
        private readonly ConcurrentDictionary<string, WebRtcRenderer> _renderers;

        public LiveStreamHub(ConcurrentDictionary<string, WebRtcRenderer> renderers, ILogger<LiveStreamHub> logger)
        {
            this._logger = logger;
            this._renderers = renderers;
        }

        public async Task RequestConnection()
        {
            this._logger.LogInformation("Connection request incoming.");
            this.DestroyConnection();

            // Initialize Connection
            PeerConnection connection = new ();
            await connection.InitializeAsync(new () { IceServers = new () { new IceServer { 
                    Urls = new () { "stun:stun.l.google.com:19302" } } } });
            this._logger.LogInformation("Connection initialized.");

            // Setup handlers
            var caller = Clients.Caller;
            connection.Connected += () => this._logger.LogInformation("Connected");
            connection.VideoTrackAdded += track => this._logger.LogInformation("Videotrack added: " + track.Name);
            connection.IceGatheringStateChanged += args => this._logger.LogInformation($"IceGathering: {args}");
            connection.IceStateChanged += args => this._logger.LogInformation($"IceState: {args}");
            connection.RenegotiationNeeded += () =>
            {
                connection.CreateOffer();
                this._logger.LogInformation("Renegotiation initiated.");
            };
            connection.LocalSdpReadytoSend += async message =>
            {
                await caller.SendAsync("SdpReceived", message.Content, message.Type.ToString().ToLower());
                this._logger.LogInformation("SDP sent: " + message);
            };
            connection.IceCandidateReadytoSend += async candidate =>
            {
                await caller.SendAsync("IceReceived", candidate.Content, candidate.SdpMid, candidate.SdpMlineIndex);
                this._logger.LogInformation("ICE sent: " + candidate);
            };

            // Store connection to process incoming messages and create offer
            Context.Items.Add("WebRtc", connection);
            connection.CreateOffer();
            this._logger.LogInformation("Offer created.");
        }

        public async Task SdpReceived(string sdp, string type)
        {
            SdpMessage message = new () { Content = sdp, Type = type == "offer" ? SdpMessageType.Offer : SdpMessageType.Answer };
            this._logger.LogInformation("SDP received: " + type);

            if (this.Context.Items.TryGetValue("WebRtc", out object temp))
            {
                PeerConnection connection = (PeerConnection)temp;
                await connection.SetRemoteDescriptionAsync(message);
                if (message.Type == SdpMessageType.Offer) { connection.CreateAnswer(); }
            }
        }

        public void IceReceived(string candidate, string sdpMid, string sdpMLineIndex)
        {
            if (candidate != null && this.Context.Items.TryGetValue("WebRtc", out object temp))
            {
                this._logger.LogInformation("ICE received: " + candidate);
                IceCandidate iceCandidate = new () { Content = candidate, SdpMid = sdpMid, SdpMlineIndex = int.Parse(sdpMLineIndex) };

                PeerConnection connection = (PeerConnection)temp;
                connection.AddIceCandidate(iceCandidate);
            }
        }

        public void SetResolution(string stream, string scale)
        {
            if (this.Context.Items.TryGetValue("WebRtc", out _) && this._renderers.TryGetValue(stream, out WebRtcRenderer renderer))
            {
                renderer.FrameSource.Scale = float.Parse(scale);
                this._logger.LogInformation("Resolution changed.");
            }
        }

        public void RequestLivestream(string stream)
        {
            stream = stream.ToLower();
            if (this.Context.Items.TryGetValue("WebRtc", out object temp) && this._renderers.TryGetValue(stream, out WebRtcRenderer renderer))
            {
                if(renderer == null)
                {
                    renderer = new WebRtcRenderer(
                        stream == "image"
                        ? new ImageFrameSource(".\\Videoframes\\", "colorshift_", 300, 30)
                        : stream == "emulator"
                        ? new BaslerFrameSource(true)
                        : stream == "basler"
                        ? new BaslerFrameSource()
                        : stream == "webcam"
                        ? new WebcamFrameSource()
                        : new IdsFrameSource()
                    );
                    renderer.NewFrametime += time => this._logger.LogInformation($"Frametime {stream}: {time}");
                    this._renderers[stream] = renderer;
                }

                PeerConnection connection = (PeerConnection)temp;
                Transceiver transceiver = transceiver = connection.Transceivers.FirstOrDefault();
                if (transceiver == null)
                {
                    transceiver = connection.AddTransceiver(MediaKind.Video);
                    transceiver.DesiredDirection = Transceiver.Direction.SendOnly;
                }
                else { transceiver.LocalVideoTrack?.Dispose(); }

                LocalVideoTrackInitConfig config = new () { trackName = stream };
                transceiver.LocalVideoTrack = LocalVideoTrack.CreateFromSource(renderer.VideoTrackSource, config);
                this._logger.LogInformation("Videotrack added.");
            }
        }

        public void RemoveLivestream(string stream)
        {
            if (this.Context.Items.TryGetValue("WebRtc", out object temp) && this._renderers.TryGetValue(stream, out WebRtcRenderer _))
            {
                PeerConnection connection = (PeerConnection)temp;
                Transceiver transceiver = connection.Transceivers.FirstOrDefault();
                if (transceiver != null) { transceiver.LocalVideoTrack?.Dispose(); }
                this._logger.LogInformation("Videotrack removed.");
            }
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            this.DestroyConnection();
            return base.OnDisconnectedAsync(exception);
        }

        public void DestroyConnection()
        {
            if(this.Context.Items.TryGetValue("WebRtc", out object temp))
            {
                PeerConnection connection = (PeerConnection)temp;

                connection.Close();
                connection.Dispose();

                this.Context.Items.Remove("WebRtc");
            }
            this._logger.LogInformation("Livestream destroyed.");
        }
    }
}