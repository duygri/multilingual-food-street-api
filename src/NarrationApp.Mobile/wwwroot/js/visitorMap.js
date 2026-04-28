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

        if (!accessToken) {
            container.innerHTML = "<div style='padding:18px;color:#eff6ff;'>Chưa có Mapbox access token.</div>";
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
                centerKey: null,
                markersKey: null
            };

            instances.set(containerId, instance);
        }

        instance.map.resize();

        const centerKey = `${snapshot.centerLat.toFixed(6)}|${snapshot.centerLng.toFixed(6)}|${snapshot.zoom.toFixed(2)}`;
        if (instance.centerKey !== centerKey) {
            if (instance.centerKey !== null) {
                instance.map.easeTo({
                    center: [snapshot.centerLng, snapshot.centerLat],
                    zoom: snapshot.zoom,
                    duration: 280
                });
            }

            instance.centerKey = centerKey;
        }

        const markersKey = snapshot.markers
            .map(marker => `${marker.id}:${marker.latitude.toFixed(6)}:${marker.longitude.toFixed(6)}:${marker.isSelected ? 1 : 0}:${marker.isNearest ? 1 : 0}:${marker.accent}`)
            .join("|");

        if (instance.markersKey === markersKey) {
            return;
        }

        for (const marker of instance.markers) {
            marker.remove();
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
