window.imageViewer = {
    instances: {},

    init: function (containerId, layerId) {
        const container = document.getElementById(containerId);
        const layer = document.getElementById(layerId);
        if (!container || !layer) return;

        // ЗАЩИТА ОТ ДВОЙНОЙ ИНИЦИАЛИЗАЦИИ В BLAZOR
        if (this.instances[containerId]) return;

        container.style.overflow = 'hidden';
        container.style.cursor = 'grab';

        layer.style.transformOrigin = '0 0';
        layer.style.transition = 'transform 0.1s ease-out';

        const state = { scale: 1, posX: 0, posY: 0, isDragging: false, startX: 0, startY: 0 };
        this.instances[containerId] = { container, layer, state };

        const updateTransform = () => {
            layer.style.transform = `translate(${state.posX}px, ${state.posY}px) scale(${state.scale})`;
        };

        // Центрируем холст при загрузке
        setTimeout(() => {
            const cRect = container.getBoundingClientRect();
            const lRect = layer.getBoundingClientRect();
            state.posX = (cRect.width - lRect.width) / 2;
            state.posY = (cRect.height - lRect.height) / 2;
            updateTransform();
        }, 150);

        container.addEventListener('mousedown', (e) => {
            if (e.target.closest('.hotspot-tooltip') || e.target.closest('.hotspot-trigger')) return;
            state.isDragging = true;
            container.style.cursor = 'grabbing';
            layer.style.transition = 'none';
            state.startX = e.clientX - state.posX;
            state.startY = e.clientY - state.posY;
        });

        window.addEventListener('mouseup', () => {
            if (state.isDragging) {
                state.isDragging = false;
                container.style.cursor = 'grab';
                layer.style.transition = 'transform 0.1s ease-out';
            }
        });

        container.addEventListener('mouseleave', () => {
            if (state.isDragging) {
                state.isDragging = false;
                container.style.cursor = 'grab';
                layer.style.transition = 'transform 0.1s ease-out';
            }
        });

        container.addEventListener('mousemove', (e) => {
            if (!state.isDragging) return;
            e.preventDefault();
            state.posX = e.clientX - state.startX;
            state.posY = e.clientY - state.startY;
            updateTransform();
        });

        container.addEventListener('wheel', (e) => {
            e.preventDefault();
            const rect = container.getBoundingClientRect();
            const mouseX = e.clientX - rect.left;
            const mouseY = e.clientY - rect.top;

            const layerX = (mouseX - state.posX) / state.scale;
            const layerY = (mouseY - state.posY) / state.scale;

            const zoomFactor = e.deltaY > 0 ? 0.85 : 1.15;
            state.scale *= zoomFactor;
            state.scale = Math.max(0.15, Math.min(state.scale, 8));

            state.posX = mouseX - (layerX * state.scale);
            state.posY = mouseY - (layerY * state.scale);

            updateTransform();
        }, { passive: false });
    },

    focusOnCoordinates: function (containerId, x, y) {
        const inst = this.instances[containerId];
        if (!inst) return;
        const { container, layer, state } = inst;

        layer.style.transition = 'transform 0.4s cubic-bezier(0.25, 1, 0.5, 1)';
        state.scale = 2.0; // Приближаем в 2 раза при фокусе

        const cRect = container.getBoundingClientRect();
        state.posX = (cRect.width / 2) - (x * state.scale);
        state.posY = (cRect.height / 2) - (y * state.scale);

        layer.style.transform = `translate(${state.posX}px, ${state.posY}px) scale(${state.scale})`;
    }
};