// Text-to-Speech Service using Browser SpeechSynthesis API
window.TtsService = {
    synth: window.speechSynthesis,
    queue: [],
    isEnabled: true,
    preferredLang: 'vi-VN',

    // Initialize TTS
    init: function () {
        if (!this.synth) {
            console.warn('[TTS] SpeechSynthesis not supported');
            return false;
        }
        console.log('[TTS] Initialized');
        return true;
    },

    // Enable/disable TTS
    setEnabled: function (enabled) {
        this.isEnabled = enabled;
        if (!enabled) {
            this.stop();
        }
        console.log('[TTS] Enabled:', enabled);
    },

    // Set preferred language
    setLanguage: function (lang) {
        this.preferredLang = lang;
        console.log('[TTS] Language set to:', lang);
    },

    // Speak text
    speak: function (text, lang) {
        if (!this.synth || !this.isEnabled) {
            console.log('[TTS] Skipped - disabled or unsupported');
            return false;
        }

        // Cancel current speech
        this.synth.cancel();

        const utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = lang || this.preferredLang;
        utterance.rate = 0.9; // Slightly slower for clarity
        utterance.pitch = 1;
        utterance.volume = 1;

        // Get Vietnamese voice if available
        const voices = this.synth.getVoices();
        const preferredVoice = voices.find(v =>
            v.lang.startsWith(lang?.substring(0, 2) || 'vi')
        );
        if (preferredVoice) {
            utterance.voice = preferredVoice;
        }

        utterance.onstart = () => console.log('[TTS] Speaking:', text.substring(0, 50) + '...');
        utterance.onend = () => console.log('[TTS] Finished speaking');
        utterance.onerror = (e) => console.error('[TTS] Error:', e.error);

        this.synth.speak(utterance);
        return true;
    },

    // Speak POI info when entering geofence
    speakPoi: function (poiName, poiDescription, lang) {
        if (!this.isEnabled) return;

        const text = poiDescription
            ? `${poiName}. ${poiDescription}`
            : poiName;

        this.speak(text, lang);
    },

    // Announce geofence entry
    announceGeofenceEntry: function (poiName, lang) {
        if (!this.isEnabled) return;

        const announcement = lang?.startsWith('en')
            ? `You are near ${poiName}`
            : `Bạn đang ở gần ${poiName}`;

        this.speak(announcement, lang);
    },

    // Stop speaking
    stop: function () {
        if (this.synth) {
            this.synth.cancel();
            console.log('[TTS] Stopped');
        }
    },

    // Check if currently speaking
    isSpeaking: function () {
        return this.synth ? this.synth.speaking : false;
    },

    // Get available voices
    getVoices: function () {
        if (!this.synth) return [];
        return this.synth.getVoices().map(v => ({
            name: v.name,
            lang: v.lang,
            default: v.default
        }));
    },

    // Get Vietnamese voices
    getVietnameseVoices: function () {
        return this.getVoices().filter(v => v.lang.startsWith('vi'));
    }
};

// Initialize on load
if (window.speechSynthesis) {
    // Voices may load async
    window.speechSynthesis.onvoiceschanged = () => {
        window.TtsService.init();
    };
}
