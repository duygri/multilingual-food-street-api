window.touristMap = (() => {
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
                markers: []
            };

            instances.set(containerId, instance);
        }

        instance.map.easeTo({
            center: [snapshot.centerLng, snapshot.centerLat],
            zoom: snapshot.zoom,
            duration: 600
        });

        for (const marker of instance.markers) {
            marker.remove();
        }

        instance.markers = snapshot.markers.map(marker => {
            const element = document.createElement("button");
            element.type = "button";
            element.className = `tourist-map-marker${marker.isSelected ? " is-selected" : ""}`;
            element.style.background = marker.accent;
            element.title = marker.label;
            element.addEventListener("click", () => {
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync("SelectPoiFromMap", marker.id);
                }
            });

            const popupContent = document.createElement("div");
            popupContent.className = "tourist-map-popup__title";
            popupContent.textContent = marker.label;

            return new mapboxgl.Marker({ element })
                .setLngLat([marker.longitude, marker.latitude])
                .setPopup(new mapboxgl.Popup({ offset: 16, className: "tourist-map-popup" }).setDOMContent(popupContent))
                .addTo(instance.map);
        });
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
        dispose
    };
})();
