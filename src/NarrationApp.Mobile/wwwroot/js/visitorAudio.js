window.visitorAudio = (() => {
    const audio = new Audio();
    audio.preload = "auto";

    let bridge = null;
    let lastUrl = "";

    function wire(dotNetRef) {
        bridge = dotNetRef || bridge;
    }

    function notify(state, message) {
        if (bridge) {
            bridge.invokeMethodAsync("OnAudioStateChanged", state, message);
        }
    }

    function toWholeSeconds(value) {
        return Number.isFinite(value) && value > 0 ? Math.floor(value) : 0;
    }

    function notifyProgress(currentSeconds, durationSeconds) {
        if (bridge) {
            bridge.invokeMethodAsync(
                "OnAudioProgressChanged",
                toWholeSeconds(currentSeconds),
                toWholeSeconds(durationSeconds));
        }
    }

    audio.addEventListener("playing", () => notify("playing", "Đang phát audio"));
    audio.addEventListener("pause", () => {
        if (!audio.ended) {
            notify("paused", "Đã tạm dừng");
        }
    });
    audio.addEventListener("loadedmetadata", () => notifyProgress(audio.currentTime, audio.duration));
    audio.addEventListener("timeupdate", () => notifyProgress(audio.currentTime, audio.duration));
    audio.addEventListener("ended", () => {
        notifyProgress(audio.duration, audio.duration);
        notify("ended", "Đã phát xong");
    });
    audio.addEventListener("error", () => notify("error", "Phát audio thất bại"));

    function setSource(url) {
        if (lastUrl === url) {
            return;
        }

        audio.pause();
        audio.src = url;
        lastUrl = url;
        notifyProgress(0, 0);
    }

    async function preload(url, dotNetRef) {
        wire(dotNetRef);
        setSource(url);
        audio.load();
        notify("ready", "Audio đã sẵn sàng");
    }

    async function play(url, dotNetRef) {
        wire(dotNetRef);
        setSource(url);

        if (audio.ended) {
            audio.currentTime = 0;
            notifyProgress(0, audio.duration);
        }

        await audio.play();
    }

    function pause() {
        audio.pause();
    }

    function setRate(rate) {
        if (Number.isFinite(rate) && rate > 0) {
            audio.playbackRate = rate;
        }
    }

    function seek(offsetSeconds) {
        if (!Number.isFinite(offsetSeconds) || !Number.isFinite(audio.currentTime)) {
            return;
        }

        const duration = Number.isFinite(audio.duration) && audio.duration > 0 ? audio.duration : audio.currentTime;
        const nextTime = Math.min(Math.max(audio.currentTime + offsetSeconds, 0), duration);
        audio.currentTime = nextTime;
        notifyProgress(audio.currentTime, audio.duration);
    }

    function restart() {
        audio.currentTime = 0;
        notifyProgress(audio.currentTime, audio.duration);
    }

    function dispose() {
        audio.pause();
        audio.removeAttribute("src");
        audio.load();
        lastUrl = "";
        bridge = null;
    }

    return {
        preload,
        play,
        pause,
        setRate,
        seek,
        restart,
        dispose
    };
})();
