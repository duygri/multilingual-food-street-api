// Push Notification Service
window.PushNotificationService = {
    swRegistration: null,
    isSubscribed: false,
    sessionId: null,

    // Initialize Push Notifications
    init: async function (sessionId) {
        this.sessionId = sessionId;

        if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
            console.warn('[Push] Push notifications not supported');
            return false;
        }

        try {
            // Register service worker
            this.swRegistration = await navigator.serviceWorker.register('/sw-push.js');
            console.log('[Push] Service Worker registered');

            // Check subscription status
            const subscription = await this.swRegistration.pushManager.getSubscription();
            this.isSubscribed = subscription !== null;

            return true;
        } catch (error) {
            console.error('[Push] Init failed:', error);
            return false;
        }
    },

    // Request permission and subscribe
    subscribe: async function (apiBaseUrl) {
        if (!this.swRegistration) {
            console.error('[Push] Not initialized');
            return false;
        }

        try {
            // Request notification permission
            const permission = await Notification.requestPermission();
            if (permission !== 'granted') {
                console.log('[Push] Permission denied');
                return false;
            }

            // Subscribe to push
            const subscription = await this.swRegistration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: this.urlBase64ToUint8Array(
                    // This is a placeholder - in production, use VAPID public key
                    'BEl62iUYgUivxIkv69yViEuiBIa-Ib9-SkvMeAtA3LFgDzkrxZJjSgSnfckjBJuBkr3qBUYIHBQFLXYp5Nksh8U'
                )
            });

            // Send to server
            const response = await fetch(`${apiBaseUrl}/api/notifications/subscribe`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    sessionId: this.sessionId,
                    endpoint: subscription.endpoint,
                    keys: {
                        p256dh: btoa(String.fromCharCode(...new Uint8Array(subscription.getKey('p256dh')))),
                        auth: btoa(String.fromCharCode(...new Uint8Array(subscription.getKey('auth'))))
                    }
                })
            });

            this.isSubscribed = response.ok;
            console.log('[Push] Subscribed successfully');
            return true;
        } catch (error) {
            console.error('[Push] Subscribe failed:', error);
            return false;
        }
    },

    // Show local notification (fallback when service worker not available)
    showLocalNotification: function (title, body, icon, tag) {
        if (!('Notification' in window)) {
            console.warn('[Push] Notifications not supported');
            return;
        }

        if (Notification.permission === 'granted') {
            new Notification(title, {
                body: body,
                icon: icon || '/favicon.png',
                tag: tag,
                requireInteraction: false,
                silent: false
            });
        }
    },

    // Show notification for geofence entry
    notifyGeofenceEntry: function (poiName, poiDescription, poiImageUrl) {
        this.showLocalNotification(
            `📍 ${poiName}`,
            poiDescription || 'Bạn đang ở gần địa điểm này!',
            poiImageUrl
        );
    },

    // Helper: Convert VAPID key
    urlBase64ToUint8Array: function (base64String) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding)
            .replace(/-/g, '+')
            .replace(/_/g, '/');
        const rawData = window.atob(base64);
        return Uint8Array.from([...rawData].map(char => char.charCodeAt(0)));
    },

    // Request notification permission
    requestPermission: async function () {
        if (!('Notification' in window)) {
            return 'unsupported';
        }

        if (Notification.permission === 'granted') {
            return 'granted';
        }

        const permission = await Notification.requestPermission();
        return permission;
    },

    // Get current permission status
    getPermissionStatus: function () {
        if (!('Notification' in window)) {
            return 'unsupported';
        }
        return Notification.permission;
    }
};
