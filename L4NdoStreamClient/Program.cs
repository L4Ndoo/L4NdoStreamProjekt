using Microsoft.MixedReality.WebRTC;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Generic;

namespace L4NdoStreamClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            HubConnection signalR = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/livestream")
                .Build();

            PeerConnection webrtc = new PeerConnection();
            await webrtc.InitializeAsync(new PeerConnectionConfiguration
            {
                IceServers = new List<IceServer> {
                    new IceServer{ Urls = { "stun:stun.l.google.com:19302" } }
                }
            });
            Console.WriteLine("Connection initialized.");

            webrtc.VideoTrackAdded += track =>
            {
                Console.WriteLine("TRACK ADDED");
                int framecount = 0;
                track.Argb32VideoFrameReady += frame =>
                {
                    if (++framecount % 10 == 0) Console.WriteLine($"{framecount} Frames received.");
                };
                track.I420AVideoFrameReady += frame =>
                {
                    if (++framecount % 10 == 0) Console.WriteLine($"{framecount} Frames received.");
                };
            };
            webrtc.AudioTrackAdded += track =>
            {
                Console.WriteLine("TRACK ADDED");
                int framecount = 0;
                track.AudioFrameReady += frame =>
                {
                    if (++framecount % 10 == 0) Console.WriteLine($"{framecount} Frames received.");
                };
            };
            webrtc.LocalSdpReadytoSend += async message =>
            {
                await signalR.SendAsync("SdpReceived", message.Content, message.Type.ToString().ToLower());
            };
            webrtc.IceCandidateReadytoSend += async candidate =>
            {
                await signalR.SendAsync("IceReceived", candidate.Content, candidate.SdpMid, candidate.SdpMlineIndex);
            };

            signalR.On<string, string>("SdpReceived", async (content, type) =>
            {
                SdpMessage message = new SdpMessage
                {
                    Content = content,
                    Type = type == "offer" ? SdpMessageType.Offer : SdpMessageType.Answer
                };
                await webrtc.SetRemoteDescriptionAsync(message);
                webrtc.CreateAnswer();
            });

            signalR.On<string, string, int>("IceReceived", (content, sdpMid, sdpLine) =>
            {
                return Task.Run(() =>
                {
                    IceCandidate candidate = new IceCandidate
                    {
                        Content = content,
                        SdpMid = sdpMid,
                        SdpMlineIndex = sdpLine
                    };
                    webrtc.AddIceCandidate(candidate);
                });
            });

            await signalR.StartAsync();
            await signalR.SendAsync("requestLivestream");
            Console.ReadLine();
        }
    }
}
