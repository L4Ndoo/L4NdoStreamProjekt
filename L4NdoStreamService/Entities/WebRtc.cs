using Microsoft.MixedReality.WebRTC;

namespace L4NdoStreamService.Entities
{
    public class WebRtc
    {
        public PeerConnection Connection { get; set; }
        public bool MakingOffer { get; set; }
        public IceConnectionState IceConnectionState { get; set; }
        public IceGatheringState IceGatheringState { get; set; }

        public WebRtc(PeerConnection connection)
        {
            this.Connection = connection;
            this.Connection.IceStateChanged += args => IceConnectionState = args;
            this.Connection.IceGatheringStateChanged += args => IceGatheringState = args;
        }
    }
}
