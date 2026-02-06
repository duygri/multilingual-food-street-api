// GPS Tracking với Battery Optimization
// Adaptive Polling + Distance Threshold

window.GpsTracker = {
    watchId: null,
    lastPosition: null,
    dotNetRef: null,
    
    // Config
    config: {
        distanceThreshold: 10, // meters - chỉ gửi khi di chuyển > 10m
        movingInterval: 10000, // 10s khi di chuyển
        stationaryInterval: 30000, // 30s khi đứng yên
        speedThreshold: 0.5 // m/s - dưới ngưỡng này = đứng yên
    },

    // Start tracking
    start: function(dotNetRef) {
        this.dotNetRef = dotNetRef;
        
        if (!navigator.geolocation) {
            this.notifyError('Geolocation không được hỗ trợ');
            return false;
        }

        const options = {
            enableHighAccuracy: true,
            maximumAge: 10000,
            timeout: 15000
        };

        this.watchId = navigator.geolocation.watchPosition(
            (pos) => this.onPositionUpdate(pos),
            (err) => this.onError(err),
            options
        );

        console.log('[GPS] Started tracking');
        return true;
    },

    // Stop tracking
    stop: function() {
        if (this.watchId !== null) {
            navigator.geolocation.clearWatch(this.watchId);
            this.watchId = null;
            console.log('[GPS] Stopped tracking');
        }
    },

    // Position update handler
    onPositionUpdate: function(position) {
        const newPos = {
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
            accuracy: position.coords.accuracy,
            speed: position.coords.speed || 0,
            timestamp: position.timestamp
        };

        // Distance filtering - chỉ gửi khi di chuyển > threshold
        if (this.lastPosition && !this.shouldUpdate(newPos)) {
            console.log('[GPS] Skipped - distance below threshold');
            return;
        }

        this.lastPosition = newPos;
        
        // Notify .NET
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('OnPositionChanged', 
                newPos.latitude, 
                newPos.longitude, 
                newPos.accuracy, 
                newPos.speed
            );
        }
    },

    // Check if should update based on distance
    shouldUpdate: function(newPos) {
        if (!this.lastPosition) return true;
        
        const dist = this.haversine(
            this.lastPosition.latitude, this.lastPosition.longitude,
            newPos.latitude, newPos.longitude
        );
        
        return dist >= this.config.distanceThreshold;
    },

    // Haversine distance (meters)
    haversine: function(lat1, lon1, lat2, lon2) {
        const R = 6371000; // Earth radius meters
        const dLat = this.toRad(lat2 - lat1);
        const dLon = this.toRad(lon2 - lon1);
        const a = Math.sin(dLat/2) * Math.sin(dLat/2) +
                  Math.cos(this.toRad(lat1)) * Math.cos(this.toRad(lat2)) *
                  Math.sin(dLon/2) * Math.sin(dLon/2);
        const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
        return R * c;
    },

    toRad: function(deg) {
        return deg * Math.PI / 180;
    },

    // Error handler
    onError: function(error) {
        let message = '';
        switch(error.code) {
            case error.PERMISSION_DENIED:
                message = 'Người dùng từ chối cấp quyền vị trí';
                break;
            case error.POSITION_UNAVAILABLE:
                message = 'Không thể xác định vị trí';
                break;
            case error.TIMEOUT:
                message = 'Hết thời gian chờ GPS';
                break;
            default:
                message = 'Lỗi GPS không xác định';
        }
        this.notifyError(message);
    },

    notifyError: function(message) {
        console.error('[GPS] Error:', message);
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('OnGpsError', message);
        }
    },

    // Get current position once
    getCurrentPosition: function(dotNetRef) {
        if (!navigator.geolocation) {
            dotNetRef.invokeMethodAsync('OnGpsError', 'Geolocation không được hỗ trợ');
            return;
        }

        navigator.geolocation.getCurrentPosition(
            (pos) => {
                dotNetRef.invokeMethodAsync('OnPositionChanged',
                    pos.coords.latitude,
                    pos.coords.longitude,
                    pos.coords.accuracy,
                    pos.coords.speed || 0
                );
            },
            (err) => {
                dotNetRef.invokeMethodAsync('OnGpsError', err.message);
            },
            { enableHighAccuracy: true, timeout: 10000 }
        );
    }
};
