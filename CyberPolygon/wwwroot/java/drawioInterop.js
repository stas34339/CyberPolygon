window.drawioInterop = {
    initViewer: function (iframeId, xmlContent) {
        const iframe = document.getElementById(iframeId);
        if (!iframe) return;

        const handleMessage = function (evt) {
            // Слушаем сообщения только от нашего фрейма
            if (evt.source !== iframe.contentWindow) return;

            try {
                const data = JSON.parse(evt.data);

                // Как только Draw.io загрузился, отправляем ему XML
                if (data.event === 'init') {
                    iframe.contentWindow.postMessage(JSON.stringify({
                        action: 'load',
                        xml: xmlContent
                    }), '*');
                }
            } catch (e) {
                // Игнорируем не-JSON сообщения
            }
        };

        window.addEventListener('message', handleMessage);
    }
};