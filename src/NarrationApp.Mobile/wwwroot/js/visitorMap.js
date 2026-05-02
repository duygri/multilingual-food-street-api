window.visitorMap = (() => {
    const instances = new Map();

    function ensureContainer(containerId) {
        const container = document.getElementById(containerId);
        if (!container) {
            return null;
        }

        if (!window.mapboxgl) {
            container.innerHTML = "<div style='padding:18px;color:#eff6ff;'>Mapbox chưa tải được.</div>";
            return null;
        }

        return container;
    }

    function render(containerId, accessToken, styleUrl, snapshot, dotNetRef) {
        const container = ensureContainer(containerId);
        if (!container) {
            return;
        }

        if (!accessToken || accessToken.startsWith("YOUR_")) {
            container.innerHTML = "<div style='padding:18px;color:#eff6ff;'>Chưa có Mapbox access token.</div>";
            console.warn("[visitorMap] Mapbox token is a placeholder – skipping render.");
            return;
        }

        mapboxgl.accessToken = accessToken;

        let instance = instances.get(containerId);
        if (!instance) {
            const map = new mapboxgl.Map({
                container: containerId,
                style: styleUrl,
                center: [snapshot.centerLng, snapshot.centerLat],
                zoom: snapshot.zoom,
                attributionControl: false
            });

            instance = {
                map,
                markers: [],
                userMarker: null,
                centerKey: null,
                markersKey: null
            };

            instances.set(containerId, instance);
        }

        instance.map.resize();

        const centerKey = `${snapshot.centerLat.toFixed(6)}|${snapshot.centerLng.toFixed(6)}|${snapshot.zoom.toFixed(2)}`;
        const needsViewportAdjust = instance.centerKey !== centerKey;
        const hasMultipleMarkers = snapshot.markers.length > 1;
        instance.centerKey = centerKey;

        const markersKey = snapshot.markers
            .map(marker => `${marker.id}:${marker.latitude.toFixed(6)}:${marker.longitude.toFixed(6)}:${marker.isSelected ? 1 : 0}:${marker.isNearest ? 1 : 0}:${marker.accent}`)
            .concat(snapshot.userLocation
                ? [`user:${snapshot.userLocation.latitude.toFixed(6)}:${snapshot.userLocation.longitude.toFixed(6)}`]
                : [])
            .join("|");

        if (instance.markersKey === markersKey) {
            return;
        }

        for (const marker of instance.markers) {
            marker.remove();
        }

        if (instance.userMarker) {
            instance.userMarker.remove();
            instance.userMarker = null;
        }

        instance.markers = snapshot.markers.map(marker => {
            const element = document.createElement("button");
            element.type = "button";
            element.className = `visitor-map-marker${marker.isSelected ? " is-selected" : ""}${marker.isNearest ? " is-nearest" : ""}`;
            element.style.background = marker.accent;
            element.title = marker.label;
            element.addEventListener("click", () => {
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync("SelectPoiFromMap", marker.id);
                }
            });

            const popupContent = document.createElement("div");
            popupContent.className = "visitor-map-popup__title";
            popupContent.textContent = marker.label;

            return new mapboxgl.Marker({ element })
                .setLngLat([marker.longitude, marker.latitude])
                .setPopup(new mapboxgl.Popup({ offset: 16, className: "visitor-map-popup" }).setDOMContent(popupContent))
                .addTo(instance.map);
        });

        if (snapshot.userLocation) {
            const userElement = document.createElement("div");
            userElement.className = "visitor-map-user-marker";
            userElement.title = snapshot.userLocation.label;

            instance.userMarker = new mapboxgl.Marker({ element: userElement, anchor: "center" })
                .setLngLat([snapshot.userLocation.longitude, snapshot.userLocation.latitude])
                .addTo(instance.map);
        }

        if (needsViewportAdjust) {
            if (hasMultipleMarkers) {
                const bounds = new mapboxgl.LngLatBounds();
                for (const marker of snapshot.markers) {
                    bounds.extend([marker.longitude, marker.latitude]);
                }

                instance.map.fitBounds(bounds, {
                    padding: { top: 164, right: 28, bottom: 220, left: 28 },
                    maxZoom: 15.4,
                    duration: 320
                });
            } else {
                instance.map.easeTo({
                    center: [snapshot.centerLng, snapshot.centerLat],
                    zoom: snapshot.zoom,
                    duration: 280
                });
            }
        }

        instance.markersKey = markersKey;
    }

    function zoomIn(containerId) {
        const instance = instances.get(containerId);
        if (instance) {
            instance.map.zoomIn({ duration: 300 });
        }
    }

    function zoomOut(containerId) {
        const instance = instances.get(containerId);
        if (instance) {
            instance.map.zoomOut({ duration: 300 });
        }
    }

    function dispose(containerId) {
        const instance = instances.get(containerId);
        if (!instance) {
            return;
        }

        for (const marker of instance.markers) {
            marker.remove();
        }

        if (instance.userMarker) {
            instance.userMarker.remove();
        }

        instance.map.remove();
        instances.delete(containerId);
    }

    return {
        render,
        zoomIn,
        zoomOut,
        dispose
    };
})();
