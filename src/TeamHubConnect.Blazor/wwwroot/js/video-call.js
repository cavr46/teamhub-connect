// WebRTC Video Call Implementation
class VideoCallManager {
    constructor() {
        this.localStream = null;
        this.peerConnections = new Map();
        this.isCallActive = false;
        this.configuration = {
            iceServers: [
                { urls: 'stun:stun.l.google.com:19302' },
                { urls: 'stun:stun1.l.google.com:19302' }
            ]
        };
    }

    async initializePreview(videoElement) {
        try {
            this.localStream = await navigator.mediaDevices.getUserMedia({
                video: { width: 1280, height: 720 },
                audio: true
            });
            videoElement.srcObject = this.localStream;
        } catch (error) {
            console.error('Error accessing media devices:', error);
            throw error;
        }
    }

    async joinCall(localVideoElement, channelId) {
        try {
            if (!this.localStream) {
                await this.initializePreview(localVideoElement);
            }

            this.isCallActive = true;
            localVideoElement.srcObject = this.localStream;

            // Connect to SignalR hub for signaling
            await this.connectToSignaling(channelId);

            console.log('Joined call for channel:', channelId);
        } catch (error) {
            console.error('Error joining call:', error);
            throw error;
        }
    }

    async connectToSignaling(channelId) {
        // In a real implementation, this would connect to SignalR
        // and handle signaling for WebRTC peer connections
        console.log('Connecting to signaling server for channel:', channelId);
    }

    async createPeerConnection(peerId) {
        const peerConnection = new RTCPeerConnection(this.configuration);

        // Add local stream tracks
        if (this.localStream) {
            this.localStream.getTracks().forEach(track => {
                peerConnection.addTrack(track, this.localStream);
            });
        }

        // Handle remote stream
        peerConnection.ontrack = (event) => {
            const remoteVideo = document.getElementById(`remoteVideo_${peerId}`);
            if (remoteVideo && event.streams[0]) {
                remoteVideo.srcObject = event.streams[0];
            }
        };

        // Handle ICE candidates
        peerConnection.onicecandidate = (event) => {
            if (event.candidate) {
                // Send ICE candidate to remote peer via signaling
                this.sendSignalingMessage({
                    type: 'ice-candidate',
                    candidate: event.candidate,
                    to: peerId
                });
            }
        };

        // Handle connection state changes
        peerConnection.onconnectionstatechange = () => {
            console.log(`Connection state for ${peerId}:`, peerConnection.connectionState);
        };

        this.peerConnections.set(peerId, peerConnection);
        return peerConnection;
    }

    async toggleMute(isMuted) {
        if (this.localStream) {
            const audioTrack = this.localStream.getAudioTracks()[0];
            if (audioTrack) {
                audioTrack.enabled = !isMuted;
            }
        }
    }

    async toggleVideo(isVideoOff) {
        if (this.localStream) {
            const videoTrack = this.localStream.getVideoTracks()[0];
            if (videoTrack) {
                videoTrack.enabled = !isVideoOff;
            }
        }
    }

    async toggleScreenShare(isSharing) {
        try {
            if (isSharing) {
                // Start screen sharing
                const screenStream = await navigator.mediaDevices.getDisplayMedia({
                    video: true,
                    audio: true
                });

                // Replace video track in all peer connections
                const videoTrack = screenStream.getVideoTracks()[0];
                this.peerConnections.forEach(async (pc) => {
                    const sender = pc.getSenders().find(s => 
                        s.track && s.track.kind === 'video'
                    );
                    if (sender) {
                        await sender.replaceTrack(videoTrack);
                    }
                });

                // Handle screen share ending
                videoTrack.onended = () => {
                    this.stopScreenShare();
                };

            } else {
                await this.stopScreenShare();
            }
        } catch (error) {
            console.error('Error toggling screen share:', error);
        }
    }

    async stopScreenShare() {
        if (this.localStream) {
            const videoTrack = this.localStream.getVideoTracks()[0];
            if (videoTrack) {
                // Replace screen share with camera
                this.peerConnections.forEach(async (pc) => {
                    const sender = pc.getSenders().find(s => 
                        s.track && s.track.kind === 'video'
                    );
                    if (sender) {
                        await sender.replaceTrack(videoTrack);
                    }
                });
            }
        }
    }

    async endCall() {
        try {
            // Close all peer connections
            this.peerConnections.forEach((pc) => {
                pc.close();
            });
            this.peerConnections.clear();

            // Stop local stream
            if (this.localStream) {
                this.localStream.getTracks().forEach(track => {
                    track.stop();
                });
                this.localStream = null;
            }

            this.isCallActive = false;
            console.log('Call ended');
        } catch (error) {
            console.error('Error ending call:', error);
        }
    }

    sendSignalingMessage(message) {
        // In a real implementation, this would send via SignalR
        console.log('Sending signaling message:', message);
    }

    handleSignalingMessage(message) {
        // Handle incoming signaling messages
        switch (message.type) {
            case 'offer':
                this.handleOffer(message);
                break;
            case 'answer':
                this.handleAnswer(message);
                break;
            case 'ice-candidate':
                this.handleIceCandidate(message);
                break;
            default:
                console.log('Unknown message type:', message.type);
        }
    }

    async handleOffer(message) {
        const peerConnection = await this.createPeerConnection(message.from);
        await peerConnection.setRemoteDescription(message.offer);
        
        const answer = await peerConnection.createAnswer();
        await peerConnection.setLocalDescription(answer);
        
        this.sendSignalingMessage({
            type: 'answer',
            answer: answer,
            to: message.from
        });
    }

    async handleAnswer(message) {
        const peerConnection = this.peerConnections.get(message.from);
        if (peerConnection) {
            await peerConnection.setRemoteDescription(message.answer);
        }
    }

    async handleIceCandidate(message) {
        const peerConnection = this.peerConnections.get(message.from);
        if (peerConnection) {
            await peerConnection.addIceCandidate(message.candidate);
        }
    }

    cleanup() {
        this.endCall();
    }
}

// Global instance
const videoCallManager = new VideoCallManager();

// Export functions for Blazor interop
window.videoCall = {
    initializePreview: (videoElement) => videoCallManager.initializePreview(videoElement),
    joinCall: (localVideoElement, channelId) => videoCallManager.joinCall(localVideoElement, channelId),
    endCall: () => videoCallManager.endCall(),
    toggleMute: (isMuted) => videoCallManager.toggleMute(isMuted),
    toggleVideo: (isVideoOff) => videoCallManager.toggleVideo(isVideoOff),
    toggleScreenShare: (isSharing) => videoCallManager.toggleScreenShare(isSharing),
    cleanup: () => videoCallManager.cleanup()
};

// Auto-export for ES6 modules
export const {
    initializePreview,
    joinCall,
    endCall,
    toggleMute,
    toggleVideo,
    toggleScreenShare,
    cleanup
} = window.videoCall;