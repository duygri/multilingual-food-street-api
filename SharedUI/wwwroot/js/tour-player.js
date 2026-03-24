window.TourPlayer = {
    audio: new Audio(),
    synth: window.speechSynthesis,
    isSpeaking: false,
    dotNetHelper: null, // to call back C#

    init: function (dotNetHelper) {
        this.dotNetHelper = dotNetHelper;
        
        // Listen to Audio events
        this.audio.onplay = () => this.notifyStateChange(true);
        this.audio.onpause = () => this.notifyStateChange(false);
        this.audio.onended = () => {
            this.notifyStateChange(false);
            this.notifyEnded();
        };
        
        // Load voices
        if (this.synth.onvoiceschanged !== undefined) {
            this.synth.onvoiceschanged = () => this.synth.getVoices();
        }
    },

    playAudio: function (url) {
        this.stopInner();
        this.audio.src = url;
        this.audio.play().catch(e => console.error("Audio play error", e));
    },

    playTts: function (text, lang = 'vi-VN') {
        this.stopInner();
        
        const utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = lang;
        utterance.rate = 1.0;
        
        utterance.onstart = () => {
            this.isSpeaking = true;
            this.notifyStateChange(true);
        };
        utterance.onend = () => {
            this.isSpeaking = false;
            this.notifyStateChange(false);
            this.notifyEnded();
        };
        utterance.onpause = () => this.notifyStateChange(false);
        utterance.onresume = () => this.notifyStateChange(true);
        utterance.onerror = (e) => {
            console.error("TTS error", e);
            this.isSpeaking = false;
            this.notifyStateChange(false);
        };

        this.synth.speak(utterance);
    },

    stopInner: function () {
        if (!this.audio.paused) {
            this.audio.pause();
            this.audio.currentTime = 0;
        }
        if (this.synth.speaking) {
            this.synth.cancel();
            this.isSpeaking = false;
        }
    },

    pause: function () {
        if (!this.audio.paused) {
            this.audio.pause();
        } else if (this.synth.speaking && !this.synth.paused) {
            this.synth.pause();
            this.isSpeaking = false;
            this.notifyStateChange(false);
        }
    },

    resume: function () {
        if (this.audio.src && this.audio.paused) {
            this.audio.play();
        } else if (this.synth.paused) {
            this.synth.resume();
            this.isSpeaking = true;
            this.notifyStateChange(true);
        }
    },

    stop: function () {
        this.stopInner();
        this.notifyStateChange(false);
    },

    notifyStateChange: function (isPlaying) {
        if (this.dotNetHelper) {
            this.dotNetHelper.invokeMethodAsync('OnPlayerStateChanged', isPlaying);
        }
    },

    notifyEnded: function () {
        if (this.dotNetHelper) {
            this.dotNetHelper.invokeMethodAsync('OnPlayerEnded');
        }
    }
};
