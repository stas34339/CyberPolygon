window.imageViewer = {
    isDragging: false,
    startX: 0,
    startY: 0,
    translateX: 0,
    translateY: 0,
    scale: 1, // Добавили переменную для масштаба

    init: function (containerId, layerId) {
        var container = document.getElementById(containerId);
        var layer = document.getElementById(layerId);

        if (!container || !layer) return;

        window.onmousemove = null;
        window.onmouseup = null;
        this.isDragging = false;

        // Сбрасываем позицию И МАСШТАБ при каждом новом входе на страницу
        this.translateX = 0;
        this.translateY = 0;
        this.scale = 1;
        layer.style.transform = `translate(0px, 0px) scale(1)`;

        // 1. НАЖАТИЕ (ПЕРЕТАСКИВАНИЕ)
        container.onmousedown = (e) => {
            if (e.target.closest('.network-hotspot')) return;

            this.isDragging = true;
            this.startX = e.clientX - this.translateX;
            this.startY = e.clientY - this.translateY;
            container.style.cursor = "grabbing";
        };

        // 2. ДВИЖЕНИЕ
        window.onmousemove = (e) => {
            if (!this.isDragging) return;
            e.preventDefault();

            this.translateX = e.clientX - this.startX;
            this.translateY = e.clientY - this.startY;

            var activeLayer = document.getElementById(layerId);
            if (activeLayer) {
                // Применяем позицию с сохранением текущего зума
                activeLayer.style.transform = `translate(${this.translateX}px, ${this.translateY}px) scale(${this.scale})`;
            }
        };

        // 3. ОТПУСКАНИЕ КНОПКИ
        window.onmouseup = () => {
            this.isDragging = false;
            var activeContainer = document.getElementById(containerId);
            if (activeContainer) activeContainer.style.cursor = "grab";
        };

        // 4. КОЛЕСИКО МЫШИ (ЗУМ)
        container.onwheel = (e) => {
            e.preventDefault(); // Блокируем прокрутку самой страницы вниз-вверх

            var zoomIntensity = 0.1;
            // Определяем направление прокрутки
            var wheel = e.deltaY < 0 ? 1 : -1;

            this.scale += wheel * zoomIntensity;

            // Устанавливаем лимиты (чтобы не отдалить в микропиксель и не приблизить слишком близко)
            if (this.scale < 0.4) this.scale = 0.4;
            if (this.scale > 2.5) this.scale = 2.5;

            var activeLayer = document.getElementById(layerId);
            if (activeLayer) {
                activeLayer.style.transform = `translate(${this.translateX}px, ${this.translateY}px) scale(${this.scale})`;
            }
        };
    },

    focusOnCoordinates: function (containerId, x, y) {
        var container = document.getElementById(containerId);
        if (!container) return;
        var layer = container.firstElementChild;
        if (!layer) return;

        var containerRect = container.getBoundingClientRect();

        this.translateX = (containerRect.width / 2) - x;
        this.translateY = (containerRect.height / 2) - y;
        this.scale = 1; // Сбрасываем зум при фокусе на узел

        layer.style.transition = "transform 0.3s ease-out";
        layer.style.transform = `translate(${this.translateX}px, ${this.translateY}px) scale(${this.scale})`;

        setTimeout(() => {
            if (layer) layer.style.transition = "none";
        }, 300);
    }
};