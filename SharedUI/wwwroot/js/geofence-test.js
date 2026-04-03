// Geofence Test Map Helper - Web-only tool for testing POI proximity
// Uses Google Maps JavaScript API

window.GeofenceTestMap = {
    map: null,
    mapElementId: null,
    testMarker: null,
    poiMarkers: [],
    geofenceCircles: [],
    infoWindow: null,
    dotNetRef: null,

    // Initialize the test map
    init: async function (elementId, lat, lng, zoom, dotNetRef, apiKey) {
        this.dotNetRef = dotNetRef;

        if (this.map) {
            this.destroy();
        }

        try {
            if (window.AppMapHelper && window.AppMapHelper.loadScript) {
                await window.AppMapHelper.loadScript(apiKey);
            }

            this.mapElementId = elementId;
            this.infoWindow = new google.maps.InfoWindow();
            this.map = new google.maps.Map(document.getElementById(elementId), {
                center: { lat: Number(lat), lng: Number(lng) },
                zoom: zoom || 15,
                mapTypeControl: false,
                streetViewControl: false,
                fullscreenControl: false,
                gestureHandling: "greedy"
            });

            this.testMarker = new google.maps.Marker({
                position: { lat: Number(lat), lng: Number(lng) },
                map: this.map,
                draggable: true,
                title: "Test position",
                label: {
                    text: "🧑",
                    fontSize: "22px"
                }
            });

            this.testMarker.addListener("dragend", () => {
                const position = this.testMarker.getPosition();
                if (position && this.dotNetRef) {
                    this.dotNetRef.invokeMethodAsync("OnTestPositionChanged", position.lat(), position.lng());
                }
            });

            this.map.addListener("click", (event) => {
                const position = {
                    lat: event.latLng.lat(),
                    lng: event.latLng.lng()
                };

                this.testMarker.setPosition(position);
                if (this.dotNetRef) {
                    this.dotNetRef.invokeMethodAsync("OnTestPositionChanged", position.lat, position.lng);
                }
            });

            console.log("[GeofenceTest] Map initialized at", lat, lng);
            return true;
        } catch (e) {
            console.error("[GeofenceTest] Init Error", e);
            return false;
        }
    },

    updateTestPosition: function (lat, lng) {
        if (!this.map || !this.testMarker) return;
        const position = { lat: Number(lat), lng: Number(lng) };
        this.testMarker.setPosition(position);
        this.map.panTo(position);
    },

    setPoisWithGeofence: function (pois) {
        if (!this.map || !window.google?.maps) return;

        this.poiMarkers.forEach(marker => marker.setMap(null));
        this.poiMarkers = [];

        this.geofenceCircles.forEach(circle => circle.setMap(null));
        this.geofenceCircles = [];

        const bounds = new google.maps.LatLngBounds();

        pois.forEach((poi) => {
            const isInside = poi.isInGeofence;
            const position = { lat: Number(poi.latitude), lng: Number(poi.longitude) };

            const distText = poi.distance >= 1000
                ? (poi.distance / 1000).toFixed(1) + ' km'
                : poi.distance.toFixed(0) + ' m';

            const statusText = isInside
                ? '<span style="color:#16a34a; font-weight:700;">✅ TRONG geofence</span>'
                : '<span style="color:#94a3b8;">⚪ Ngoài geofence</span>';

            const popupHTML = `
                    <div style="font-family: system-ui, sans-serif;">
                        <strong style="font-size:14px;">${poi.name}</strong><br>
                        <span style="color:#64748b; font-size:12px;">${poi.description || ''}</span><br>
                        <div style="margin-top:6px; padding-top:6px; border-top:1px solid #f1f5f9;">
                            <span style="font-size:12px;">📏 ${distText} | 📐 R=${poi.radius}m</span><br>
                            ${statusText}
                        </div>
                    </div>
                `;

            const circle = new google.maps.Circle({
                map: this.map,
                center: position,
                radius: Number(poi.radius),
                fillColor: isInside ? "#22c55e" : "#3b82f6",
                fillOpacity: isInside ? 0.2 : 0.08,
                strokeColor: isInside ? "#16a34a" : "#2563eb",
                strokeOpacity: 1,
                strokeWeight: isInside ? 3 : 2
            });

            const marker = new google.maps.Marker({
                position,
                map: this.map,
                title: poi.name,
                icon: {
                    path: google.maps.SymbolPath.CIRCLE,
                    fillColor: isInside ? "#16a34a" : "#2563eb",
                    fillOpacity: 1,
                    strokeColor: "#ffffff",
                    strokeWeight: 2,
                    scale: 10
                }
            });

            marker.addListener("click", () => {
                this.infoWindow.setContent(popupHTML);
                this.infoWindow.open({ map: this.map, anchor: marker });
            });

            this.geofenceCircles.push(circle);
            this.poiMarkers.push(marker);
            bounds.extend(position);
        });

        const testPosition = this.testMarker?.getPosition?.();
        if (testPosition) {
            bounds.extend({ lat: testPosition.lat(), lng: testPosition.lng() });
        }

        if (!bounds.isEmpty()) {
            this.map.fitBounds(bounds, 80);
        }

        console.log("[GeofenceTest] POIs updated:", pois.length);
    },

    fitAll: function () {
        if (!this.map || !window.google?.maps) return;

        const bounds = new google.maps.LatLngBounds();
        let hasPoints = false;

        this.poiMarkers.forEach(m => {
            const pos = m.getPosition?.();
            if (pos) {
                bounds.extend({ lat: pos.lat(), lng: pos.lng() });
                hasPoints = true;
            }
        });

        if (this.testMarker) {
            const pos = this.testMarker.getPosition?.();
            if (pos) {
                bounds.extend({ lat: pos.lat(), lng: pos.lng() });
                hasPoints = true;
            }
        }

        if (hasPoints) {
            this.map.fitBounds(bounds, 80);
        }
    },

    destroy: function () {
        if (this.map) {
            this.poiMarkers.forEach(marker => marker.setMap(null));
            if(this.testMarker) this.testMarker.setMap(null);
            this.geofenceCircles.forEach(circle => circle.setMap(null));

            this.poiMarkers = [];
            this.geofenceCircles = [];
            if (this.infoWindow) {
                this.infoWindow.close();
                this.infoWindow = null;
            }
            google.maps.event.clearInstanceListeners(this.map);
            this.map = null;
            this.testMarker = null;
            this.dotNetRef = null;

            if (this.mapElementId) {
                const element = document.getElementById(this.mapElementId);
                if (element) {
                    element.innerHTML = "";
                }
                this.mapElementId = null;
            }
        }
    }
};
