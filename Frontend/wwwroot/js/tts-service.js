// Text-to-Speech Service with Queue, Dedup, and Auto-interrupt
window.TtsService = {
    synth: window.speechSynthesis,
    isEnabled: true,
    preferredLang: 'vi-VN',

    // === QUEUE: Quản lý hàng chờ audio ===
    queue: [],
    isProcessing: false,
    
    // === DEDUP: Chống phát trùng lặp ===
    recentlyPlayed: {},     // { textHash: timestamp }
    dedupCooldownMs: 60000, // 1 phút không phát lại cùng nội dung

    // === AUDIO: Quản lý audio file ===
    currentAudio: null,

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
            this.stopAll();
        }
        console.log('[TTS] Enabled:', enabled);
    },

    // Set preferred language
    setLanguage: function (lang) {
        this.preferredLang = lang;
        console.log('[TTS] Language set to:', lang);
    },

    // === DEDUP: Simple hash cho text ===
    _hashText: function (text) {
        let hash = 0;
        for (let i = 0; i < text.length; i++) {
            const char = text.charCodeAt(i);
            hash = ((hash << 5) - hash) + char;
            hash |= 0; // Convert to 32bit integer
        }
        return hash.toString();
    },

    // === DEDUP: Kiểm tra đã phát gần đây chưa ===
    _wasRecentlyPlayed: function (text) {
        const hash = this._hashText(text);
        const lastPlayed = this.recentlyPlayed[hash];
        if (lastPlayed && (Date.now() - lastPlayed) < this.dedupCooldownMs) {
            console.log('[TTS] Skipped - recently played');
            return true;
        }
        return false;
    },

    _markAsPlayed: function (text) {
        const hash = this._hashText(text);
        this.recentlyPlayed[hash] = Date.now();
        
        // Cleanup old entries (> 5 phút)
        const now = Date.now();
        Object.keys(this.recentlyPlayed).forEach(key => {
            if (now - this.recentlyPlayed[key] > 300000) {
                delete this.recentlyPlayed[key];
            }
        });
    },

    // === QUEUE: Thêm vào hàng chờ thay vì phát ngay ===
    enqueue: function (text, lang, priority) {
        if (!this.isEnabled) return false;
        if (this._wasRecentlyPlayed(text)) return false;

        // Priority cao (thông báo quan trọng) → dừng tất cả, phát ngay
        if (priority === 'high') {
            this.stopAll();
            this.queue = []; // Xóa queue
            this._speak(text, lang);
            return true;
        }

        // Thêm vào queue
        this.queue.push({ text, lang });
        this._processQueue();
        return true;
    },

    // === QUEUE: Xử lý hàng chờ tuần tự ===
    _processQueue: function () {
        if (this.isProcessing || this.queue.length === 0) return;

        this.isProcessing = true;
        const item = this.queue.shift();
        this._speak(item.text, item.lang);
    },

    // Speak text (internal)
    _speak: function (text, lang) {
        if (!this.synth || !this.isEnabled) {
            this.isProcessing = false;
            return false;
        }

        // Đánh dấu đã phát
        this._markAsPlayed(text);

        const utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = lang || this.preferredLang;
        utterance.rate = 0.9;
        utterance.pitch = 1;
        utterance.volume = 1;

        // Get best voice
        const voices = this.synth.getVoices();
        const preferredVoice = voices.find(v =>
            v.lang.startsWith(lang?.substring(0, 2) || 'vi')
        );
        if (preferredVoice) {
            utterance.voice = preferredVoice;
        }

        utterance.onstart = () => console.log('[TTS] Speaking:', text.substring(0, 50) + '...');
        
        utterance.onend = () => {
            console.log('[TTS] Finished speaking');
            this.isProcessing = false;
            // Phát tiếp item tiếp theo trong queue
            this._processQueue();
        };

        utterance.onerror = (e) => {
            console.error('[TTS] Error:', e.error);
            this.isProcessing = false;
            this._processQueue();
        };

        this.synth.speak(utterance);
        return true;
    },

    // Public speak (backwards compatible)
    speak: function (text, lang) {
        return this.enqueue(text, lang, 'normal');
    },

    // Speak POI info when entering geofence
    speakPoi: function (poiName, poiDescription, lang) {
        if (!this.isEnabled) return;

        const text = poiDescription
            ? `${poiName}. ${poiDescription}`
            : poiName;

        // POI thuyết minh → dừng audio trước, phát ngay (high priority)
        this.enqueue(text, lang, 'high');
    },

    // Announce geofence entry
    announceGeofenceEntry: function (poiName, lang) {
        if (!this.isEnabled) return;

        const announcement = lang?.startsWith('en')
            ? `You are near ${poiName}`
            : `Bạn đang ở gần ${poiName}`;

        this.enqueue(announcement, lang, 'normal');
    },

    // === AUDIO FILE: Phát file audio thay vì TTS ===
    playAudioFile: function (url) {
        if (!this.isEnabled) return false;

        // Dừng TTS nếu đang nói
        this.stop();

        // Dừng audio file cũ nếu đang phát
        if (this.currentAudio) {
            this.currentAudio.pause();
            this.currentAudio = null;
        }

        this.currentAudio = new Audio(url);
        this.currentAudio.onended = () => {
            console.log('[TTS] Audio file finished');
            this.currentAudio = null;
            // Tiếp tục xử lý TTS queue nếu còn
            this._processQueue();
        };
        this.currentAudio.onerror = (e) => {
            console.error('[TTS] Audio file error:', e);
            this.currentAudio = null;
            this._processQueue();
        };
        this.currentAudio.play();
        console.log('[TTS] Playing audio file:', url);
        return true;
    },

    // Stop TTS only
    stop: function () {
        if (this.synth) {
            this.synth.cancel();
            this.isProcessing = false;
        }
    },

    // === STOP ALL: Dừng tất cả (TTS + Audio file + Queue) ===
    stopAll: function () {
        this.stop();
        this.queue = [];
        if (this.currentAudio) {
            this.currentAudio.pause();
            this.currentAudio = null;
        }
        console.log('[TTS] All stopped');
    },

    // Check if currently speaking or playing
    isSpeaking: function () {
        const ttsSpeaking = this.synth ? this.synth.speaking : false;
        const audioPlaying = this.currentAudio && !this.currentAudio.paused;
        return ttsSpeaking || audioPlaying;
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
    window.speechSynthesis.onvoiceschanged = () => {
        window.TtsService.init();
    };
}
